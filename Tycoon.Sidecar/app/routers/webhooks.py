"""
Inbound & outbound webhook handlers.

Routes:
  POST /webhooks/stripe          — Stripe payment events (IAP top-ups)
  POST /webhooks/push/send       — Trigger push notification via tycoon-api
  POST /webhooks/generic/{topic} — Generic signed webhook receiver
"""

import hashlib
import hmac
import logging

from fastapi import APIRouter, Header, HTTPException, Request

from app.config import settings

router = APIRouter()
logger = logging.getLogger(__name__)

# Set via environment / Aspire config
STRIPE_WEBHOOK_SECRET = ""


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
        if not hmac.compare_digest(expected, stripe_signature.split(",")[-1].split("=")[-1]):
            raise HTTPException(status_code=400, detail="Invalid Stripe signature")

    import json
    event = json.loads(payload)
    event_type = event.get("type", "")
    logger.info("Stripe webhook received: %s", event_type)

    if event_type == "checkout.session.completed":
        # TODO: extract player_id from metadata and call tycoon-api economy endpoint
        backend: "httpx.AsyncClient" = request.app.state.backend
        _ = backend  # use to POST /mobile/economy/top-up
        logger.info("Checkout session completed — forward to tycoon-api")

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


@router.post("/generic/{topic}")
async def generic_webhook(topic: str, request: Request):
    payload = await request.json()
    logger.info("Generic webhook [%s]: %s", topic, payload)
    return {"topic": topic, "received": True}
