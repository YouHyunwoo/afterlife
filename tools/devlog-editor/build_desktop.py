"""개발 환경에서 실행: Python/FFmpeg/플러그인을 포함한 Windows 폴더형 배포본."""
import subprocess
import argparse
import sys
from pathlib import Path
from prepare_tts_assets import prepare

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
OUTPUT=ROOT/'.tooling/devlog-editor/dist'


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--distpath',type=Path,default=OUTPUT)
    output=parser.parse_args().distpath.resolve()
    tts_assets=prepare()
    args=[sys.executable,'-m','PyInstaller','--noconfirm','--windowed','--onedir',
          '--name','AfterlifeEditor','--distpath',str(output),
          '--workpath',str(ROOT/'.tooling/devlog-editor/build'),
          '--specpath',str(ROOT/'.tooling/devlog-editor'),
          '--collect-all','imageio_ffmpeg','--collect-all','webview',
          '--collect-all','onnxruntime',
          '--hidden-import','render_tracks','--hidden-import','tts']
    for name in ('ui.html','editor.css','editor.js','timeline_edit.js','live_audio.js','desktop_ui.js','plugin_host.js','smoke_ui.js','plugins'):
        args+=['--add-data',str(HERE/name)+(':'+name if name=='plugins' else ':.')]
    args+=['--add-data',str(tts_assets)+':tts_assets',
           '--add-data',str(HERE/'third_party/CODE-LICENSE.txt')+':third_party']
    args+=[str(HERE/'desktop.py')]
    subprocess.run(args,check=True)
    print(output/'AfterlifeEditor/AfterlifeEditor.exe')


if __name__=='__main__':main()
