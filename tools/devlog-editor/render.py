"""실시간 재생 상태와 독립적인 샘플 단위 오디오 합성·FFmpeg 내보내기."""
import json
import math
import re
import subprocess
import threading
import shutil
from pathlib import Path
import numpy as np
import soundfile as sf
import imageio_ffmpeg
from project import resolve, duration, timed_shots, validate

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
RATE = 48000
FLAGS = getattr(subprocess, 'CREATE_NO_WINDOW', 0)


def publish_output(source, output):
    """캐시와 프로젝트가 다른 드라이브여도 출력. 기존 파일은 덮어쓰지 않는다."""
    output=Path(output);source=Path(source)
    output.parent.mkdir(parents=True,exist_ok=True)
    # 배타적 생성으로 렌더 도중 같은 이름의 파일이 생긴 경우도 보존한다.
    destination=output.open('xb')
    try:
        with destination,source.open('rb') as stream:
            shutil.copyfileobj(stream,destination,1024*1024)
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    source.unlink()

def run(args, **kwargs):
    return subprocess.run([FFMPEG, '-nostdin', '-hide_banner', *args], creationflags=FLAGS, **kwargs)

def probe(path):
    r = run(['-i', str(path)], capture_output=True, text=True, encoding='utf-8', errors='replace')
    m = re.search(r'Duration: (\d+):(\d+):([\d.]+)', r.stderr)
    if not m or 'Video:' not in r.stderr:
        raise ValueError('재생 가능한 영상 파일이 아닙니다.')
    seconds = int(m[1]) * 3600 + int(m[2]) * 60 + float(m[3])
    return dict(duration=seconds, audio='Audio:' in r.stderr)

def decode(path, limit=None, start=0):
    args = ['-v', 'error']
    if start: args += ['-ss', str(start)]
    args += ['-i', str(path)]
    if limit is not None: args += ['-t', str(limit)]
    r = run(args + ['-vn', '-f', 'f32le', '-ar', str(RATE), '-ac', '2', 'pipe:1'], capture_output=True)
    if r.returncode:
        raise ValueError(r.stderr.decode('utf-8', 'replace')[-1600:])
    return np.frombuffer(r.stdout, np.float32).reshape(-1, 2).copy()

def smooth(x):
    x = np.clip(x, 0, 1)
    return x*x*(3-2*x)

def mix(project, project_file, out, cancel=None):
    if project.get('schemaVersion') == 2:
        from render_tracks import mix as track_mix
        return track_mix(project, project_file, out, cancel)
    validate(project, project_file, files=True)
    total = duration(project)
    n = round(total * RATE)
    audio = np.zeros((n, 2), np.float32)
    diagnostics = []
    for start, shot in timed_shots(project):
        if cancel and cancel.is_set(): raise InterruptedError('취소했습니다.')
        for cue in shot['cues']:
            clip = decode(resolve(project_file, cue['path']), shot['duration'] + .1)
            available = round((shot['duration'] - cue['start']) * RATE)
            if len(clip) > available + RATE * .02:
                raise ValueError(f"{shot['title']}: 음성이 샷 끝을 넘습니다. 샷 길이나 문장 시작을 조절하세요.")
            first = round((start + cue['start']) * RATE)
            count = min(len(clip), available, n-first)
            audio[first:first+count] += clip[:count] * project['narrationVolume'] * cue.get('volume', 1)
        if project['video']['volume'] > 0:
            if probe(resolve(project_file, project['video']['path']))['audio']:
                original = decode(resolve(project_file, project['video']['path']), shot['duration'], project['video']['offset']+shot['sourceIn'])
                first = round(start*RATE); count = min(len(original), round(shot['duration']*RATE), n-first)
                audio[first:first+count] += original[:count] * project['video']['volume']
    music = decode(resolve(project_file, project['music']['path']))
    if len(music)<100: raise ValueError('배경음악 파일에 재생 가능한 오디오가 없습니다.')
    # 영상이 길어져도 4초 크로스페이드로 음악을 이어 붙인다.
    overlap = min(4*RATE, len(music)//4)
    step = len(music)-overlap
    bed = np.zeros((n, 2), np.float32)
    for first in range(0, n, step):
        part = music.copy()
        if first: part[:overlap] *= np.linspace(0,1,overlap,dtype=np.float32)[:,None]
        if first+len(part)<n: part[-overlap:] *= np.linspace(1,0,overlap,dtype=np.float32)[:,None]
        count = min(len(part), n-first)
        bed[first:first+count] += part[:count]
    t = np.arange(n, dtype=np.float32)/RATE
    settings = project['music']
    gain = np.full(n, settings['volume'], np.float32)
    for start, shot in timed_shots(project):
        if shot.get('credits'):
            mask = (t>=start)&(t<start+shot['duration'])
            gain[mask] *= 1+(settings['creditsMultiplier']-1)*smooth((t[mask]-start)/max(.01, settings.get('creditsRamp',2)))
    gain *= np.clip(t/max(.01,settings.get('fadeIn',2)),0,1)*np.clip((total-t)/max(.01,settings.get('fadeOut',3)),0,1)
    audio += bed*gain[:,None]
    peak = float(np.max(np.abs(audio)))
    if peak > .98:
        audio *= .98/peak
        diagnostics.append(f'합산 피크 보호: 전체 볼륨 {20*math.log10(.98/peak):.1f} dB 보정')
    sf.write(out, audio, RATE, subtype='PCM_16')
    return dict(duration=total, samples=n, peakBeforeLimit=peak, notes=diagnostics)

def ass_time(t):
    cs = round(t*100); h,cs=divmod(cs,360000); m,cs=divmod(cs,6000); s,cs=divmod(cs,100)
    return f'{h}:{m:02}:{s:02}.{cs:02}'

def subtitles(project, out, width=1920, height=1080):
    header = '''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Malgun Gothic,38,&H00FFFFFF,&H00FFFFFF,&H00201810,&H80201810,0,0,0,0,100,100,0,0,3,10,0,2,150,150,80,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
    header=header.replace('PlayResX: 1920',f'PlayResX: {width}').replace('PlayResY: 1080',f'PlayResY: {height}')
    lines=[]
    for start,shot in timed_shots(project):
        for cue in shot['cues']:
            text=cue['text'].replace('\\','／').replace('{','（').replace('}','）').replace('\n',r'\N')
            lines.append(f"Dialogue: 0,{ass_time(start+cue['start'])},{ass_time(start+cue['start']+cue['duration'])},Default,,0,0,0,,{text}")
    Path(out).write_text(header+'\n'.join(lines),encoding='utf-8-sig')

def export(project, project_file, output, work, progress=lambda x:None, cancel=None):
    if project.get('schemaVersion') == 2:
        from render_tracks import export as track_export
        return track_export(project, project_file, output, work, progress, cancel)
    validate(project,project_file,files=True)
    output=Path(output); work=Path(work);work.mkdir(parents=True,exist_ok=True)
    if output.exists(): raise ValueError('같은 이름의 파일이 있습니다. 다른 이름으로 내보내세요.')
    src=resolve(project_file,project['video']['path']);info=probe(src)
    for _,shot in timed_shots(project):
        end=project['video']['offset']+shot['sourceIn']+shot['duration']
        if project['video']['offset']+shot['sourceIn']>=info['duration']:
            raise ValueError(f"{shot['title']}: 원본 시작이 영상 길이를 넘습니다.")
        if end>info['duration']+.05 and not project['video']['padEnd']:
            raise ValueError('녹화본이 짧습니다. 끝 프레임 유지 옵션을 켜거나 샷을 줄여주세요.')
    progress(dict(phase='오디오 합성',percent=0))
    report=mix(project,project_file,work/'mix.wav',cancel)
    (work/'audio-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if cancel and cancel.is_set(): raise InterruptedError('취소했습니다.')
    # 각 샷을 순차 인코딩해 긴 단일 split 그래프의 프레임 버퍼 메모리를 피한다.
    from aspect_ratio import dimensions
    total=duration(project); width,height=dimensions(project['export']);fps=project['export']['fps']
    parts=[]
    segments=[]; cursor=0
    for start,shot in sorted(timed_shots(project),key=lambda pair:pair[0]):
        if start>cursor: segments.append((cursor,dict(duration=start-cursor, gap=True)))
        segments.append((start,shot)); cursor=start+shot['duration']
    for i,(start,shot) in enumerate(segments):
        if cancel and cancel.is_set(): raise InterruptedError('취소했습니다.')
        frames=round((start+shot['duration'])*fps)-round(start*fps)
        if frames<=0: continue
        part=work/f'part-{i:03}.mp4';parts.append(part)
        if shot.get('gap'):
            execute(['-v','error','-y','-f','lavfi','-i',f'color=c=black:s={width}x{height}:r={fps}',
                     '-frames:v',str(frames),'-an','-c:v','libx264','-preset','fast','-crf','18',
                     '-pix_fmt','yuv420p','-threads','4',str(part)],cancel,work/'ffmpeg.log')
            continue
        filters=f'scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2,setsar=1,fps={fps},tpad=stop_mode=clone:stop_duration={shot["duration"]}'
        args=['-v','error','-y','-ss',str(project['video']['offset']+shot['sourceIn']),'-i',str(src),'-frames:v',str(frames),'-an','-vf',filters,'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4',str(part)]
        execute(args,cancel,work/'ffmpeg.log')
        progress(dict(phase=f'영상 인코딩 {i+1}/{len(project["shots"])}',percent=round((start+shot['duration'])/total*90,1)))
    (work/'concat.txt').write_text('\n'.join(f"file '{p.name}'" for p in parts),encoding='utf-8')
    args=['-v','error','-y','-f','concat','-safe','0','-i','concat.txt','-i','mix.wav','-map','0:v:0','-map','1:a:0','-t',str(total)]
    if project['export']['subtitles']:
        subtitles(project,work/'subtitles.ass',width,height)
        args+=['-vf','subtitles=subtitles.ass','-c:v','libx264','-preset','fast','-crf','18','-threads','4']
    else: args+=['-c:v','copy']
    args+=['-c:a','aac','-b:a','192k','-ar',str(RATE),'-movflags','+faststart','export.mp4']
    execute(args,cancel,work/'ffmpeg.log',work)
    output.parent.mkdir(parents=True,exist_ok=True)
    publish_output(work/'export.mp4',output)
    progress(dict(phase='완료',percent=100,output=str(output)))
    return report

def execute(args,cancel,log,cwd=None):
    with open(log,'ab') as stream:
        process=subprocess.Popen([FFMPEG,'-nostdin','-hide_banner',*args],stdout=stream,stderr=stream,cwd=cwd,creationflags=FLAGS)
        while True:
            try:
                code=process.wait(timeout=.25);break
            except subprocess.TimeoutExpired:
                if cancel and cancel.is_set():
                    process.terminate();process.wait();raise InterruptedError('취소했습니다.')
    if code: raise RuntimeError(Path(log).read_text(encoding='utf-8',errors='replace')[-2200:])
