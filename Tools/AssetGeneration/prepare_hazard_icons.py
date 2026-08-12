"""Prepare Road Ready hazard icons as aligned found and undiscovered sprites."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFont, ImageOps


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
ICON_DIR = PROJECT_ROOT / "Assets" / "Sprites" / "RoadReadyUI" / "HazardIcons"
SOURCE_DIR = ICON_DIR / "Sources"
PREVIEW_PATH = PROJECT_ROOT / "Docs" / "DUX_Assets" / "road-ready-hazard-icons.png"

CANVAS_SIZE = 1024
TARGET_CONTENT = 850

SOURCES = {
    "Headphones": SOURCE_DIR / "RR_Hazard_Headphones_Cutout.png",
    "Phone": SOURCE_DIR / "RR_Hazard_Phone_Cutout.png",
    "BrakePedal": SOURCE_DIR / "RR_Hazard_BrakePedal_Source.png",
    "HeadlightSwitch": SOURCE_DIR / "RR_Hazard_HeadlightSwitch_Source.png",
}

DISPLAY_NAMES = {
    "Headphones": "HEADPHONES",
    "Phone": "PHONE",
    "BrakePedal": "BRAKE PEDAL",
    "HeadlightSwitch": "HEADLIGHT SWITCH",
}


def normalise(source_path: Path) -> Image.Image:
    image = Image.open(source_path).convert("RGBA")
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"{source_path.name}: no visible subject")
    subject = image.crop(bounds)
    subject.thumbnail((TARGET_CONTENT, TARGET_CONTENT), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (255, 255, 255, 0))
    position = ((CANVAS_SIZE - subject.width) // 2, (CANVAS_SIZE - subject.height) // 2)
    canvas.alpha_composite(subject, position)
    return canvas


def undiscovered(found: Image.Image) -> Image.Image:
    """Create a readable neutral state without changing the silhouette."""
    alpha = found.getchannel("A")
    grey = ImageOps.grayscale(found.convert("RGB"))
    grey = ImageEnhance.Contrast(grey).enhance(0.82)
    grey = ImageEnhance.Brightness(grey).enhance(0.68)
    tinted = ImageOps.colorize(grey, black="#18222B", white="#AAB1B5").convert("RGBA")
    tinted.putalpha(alpha.point(lambda value: round(value * 0.82)))
    return tinted


def validate(path: Path) -> None:
    image = Image.open(path).convert("RGBA")
    if image.size != (CANVAS_SIZE, CANVAS_SIZE):
        raise ValueError(f"{path.name}: expected {CANVAS_SIZE} x {CANVAS_SIZE}")
    alpha = image.getchannel("A")
    if alpha.getextrema() != (0, 255) and not path.name.endswith("_Unfound.png"):
        raise ValueError(f"{path.name}: invalid transparency range {alpha.getextrema()}")
    corners = [
        alpha.getpixel((0, 0)),
        alpha.getpixel((CANVAS_SIZE - 1, 0)),
        alpha.getpixel((0, CANVAS_SIZE - 1)),
        alpha.getpixel((CANVAS_SIZE - 1, CANVAS_SIZE - 1)),
    ]
    if any(corner != 0 for corner in corners):
        raise ValueError(f"{path.name}: transparent corners are missing")
    coverage = sum(1 for value in alpha.getdata() if value > 16) / (CANVAS_SIZE * CANVAS_SIZE)
    if not 0.12 <= coverage <= 0.80:
        raise ValueError(f"{path.name}: unexpected visible coverage {coverage:.2%}")


def checker(size: tuple[int, int], cell: int = 24) -> Image.Image:
    result = Image.new("RGBA", size, (34, 41, 48, 255))
    draw = ImageDraw.Draw(result)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(45, 53, 60, 255))
    return result


def make_preview(found_paths: list[Path], unfound_paths: list[Path]) -> None:
    margin = 46
    tile = 280
    gap = 28
    row_gap = 100
    width = margin * 2 + tile * 4 + gap * 3
    height = 860
    preview = Image.new("RGB", (width, height), (237, 232, 221))
    draw = ImageDraw.Draw(preview)
    heading_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 32)
    row_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 19)
    label_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 16)
    draw.text((margin, 22), "Road Ready - Hazard HUD Icons", fill=(29, 34, 39), font=heading_font)
    draw.text((margin, 76), "FOUND / IDENTIFIED", fill=(29, 75, 50), font=row_font)
    draw.text((margin, 468), "NOT FOUND / UNKNOWN", fill=(74, 80, 85), font=row_font)

    for row, paths in enumerate((found_paths, unfound_paths)):
        y = 112 + row * (tile + row_gap)
        for column, path in enumerate(paths):
            x = margin + column * (tile + gap)
            tile_image = checker((tile, tile))
            icon = Image.open(path).convert("RGBA").resize((250, 250), Image.Resampling.LANCZOS)
            tile_image.alpha_composite(icon, (15, 15))
            preview.paste(tile_image.convert("RGB"), (x, y))
            key = path.name.replace("RR_Hazard_", "").replace("_Found.png", "").replace("_Unfound.png", "")
            label = DISPLAY_NAMES[key]
            bounds = draw.textbbox((0, 0), label, font=label_font)
            draw.text(
                (x + (tile - (bounds[2] - bounds[0])) / 2, y + tile + 12),
                label,
                fill=(39, 45, 51),
                font=label_font,
            )

    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW_PATH, optimize=True)


def main() -> None:
    ICON_DIR.mkdir(parents=True, exist_ok=True)
    found_paths: list[Path] = []
    unfound_paths: list[Path] = []
    for key, source_path in SOURCES.items():
        found = normalise(source_path)
        found_path = ICON_DIR / f"RR_Hazard_{key}_Found.png"
        unfound_path = ICON_DIR / f"RR_Hazard_{key}_Unfound.png"
        found.save(found_path, optimize=True)
        undiscovered(found).save(unfound_path, optimize=True)
        validate(found_path)
        validate(unfound_path)
        found_paths.append(found_path)
        unfound_paths.append(unfound_path)
        print(f"Created {found_path.name} and {unfound_path.name}")

    make_preview(found_paths, unfound_paths)
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()
