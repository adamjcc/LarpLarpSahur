"""Compose transparent Road Ready start-menu logos from the approved icon."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
LOGO_DIR = PROJECT_ROOT / "Assets" / "Sprites" / "RoadReadyUI" / "Logo"
PREVIEW_PATH = PROJECT_ROOT / "Docs" / "DUX_Assets" / "road-ready-logo-versions.png"
ICON_PATH = LOGO_DIR / "RR_Logo_IconSource.png"
FONT_PATH = PROJECT_ROOT / "Assets" / "Fonts" / "CaveatBrush-Regular.ttf"

NAVY = (16, 24, 32, 255)
GREEN = (22, 122, 68, 255)
CREAM = (247, 244, 232, 255)
AMBER = (255, 199, 44, 255)
WHITE = (255, 255, 255, 255)
TRANSPARENT = (255, 255, 255, 0)


def trimmed_icon() -> Image.Image:
    icon = Image.open(ICON_PATH).convert("RGBA")
    bounds = icon.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("The generated logo icon has no visible pixels")
    return icon.crop(bounds)


def fit_font(text: str, maximum_width: int, starting_size: int) -> ImageFont.FreeTypeFont:
    probe = Image.new("L", (16, 16), 0)
    draw = ImageDraw.Draw(probe)
    size = starting_size
    while size > 24:
        candidate = ImageFont.truetype(str(FONT_PATH), size)
        bounds = draw.textbbox((0, 0), text, font=candidate, stroke_width=0)
        if bounds[2] - bounds[0] <= maximum_width:
            return candidate
        size -= 2
    return ImageFont.truetype(str(FONT_PATH), size)


def draw_bubbly_text(
    canvas: Image.Image,
    position: tuple[int, int],
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
    anchor: str = "mm",
) -> None:
    draw = ImageDraw.Draw(canvas)
    # Dark outside keyline keeps the white bubble readable on a bright menu.
    draw.text(position, text, font=font, fill=fill, stroke_width=23, stroke_fill=NAVY, anchor=anchor)
    draw.text(position, text, font=font, fill=fill, stroke_width=14, stroke_fill=WHITE, anchor=anchor)
    # A thin navy separator preserves the hand-painted letter shapes.
    draw.text(position, text, font=font, fill=fill, stroke_width=4, stroke_fill=NAVY, anchor=anchor)


def crop_with_padding(image: Image.Image, padding: int = 40) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("Logo canvas is empty")
    left = max(0, bounds[0] - padding)
    top = max(0, bounds[1] - padding)
    right = min(image.width, bounds[2] + padding)
    bottom = min(image.height, bounds[3] + padding)
    return image.crop((left, top, right, bottom))


def make_icon_logo(
    road_colour: tuple[int, int, int, int],
    ready_colour: tuple[int, int, int, int],
) -> Image.Image:
    canvas = Image.new("RGBA", (1800, 780), TRANSPARENT)
    icon = trimmed_icon()
    icon.thumbnail((570, 570), Image.Resampling.LANCZOS)
    canvas.alpha_composite(icon, (60, (canvas.height - icon.height) // 2))

    text_centre_x = 1170
    font = fit_font("ROAD READY", 1040, 285)
    draw_bubbly_text(canvas, (text_centre_x, 265), "ROAD", font, road_colour)
    draw_bubbly_text(canvas, (text_centre_x, 520), "READY", font, ready_colour)
    return crop_with_padding(canvas)


def make_wordmark(
    road_colour: tuple[int, int, int, int],
    ready_colour: tuple[int, int, int, int],
) -> Image.Image:
    canvas = Image.new("RGBA", (1500, 800), TRANSPARENT)
    font = fit_font("ROAD READY", 1300, 330)
    draw_bubbly_text(canvas, (750, 270), "ROAD", font, road_colour)
    draw_bubbly_text(canvas, (750, 555), "READY", font, ready_colour)
    return crop_with_padding(canvas)


def save_logo(filename: str, image: Image.Image) -> Path:
    path = LOGO_DIR / filename
    image.save(path, optimize=True)
    return path


def checker(size: tuple[int, int], cell: int = 28) -> Image.Image:
    result = Image.new("RGBA", size, (31, 38, 44, 255))
    draw = ImageDraw.Draw(result)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(42, 50, 57, 255))
    return result


def make_preview(paths: list[Path]) -> None:
    preview = Image.new("RGB", (1600, 1120), (235, 230, 219))
    draw = ImageDraw.Draw(preview)
    heading_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 36)
    label_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 19)
    draw.text((50, 28), "Road Ready - Start Menu Logo Options", font=heading_font, fill=(29, 34, 39))

    labels = (
        "A. ICON + GREEN / CREAM",
        "B. ICON + AMBER / GREEN",
        "C. WORDMARK + GREEN / CREAM",
        "D. WORDMARK + CREAM / AMBER",
    )
    tile_width, tile_height = 720, 440
    positions = ((50, 100), (830, 100), (50, 610), (830, 610))

    for path, label, (x, y) in zip(paths, labels, positions, strict=True):
        tile = checker((tile_width, tile_height))
        logo = Image.open(path).convert("RGBA")
        logo.thumbnail((tile_width - 70, tile_height - 70), Image.Resampling.LANCZOS)
        tile.alpha_composite(logo, ((tile_width - logo.width) // 2, (tile_height - logo.height) // 2))
        preview.paste(tile.convert("RGB"), (x, y))
        draw.text((x, y + tile_height + 15), label, font=label_font, fill=(42, 48, 54))

    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW_PATH, optimize=True)


def validate(path: Path) -> None:
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A")
    if alpha.getextrema() != (0, 255):
        raise ValueError(f"{path.name}: missing useful transparency")
    corners = (alpha.getpixel((0, 0)), alpha.getpixel((image.width - 1, 0)), alpha.getpixel((0, image.height - 1)), alpha.getpixel((image.width - 1, image.height - 1)))
    if any(value != 0 for value in corners):
        raise ValueError(f"{path.name}: transparent padding is missing")


def main() -> None:
    LOGO_DIR.mkdir(parents=True, exist_ok=True)
    outputs = [
        save_logo("RR_Logo_Icon_GreenCream.png", make_icon_logo(GREEN, CREAM)),
        save_logo("RR_Logo_Icon_AmberGreen.png", make_icon_logo(AMBER, GREEN)),
        save_logo("RR_Logo_Wordmark_GreenCream.png", make_wordmark(GREEN, CREAM)),
        save_logo("RR_Logo_Wordmark_CreamAmber.png", make_wordmark(CREAM, AMBER)),
    ]
    for path in outputs:
        validate(path)
        with Image.open(path) as image:
            print(f"Created {path.name}: {image.width} x {image.height}")
    make_preview(outputs)
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()
