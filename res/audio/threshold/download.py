from pathlib import Path
import urllib.request,re,json,hashlib,concurrent.futures
HERE=Path(__file__).resolve().parent
SOURCES=[
 dict(key='afterlife',title='Decoherence',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2022/03/sb_decoherence.mp3',slug='decoherence'),
 dict(key='night',title='Intervention (No Piano Melody)',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2019/01/sb_intervention_nomelody.mp3',slug='intervention'),
 dict(key='water',title='Water bubbles loop',author='GoldenTricycle',page='https://freesound.org/people/GoldenTricycle/sounds/521392/',license='CC0-1.0',license_url='https://creativecommons.org/publicdomain/zero/1.0/'),
 dict(key='whoosh',title='Backwards Whoosh',author='SlavicMagic',page='https://freesound.org/people/SlavicMagic/sounds/446010/',license='CC0-1.0',license_url='https://creativecommons.org/publicdomain/zero/1.0/'),
 dict(key='title',title='Undertow',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2019/12/sb_undertow.mp3',slug='undertow'),
 dict(key='playing',title='Childhood',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2019/01/sb_childhood.mp3',slug='childhood'),
 dict(key='over',title='Unraveling',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2026/05/Unraveling.mp3',slug='unraveling'),
 dict(key='clear',title='Ascension',url='https://www.scottbuckley.com.au/library/wp-content/uploads/2019/02/sb_ascension.mp3',slug='ascension')]
def fetch(url):
 return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36','Accept':'*/*','Referer':'https://www.scottbuckley.com.au/library/'}),timeout=90).read()
def get(item):
 if 'slug' in item:
  item.update(author='Scott Buckley',page='https://www.scottbuckley.com.au/library/'+item.pop('slug')+'/',license='CC-BY-4.0',license_url='https://creativecommons.org/licenses/by/4.0/')
 try:
  html=fetch(item['page']); (HERE/(item['key']+'-source.html')).write_bytes(html)
 except Exception:
  if 'url' not in item: raise
  html=b''
  item['page_note']='License verified in web research; direct page download unavailable'
 if 'url' not in item:
  urls=sorted(set(re.findall(r'https[^\s\"<>]+hq\.mp3',html.decode())))
  if not urls: raise RuntimeError('No public preview URL '+item['page'])
  item['url']=urls[0]; item['format']='Freesound public HQ MP3 preview, not original WAV'
 filename=item['key']+'_source.mp3'; path=HERE/filename
 if not path.exists(): path.write_bytes(fetch(item['url']))
 item.update(file=filename,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),verified='2026-09-26')
 print(item['key'],path.stat().st_size,flush=True)
 return item
if __name__=='__main__':
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool: results=list(pool.map(get,SOURCES))
 (HERE/'sources.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
