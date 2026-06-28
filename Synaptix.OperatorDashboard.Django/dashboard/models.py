from django.db import models


class OperatorSavedView(models.Model):
    owner_email = models.EmailField(db_index=True)
    name = models.CharField(max_length=120)
    is_shared = models.BooleanField(default=False, db_index=True)
    is_archived = models.BooleanField(default=False, db_index=True)
    query = models.JSONField(default=dict)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        db_table = "operator_saved_views"
        unique_together = ("owner_email", "name")
        indexes = [
            models.Index(fields=["owner_email", "is_archived"]),
            models.Index(fields=["is_shared", "is_archived"]),
        ]


class OperatorSavedViewAuditEvent(models.Model):
    actor_email = models.EmailField(db_index=True)
    owner_email = models.EmailField(db_index=True)
    view_name = models.CharField(max_length=120, db_index=True)
    action = models.CharField(max_length=40, db_index=True)
    metadata = models.JSONField(default=dict)
    created_at = models.DateTimeField(auto_now_add=True, db_index=True)

    class Meta:
        db_table = "operator_saved_view_audit_events"
        indexes = [
            models.Index(fields=["created_at", "actor_email"]),
            models.Index(fields=["created_at", "owner_email"]),
            models.Index(fields=["-created_at"]),
        ]


class ProbeCheckRecord(models.Model):
    service_name = models.CharField(max_length=64, db_index=True)
    status = models.CharField(max_length=16, db_index=True)
    latency_ms = models.IntegerField()
    detail = models.CharField(max_length=255, blank=True)
    checked_at = models.DateTimeField(db_index=True)

    class Meta:
        db_table = "operator_probe_check_records"
        ordering = ["-checked_at"]
        indexes = [
            models.Index(fields=["service_name", "checked_at"]),
            models.Index(fields=["service_name", "-checked_at"]),
            models.Index(fields=["status", "checked_at"]),
        ]
