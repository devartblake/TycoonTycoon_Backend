"""
MongoDB settlement log.

Collection: crypto_settlements
Unique index: withdrawal_id  (idempotency guard)
"""
from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from motor.motor_asyncio import AsyncIOMotorDatabase
from pymongo import ASCENDING
from pymongo.errors import DuplicateKeyError


COLLECTION = "crypto_settlements"


async def ensure_indexes(db: AsyncIOMotorDatabase) -> None:
    await db[COLLECTION].create_index(
        [("withdrawal_id", ASCENDING)], unique=True, background=True
    )


async def is_settled(db: AsyncIOMotorDatabase, withdrawal_id: str) -> bool:
    doc = await db[COLLECTION].find_one(
        {"withdrawal_id": withdrawal_id, "status": {"$in": ["settled", "failed", "rejected"]}}
    )
    return doc is not None


async def get_retry_count(db: AsyncIOMotorDatabase, withdrawal_id: str) -> int:
    doc = await db[COLLECTION].find_one({"withdrawal_id": withdrawal_id})
    return doc.get("retry_count", 0) if doc else 0


async def increment_retry(db: AsyncIOMotorDatabase, withdrawal_id: str) -> int:
    result = await db[COLLECTION].find_one_and_update(
        {"withdrawal_id": withdrawal_id},
        {"$inc": {"retry_count": 1}, "$set": {"updated_at": _now()}},
        upsert=True,
        return_document=True,
    )
    return result.get("retry_count", 1)


async def record(
    db: AsyncIOMotorDatabase,
    withdrawal_id: str,
    player_id: str,
    units: float,
    to_wallet_address: str,
    network: str,
    on_chain_tx_id: str | None,
    status: str,
    error: str | None = None,
) -> None:
    now = _now()
    doc: dict[str, Any] = {
        "withdrawal_id": withdrawal_id,
        "player_id": player_id,
        "units": units,
        "to_wallet_address": to_wallet_address,
        "network": network,
        "on_chain_tx_id": on_chain_tx_id,
        "status": status,
        "error": error,
        "retry_count": 0,
        "created_at": now,
        "settled_at": now if status == "settled" else None,
    }
    try:
        await db[COLLECTION].insert_one(doc)
    except DuplicateKeyError:
        # Already recorded — update status only
        await db[COLLECTION].update_one(
            {"withdrawal_id": withdrawal_id},
            {"$set": {
                "on_chain_tx_id": on_chain_tx_id,
                "status": status,
                "error": error,
                "settled_at": now if status == "settled" else None,
                "updated_at": now,
            }},
        )


async def list_history(
    db: AsyncIOMotorDatabase, page: int = 1, page_size: int = 20
) -> dict[str, Any]:
    skip = (page - 1) * page_size
    cursor = db[COLLECTION].find({}, {"_id": 0}).sort("created_at", -1).skip(skip).limit(page_size)
    items = await cursor.to_list(length=page_size)
    total = await db[COLLECTION].count_documents({})
    return {"page": page, "page_size": page_size, "total": total, "items": items}


async def list_pending(db: AsyncIOMotorDatabase) -> list[dict[str, Any]]:
    cursor = db[COLLECTION].find({"status": "pending"}, {"_id": 0}).sort("created_at", 1)
    return await cursor.to_list(length=200)


def _now() -> datetime:
    return datetime.now(timezone.utc)
