import json
import os
import requests
from typing import Dict, List, Optional

GITHUB_TOKEN = os.environ["GITHUB_TOKEN"]
REPO_OWNER = "devartblake"
REPO_NAME = "TycoonTycoon_Backend"
API_VERSION = "2022-11-28"

BASE_URL = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}"
HEADERS = {
    "Accept": "application/vnd.github+json",
    "Authorization": f"Bearer {GITHUB_TOKEN}",
    "X-GitHub-Api-Version": API_VERSION,
}

JSON_FILE = os.path.join(os.path.dirname(__file__), "..", "ops", "issues", "unified_personalization_github_issues.json")


def gh_get(url: str):
    r = requests.get(url, headers=HEADERS, timeout=30)
    r.raise_for_status()
    return r.json()


def gh_post(url: str, payload: dict):
    r = requests.post(url, headers=HEADERS, json=payload, timeout=30)
    r.raise_for_status()
    return r.json()


def list_repo_labels() -> Dict[str, dict]:
    labels = {}
    page = 1
    while True:
        r = requests.get(
            f"{BASE_URL}/labels",
            headers=HEADERS,
            params={"per_page": 100, "page": page},
            timeout=30,
        )
        r.raise_for_status()
        batch = r.json()
        if not batch:
            break
        for label in batch:
            labels[label["name"]] = label
        page += 1
    return labels


def create_label_if_missing(name: str, existing: Dict[str, dict]):
    if name in existing:
        return
    # GitHub requires a color for label creation.
    payload = {
        "name": name,
        "color": "0e8a16",
        "description": f"Imported label: {name}",
    }
    gh_post(f"{BASE_URL}/labels", payload)
    existing[name] = {"name": name}


def list_repo_milestones() -> Dict[str, dict]:
    milestones = {}
    page = 1
    while True:
        r = requests.get(
            f"{BASE_URL}/milestones",
            headers=HEADERS,
            params={"state": "all", "per_page": 100, "page": page},
            timeout=30,
        )
        r.raise_for_status()
        batch = r.json()
        if not batch:
            break
        for ms in batch:
            milestones[ms["title"]] = ms
        page += 1
    return milestones


def create_milestone_if_missing(title: str, existing: Dict[str, dict]):
    if title in existing:
        return existing[title]
    payload = {"title": title}
    ms = gh_post(f"{BASE_URL}/milestones", payload)
    existing[title] = ms
    return ms


def create_issue(issue: dict, milestone_number: Optional[int]):
    assignees = issue.get("assignees", [])
    # Your JSON currently uses role names, not GitHub usernames.
    # Keep them out unless you replace them with real usernames.
    valid_assignees = []

    payload = {
        "title": issue["title"],
        "body": (
            f'{issue["body"]}\n\n'
            f'---\n'
            f'**Owner (planning):** {", ".join(assignees) if assignees else "Unassigned"}\n'
            f'**Estimate:** {issue.get("estimate", "TBD")}\n'
            f'**Sprint:** {", ".join([l for l in issue.get("labels", []) if l.startswith("sprint:")]) or "Unassigned"}\n'
        ),
        "labels": issue.get("labels", []),
    }

    if milestone_number is not None:
        payload["milestone"] = milestone_number
    if valid_assignees:
        payload["assignees"] = valid_assignees

    created = gh_post(f"{BASE_URL}/issues", payload)
    return created["html_url"]


def main():
    with open(JSON_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    issues: List[dict] = data["issues"]

    existing_labels = list_repo_labels()
    existing_milestones = list_repo_milestones()

    # Ensure labels and milestones exist
    all_labels = sorted({label for issue in issues for label in issue.get("labels", [])})
    all_milestones = sorted({issue["milestone"] for issue in issues if issue.get("milestone")})

    for label in all_labels:
        create_label_if_missing(label, existing_labels)

    for milestone_title in all_milestones:
        create_milestone_if_missing(milestone_title, existing_milestones)

    # Refresh milestones in case new ones were created
    existing_milestones = list_repo_milestones()

    created_urls = []
    for issue in issues:
        milestone_title = issue.get("milestone")
        milestone_number = None
        if milestone_title:
            milestone_number = existing_milestones[milestone_title]["number"]

        url = create_issue(issue, milestone_number)
        created_urls.append(url)
        print(f"Created: {url}")

    print("\nDone.")
    print(f"Created {len(created_urls)} issues.")


if __name__ == "__main__":
    main()