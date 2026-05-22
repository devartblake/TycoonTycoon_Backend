from __future__ import annotations

from typing import Any

import plotly.graph_objects as go
import plotly.io as pio
from django.utils.safestring import SafeString, mark_safe
from plotly.offline import get_plotlyjs


_PLOT_CONFIG = {
    "displayModeBar": False,
    "responsive": True,
}

_BASE_LAYOUT = {
    "template": "plotly_white",
    "paper_bgcolor": "rgba(0,0,0,0)",
    "plot_bgcolor": "rgba(0,0,0,0)",
    "font": {"family": "Inter, Segoe UI, system-ui, sans-serif", "color": "#34343c", "size": 12},
    "margin": {"l": 42, "r": 16, "t": 20, "b": 42},
    "height": 320,
}


def plotly_runtime_script() -> SafeString:
    """Inline Plotly once on pages that render server-generated charts."""
    return mark_safe(f"<script>{get_plotlyjs()}</script>")


def top_skus_chart(top_skus: list[dict[str, Any]] | None) -> SafeString:
    rows = _valid_rows(top_skus, "sku", "purchaseCount")
    if not rows:
        return mark_safe("")

    labels = [str(row.get("sku") or "-") for row in rows]
    values = [_number(row.get("purchaseCount")) for row in rows]
    fig = go.Figure(
        data=[
            go.Bar(
                x=labels,
                y=values,
                marker_color="#0f766e",
                hovertemplate="<b>%{x}</b><br>Purchases: %{y}<extra></extra>",
            )
        ]
    )
    fig.update_layout(
        **_BASE_LAYOUT,
        yaxis_title="Purchases",
        xaxis_title="SKU",
        xaxis_tickangle=-20,
    )
    return _render(fig)


def archetype_distribution_chart(archetypes: list[dict[str, Any]] | None) -> SafeString:
    rows = _valid_rows(archetypes, "archetype", "count")
    if not rows:
        return mark_safe("")

    labels = [str(row.get("archetype") or "-") for row in rows]
    values = [_number(row.get("count")) for row in rows]
    fig = go.Figure(
        data=[
            go.Pie(
                labels=labels,
                values=values,
                hole=0.56,
                marker={"colors": ["#0f766e", "#2563eb", "#a15c07", "#7c3aed", "#14804a", "#b42318"]},
                hovertemplate="<b>%{label}</b><br>Profiles: %{value}<extra></extra>",
            )
        ]
    )
    fig.update_layout(**_BASE_LAYOUT, showlegend=True)
    return _render(fig)


def recommendation_performance_chart(performance: list[dict[str, Any]] | None) -> SafeString:
    rows = [row for row in performance or [] if isinstance(row, dict) and row.get("type")]
    if not rows:
        return mark_safe("")

    labels = [str(row.get("type") or "-") for row in rows]
    fig = go.Figure()
    for name, color in (("accepted", "#14804a"), ("dismissed", "#b42318"), ("pending", "#a15c07")):
        fig.add_trace(
            go.Bar(
                name=name.title(),
                x=labels,
                y=[_number(row.get(name)) for row in rows],
                marker_color=color,
                hovertemplate=f"<b>%{{x}}</b><br>{name.title()}: %{{y}}<extra></extra>",
            )
        )
    fig.update_layout(
        **_BASE_LAYOUT,
        barmode="group",
        yaxis_title="Recommendations",
        xaxis_title="Type",
        legend={"orientation": "h", "yanchor": "bottom", "y": 1.02, "xanchor": "right", "x": 1},
    )
    return _render(fig)


def _valid_rows(rows: list[dict[str, Any]] | None, label_key: str, value_key: str) -> list[dict[str, Any]]:
    return [
        row
        for row in rows or []
        if isinstance(row, dict) and row.get(label_key) not in (None, "") and _number(row.get(value_key)) > 0
    ]


def _number(value: Any) -> float:
    try:
        return float(value or 0)
    except (TypeError, ValueError):
        return 0


def _render(fig: go.Figure) -> SafeString:
    html = pio.to_html(
        fig,
        include_plotlyjs=False,
        full_html=False,
        config=_PLOT_CONFIG,
        div_id=None,
    )
    return mark_safe(html)
