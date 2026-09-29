"""프로젝트 화면 비율과 인코더용 짝수 해상도."""

RATIOS = {'16:9': (16, 9), '9:16': (9, 16), '1:1': (1, 1),
          '4:3': (4, 3), '21:9': (21, 9)}


def dimensions(export):
    ratio = export.get('aspectRatio', '16:9')
    if ratio not in RATIOS:
        raise ValueError('지원하지 않는 화면 비율입니다.')
    short = export['height']
    numerator, denominator = RATIOS[ratio]
    if numerator >= denominator:
        return 2 * round(short * numerator / denominator / 2), short
    return short, 2 * round(short * denominator / numerator / 2)
