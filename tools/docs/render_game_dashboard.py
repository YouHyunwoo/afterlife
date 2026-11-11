#!/usr/bin/env python3
"""Build an offline, read-only dashboard from project Markdown. Python 3.10+."""
from __future__ import annotations

import argparse
import base64
import hashlib
import html
import json
import os
from pathlib import Path
import re
import sys
from datetime import datetime
from urllib.parse import quote, unquote, urlsplit

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent / 'vendor'))
import mistune

STAGES = [
    ('01', '아이디어 구체화', ['01-game-analysis.md']),
    ('02', '타깃과 핵심 경험', ['02-experience-brief.md']),
    ('03', '플레이 반복 구조', ['03-core-loop.md']),
    ('04', '규칙·행동·경험', ['04-mda-risks.md']),
    ('05', '시스템과 성장', ['05-system-map.md']),
    ('06', '플레이 흐름과 UX', ['06-player-flow.md']),
    ('07', '프로토타입과 검증', ['07-validation.md']),
    ('08', 'MVP 범위와 계획', ['08-gdd-mvp.md']),
    ('mvp', 'MVP 개발', ['milestones.md']),
    ('iterations', '테스트·수정 반복', []),
    ('release', '출시 준비', ['release-checklist.md']),
    ('operations', '출시·운영', ['operations.md']),
]
EXTRA = [('records', '진행·결정 기록', ['status.md', 'decisions.md'])]


def fields(text):
    """Only parse top-level metadata bullets, not arbitrary prose or code."""
    result = {}
    fence = None
    for line in text.splitlines():
        match = re.match(r'^\s*(`{3,}|~{3,})', line)
        if match:
            mark = match[1]
            if fence is None:
                fence = mark
            elif mark[0] == fence[0] and len(mark) >= len(fence):
                fence = None
            continue
        if fence:
            continue
        match = re.match(r'^- ([^:\n]+):\s*(.*)$', line)
        if match:
            result.setdefault(match[1].strip(), match[2].strip())
    return result


def slug(text):
    text = html.unescape(re.sub(r'<[^>]*>', '', text)).strip().lower()
    return re.sub(r'\s+', '-', re.sub(r'[^\w\s-]', '', text)) or 'section'


def doc_id(path):
    return 'doc-' + hashlib.sha256(path.as_posix().encode()).hexdigest()[:16]


def local_path(root, name):
    path = (root / name).resolve()
    if not path.is_relative_to(root):
        raise ValueError(f'문서 경로는 문서 루트 안에 있어야 합니다: {name}')
    return path


class Renderer(mistune.HTMLRenderer):
    def __init__(self, source, root, output, documents):
        super().__init__(escape=True)
        self.source, self.root, self.output = source, root, output
        self.documents = documents
        self.headings = {}

    def safe_url(self, url):
        # Decode before checking: entity/percent/control-character obfuscation.
        check = re.sub(r'[\x00-\x20\x7f]', '', html.unescape(unquote(url)))
        try:
            parts = urlsplit(check)
            if parts.scheme and parts.scheme.lower() not in ('https', 'http', 'mailto', 'tel'):
                return '#blocked-link'
            if check.startswith(('//', '\\')):
                return '#blocked-link'
        except ValueError:
            return '#blocked-link'
        return html.escape(url, quote=True)

    def resolve_url(self, url, image=False):
        if self.safe_url(url) == '#blocked-link':
            return '#blocked-link'
        parts = urlsplit(url)
        if parts.scheme or parts.netloc:
            return url
        target = (self.source.parent / unquote(parts.path)).resolve() if parts.path else self.source
        if not image and target in self.documents:
            suffix = '--' + slug(unquote(parts.fragment)) if parts.fragment else ''
            return '#' + self.documents[target] + suffix
        relative = quote(Path(os.path.relpath(target, self.output.parent)).as_posix(), safe='/')
        return relative + ('?' + parts.query if parts.query else '') + ('#' + parts.fragment if parts.fragment else '')

    def link(self, text, url, title=None):
        return super().link(text, self.resolve_url(url), title)

    def image(self, text, url, title=None):
        return super().image(text, self.resolve_url(url, image=True), title)

    def heading(self, text, level, **attrs):
        base = slug(text)
        count = self.headings.get(base, 0)
        self.headings[base] = count + 1
        anchor = base + (f'-{count}' if count else '')
        tag = min(level + 2, 6)
        return f'<h{tag} id="{self.documents[self.source]}--{html.escape(anchor)}">{text}</h{tag}>\n'


def render_markdown(text, path, root, output, documents):
    renderer = Renderer(path, root, output, documents)
    md = mistune.create_markdown(renderer=renderer, plugins=['table', 'task_lists', 'strikethrough'])
    return md(text)


def build(root, output, index=None):
    root, output = root.resolve(), output.resolve()
    if not root.is_dir():
        raise ValueError(f'문서 폴더가 없습니다: {root}')
    if output.suffix.lower() != '.html':
        raise ValueError('출력은 .html 파일이어야 합니다.')
    if output.exists() and '<!-- game-design-to-release:generated -->' not in output.read_text(encoding='utf-8'):
        raise ValueError('기존 HTML은 이 도구의 생성물이 아닙니다. 다른 --output 경로를 지정하세요.')
    mapping = {}
    if index:
        mapping = json.loads(index.read_text(encoding='utf-8-sig'))
        allowed = {s[0] for s in STAGES + EXTRA}
        if not isinstance(mapping, dict) or set(mapping) - allowed:
            raise ValueError('문서 색인은 알려진 단계 ID를 키로 갖는 JSON 객체여야 합니다.')
        if any(not isinstance(v, list) or any(not isinstance(p, str) for p in v) for v in mapping.values()):
            raise ValueError('각 단계에는 Markdown 상대 경로 배열을 지정하세요.')
    documents, contents, stage_paths, warnings = {}, {}, {}, []
    for sid, _, defaults in STAGES + EXTRA:
        names = mapping.get(sid, defaults)
        if sid == 'iterations' and sid not in mapping:
            names = [p.relative_to(root).as_posix() for p in sorted((root / 'experiments').glob('*/*.md'))]
        paths = []
        for name in names:
            path = local_path(root, name)
            if path.suffix.lower() != '.md':
                raise ValueError(f'Markdown 파일만 등록하세요: {name}')
            if not path.is_file():
                if sid in mapping:
                    warnings.append(f'등록 문서 누락: {name}')
                    paths.append(path)
                continue
            if path in documents:
                raise ValueError(f'문서가 중복 등록되었습니다: {name}')
            documents[path] = doc_id(path.relative_to(root))
            contents[path] = path.read_text(encoding='utf-8-sig')
            paths.append(path)
        stage_paths[sid] = paths
    status_path = root / 'status.md'
    meta = fields(status_path.read_text(encoding='utf-8-sig')) if status_path.is_file() else {}
    current = meta.get('현재 단계 ID', '')
    if not current:
        match = re.search(r'(?<!\d)([1-8])\s*(?:단계|주차)', meta.get('현재 모드와 단계', ''))
        current = match[1].zfill(2) if match else ''
    if current and current not in {s[0] for s in STAGES}:
        warnings.append(f'알 수 없는 현재 단계 ID: {current}')
        current = ''
    nav, sections = [], []
    for sid, title, _ in STAGES + EXTRA:
        paths = stage_paths[sid]
        existing = [p for p in paths if p in contents]
        state = meta.get(f'{sid} 상태') or (meta.get('단계 상태') if sid == current else '')
        state = state or ('기록 있음 · 상태 미지정' if existing else '미시작')
        summary = meta.get(f'{sid} 요약', '')
        if not summary and existing:
            summary = fields(contents[existing[0]]).get('핵심 요약', '')
        summary = summary or ('문서를 펼쳐 세부 내용과 근거를 확인하세요.' if existing else '아직 작성된 산출물이 없습니다.')
        label = sid if sid.isdigit() else {'mvp': 'MVP', 'iterations': '반복', 'release': '출시', 'operations': '운영', 'records': '기록'}[sid]
        nav.append(f'<a href="#stage-{sid}"' + (' aria-current="step"' if sid == current else '') + f'><span>{label}</span>{title}</a>')
        cards, last_round = [], None
        for path in paths:
            relative = path.relative_to(root).as_posix()
            if sid == 'iterations' and path.parent != last_round:
                cards.append(f'<h3 class="round">회차 · {html.escape(path.parent.name)}</h3>')
                last_round = path.parent
            if path not in contents:
                cards.append(f'<p class="warning">등록 문서를 찾을 수 없습니다: {html.escape(relative)}</p>')
                continue
            text = contents[path]
            heading = re.search(r'^#\s+(.+)$', text, re.M)
            doc_title = heading[1] if heading else path.stem
            link = quote(Path(os.path.relpath(path, output.parent)).as_posix(), safe='/')
            body = render_markdown(text, path, root, output, documents)
            cards.append(f'<details class="artifact" id="{documents[path]}"><summary>{html.escape(doc_title)}<small>{html.escape(relative)}</small></summary><div class="markdown"><a class="source" href="{link}">Markdown 원본 열기 ↗</a>{body}</div></details>')
        sections.append(f'<details class="stage" id="stage-{sid}"' + (' open' if sid == current else '') + f'><summary><span class="stage-number">{label}</span><span>{title}</span><span class="badge">{html.escape(state)}</span></summary><div class="stage-body"><p class="stage-description">{html.escape(summary)}</p>{"".join(cards)}</div></details>')
    template = (Path(__file__).resolve().parent / 'assets/dashboard.html').read_text(encoding='utf-8')
    script = re.search(r'<script>(.*?)</script>', template, re.S)[1]
    digest = base64.b64encode(hashlib.sha256(script.encode()).digest()).decode()
    values = {
        'TITLE': html.escape(meta.get('프로젝트/한 줄 소개') or '새 게임 프로젝트'),
        'CURRENT': html.escape(meta.get('현재 모드와 단계') or next((s[1] for s in STAGES if s[0] == current), '시작 위치 미정')),
        'NEXT': html.escape(meta.get('다음 행동') or '아이디어와 현재 작업을 확인하고 첫 단계를 시작합니다.'),
        'UPDATED': datetime.now().astimezone().strftime('%Y-%m-%d %H:%M %z'),
        'NAV': ''.join(nav), 'SECTIONS': ''.join(sections), 'SCRIPT_HASH': digest,
        'WARNINGS': ''.join(f'<p class="warning">{html.escape(w)}</p>' for w in warnings),
    }
    result = re.sub(r'\{\{([A-Z_]+)\}\}', lambda m: values[m[1]], template)
    output.parent.mkdir(parents=True, exist_ok=True)
    # Preserve the last complete HTML if reading/rendering fails; replace atomically.
    import tempfile
    temp = None
    try:
        with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', dir=output.parent, suffix='.tmp', delete=False) as handle:
            temp = Path(handle.name)
            handle.write(result)
        temp.replace(output)
    finally:
        if temp and temp.exists():
            temp.unlink()
    return warnings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--docs-dir', type=Path, default=Path('docs/game-process'))
    parser.add_argument('--output', type=Path, help='Default: <docs-dir>/index.html')
    parser.add_argument('--index', type=Path, help='Default: <docs-dir>/dashboard-index.json when present')
    args = parser.parse_args()
    index = args.index or args.docs_dir / 'dashboard-index.json'
    try:
        if args.index and not args.index.is_file():
            raise ValueError(f'지정한 문서 색인이 없습니다: {args.index}')
        warnings = build(args.docs_dir, args.output or args.docs_dir / 'index.html', index if index.exists() else None)
    except (OSError, ValueError) as error:
        parser.exit(1, f'생성 실패: {error}\n')
    for warning in warnings:
        print(f'주의: {warning}', file=sys.stderr)
    print(f'생성 완료: {(args.output or args.docs_dir / "index.html").resolve()}')


if __name__ == '__main__':
    main()
