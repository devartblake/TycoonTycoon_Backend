from django.db import models


class OperatorSavedView(models.Model):
    owner_email = models.EmailField()
    name = models.CharField(max_length=120)
    is_shared = models.BooleanField(default=False)
    is_archived = models.BooleanField(default=False)
    query = models.JSONField(default=dict)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        db_table = "operator_saved_views"
        unique_together = ("owner_email", "name")


class OperatorSavedViewAuditEvent(models.Model):
    actor_email = models.EmailField()
    owner_email = models.EmailField()
    view_name = models.CharField(max_length=120)
    action = models.CharField(max_length=40)
    metadata = models.JSONField(default=dict)
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        db_table = "operator_saved_view_audit_events"


class ProbeCheckRecord(models.Model):
    service_name = models.CharField(max_length=64)
    status = models.CharField(max_length=16)  # healthy / degraded / offline
    latency_ms = models.IntegerField()
    detail = models.CharField(max_length=255, blank=True)
    checked_at = models.DateTimeField(db_index=True)

    class Meta:
        db_table = "operator_probe_check_records"
        ordering = ["-checked_at"]
