import os
from pathlib import Path

from django.core.exceptions import ImproperlyConfigured
from dotenv import load_dotenv

load_dotenv()

BASE_DIR = Path(__file__).resolve().parent.parent

SECRET_KEY = os.getenv("DJANGO_SECRET_KEY", "dev-only-change-me")
DEBUG = os.getenv("DJANGO_DEBUG", "true").lower() == "true"
ALLOWED_HOSTS = [host.strip() for host in os.getenv("DJANGO_ALLOWED_HOSTS", "*").split(",")]

# Origins (scheme + host) trusted for unsafe (POST) requests. Required since
# Django 4.0 for the login form to pass CSRF when served over HTTPS behind a
# reverse proxy (e.g. https://admin.synaptixplay.com).
CSRF_TRUSTED_ORIGINS = [
    origin.strip()
    for origin in os.getenv("DJANGO_CSRF_TRUSTED_ORIGINS", "").split(",")
    if origin.strip()
]

# Behind Traefik (TLS terminates at the proxy), trust the forwarded scheme so
# Django treats requests as HTTPS for CSRF origin and secure-cookie checks.
SECURE_PROXY_SSL_HEADER = ("HTTP_X_FORWARDED_PROTO", "https")
USE_X_FORWARDED_HOST = True

# Secure cookies are opt-in (enable in HTTPS deployments). Not derived from
# DEBUG because the container can run with DEBUG=false over plain HTTP locally,
# where secure-only cookies would silently break login.
_secure_cookies = os.getenv("DJANGO_SECURE_COOKIES", "false").lower() == "true"
SESSION_COOKIE_SECURE = _secure_cookies
CSRF_COOKIE_SECURE = _secure_cookies

# The permissive defaults above (dev SECRET_KEY, wildcard ALLOWED_HOSTS) are
# intentional for local development where DEBUG is on. When DEBUG is off — i.e.
# any real deployment — refuse to start on those insecure defaults rather than
# silently shipping them. Correct deployments already inject these via env, so
# this only trips on genuine misconfiguration.
if not DEBUG:
    if SECRET_KEY == "dev-only-change-me":
        raise ImproperlyConfigured(
            "DJANGO_SECRET_KEY must be set to a unique secret value when DEBUG is disabled."
        )
    if "*" in ALLOWED_HOSTS:
        raise ImproperlyConfigured(
            "DJANGO_ALLOWED_HOSTS must list explicit hostnames (not '*') when DEBUG is disabled."
        )

INSTALLED_APPS = [
    "django.contrib.admin",
    "django.contrib.auth",
    "django.contrib.contenttypes",
    "django.contrib.sessions",
    "django.contrib.messages",
    "django.contrib.staticfiles",
    "dashboard",
]

MIDDLEWARE = [
    "django.middleware.security.SecurityMiddleware",
    "django.contrib.sessions.middleware.SessionMiddleware",
    "django.middleware.common.CommonMiddleware",
    "django.middleware.csrf.CsrfViewMiddleware",
    "django.contrib.auth.middleware.AuthenticationMiddleware",
    "django.contrib.messages.middleware.MessageMiddleware",
    "django.middleware.clickjacking.XFrameOptionsMiddleware",
]

if not DEBUG:
    MIDDLEWARE.insert(1, "whitenoise.middleware.WhiteNoiseMiddleware")

ROOT_URLCONF = "operator_dashboard.urls"

TEMPLATES = [
    {
        "BACKEND": "django.template.backends.django.DjangoTemplates",
        "DIRS": [],
        "APP_DIRS": True,
        "OPTIONS": {
            "context_processors": [
                "django.template.context_processors.request",
                "django.contrib.auth.context_processors.auth",
                "django.contrib.messages.context_processors.messages",
            ],
        },
    },
]

WSGI_APPLICATION = "operator_dashboard.wsgi.application"
ASGI_APPLICATION = "operator_dashboard.asgi.application"

DATABASES = {
    "default": {
        "ENGINE": "django.db.backends.sqlite3",
        "NAME": BASE_DIR / "db.sqlite3",
    }
}

LANGUAGE_CODE = "en-us"
TIME_ZONE = "UTC"
USE_I18N = True
USE_TZ = True

STATIC_URL = "/static/"
STATIC_ROOT = BASE_DIR / "staticfiles"

if not DEBUG:
    STORAGES = {
        "default": {
            "BACKEND": "django.core.files.storage.FileSystemStorage",
        },
        "staticfiles": {
            "BACKEND": "whitenoise.storage.CompressedStaticFilesStorage",
        },
    }

DEFAULT_AUTO_FIELD = "django.db.models.BigAutoField"

DOTNET_API_BASE_URL = os.getenv("DOTNET_API_BASE_URL", "http://localhost:5000")
FASTAPI_BASE_URL = os.getenv("FASTAPI_BASE_URL", "http://localhost:8100")
API_REQUEST_TIMEOUT_SECONDS = float(os.getenv("API_REQUEST_TIMEOUT_SECONDS", "5"))

MINIO_BASE_URL = os.getenv("MINIO_BASE_URL", "http://localhost:9000")
INSTALLER_MAX_UPLOAD_BYTES = int(os.getenv("INSTALLER_MAX_UPLOAD_BYTES", str(1024 * 1024 * 1024)))
INSTALLER_ALLOW_LOCAL_MIGRATION_RUN = os.getenv("INSTALLER_ALLOW_LOCAL_MIGRATION_RUN", "false").lower() == "true"

ADMIN_OPS_HEADER = os.getenv("ADMIN_OPS_HEADER", os.getenv("AdminOps__Header", "X-Admin-Ops-Key"))
ADMIN_OPS_KEY = os.getenv("AdminOps__Key", os.getenv("ADMIN_OPS_KEY", ""))
ADMIN_AUTH_TRANSPORT = os.getenv("ADMIN_AUTH_TRANSPORT", "auto").strip().lower()
KMS_API_BASE_URL = os.getenv("KMS_API_BASE_URL", "")
KMS_SERVICE_TOKEN = os.getenv("KMS_SERVICE_TOKEN", "")
LOGIN_URL = "/login"
