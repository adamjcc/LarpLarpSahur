"""Generate tintable, modular UI shape sprites for Road Ready.

Every exported sprite is a pure-white RGBA mask. Colour should be applied in
Unity through the UI Image component's vertex colour. Shapes are rendered at a
higher resolution first and then downsampled for smooth, clean edges.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(r"C:\UNITYPROJECTS\LarpLarpSahur")
OUTPUT_DIR = PROJECT_ROOT / "Assets" / "Sprites" / "RoadReadyUI" / "ModularShapes"
PREVIEW_DIR = PROJECT_ROOT / "Docs" / "DUX_Assets"

SIZE = 512
SCALE = 4
HIGH_SIZE = SIZE * SCALE
WHITE = (255, 255, 255, 255)
TRANSPARENT_WHITE = (255, 255, 255, 0)


def _scaled_box(box: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    return tuple(value * SCALE for value in box)


def _scaled_points(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [(round(x * SCALE), round(y * SCALE)) for x, y in points]


def _new_high_res_mask() -> Image.Image:
    return Image.new("RGBA", (HIGH_SIZE, HIGH_SIZE), TRANSPARENT_WHITE)


def _finish_mask(high_res: Image.Image) -> Image.Image:
    """Downsample while keeping every RGB channel pure white."""
    resized = high_res.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    alpha = resized.getchannel("A")
    finished = Image.new("RGBA", (SIZE, SIZE), TRANSPARENT_WHITE)
    finished.putalpha(alpha)
    return finished


def rounded_rectangle_fill() -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        _scaled_box((24, 24, 488, 488)),
        radius=64 * SCALE,
        fill=WHITE,
    )
    return _finish_mask(image)


def rounded_rectangle_outline(
    box: tuple[int, int, int, int], radius: int, width: int
) -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        _scaled_box(box),
        radius=radius * SCALE,
        outline=WHITE,
        width=width * SCALE,
    )
    return _finish_mask(image)


def circle_fill() -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    draw.ellipse(_scaled_box((32, 32, 480, 480)), fill=WHITE)
    return _finish_mask(image)


def circle_outline(box: tuple[int, int, int, int], width: int) -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    draw.ellipse(
        _scaled_box(box),
        outline=WHITE,
        width=width * SCALE,
    )
    return _finish_mask(image)


def _arc_points(
    centre_x: float,
    centre_y: float,
    radius: float,
    start_angle: float,
    end_angle: float,
    segments: int = 24,
) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    for step in range(segments + 1):
        progress = step / segments
        angle = math.radians(start_angle + (end_angle - start_angle) * progress)
        points.append(
            (
                centre_x + math.cos(angle) * radius,
                centre_y + math.sin(angle) * radius,
            )
        )
    return points


def _right_arrow_path(inset: bool = False) -> list[tuple[float, float]]:
    if inset:
        # The inward line retains the same rounded-left and pointed-right form.
        return (
            [(88, 112), (340, 112), (454, 256), (340, 400), (88, 400)]
            + _arc_points(88, 364, 36, 90, 180)
            + [(52, 148)]
            + _arc_points(88, 148, 36, 180, 270)
        )

    return (
        [(76, 84), (350, 84), (488, 256), (350, 428), (76, 428)]
        + _arc_points(76, 380, 48, 90, 180)
        + [(28, 132)]
        + _arc_points(76, 132, 48, 180, 270)
    )


def right_arrow_fill() -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    draw.polygon(_scaled_points(_right_arrow_path()), fill=WHITE)
    return _finish_mask(image)


def right_arrow_outline(inset: bool, width: int) -> Image.Image:
    image = _new_high_res_mask()
    draw = ImageDraw.Draw(image)
    points = _scaled_points(_right_arrow_path(inset))
    draw.line(
        points + [points[0]],
        fill=WHITE,
        width=width * SCALE,
        joint="curve",
    )
    return _finish_mask(image)


def _tint(mask: Image.Image, colour: tuple[int, int, int, int]) -> Image.Image:
    tinted = Image.new("RGBA", mask.size, colour)
    tinted.putalpha(mask.getchannel("A"))
    return tinted


def _checker(size: tuple[int, int], cell: int = 24) -> Image.Image:
    image = Image.new("RGBA", size, (39, 45, 52, 255))
    draw = ImageDraw.Draw(image)
    alternate = (50, 57, 65, 255)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=alternate)
    return image


def _font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path(r"C:\Windows\Fonts\arialbd.ttf") if bold else Path(r"C:\Windows\Fonts\arial.ttf"),
        Path(r"C:\Windows\Fonts\segoeuib.ttf") if bold else Path(r"C:\Windows\Fonts\segoeui.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


def _make_contact_sheet(
    families: dict[str, tuple[Image.Image, Image.Image, Image.Image]]
) -> None:
    margin = 54
    label_height = 54
    tile = 260
    gap = 28
    columns = 4
    rows = len(families)
    width = margin * 2 + columns * tile + (columns - 1) * gap
    height = margin * 2 + 64 + rows * (tile + label_height + gap)
    sheet = Image.new("RGB", (width, height), (235, 229, 217))
    draw = ImageDraw.Draw(sheet)
    title_font = _font(34, bold=True)
    label_font = _font(21, bold=True)
    small_font = _font(17)
    draw.text((margin, 22), "Road Ready - Modular UI Shape Masks", fill=(26, 31, 36), font=title_font)

    column_labels = ("Pure-white fill", "Inner outline", "Outer outline", "Tinted stack example")
    for col, label in enumerate(column_labels):
        x = margin + col * (tile + gap)
        draw.text((x, 69), label, fill=(49, 56, 62), font=small_font)

    palettes = {
        "Rounded rectangle": ((20, 51, 82, 255), (247, 231, 190, 255), (32, 36, 40, 255)),
        "Circle / capsule": ((221, 69, 59, 255), (247, 231, 190, 255), (20, 51, 82, 255)),
        "Right-arrow sign": ((45, 111, 82, 255), (247, 231, 190, 255), (32, 36, 40, 255)),
    }

    for row, (family_name, (fill, inner, outer)) in enumerate(families.items()):
        y = 103 + row * (tile + label_height + gap)
        for col, mask in enumerate((fill, inner, outer)):
            x = margin + col * (tile + gap)
            tile_image = _checker((tile, tile))
            preview_mask = mask.resize((tile - 26, tile - 26), Image.Resampling.LANCZOS)
            tile_image.alpha_composite(preview_mask, (13, 13))
            sheet.paste(tile_image.convert("RGB"), (x, y))

        fill_colour, inner_colour, outer_colour = palettes[family_name]
        example = _checker((tile, tile))
        for mask, colour in ((fill, fill_colour), (outer, outer_colour), (inner, inner_colour)):
            layer = _tint(mask, colour).resize((tile - 26, tile - 26), Image.Resampling.LANCZOS)
            example.alpha_composite(layer, (13, 13))
        example_x = margin + 3 * (tile + gap)
        sheet.paste(example.convert("RGB"), (example_x, y))
        draw.text((margin, y + tile + 11), family_name, fill=(26, 31, 36), font=label_font)

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    sheet.save(PREVIEW_DIR / "road-ready-modular-shapes-preview.png", optimize=True)


def _nine_slice(
    image: Image.Image,
    target_size: tuple[int, int],
    borders: tuple[int, int, int, int],
) -> Image.Image:
    """Resize an image using Unity-style left, right, top and bottom borders."""
    left, right, top, bottom = borders
    source_width, source_height = image.size
    target_width, target_height = target_size
    source_x = (0, left, source_width - right, source_width)
    source_y = (0, top, source_height - bottom, source_height)
    target_x = (0, left, target_width - right, target_width)
    target_y = (0, top, target_height - bottom, target_height)
    result = Image.new("RGBA", target_size, TRANSPARENT_WHITE)

    for row in range(3):
        for column in range(3):
            source_box = (
                source_x[column],
                source_y[row],
                source_x[column + 1],
                source_y[row + 1],
            )
            target_box = (
                target_x[column],
                target_y[row],
                target_x[column + 1],
                target_y[row + 1],
            )
            target_piece_size = (
                target_box[2] - target_box[0],
                target_box[3] - target_box[1],
            )
            piece = image.crop(source_box).resize(target_piece_size, Image.Resampling.LANCZOS)
            result.alpha_composite(piece, (target_box[0], target_box[1]))
    return result


def _make_resize_test(
    families: dict[str, tuple[Image.Image, Image.Image, Image.Image]]
) -> None:
    canvas = Image.new("RGB", (1320, 1140), (235, 229, 217))
    draw = ImageDraw.Draw(canvas)
    title_font = _font(34, bold=True)
    label_font = _font(20, bold=True)
    draw.text((52, 25), "9-slice and scaling preview", fill=(26, 31, 36), font=title_font)

    tests = [
        (
            "Rounded rectangle - freely resizable",
            "Rounded rectangle",
            (1040, 220),
            (96, 96, 96, 96),
            (1040, 220),
        ),
        (
            "Circle stretched horizontally into a capsule",
            "Circle / capsule",
            (1040, 512),
            (224, 224, 224, 224),
            (1040, 240),
        ),
        (
            "Right-arrow sign - horizontal resize",
            "Right-arrow sign",
            (1040, 512),
            (96, 176, 128, 128),
            (1040, 300),
        ),
    ]
    palettes = {
        "Rounded rectangle": ((20, 51, 82, 255), (247, 231, 190, 255), (32, 36, 40, 255)),
        "Circle / capsule": ((221, 69, 59, 255), (247, 231, 190, 255), (20, 51, 82, 255)),
        "Right-arrow sign": ((45, 111, 82, 255), (247, 231, 190, 255), (32, 36, 40, 255)),
    }

    y = 91
    for label, family_name, target_size, borders, display_size in tests:
        draw.text((52, y), label, fill=(49, 56, 62), font=label_font)
        y += 34
        backdrop = _checker(target_size, cell=28)
        fill, inner, outer = families[family_name]
        fill_colour, inner_colour, outer_colour = palettes[family_name]
        for mask, colour in ((fill, fill_colour), (outer, outer_colour), (inner, inner_colour)):
            sliced = _nine_slice(_tint(mask, colour), target_size, borders)
            backdrop.alpha_composite(sliced)
        displayed = backdrop.resize(display_size, Image.Resampling.LANCZOS)
        canvas.paste(displayed.convert("RGB"), (140, y))
        y += display_size[1] + 34

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    canvas.save(PREVIEW_DIR / "road-ready-modular-shapes-resize-test.png", optimize=True)


def _validate_and_save(filename: str, image: Image.Image) -> None:
    if image.size != (SIZE, SIZE) or image.mode != "RGBA":
        raise ValueError(f"{filename}: expected a 512 x 512 RGBA image")
    if image.getchannel("A").getextrema() != (0, 255):
        raise ValueError(f"{filename}: alpha channel must contain transparent and opaque pixels")
    if any(channel.getextrema() != (255, 255) for channel in image.convert("RGB").split()):
        raise ValueError(f"{filename}: RGB channels must remain pure white")
    image.save(OUTPUT_DIR / filename, optimize=True)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    families = {
        "Rounded rectangle": (
            rounded_rectangle_fill(),
            rounded_rectangle_outline((58, 58, 454, 454), radius=42, width=10),
            rounded_rectangle_outline((32, 32, 480, 480), radius=58, width=14),
        ),
        "Circle / capsule": (
            circle_fill(),
            circle_outline((68, 68, 444, 444), width=10),
            circle_outline((40, 40, 472, 472), width=14),
        ),
        "Right-arrow sign": (
            right_arrow_fill(),
            right_arrow_outline(inset=True, width=10),
            right_arrow_outline(inset=False, width=14),
        ),
    }

    filenames = {
        "Rounded rectangle": (
            "RR_Modular_RoundedRect_Fill.png",
            "RR_Modular_RoundedRect_InnerOutline.png",
            "RR_Modular_RoundedRect_OuterOutline.png",
        ),
        "Circle / capsule": (
            "RR_Modular_Circle_Fill.png",
            "RR_Modular_Circle_InnerOutline.png",
            "RR_Modular_Circle_OuterOutline.png",
        ),
        "Right-arrow sign": (
            "RR_Modular_RightArrow_Fill.png",
            "RR_Modular_RightArrow_InnerOutline.png",
            "RR_Modular_RightArrow_OuterOutline.png",
        ),
    }

    for family_name, images in families.items():
        for filename, image in zip(filenames[family_name], images, strict=True):
            _validate_and_save(filename, image)

    _make_contact_sheet(families)
    _make_resize_test(families)

    print(f"Generated {sum(len(images) for images in families.values())} sprites in {OUTPUT_DIR}")
    print(f"Generated previews in {PREVIEW_DIR}")


if __name__ == "__main__":
    main()
