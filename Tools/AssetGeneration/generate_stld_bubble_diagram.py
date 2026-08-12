"""Generate the Road Ready STLD bubble diagram as editable SVG and PNG."""

from __future__ import annotations

import html
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
OUTPUT_DIR = PROJECT_ROOT / "Docs" / "STLD_Assets"
SVG_PATH = OUTPUT_DIR / "STLD_RoadReady_BubbleDiagram.svg"
PNG_PATH = OUTPUT_DIR / "STLD_RoadReady_BubbleDiagram.png"

WIDTH = 1800
HEIGHT = 1320
PNG_SCALE = 2

COLOURS = {
    "background": "#F8F7F3",
    "ink": "#20262D",
    "muted": "#5F6872",
    "line": "#56616C",
    "flow": "#263846",
    "start_fill": "#EEF1F3",
    "start_stroke": "#7B8790",
    "observe_fill": "#FFF2CD",
    "observe_stroke": "#C38A1A",
    "investigate_fill": "#E5F1F7",
    "investigate_stroke": "#31759B",
    "secondary_fill": "#F3F8FB",
    "secondary_stroke": "#6091AC",
    "intervene_fill": "#FFE7D6",
    "intervene_stroke": "#D56A31",
    "resolve_fill": "#E7F4E8",
    "resolve_stroke": "#4D8A58",
    "debrief_fill": "#F0ECF6",
    "debrief_stroke": "#766196",
    "white": "#FFFFFF",
}


def colour(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[index : index + 2], 16) for index in (0, 2, 4))


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    filename = "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path(r"C:\Windows\Fonts") / filename), size=size)


def scaled_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return font(size * PNG_SCALE, bold)


def centre(box: tuple[int, int, int, int]) -> tuple[float, float]:
    x1, y1, x2, y2 = box
    return ((x1 + x2) / 2, (y1 + y2) / 2)


def ellipse_edge(box: tuple[int, int, int, int], toward: tuple[float, float]) -> tuple[float, float]:
    cx, cy = centre(box)
    rx = (box[2] - box[0]) / 2
    ry = (box[3] - box[1]) / 2
    dx = toward[0] - cx
    dy = toward[1] - cy
    if dx == 0 and dy == 0:
        return (cx, cy)
    factor = 1 / math.sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry))
    return (cx + dx * factor, cy + dy * factor)


def line_between(
    box_a: tuple[int, int, int, int],
    box_b: tuple[int, int, int, int],
    dashed: bool = False,
) -> tuple[tuple[float, float], tuple[float, float]]:
    centre_a = centre(box_a)
    centre_b = centre(box_b)
    return ellipse_edge(box_a, centre_b), ellipse_edge(box_b, centre_a)


NODES = {
    "start": {
        "box": (42, 390, 252, 540),
        "title": "GAME START",
        "lines": ["Objective and controls", "Intensity: LOW"],
        "fill": "start_fill",
        "stroke": "start_stroke",
    },
    "observe": {
        "box": (300, 340, 590, 575),
        "title": "OBSERVE",
        "lines": ["Bird's-eye incident view", "Full collision plays", "Intensity: HIGH"],
        "fill": "observe_fill",
        "stroke": "observe_stroke",
    },
    "hub": {
        "box": (650, 275, 1050, 615),
        "title": "MAIN CROSSING",
        "subtitle": "Investigation hub",
        "lines": [
            "Frozen aftermath",
            "Impact point is the landmark",
            "Free-roam, inspect and talk",
            "Intensity: LOW-MEDIUM",
        ],
        "fill": "investigate_fill",
        "stroke": "investigate_stroke",
    },
    "pedestrian": {
        "box": (505, 720, 815, 950),
        "title": "PEDESTRIAN SIDE",
        "lines": ["Pavement and kerb", "Phone + headphones", "Pedestrian POV replay"],
        "fill": "secondary_fill",
        "stroke": "secondary_stroke",
    },
    "vehicle": {
        "box": (890, 710, 1255, 960),
        "title": "VEHICLE SIDE",
        "lines": ["Car exterior + passenger seat", "Headlights + speed/brake", "Driver POV replay"],
        "fill": "secondary_fill",
        "stroke": "secondary_stroke",
    },
    "environment": {
        "box": (700, 1010, 1060, 1155),
        "title": "ENVIRONMENTAL CLUES",
        "lines": ["Road signs, markings and red herrings"],
        "fill": "secondary_fill",
        "stroke": "secondary_stroke",
    },
    "intervene": {
        "box": (1165, 320, 1465, 590),
        "title": "INTERVENE",
        "subtitle": "Same crossing reused",
        "lines": ["Return to 3 seconds before impact", "Change the four hazards", "Slow world, timed action", "Intensity: PEAK"],
        "fill": "intervene_fill",
        "stroke": "intervene_stroke",
    },
    "resolve": {
        "box": (1515, 345, 1770, 565),
        "title": "RESOLVE",
        "lines": ["Full-speed replay", "Collision or safe stop", "Intensity: HIGH"],
        "fill": "resolve_fill",
        "stroke": "resolve_stroke",
    },
    "debrief": {
        "box": (1495, 730, 1775, 955),
        "title": "DEBRIEF",
        "lines": ["Score and outcome", "Found, missed and changed", "Retry loops to Observe", "Intensity: LOW"],
        "fill": "debrief_fill",
        "stroke": "debrief_stroke",
    },
}

REQUIRED_EDGES = [
    ("start", "observe", ""),
    ("observe", "hub", "Enter investigation"),
    ("hub", "intervene", "Confirm when ready"),
    ("intervene", "resolve", "Changes applied"),
    ("resolve", "debrief", "Outcome explained"),
]

FREE_EDGES = [
    ("hub", "pedestrian"),
    ("hub", "vehicle"),
    ("hub", "environment"),
]


def draw_arrow_png(
    draw: ImageDraw.ImageDraw,
    start: tuple[float, float],
    end: tuple[float, float],
    dashed: bool = False,
    both_ways: bool = False,
    width: int = 4,
    draw_line: bool = True,
    show_arrow: bool = True,
) -> None:
    scale = PNG_SCALE
    sx, sy = start[0] * scale, start[1] * scale
    ex, ey = end[0] * scale, end[1] * scale
    stroke = colour(COLOURS["flow"])

    if draw_line and dashed:
        dx, dy = ex - sx, ey - sy
        distance = math.hypot(dx, dy)
        unit_x, unit_y = dx / distance, dy / distance
        dash = 16 * scale
        gap = 10 * scale
        position = 0.0
        while position < distance:
            segment_end = min(position + dash, distance)
            draw.line(
                (
                    sx + unit_x * position,
                    sy + unit_y * position,
                    sx + unit_x * segment_end,
                    sy + unit_y * segment_end,
                ),
                fill=stroke,
                width=width * scale,
            )
            position += dash + gap
    elif draw_line:
        draw.line((sx, sy, ex, ey), fill=stroke, width=width * scale)

    def arrowhead(tip_x: float, tip_y: float, from_x: float, from_y: float) -> None:
        angle = math.atan2(tip_y - from_y, tip_x - from_x)
        length = 16 * scale
        spread = math.radians(28)
        points = [
            (tip_x, tip_y),
            (tip_x - length * math.cos(angle - spread), tip_y - length * math.sin(angle - spread)),
            (tip_x - length * math.cos(angle + spread), tip_y - length * math.sin(angle + spread)),
        ]
        draw.polygon(points, fill=stroke)

    if show_arrow:
        arrowhead(ex, ey, sx, sy)
        if both_ways:
            arrowhead(sx, sy, ex, ey)


def draw_centred_text_png(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    title: str,
    subtitle: str | None,
    lines: list[str],
) -> None:
    cx, cy = centre(box)
    title_font = scaled_font(20, bold=True)
    subtitle_font = scaled_font(16, bold=True)
    body_font = scaled_font(16)
    spacing = 10 * PNG_SCALE
    entries: list[tuple[str, ImageFont.FreeTypeFont, tuple[int, int, int]]] = [
        (title, title_font, colour(COLOURS["ink"]))
    ]
    if subtitle:
        entries.append((subtitle, subtitle_font, colour(COLOURS["muted"])))
    for line in lines:
        entries.append((line, body_font, colour(COLOURS["ink"])))

    heights = []
    for text, text_font, _ in entries:
        bounds = draw.textbbox((0, 0), text, font=text_font)
        heights.append(bounds[3] - bounds[1])
    total_height = sum(heights) + spacing * (len(entries) - 1)
    current_y = cy * PNG_SCALE - total_height / 2

    for (text, text_font, text_colour), text_height in zip(entries, heights, strict=True):
        bounds = draw.textbbox((0, 0), text, font=text_font)
        text_width = bounds[2] - bounds[0]
        draw.text(
            (cx * PNG_SCALE - text_width / 2, current_y),
            text,
            font=text_font,
            fill=text_colour,
        )
        current_y += text_height + spacing


def create_png() -> None:
    image = Image.new(
        "RGB",
        (WIDTH * PNG_SCALE, HEIGHT * PNG_SCALE),
        colour(COLOURS["background"]),
    )
    draw = ImageDraw.Draw(image)

    draw.text((50 * PNG_SCALE, 35 * PNG_SCALE), "ROAD READY - BUBBLE DIAGRAM", font=scaled_font(32, True), fill=colour(COLOURS["ink"]))
    draw.text((50 * PNG_SCALE, 82 * PNG_SCALE), "Spatial flow, activity areas and gameplay rhythm", font=scaled_font(18), fill=colour(COLOURS["muted"]))
    draw.line((50 * PNG_SCALE, 122 * PNG_SCALE, 1750 * PNG_SCALE, 122 * PNG_SCALE), fill=colour("#CDD2D5"), width=2 * PNG_SCALE)

    for source, target, _ in REQUIRED_EDGES:
        start, end = line_between(NODES[source]["box"], NODES[target]["box"])
        draw_arrow_png(draw, start, end)

    for source, target in FREE_EDGES:
        start, end = line_between(NODES[source]["box"], NODES[target]["box"])
        draw_arrow_png(draw, start, end, dashed=True, both_ways=True, width=3)

    # Retry loop is deliberately lighter so it does not compete with the main flow.
    retry_start = ellipse_edge(NODES["debrief"]["box"], (1400, 1200))
    retry_end = ellipse_edge(NODES["observe"]["box"], (290, 1200))
    retry_points = [
        (retry_start[0] * PNG_SCALE, retry_start[1] * PNG_SCALE),
        (1430 * PNG_SCALE, 1200 * PNG_SCALE),
        (290 * PNG_SCALE, 1200 * PNG_SCALE),
        (retry_end[0] * PNG_SCALE, retry_end[1] * PNG_SCALE),
    ]
    for segment_start, segment_end in zip(retry_points, retry_points[1:]):
        draw_arrow_png(
            draw,
            (segment_start[0] / PNG_SCALE, segment_start[1] / PNG_SCALE),
            (segment_end[0] / PNG_SCALE, segment_end[1] / PNG_SCALE),
            dashed=True,
            width=2,
            show_arrow=False,
        )
    draw_arrow_png(
        (draw),
        (retry_points[-2][0] / PNG_SCALE, retry_points[-2][1] / PNG_SCALE),
        retry_end,
        width=2,
        draw_line=False,
    )
    draw.text((1200 * PNG_SCALE, 1204 * PNG_SCALE), "Retry to Observe", font=scaled_font(15, True), fill=colour(COLOURS["muted"]))

    for data in NODES.values():
        box = tuple(value * PNG_SCALE for value in data["box"])
        draw.ellipse(
            box,
            fill=colour(COLOURS[data["fill"]]),
            outline=colour(COLOURS[data["stroke"]]),
            width=4 * PNG_SCALE,
        )
        draw_centred_text_png(
            draw,
            data["box"],
            data["title"],
            data.get("subtitle"),
            data["lines"],
        )

    # Edge labels are added after bubbles so they stay unobstructed.
    labels = [
        (608, 330, "Enter investigation"),
        (1048, 322, "Confirm when ready"),
        (1468, 326, "Changes applied"),
        (1580, 630, "Outcome explained"),
        (550, 655, "Explore in any order"),
    ]
    for x, y, text in labels:
        bounds = draw.textbbox((0, 0), text, font=scaled_font(14, True))
        pad_x, pad_y = 8 * PNG_SCALE, 5 * PNG_SCALE
        background_box = (
            x * PNG_SCALE - pad_x,
            y * PNG_SCALE - pad_y,
            x * PNG_SCALE + (bounds[2] - bounds[0]) + pad_x,
            y * PNG_SCALE + (bounds[3] - bounds[1]) + pad_y,
        )
        draw.rounded_rectangle(background_box, radius=8 * PNG_SCALE, fill=colour(COLOURS["background"]))
        draw.text((x * PNG_SCALE, y * PNG_SCALE), text, font=scaled_font(14, True), fill=colour(COLOURS["muted"]))

    legend_y = 1260
    draw.line((50 * PNG_SCALE, legend_y * PNG_SCALE, 102 * PNG_SCALE, legend_y * PNG_SCALE), fill=colour(COLOURS["flow"]), width=4 * PNG_SCALE)
    draw.text((116 * PNG_SCALE, (legend_y - 11) * PNG_SCALE), "Required phase flow", font=scaled_font(14), fill=colour(COLOURS["muted"]))
    for start_x in range(335, 390, 18):
        draw.line((start_x * PNG_SCALE, legend_y * PNG_SCALE, (start_x + 10) * PNG_SCALE, legend_y * PNG_SCALE), fill=colour(COLOURS["flow"]), width=3 * PNG_SCALE)
    draw.text((405 * PNG_SCALE, (legend_y - 11) * PNG_SCALE), "Free exploration and return", font=scaled_font(14), fill=colour(COLOURS["muted"]))
    draw.text((700 * PNG_SCALE, (legend_y - 11) * PNG_SCALE), "Bubble size shows relative importance and activity, not exact scale.", font=scaled_font(14), fill=colour(COLOURS["muted"]))

    image.save(PNG_PATH, optimize=True)


def svg_text(x: float, y: float, text: str, size: int, weight: int = 400, colour_key: str = "ink") -> str:
    return f'<text x="{x}" y="{y}" text-anchor="middle" font-family="Arial, sans-serif" font-size="{size}" font-weight="{weight}" fill="{COLOURS[colour_key]}">{html.escape(text)}</text>'


def create_svg() -> None:
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
        "<defs>",
        f'<marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M 0 0 L 10 5 L 0 10 z" fill="{COLOURS["flow"]}"/></marker>',
        "</defs>",
        f'<rect width="{WIDTH}" height="{HEIGHT}" fill="{COLOURS["background"]}"/>',
        f'<text x="50" y="70" font-family="Arial, sans-serif" font-size="32" font-weight="700" fill="{COLOURS["ink"]}">ROAD READY - BUBBLE DIAGRAM</text>',
        f'<text x="50" y="105" font-family="Arial, sans-serif" font-size="18" fill="{COLOURS["muted"]}">Spatial flow, activity areas and gameplay rhythm</text>',
        '<line x1="50" y1="122" x2="1750" y2="122" stroke="#CDD2D5" stroke-width="2"/>',
    ]

    for source, target, _ in REQUIRED_EDGES:
        start, end = line_between(NODES[source]["box"], NODES[target]["box"])
        parts.append(f'<line x1="{start[0]:.1f}" y1="{start[1]:.1f}" x2="{end[0]:.1f}" y2="{end[1]:.1f}" stroke="{COLOURS["flow"]}" stroke-width="4" marker-end="url(#arrow)"/>')

    for source, target in FREE_EDGES:
        start, end = line_between(NODES[source]["box"], NODES[target]["box"])
        parts.append(f'<line x1="{start[0]:.1f}" y1="{start[1]:.1f}" x2="{end[0]:.1f}" y2="{end[1]:.1f}" stroke="{COLOURS["flow"]}" stroke-width="3" stroke-dasharray="16 10" marker-start="url(#arrow)" marker-end="url(#arrow)"/>')

    retry_start = ellipse_edge(NODES["debrief"]["box"], (1400, 1200))
    retry_end = ellipse_edge(NODES["observe"]["box"], (290, 1200))
    parts.append(f'<path d="M {retry_start[0]:.1f} {retry_start[1]:.1f} L 1430 1200 L 290 1200 L {retry_end[0]:.1f} {retry_end[1]:.1f}" fill="none" stroke="{COLOURS["line"]}" stroke-width="2" stroke-dasharray="10 8" marker-end="url(#arrow)"/>')
    parts.append(f'<text x="1200" y="1205" font-family="Arial, sans-serif" font-size="15" font-weight="700" fill="{COLOURS["muted"]}">Retry to Observe</text>')

    for data in NODES.values():
        x1, y1, x2, y2 = data["box"]
        cx, cy = centre(data["box"])
        rx, ry = (x2 - x1) / 2, (y2 - y1) / 2
        parts.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{COLOURS[data["fill"]]}" stroke="{COLOURS[data["stroke"]]}" stroke-width="4"/>')

        entries = [(data["title"], 20, 700, "ink")]
        if data.get("subtitle"):
            entries.append((data["subtitle"], 16, 700, "muted"))
        entries.extend((line, 16, 400, "ink") for line in data["lines"])
        line_height = 28
        start_y = cy - ((len(entries) - 1) * line_height) / 2 + 6
        for index, (text_value, size, weight, colour_key) in enumerate(entries):
            parts.append(svg_text(cx, start_y + index * line_height, text_value, size, weight, colour_key))

    labels = [
        (608, 330, "Enter investigation"),
        (1048, 322, "Confirm when ready"),
        (1468, 326, "Changes applied"),
        (1580, 630, "Outcome explained"),
        (550, 655, "Explore in any order"),
    ]
    for x, y, text_value in labels:
        estimated_width = len(text_value) * 8.2 + 16
        parts.append(f'<rect x="{x - 8}" y="{y - 18}" width="{estimated_width:.1f}" height="27" rx="8" fill="{COLOURS["background"]}"/>')
        parts.append(f'<text x="{x}" y="{y}" font-family="Arial, sans-serif" font-size="14" font-weight="700" fill="{COLOURS["muted"]}">{html.escape(text_value)}</text>')

    parts.extend(
        [
            f'<line x1="50" y1="1260" x2="102" y2="1260" stroke="{COLOURS["flow"]}" stroke-width="4" marker-end="url(#arrow)"/>',
            f'<text x="116" y="1265" font-family="Arial, sans-serif" font-size="14" fill="{COLOURS["muted"]}">Required phase flow</text>',
            f'<line x1="335" y1="1260" x2="390" y2="1260" stroke="{COLOURS["flow"]}" stroke-width="3" stroke-dasharray="10 8"/>',
            f'<text x="405" y="1265" font-family="Arial, sans-serif" font-size="14" fill="{COLOURS["muted"]}">Free exploration and return</text>',
            f'<text x="700" y="1265" font-family="Arial, sans-serif" font-size="14" fill="{COLOURS["muted"]}">Bubble size shows relative importance and activity, not exact scale.</text>',
            "</svg>",
        ]
    )
    SVG_PATH.write_text("\n".join(parts), encoding="utf-8")


def validate() -> None:
    image = Image.open(PNG_PATH)
    if image.size != (WIDTH * PNG_SCALE, HEIGHT * PNG_SCALE):
        raise ValueError(f"Unexpected PNG size: {image.size}")
    svg = SVG_PATH.read_text(encoding="utf-8")
    for required_text in ("MAIN CROSSING", "PEDESTRIAN SIDE", "VEHICLE SIDE", "INTERVENE", "DEBRIEF"):
        if required_text not in svg:
            raise ValueError(f"Missing SVG label: {required_text}")


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    create_png()
    create_svg()
    validate()
    print(f"Created {PNG_PATH}")
    print(f"Created {SVG_PATH}")


if __name__ == "__main__":
    main()
