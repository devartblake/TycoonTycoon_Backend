#!/bin/sh
set -e

echo "Running Django migrations..."
python manage.py migrate --noinput

echo "Starting gunicorn..."
exec gunicorn operator_dashboard.wsgi:application \
  --bind 0.0.0.0:8200 \
  --workers 2 \
  --timeout 30
