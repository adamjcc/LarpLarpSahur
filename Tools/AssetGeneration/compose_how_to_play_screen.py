"""Compose the Road Ready How to Play screen using the project's own fonts."""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
TUTORIAL_DIR = PROJECT_ROOT / "Assets" / "Sprites" / "RoadReadyUI" / "Tutorial"
SOURCE_PATH = TUTORIAL_DIR / "RR_HowToPlay_CardSource.png"
OUTPUT_PATH = TUTORIAL_DIR / "RR_HowToPlay_Screen.png"
PREVIEW_PATH = PROJECT_ROOT / "Docs" / "DUX_Assets" / "road-ready-how-to-play-preview.png"

CAVEAT = PROJECT_ROOT / "Assets" / "Fonts" / "CaveatBrush-Regular.ttf"
ANNIE = PROJECT_ROOT / "Assets" / "Fonts" / "AnnieUseYourTelescope-Regular.ttf"

WIDTH = 3840
HEIGHT = 2160
SCALE = 2

NAVY = (16, 24, 32, 255)
GREEN = (22, 122, 68, 255)
AMBER = (255, 184, 31, 255)
RED = (214, 61, 49, 255)
CREAM = (247, 244, 232, 255)
MUTED = (76, 86, 91, 255)
WHITE = (255, 255, 255, 255)


def caveat(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(CAVEAT), size * SCALE)


def annie(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(ANNIE), size * SCALE)


def centred_text(
    draw: ImageDraw.ImageDraw,
    x: int,
    y: int,
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
    stroke_width: int = 0,
    stroke_fill: tuple[int, int, int, int] | None = None,
) -> None:
    draw.text(
        (x * SCALE, y * SCALE),
        text,
        font=font,
        fill=fill,
        anchor="mm",
        align="center",
        stroke_width=stroke_width * SCALE,
        stroke_fill=stroke_fill,
    )


def draw_step_number(draw: ImageDraw.ImageDraw, centre: tuple[int, int], number: str, colour: tuple[int, int, int, int]) -> None:
    x, y = centre
    radius = 25
    draw.ellipse(
        ((x - radius) * SCALE, (y - radius) * SCALE, (x + radius) * SCALE, (y + radius) * SCALE),
        fill=colour,
        outline=NAVY,
        width=5 * SCALE,
    )
    centred_text(draw, x, y - 1, number, caveat(26), WHITE, stroke_width=1, stroke_fill=NAVY)


def draw_eye(draw: ImageDraw.ImageDraw, centre: tuple[int, int]) -> None:
    x, y = centre
    points = []
    for index in range(25):
        angle = math.pi * index / 24
        points.append(((x - 75 + 150 * index / 24) * SCALE, (y - 42 * math.sin(angle)) * SCALE))
    for index in range(24, -1, -1):
        angle = math.pi * index / 24
        points.append(((x - 75 + 150 * index / 24) * SCALE, (y + 42 * math.sin(angle)) * SCALE))
    draw.line(points + [points[0]], fill=NAVY, width=7 * SCALE, joint="curve")
    draw.ellipse(((x - 29) * SCALE, (y - 29) * SCALE, (x + 29) * SCALE, (y + 29) * SCALE), fill=GREEN, outline=NAVY, width=5 * SCALE)
    draw.ellipse(((x - 9) * SCALE, (y - 9) * SCALE, (x + 9) * SCALE, (y + 9) * SCALE), fill=CREAM)


def draw_magnifier(draw: ImageDraw.ImageDraw, centre: tuple[int, int]) -> None:
    x, y = centre
    draw.ellipse(((x - 58) * SCALE, (y - 58) * SCALE, (x + 38) * SCALE, (y + 38) * SCALE), outline=NAVY, width=9 * SCALE)
    draw.line(((x + 28) * SCALE, (y + 28) * SCALE, (x + 82) * SCALE, (y + 82) * SCALE), fill=NAVY, width=15 * SCALE)
    positions = ((x - 27, y - 26), (x + 5, y - 26), (x - 27, y + 6), (x + 5, y + 6))
    colours = (GREEN, AMBER, RED, GREEN)
    for (px, py), colour in zip(positions, colours, strict=True):
        draw.ellipse(((px - 9) * SCALE, (py - 9) * SCALE, (px + 9) * SCALE, (py + 9) * SCALE), fill=colour, outline=NAVY, width=2 * SCALE)


def draw_wrench_clock(draw: ImageDraw.ImageDraw, centre: tuple[int, int]) -> None:
    x, y = centre
    draw.ellipse(((x - 64) * SCALE, (y - 64) * SCALE, (x + 64) * SCALE, (y + 64) * SCALE), fill=(255, 239, 207, 255), outline=NAVY, width=7 * SCALE)
    draw.line((x * SCALE, y * SCALE, x * SCALE, (y - 38) * SCALE), fill=NAVY, width=7 * SCALE)
    draw.line((x * SCALE, y * SCALE, (x + 32) * SCALE, (y + 16) * SCALE), fill=RED, width=7 * SCALE)
    draw.arc(((x + 38) * SCALE, (y - 78) * SCALE, (x + 122) * SCALE, (y + 6) * SCALE), start=35, end=320, fill=GREEN, width=10 * SCALE)
    draw.polygon(((x + 111) * SCALE, (y - 12) * SCALE, (x + 126) * SCALE, (y + 10) * SCALE, (x + 97) * SCALE, (y + 8) * SCALE), fill=GREEN)


def draw_replay_report(draw: ImageDraw.ImageDraw, centre: tuple[int, int]) -> None:
    x, y = centre
    draw.rounded_rectangle(((x - 47) * SCALE, (y - 60) * SCALE, (x + 47) * SCALE, (y + 58) * SCALE), radius=9 * SCALE, fill=CREAM, outline=NAVY, width=6 * SCALE)
    draw.line(((x - 25) * SCALE, (y - 27) * SCALE, (x + 25) * SCALE, (y - 27) * SCALE), fill=GREEN, width=6 * SCALE)
    draw.line(((x - 25) * SCALE, (y - 3) * SCALE, (x + 18) * SCALE, (y - 3) * SCALE), fill=NAVY, width=4 * SCALE)
    draw.line(((x - 25) * SCALE, (y + 19) * SCALE, (x + 25) * SCALE, (y + 19) * SCALE), fill=NAVY, width=4 * SCALE)
    draw.arc(((x + 24) * SCALE, (y - 83) * SCALE, (x + 111) * SCALE, (y + 4) * SCALE), start=35, end=325, fill=AMBER, width=10 * SCALE)
    draw.polygon(((x + 100) * SCALE, (y - 11) * SCALE, (x + 118) * SCALE, (y + 8) * SCALE, (x + 89) * SCALE, (y + 9) * SCALE), fill=AMBER)


def keycap(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, width: int) -> None:
    box = ((x - width // 2) * SCALE, (y - 29) * SCALE, (x + width // 2) * SCALE, (y + 29) * SCALE)
    draw.rounded_rectangle(box, radius=11 * SCALE, fill=NAVY, outline=WHITE, width=3 * SCALE)
    centred_text(draw, x, y - 1, text, caveat(20), WHITE)


def compose() -> Image.Image:
    source = Image.open(SOURCE_PATH).convert("RGBA")
    card = source.resize((3600, 2100), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), (255, 255, 255, 0))
    canvas.alpha_composite(card, (120, 30))
    draw = ImageDraw.Draw(canvas)

    # The generated card's four boxes mapped onto the final 1920 x 1080 design grid.
    centres = ((300, 560), (710, 610), (1125, 560), (1575, 610))
    icon_centres = ((300, 475), (710, 490), (1125, 440), (1575, 505))
    step_colours = (GREEN, AMBER, RED, GREEN)

    centred_text(draw, 960, 155, "How to Play", caveat(92), GREEN, stroke_width=5, stroke_fill=WHITE)
    centred_text(draw, 960, 225, "See the risk. Change the outcome.", annie(36), NAVY)

    step_data = (
        ("1", "OBSERVE", "Watch the incident from above.\nNotice both road users."),
        ("2", "INVESTIGATE", "Walk around and examine clues.\nFind all 4 hazards."),
        ("3", "INTERVENE", "Go back 3 seconds.\nChange hazards before impact."),
        ("4", "REVIEW", "Watch the new outcome.\nRead your score and debrief."),
    )
    icon_drawers = (draw_eye, draw_magnifier, draw_wrench_clock, draw_replay_report)

    for index, ((number, heading, body), centre, icon_centre, colour, icon_drawer) in enumerate(
        zip(step_data, centres, icon_centres, step_colours, icon_drawers, strict=True)
    ):
        icon_drawer(draw, icon_centre)
        draw_step_number(draw, (centre[0] - 120, centre[1] - 8), number, colour)
        centred_text(draw, centre[0] + 30, centre[1] - 18, heading, caveat(43), NAVY)
        centred_text(draw, centre[0], centre[1] + 56, body, annie(25), NAVY)

    # Compact controls strip beneath the map. It sits on the paper itself.
    strip = (285 * SCALE, 835 * SCALE, 1635 * SCALE, 985 * SCALE)
    draw.rounded_rectangle(strip, radius=44 * SCALE, fill=(247, 244, 232, 235), outline=NAVY, width=7 * SCALE)
    centred_text(draw, 960, 865, "Controls", caveat(38), GREEN)

    control_items = (
        (395, "WASD", 130, "Move"),
        (645, "MOUSE", 150, "Look"),
        (910, "E / CLICK", 180, "Interact"),
        (1195, "Q / RMB", 175, "Leave view"),
        (1490, "ENTER", 145, "Continue"),
    )
    for x, key, width, label in control_items:
        keycap(draw, x, 920, key, width)
        centred_text(draw, x, 965, label, annie(22), NAVY)

    return canvas


def validate(image: Image.Image) -> None:
    if image.size != (WIDTH, HEIGHT) or image.mode != "RGBA":
        raise ValueError("Tutorial screen must be a 3840 x 2160 RGBA image")
    alpha = image.getchannel("A")
    if alpha.getextrema() != (0, 255):
        raise ValueError(f"Unexpected alpha range: {alpha.getextrema()}")
    corners = (
        alpha.getpixel((0, 0)),
        alpha.getpixel((WIDTH - 1, 0)),
        alpha.getpixel((0, HEIGHT - 1)),
        alpha.getpixel((WIDTH - 1, HEIGHT - 1)),
    )
    if any(corner != 0 for corner in corners):
        raise ValueError("The area outside the paper is not transparent")


def main() -> None:
    TUTORIAL_DIR.mkdir(parents=True, exist_ok=True)
    screen = compose()
    validate(screen)
    screen.save(OUTPUT_PATH, optimize=True)
    preview = screen.resize((1920, 1080), Image.Resampling.LANCZOS)
    preview.save(PREVIEW_PATH, optimize=True)
    print(f"Created {OUTPUT_PATH}")
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()
