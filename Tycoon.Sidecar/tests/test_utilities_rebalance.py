from app.routers.utilities import _extract_delta_summary, _validate_rebalance_delta


def test_validate_rebalance_delta_allows_small_changes():
    current = {
        "maxEnergy": 20,
        "modes": [
            {"mode": "casual", "energyCost": 3},
            {"mode": "guardian", "energyCost": 5},
        ],
    }
    proposed = {
        "maxEnergy": 21,
        "modes": [
            {"mode": "casual", "energyCost": 4},
            {"mode": "guardian", "energyCost": 4},
        ],
    }

    ok, errors = _validate_rebalance_delta(current, proposed)

    assert ok is True
    assert errors == []


def test_validate_rebalance_delta_blocks_large_changes():
    current = {
        "maxEnergy": 20,
        "modes": [
            {"mode": "casual", "energyCost": 3},
            {"mode": "guardian", "energyCost": 5},
        ],
    }
    proposed = {
        "maxEnergy": 25,
        "modes": [
            {"mode": "casual", "energyCost": 6},
            {"mode": "guardian", "energyCost": 2},
        ],
    }

    ok, errors = _validate_rebalance_delta(current, proposed)

    assert ok is False
    assert len(errors) == 3
    assert "maxEnergy delta exceeds guardrail (max ±2 per apply)" in errors
    assert "casual: energyCost delta exceeds guardrail (max ±1 per apply)" in errors
    assert "guardian: energyCost delta exceeds guardrail (max ±1 per apply)" in errors


def test_extract_delta_summary_returns_expected_fields():
    current = {
        "maxEnergy": 20,
        "regenMinutesPerEnergy": 10,
        "modes": [
            {"mode": "casual", "energyCost": 3},
            {"mode": "ranked", "energyCost": 4},
        ],
    }
    proposed = {
        "maxEnergy": 22,
        "regenMinutesPerEnergy": 8,
        "modes": [
            {"mode": "casual", "energyCost": 2},
            {"mode": "ranked", "energyCost": 4},
        ],
    }

    summary = _extract_delta_summary(current, proposed)

    assert summary["maxEnergy"] == {"from": 20, "to": 22, "delta": 2}
    assert summary["regenMinutesPerEnergy"] == {"from": 10, "to": 8, "delta": -2}
    assert summary["modes"] == [
        {"mode": "casual", "energyCost": {"from": 3, "to": 2, "delta": -1}},
    ]
