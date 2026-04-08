# Operator Dashboard Issue Planning Exports

This directory contains ready-to-import planning JSON for outstanding operator-dashboard migration tasks.

## Files

- `github_issues_import.json`: issue seed payload including labels, milestones, sprint metadata, and issue drafts.

## Suggested import flow

1. Create labels from the `labels` section (skip existing labels).
2. Create milestones from the `milestones` section.
3. Create issues from the `issues` section with mapped labels/milestones.
4. Map each issue's `sprint` field into your GitHub Project sprint/custom field.
