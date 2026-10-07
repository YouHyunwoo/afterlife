"""imagegen 원본 → 검수용 48px/32px. 검수 승인 해시와 일치할 때만 게임에 복사."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'res/art-source/ui/upgrades-v1'
GAME = ROOT / 'impls/unity/Afterlife/Assets/Prototype/Resources/UI/Upgrades'
PALETTE = ['172433', '34495e', '267b78', '7ad6c4', 'bf9145', 'edcc79', 'f3ead1', '79659b']
IDS = [f'{prefix}{i}' for prefix in ('R', 'A') for i in range(1, 11)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def shrink(source):
    image = Image.open(source).convert('RGBA')
    alpha = image.getchannel('A')
    if alpha.getextrema()[0] != 0:
        raise ValueError(f'{source.name}: 실제 투명 배경이 없어 재생성 필요')
    bounds = alpha.point(lambda value: 255 if value >= 128 else 0).getbbox()
    if bounds is None:
        raise ValueError(f'{source.name}: 빈 이미지')
    image = image.crop(bounds)
    # 40px 실루엣 + 사방 4px 여백. 원본 알파를 면적 평균 후 이진화한다.
    factor = 40 / max(image.size)
    image = image.resize((max(1, round(image.width * factor)), max(1, round(image.height * factor))), Image.Resampling.BOX)
    alpha = image.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
    palette = Image.new('P', (1, 1))
    colors = [tuple(bytes.fromhex(value)) for value in PALETTE]
    palette.putpalette([component for color in colors for component in color] + list(colors[0]) * (256 - len(colors)))
    image = image.convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE).convert('RGBA')
    image.putalpha(alpha)
    result = Image.new('RGBA', (48, 48))
    result.alpha_composite(image, ((48 - image.width) // 2, (48 - image.height) // 2))
    return result


def metrics(image):
    pixels = [image.getpixel((x, y)) for y in range(image.height) for x in range(image.width)]
    colors = {p[:3] for p in pixels if p[3]}
    bounds = image.getchannel('A').getbbox()
    passed = image.size == (48, 48) and set(p[3] for p in pixels) == {0, 255} and len(colors) <= 8 and bounds and min(bounds[0], bounds[1], 48-bounds[2], 48-bounds[3]) >= 4
    return {'size': list(image.size), 'colors': len(colors), 'alpha': sorted(set(p[3] for p in pixels)), 'bounds': bounds, 'technical_pass': bool(passed)}


def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--apply', action='store_true'); args = parser.parse_args()
    candidates = ART / 'candidates'; candidates.mkdir(parents=True, exist_ok=True)
    report = {}
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
    board = Image.new('RGB', (1000, 1040), '#172433'); draw = ImageDraw.Draw(board)
    for index, id in enumerate(IDS):
        source = ART / 'source' / f'{id}.png'
        image = shrink(source); path = candidates / f'{id}.png'; image.save(path)
        report[id] = dict(metrics(image), sha256=digest(path), source_sha256=digest(source))
        x, y = (index % 5)*200, (index // 5)*260
        draw.text((x+12, y+8), id+' · 48px / 32px', font=font, fill='#edcc79')
        board.paste(image.resize((144,144), Image.Resampling.NEAREST), (x+28,y+35), image.resize((144,144), Image.Resampling.NEAREST))
        board.paste(image, (x+30,y+194), image)
        small = image.resize((32,32), Image.Resampling.NEAREST)
        board.paste(small, (x+116,y+202), small)
    board.save(ART/'review-sheet.png')
    (ART/'technical-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    if args.apply:
        approvals = json.loads((ART/'visual-approvals.json').read_text(encoding='utf-8'))
        for id in IDS:
            approval = approvals.get(id,{})
            if not report[id]['technical_pass'] or approval.get('status') != 'pass' or approval.get('sha256') != report[id]['sha256']:
                raise ValueError(f'{id}: 기술 검사/시각 검사 승인 또는 승인 해시 누락. 적용 중단.')
        GAME.mkdir(parents=True,exist_ok=True)
        for id in IDS:
            shutil.copy2(candidates/f'{id}.png', GAME/f'{id}.png')
        print('검수 해시 일치: 20개 적용')
    else:
        print('검수 후보 생성. 시각 승인 전에는 게임 에셋에 복사하지 않음.')


if __name__ == '__main__':
    main()
