from django.db import models


class OperatorSavedView(models.Model):
    owner_email = models.EmailField()
    name = models.CharField(max_length=120)
    is_shared = models.BooleanField(default=False)
    query = models.JSONField(default=dict)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        db_table = "operator_saved_views"
        unique_together = ("owner_email", "name")
