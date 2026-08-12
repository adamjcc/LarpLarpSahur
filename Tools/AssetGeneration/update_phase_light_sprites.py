"""Update Road Ready phase lights for clean lit and neutral default states.

The existing housing, cream ring, gloss and transparent canvas are preserved.
Lit colours are softened and all external sunburst rays are removed. A shared
grey unlit state is produced from the existing unlit artwork.
"""

from __future__ import annotations

import io
import re
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
PHASE_DIR = PROJECT_ROOT / "Assets" / "Sprites" / "RoadReadyUI" / "PhaseLights"
PREVIEW_PATH = PROJECT_ROOT / "Docs" / "DUX_Assets" / "road-ready-phase-lights-updated.png"

LIT_FILES = (
    "RR_PhaseLight_Red_Lit.png",
    "RR_PhaseLight_Amber_Lit.png",
    "RR_PhaseLight_Green_Lit.png",
)

GIT_EXECUTABLE = Path(
    r"C:\Users\legal\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe"
)


def circular_keep_mask(size: int, radius: float, supersample: int = 4) -> Image.Image:
    """Return an antialiased mask that retains only the centred lamp housing."""
    high_size = size * supersample
    centre = (size - 1) / 2
    mask = Image.new("L", (high_size, high_size), 0)
    draw = ImageDraw.Draw(mask)
    bounds = tuple(
        round(value * supersample)
        for value in (
            centre - radius,
            centre - radius,
            centre + radius,
            centre + radius,
        )
    )
    draw.ellipse(bounds, fill=255)
    return mask.resize((size, size), Image.Resampling.LANCZOS)


def remove_rays(image: Image.Image) -> Image.Image:
    """Remove only the external rays, preserving the original vivid lens."""
    rgba = image.convert("RGBA")
    size = rgba.width
    keep_mask = circular_keep_mask(size, radius=size * 0.337)
    source_alpha = rgba.getchannel("A")
    rgba.putalpha(Image.composite(source_alpha, Image.new("L", rgba.size, 0), keep_mask))
    return rgba


def load_original_lit_sprite(filename: str) -> Image.Image:
    """Load the untouched sprite from Git so rerunning never alters colours."""
    repository_path = f"Assets/Sprites/RoadReadyUI/PhaseLights/{filename}"
    data = subprocess.check_output(
        [str(GIT_EXECUTABLE), "show", f"HEAD:{repository_path}"],
        cwd=PROJECT_ROOT,
    )
    if data.startswith(b"version https://git-lfs.github.com/spec/v1"):
        pointer = data.decode("ascii")
        match = re.search(r"oid sha256:([0-9a-f]{64})", pointer)
        if not match:
            raise ValueError(f"Could not read Git LFS pointer for {filename}")
        object_id = match.group(1)
        object_path = PROJECT_ROOT / ".git" / "lfs" / "objects" / object_id[:2] / object_id[2:4] / object_id
        data = object_path.read_bytes()
    return Image.open(io.BytesIO(data)).convert("RGBA")


def make_grey_unlit(image: Image.Image) -> Image.Image:
    """Convert only the coloured lens into a subdued neutral grey."""
    rgba = image.convert("RGBA")
    size = rgba.width
    centre = (size - 1) / 2
    pixels = rgba.load()
    lens_radius_squared = (size * 0.274) ** 2

    for y in range(size):
        dy = y - centre
        for x in range(size):
            if (x - centre) ** 2 + dy**2 > lens_radius_squared:
                continue
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue
            luminance = 0.2126 * red + 0.7152 * green + 0.0722 * blue
            # Compress the original red lens values into a charcoal-to-mid-grey
            # range while retaining the hand-painted shading and gloss.
            grey = round(58 + (luminance / 255) * 78)
            pixels[x, y] = (grey, grey, grey, alpha)
    return rgba


def checker(size: tuple[int, int], cell: int = 24) -> Image.Image:
    result = Image.new("RGBA", size, (38, 44, 51, 255))
    draw = ImageDraw.Draw(result)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(49, 56, 64, 255))
    return result


def make_preview(files: list[Path]) -> None:
    tile_size = 300
    gap = 28
    margin = 42
    width = margin * 2 + len(files) * tile_size + (len(files) - 1) * gap
    height = 460
    preview = Image.new("RGB", (width, height), (237, 232, 221))
    labels = ("DEFAULT / OFF", "OBSERVE", "INVESTIGATE", "INTERVENE")

    from PIL import ImageFont

    title_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 30)
    label_font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 18)
    draw = ImageDraw.Draw(preview)
    draw.text((margin, 20), "Road Ready - Updated Phase Lights", fill=(30, 35, 40), font=title_font)

    for index, (path, label) in enumerate(zip(files, labels, strict=True)):
        x = margin + index * (tile_size + gap)
        tile = checker((tile_size, tile_size))
        sprite = Image.open(path).convert("RGBA").resize((270, 270), Image.Resampling.LANCZOS)
        tile.alpha_composite(sprite, (15, 15))
        preview.paste(tile.convert("RGB"), (x, 72))
        bounds = draw.textbbox((0, 0), label, font=label_font)
        draw.text(
            (x + (tile_size - (bounds[2] - bounds[0])) / 2, 392),
            label,
            fill=(43, 49, 55),
            font=label_font,
        )

    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW_PATH, optimize=True)


def validate(path: Path, expect_no_rays: bool = True) -> None:
    image = Image.open(path).convert("RGBA")
    if image.size != (512, 512):
        raise ValueError(f"{path.name}: expected 512 x 512")
    alpha = image.getchannel("A")
    if alpha.getextrema() != (0, 255):
        raise ValueError(f"{path.name}: transparency is missing")
    if expect_no_rays:
        # The rays occupied the canvas outside the housing. That area must now
        # contain no meaningfully visible pixels.
        centre = 255.5
        pixels = alpha.load()
        for y in range(512):
            for x in range(512):
                if (x - centre) ** 2 + (y - centre) ** 2 > 174**2 and pixels[x, y] > 8:
                    raise ValueError(f"{path.name}: visible ray remains at {x}, {y}")


def validate_lens_colour(path: Path, expected: str) -> tuple[int, int, int]:
    """Check the central lens colour remains vivid and in the correct family."""
    image = Image.open(path).convert("RGB")
    samples = []
    centre = 255.5
    for y in range(190, 322):
        for x in range(190, 322):
            if (x - centre) ** 2 + (y - centre) ** 2 <= 64**2:
                samples.append(image.getpixel((x, y)))
    average = tuple(round(sum(pixel[index] for pixel in samples) / len(samples)) for index in range(3))
    red, green, blue = average
    if expected == "red" and not (red >= 220 and red > green * 2.4 and red > blue * 2.0):
        raise ValueError(f"{path.name}: lens is not distinctly red: {average}")
    if expected == "amber" and not (red >= 210 and green >= 145 and blue < 90 and red > green):
        raise ValueError(f"{path.name}: lens is not distinctly amber: {average}")
    if expected == "green" and not (green >= 210 and green > red * 2.0 and green > blue * 1.6):
        raise ValueError(f"{path.name}: lens is not distinctly green: {average}")
    return average


def main() -> None:
    updated_paths: list[Path] = []
    colour_names = ("red", "amber", "green")
    for filename, colour_name in zip(LIT_FILES, colour_names, strict=True):
        path = PHASE_DIR / filename
        updated = remove_rays(load_original_lit_sprite(filename))
        updated.save(path, optimize=True)
        validate(path)
        average = validate_lens_colour(path, colour_name)
        print(f"{filename} centre average RGB: {average}")
        updated_paths.append(path)

    grey_path = PHASE_DIR / "RR_PhaseLight_Grey_Unlit.png"
    grey_source = Image.open(PHASE_DIR / "RR_PhaseLight_Red_Unlit.png")
    make_grey_unlit(grey_source).save(grey_path, optimize=True)
    validate(grey_path)

    make_preview([grey_path, *updated_paths])
    print(f"Created {grey_path}")
    for path in updated_paths:
        print(f"Updated {path}")
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()
