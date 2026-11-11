from pathlib import Path
import concurrent.futures, hashlib, html, json, re, urllib.request, zipfile

ROOT = Path(__file__).resolve().parent
PACKS = ['impact-sounds', 'rpg-audio', 'interface-sounds', 'music-jingles', 'casino-audio', 'ui-audio', 'digital-audio', 'sci-fi-sounds']

def fetch(pack):
    page = 'https://kenney.nl/assets/' + pack
    body = urllib.request.urlopen(page, timeout=60).read().decode()
    links = re.findall(r'href=[\'"]([^\'"]+\.zip)[\'"]', body)
    url = next(html.unescape(x) for x in links if x.startswith('https://kenney.nl/'))
    archive = ROOT / 'archives' / (pack + '.zip')
    archive.parent.mkdir(parents=True, exist_ok=True)
    if not archive.exists():
        archive.write_bytes(urllib.request.urlopen(url, timeout=120).read())
    dest = ROOT / 'originals' / pack
    dest.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive) as z:
        for item in z.infolist():
            if not (dest / item.filename).resolve().is_relative_to(dest.resolve()):
                raise ValueError('Unsafe ZIP member')
        z.extractall(dest)
    files = list(dest.rglob('*'))
    audio = [str(p.relative_to(ROOT)).replace('\\', '/') for p in files if p.suffix.lower() in ('.ogg', '.wav', '.mp3')]
    result = dict(pack=pack, page=page, download=url, license='CC0', sha256=hashlib.sha256(archive.read_bytes()).hexdigest(), files=audio)
    print(pack, len(audio), 'audio files', flush=True)
    return result

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        results = list(pool.map(fetch, PACKS))
    (ROOT / 'sources.json').write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding='utf-8')
