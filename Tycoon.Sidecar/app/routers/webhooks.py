"""
Inbound & outbound webhook handlers.

Routes:
  POST /webhooks/stripe          — Stripe payment events (IAP top-ups)
  POST /webhooks/push/send       — Trigger push notification via tycoon-api
  POST /webhooks/economy/trigger-offers — Trigger economy offer orchestration
  POST /webhooks/generic/{topic} — Generic signed webhook receiver
"""

import hashlib
import hmac
import logging
from uuid import uuid4

from fastapi import APIRouter, Header, HTTPException, Request

from app.config import settings

router = APIRouter()
logger = logging.getLogger(__name__)

# Set via environment / Aspire config
STRIPE_WEBHOOK_SECRET = settings.stripe_webhook_secret


@router.post("/stripe")
async def stripe_webhook(
    request: Request,
    stripe_signature: str = Header(alias="stripe-signature", default=""),
):
    """
    Receives Stripe payment events and forwards the relevant ones
    (e.g. checkout.session.completed) to tycoon-api to credit the player's wallet.
    """
    payload = await request.body()

    if STRIPE_WEBHOOK_SECRET:
        expected = hmac.new(
            STRIPE_WEBHOOK_SECRET.encode(),
            payload,
            hashlib.sha256,
        ).hexdigest()
        provided_sig = ""
        for part in stripe_signature.split(","):
            if part.startswith("v1="):
                provided_sig = part.split("=", 1)[-1]
                break
        if not provided_sig or not hmac.compare_digest(expected, provided_sig):
            raise HTTPException(status_code=400, detail="Invalid Stripe signature")

    import json
    event = json.loads(payload)
    event_type = event.get("type", "")
    logger.info("Stripe webhook received: %s", event_type)

    if event_type == "checkout.session.completed":
        obj = event.get("data", {}).get("object", {})
        metadata = obj.get("metadata", {}) if isinstance(obj, dict) else {}
        player_id = metadata.get("playerId")
        currency = (metadata.get("currency") or "diamonds").lower()
        amount_raw = metadata.get("amount")

        try:
            amount = int(amount_raw)
        except (TypeError, ValueError):
            amount = 0

        currency_map = {"xp": 1, "coins": 2, "diamonds": 3}
        if not player_id or amount <= 0 or currency not in currency_map:
            logger.warning("Ignoring Stripe event due to missing/invalid economy metadata: %s", metadata)
            return {"received": True, "forwarded": False, "reason": "invalid_metadata"}

        backend: "httpx.AsyncClient" = request.app.state.backend
        event_id = metadata.get("eventId") or str(uuid4())
        tx_payload = {
            "eventId": event_id,
            "playerId": player_id,
            "kind": "iap-topup",
            "lines": [{"currency": currency_map[currency], "delta": amount}],
            "note": f"stripe checkout session {obj.get('id', '')}",
        }
        tx_resp = await backend.post("/admin/economy/transactions", json=tx_payload)
        logger.info("Forwarded Stripe top-up for player %s (%s %+d): status=%s", player_id, currency, amount, tx_resp.status_code)
        return {"received": True, "forwarded": True, "status": tx_resp.status_code}

    return {"received": True}


@router.post("/push/send")
async def send_push(request: Request):
    """
    Thin proxy — accepts a push notification payload and forwards to
    tycoon-api /admin/notifications (useful for server-to-server sends
    without exposing the admin ops key to external callers directly).
    """
    body = await request.json()
    backend = request.app.state.backend
    resp = await backend.post("/admin/notifications", json=body)
    return {"forwarded": True, "status": resp.status_code}


@router.post("/economy/trigger-offers")
async def trigger_economy_offers(request: Request):
    """
    Phase 2 monetization hook dispatcher.
    Accepts trigger payload and returns offer recommendation + optional push forward.
    """
    body = await request.json()
    trigger = (body.get("trigger") or "").strip()
    player_id = body.get("playerId")

    offer_by_trigger = {
        "out_of_energy": {"offer": "energy_refill", "message": "Refill now and keep your streak alive."},
        "lost_jackpot": {"offer": "revive_discount", "message": "Second chance revive available at a discount."},
        "near_promotion": {"offer": "xp_boost", "message": "Boost XP for your next promotion push."},
        "lost_guardian": {"offer": "retry_ticket", "message": "Guardian retry ticket unlocked."},
        "long_session": {"offer": "streak_multiplier", "message": "Activate streak multiplier for bonus rewards."},
    }

    recommendation = offer_by_trigger.get(trigger, {"offer": "none", "message": "No active offer for this trigger."})

    backend = request.app.state.backend
    push_requested = bool(body.get("sendPush", False))
    push_status = None

    if push_requested and recommendation["offer"] != "none":
        push_payload = {
            "title": "Tycoon Offer",
            "body": recommendation["message"],
            "targetUserIds": [player_id] if player_id else [],
            "metadata": {"trigger": trigger, "offer": recommendation["offer"]},
        }
        push_resp = await backend.post("/admin/notifications", json=push_payload)
        push_status = push_resp.status_code

    return {
        "status": "ok",
        "trigger": trigger,
        "playerId": player_id,
        "recommendation": recommendation,
        "pushRequested": push_requested,
        "pushStatus": push_status,
    }


@router.post("/generic/{topic}")
async def generic_webhook(topic: str, request: Request):
    payload = await request.json()
    logger.info("Generic webhook [%s]: %s", topic, payload)
    return {"topic": topic, "received": True}
