from django.test import SimpleTestCase

from dashboard.services.charting import (
    archetype_distribution_chart,
    recommendation_performance_chart,
    top_skus_chart,
)


class ChartingTests(SimpleTestCase):
    def test_top_skus_chart_returns_empty_for_missing_data(self):
        self.assertEqual("", str(top_skus_chart([])))
        self.assertEqual("", str(top_skus_chart([{"sku": "powerup:skip", "purchaseCount": 0}])))

    def test_top_skus_chart_renders_plotly_div(self):
        html = str(top_skus_chart([{"sku": "powerup:skip", "purchaseCount": 731}]))

        self.assertIn("plotly-graph-div", html)
        self.assertIn("powerup:skip", html)
        self.assertIn("Purchases", html)

    def test_archetype_chart_renders_plotly_div(self):
        html = str(archetype_distribution_chart([{"archetype": "new_player", "count": 4}]))

        self.assertIn("plotly-graph-div", html)
        self.assertIn("new_player", html)
        self.assertIn("Profiles", html)

    def test_recommendation_performance_chart_renders_series(self):
        html = str(
            recommendation_performance_chart(
                [{"type": "mission", "accepted": 3, "dismissed": 1, "pending": 1}]
            )
        )

        self.assertIn("plotly-graph-div", html)
        self.assertIn("mission", html)
        self.assertIn("Accepted", html)
        self.assertIn("Dismissed", html)
        self.assertIn("Pending", html)
