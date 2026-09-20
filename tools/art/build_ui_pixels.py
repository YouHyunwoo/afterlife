"""UI 아이콘을 실제 저해상도 PNG로 변환. 원본은 res/art-source/ui에 보존한다."""
from pathlib import Path
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / 'impls/unity/Afterlife/Assets/_AfterlifePrototype/Resources/UI'
SOURCE = ROOT / 'res/art-source/ui'
SOURCE.mkdir(parents=True, exist_ok=True)


def pixel_sprite(image, size):
    # 면적 평균으로 생성 이미지의 미세 잡음을 줄이고, 팔레트/알파를 고정한다.
    small = image.convert('RGBA').resize(size, Image.Resampling.BOX)
    alpha = small.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    small.putalpha(alpha)
    small = small.quantize(colors=16, method=Image.Quantize.FASTOCTREE,
                           dither=Image.Dither.NONE).convert('RGBA')
    small.putalpha(alpha)
    return small


def source(name):
    backup = SOURCE / name
    if not backup.exists():
        shutil.copy2(UI / name, backup)
    return Image.open(backup).convert('RGBA')


def main():
    atlas = source('Icons16.png')
    result = Image.new('RGBA', (160, 160))
    for row in range(4):
        for col in range(4):
            box = tuple(round(v * atlas.width / 4) for v in (col, row, col + 1, row + 1))
            result.paste(pixel_sprite(atlas.crop(box), (40, 40)), (col * 40, row * 40))
    # 기존 마지막 셀과 Sprite 사각형 안에 새 imagegen 잠금 아이콘을 배치한다.
    lock = Image.open(SOURCE / 'CameraLock-generated.png').convert('RGBA')
    mask = lock.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    lock = lock.crop(mask.getbbox())
    lock = pixel_sprite(lock, (25, 30))
    result.paste((0, 0, 0, 0), (120, 120, 160, 160))
    result.paste(lock, (129, 123))
    result.save(UI / 'Icons16.png')
    lock.save(UI / 'CameraLock.png')
    for name, size in [('LootIcon16.png', 48), ('DaySun-v1.png', 32), ('NightMoon-v1.png', 32)]:
        pixel_sprite(source(name), (size, size)).save(UI / name)
    preview = Image.new('RGBA', (640, 800), '#202b3b')
    preview.alpha_composite(result.resize((640, 640), Image.Resampling.NEAREST))
    for index, name in enumerate(['LootIcon16.png', 'DaySun-v1.png', 'NightMoon-v1.png', 'CameraLock.png']):
        icon = Image.open(UI / name).convert('RGBA')
        icon.thumbnail((128, 128), Image.Resampling.NEAREST)
        factor = min(128 // icon.width, 128 // icon.height)
        icon = icon.resize((icon.width * factor, icon.height * factor), Image.Resampling.NEAREST)
        preview.alpha_composite(icon, (index * 160 + (160-icon.width)//2, 640+(160-icon.height)//2))
    preview.convert('RGB').save(SOURCE / 'pixel-icons-preview.png')


if __name__ == '__main__':
    main()
