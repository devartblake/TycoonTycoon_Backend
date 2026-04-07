from django.db import migrations, models


class Migration(migrations.Migration):
    dependencies = [
        ("dashboard", "0001_initial"),
    ]

    operations = [
        migrations.AddField(
            model_name="operatorsavedview",
            name="is_shared",
            field=models.BooleanField(default=False),
        ),
    ]

