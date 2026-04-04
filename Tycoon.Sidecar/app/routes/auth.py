"""
Auth bridge — proxies login/refresh/logout to the Synaptix .NET API.

Routes:
  POST /auth/login    — forward credentials, return JWT + user
  POST /auth/refresh  — forward refresh token, return new JWT
  POST /auth/logout   — forward logout (invalidate device session)
"""

from fastapi import APIRouter, Request, Response
from pydantic import BaseModel

router = APIRouter()


class LoginRequest(BaseModel):
    email: str
    password: str
    device_id: str


class RefreshRequest(BaseModel):
    refresh_token: str
    device_id: str


class LogoutRequest(BaseModel):
    device_id: str


async def _proxy(request: Request, method: str, path: str, body: dict) -> Response:
    """Forward a request to the .NET backend and return its response verbatim."""
    client = request.app.state.backend
    resp = await client.request(method, path, json=body)
    return Response(
        content=resp.content,
        status_code=resp.status_code,
        media_type=resp.headers.get("content-type", "application/json"),
    )


@router.post("/login")
async def login(req: LoginRequest, request: Request) -> Response:
    return await _proxy(request, "POST", "/auth/login", req.model_dump())


@router.post("/refresh")
async def refresh(req: RefreshRequest, request: Request) -> Response:
    return await _proxy(request, "POST", "/auth/refresh", req.model_dump())


@router.post("/logout")
async def logout(req: LogoutRequest, request: Request) -> Response:
    return await _proxy(request, "POST", "/auth/logout", req.model_dump())
