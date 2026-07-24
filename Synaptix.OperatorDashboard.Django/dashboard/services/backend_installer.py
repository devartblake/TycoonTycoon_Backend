from __future__ import annotations

import json
import mimetypes
import tempfile
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any, BinaryIO, Callable

from django.conf import settings

SEED_KEYS = {
    "storeItems": ("assets/seeds/store-items.json", "seeds/store-items.json"),
    "skillNodes": ("assets/seeds/skill-nodes.json", "seeds/skill-nodes.json"),
    "seasonRewards": ("assets/seeds/season-rewards.json", "seeds/season-rewards.json"),
    "questions": ("assets/seeds/questions.json", "seeds/questions.json"),
}

CONTENT_TYPES = {
    ".json": "application/json",
    ".jsonl": "application/x-ndjson",
    ".glb": "model/gltf-binary",
    ".gltf": "model/gltf+json",
    ".fbx": "application/octet-stream",
    ".obj": "text/plain",
    ".mtl": "text/plain",
    ".zip": "application/zip",
    ".mp3": "audio/mpeg",
    ".wav": "audio/wav",
    ".ogg": "audio/ogg",
    ".m4a": "audio/mp4",
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".webp": "image/webp",
    ".gif": "image/gif",
    ".svg": "image/svg+xml",
    ".mp4": "video/mp4",
    ".webm": "video/webm",
    ".css": "text/css",
    ".js": "text/javascript",
    ".html": "text/html",
    ".txt": "text/plain",
    ".frag": "text/plain",
}


@dataclass(frozen=True)
class InstallerItem:
    source: str
    key: str
    content_type: str
    bytes: int
    kind: str


def _clean_key(value: str) -> str:
    return value.replace("\\", "/").lstrip("/")


def content_type_for(path: str, override: str | None = None) -> str:
    if override:
        return override
    suffix = Path(path).suffix.lower()
    return CONTENT_TYPES.get(suffix) or mimetypes.guess_type(path)[0] or "application/octet-stream"


def infer_asset_key(source: str) -> str:
    source = _clean_key(source)
    if source.startswith("assets/songs/"):
        return source.removeprefix("assets/")
    if source.startswith("assets/sfx/") or source.startswith("assets/sounds/"):
        return f"audio/{source.removeprefix('assets/').split('/', 1)[1]}"
    if source.startswith("assets/models/"):
        return f"models/{source.removeprefix('assets/models/')}"
    if source.startswith("assets/3d/"):
        return f"models/3d/{source.removeprefix('assets/3d/')}"
    if source.startswith("assets/avatarPackages/"):
        return f"avatar-packages/{source.removeprefix('assets/avatarPackages/')}"
    if source.startswith("assets/zip/"):
        return f"avatar-packages/{source.removeprefix('assets/zip/')}"
    if source.startswith("assets/images/"):
        return source.removeprefix("assets/")
    if source.startswith(("assets/icons/", "assets/screenshots/", "assets/splash_previews/")):
        return f"images/{source.removeprefix('assets/')}"
    if source.startswith("assets/questions/"):
        return f"questions/{source.removeprefix('assets/questions/')}"
    return f"frontend/{source}"


def generate_manifest_from_zip(zip_path: str) -> dict[str, Any]:
    with zipfile.ZipFile(zip_path) as archive:
        sources = {info.filename for info in archive.infolist() if not info.is_dir()}

    seeds = {
        name: {"source": source, "key": key}
        for name, (source, key) in SEED_KEYS.items()
        if source in sources
    }
    seed_sources = {value["source"] for value in seeds.values()}
    assets = [
        {"source": source, "key": infer_asset_key(source), "contentType": content_type_for(source)}
        for source in sorted(sources)
        if source not in seed_sources and not source.endswith("installer.manifest.json")
    ]
    return {"seeds": seeds, "assets": assets}


def load_manifest_from_zip(zip_path: str) -> dict[str, Any]:
    with zipfile.ZipFile(zip_path) as archive:
        names = {info.filename for info in archive.infolist() if not info.is_dir()}
        if "installer.manifest.json" in names:
            with archive.open("installer.manifest.json") as stream:
                return json.loads(stream.read().decode("utf-8"))
    return generate_manifest_from_zip(zip_path)


def _items_from_manifest(manifest: dict[str, Any], available: dict[str, int]) -> tuple[list[InstallerItem], list[str]]:
    warnings: list[str] = []
    items: list[InstallerItem] = []

    for name, value in (manifest.get("seeds") or {}).items():
        source = value if isinstance(value, str) else value.get("source")
        if not source:
            warnings.append(f"Seed {name} is missing a source.")
            continue
        key = value.get("key") if isinstance(value, dict) else SEED_KEYS.get(name, (None, f"seeds/{Path(source).name}"))[1]
        source = _clean_key(source)
        if source not in available:
            warnings.append(f"Seed file missing from bundle: {source}")
            continue
        items.append(InstallerItem(source, _clean_key(key), content_type_for(source, value.get("contentType") if isinstance(value, dict) else None), available[source], "seed"))

    for asset in manifest.get("assets") or []:
        source = _clean_key(asset.get("source") or "")
        if not source:
            warnings.append("Asset entry is missing a source.")
            continue
        if source not in available:
            warnings.append(f"Asset file missing from bundle: {source}")
            continue
        items.append(InstallerItem(source, _clean_key(asset.get("key") or infer_asset_key(source)), content_type_for(source, asset.get("contentType")), available[source], "asset"))

    seen: set[str] = set()
    for item in items:
        if item.key in seen:
            warnings.append(f"Duplicate MinIO object key: {item.key}")
        seen.add(item.key)

    total_bytes = sum(item.bytes for item in items)
    if total_bytes > settings.INSTALLER_MAX_UPLOAD_BYTES:
        warnings.append(f"Bundle upload size {total_bytes} exceeds INSTALLER_MAX_UPLOAD_BYTES={settings.INSTALLER_MAX_UPLOAD_BYTES}.")

    return items, warnings


def inspect_bundle(uploaded_file) -> dict[str, Any]:
    tmp_path = _save_upload_to_temp(uploaded_file)
    try:
        manifest = load_manifest_from_zip(tmp_path)
        with zipfile.ZipFile(tmp_path) as archive:
            available = {info.filename: info.file_size for info in archive.infolist() if not info.is_dir()}
        items, warnings = _items_from_manifest(manifest, available)
    finally:
        Path(tmp_path).unlink(missing_ok=True)

    seeds = [item for item in items if item.kind == "seed"]
    assets = [item for item in items if item.kind == "asset"]
    return {
        "manifest": manifest,
        "items": items,
        "warnings": warnings,
        "seed_count": len(seeds),
        "asset_count": len(assets),
        "total_bytes": sum(item.bytes for item in items),
        "preview": items[:40],
        "ok": not warnings,
    }


def upload_bundle_via_backend(
    uploaded_file,
    upload_one: Callable[[InstallerItem, BinaryIO], dict[str, Any]],
    *,
    overwrite: bool = False,
) -> dict[str, Any]:
    tmp_path = _save_upload_to_temp(uploaded_file)
    try:
        manifest = load_manifest_from_zip(tmp_path)
        with zipfile.ZipFile(tmp_path) as archive:
            available = {info.filename: info.file_size for info in archive.infolist() if not info.is_dir()}
            items, warnings = _items_from_manifest(manifest, available)
            if warnings:
                return {"ok": False, "warnings": warnings, "uploaded": 0, "failed": 0, "total_bytes": sum(item.bytes for item in items), "preview": items[:40]}

            uploaded = 0
            failed = 0
            failures: list[str] = []
            for item in items:
                try:
                    with archive.open(item.source) as body:
                        upload_one(item, body)
                    uploaded += 1
                except Exception as ex:
                    failed += 1
                    failures.append(f"{item.key}: {ex}")

        return {
            "ok": failed == 0,
            "warnings": warnings,
            "uploaded": uploaded,
            "failed": failed,
            "failures": failures,
            "total_bytes": sum(item.bytes for item in items),
            "preview": items[:40],
            "overwrite": overwrite,
            "migration_command": "$env:MIGRATION_SEED_SOURCE='MinIO'; ./scripts/run-dashboard-bootstrap.ps1 -Mode docker",
        }
    finally:
        Path(tmp_path).unlink(missing_ok=True)


def _save_upload_to_temp(uploaded_file) -> str:
    fd, tmp_path = tempfile.mkstemp(suffix=".zip")
    with open(fd, "wb") as tmp:
        for chunk in uploaded_file.chunks():
            tmp.write(chunk)
    return tmp_path
