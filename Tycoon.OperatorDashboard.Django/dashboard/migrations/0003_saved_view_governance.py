from django.db import migrations, models


class Migration(migrations.Migration):
    dependencies = [
        ("dashboard", "0002_operator_saved_view_is_shared"),
    ]

    operations = [
        migrations.AddField(
            model_name="operatorsavedview",
            name="is_archived",
            field=models.BooleanField(default=False),
        ),
        migrations.CreateModel(
            name="OperatorSavedViewAuditEvent",
            fields=[
                ("id", models.BigAutoField(auto_created=True, primary_key=True, serialize=False, verbose_name="ID")),
                ("actor_email", models.EmailField(max_length=254)),
                ("owner_email", models.EmailField(max_length=254)),
                ("view_name", models.CharField(max_length=120)),
                ("action", models.CharField(max_length=40)),
                ("metadata", models.JSONField(default=dict)),
                ("created_at", models.DateTimeField(auto_now_add=True)),
            ],
            options={
                "db_table": "operator_saved_view_audit_events",
            },
        ),
    ]

