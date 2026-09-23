"""독립 트랙 모델(v2), v1 호환 변환 및 영상 레이어 구간 계산."""
import copy
import math


def clips(project, kind=None, enabled=True):
    for track in project['tracks']:
        if enabled and not track.get('enabled', True):
            continue
        if kind is None or track['kind'] == kind:
            for clip in track['clips']:
                yield track, clip


def duration(project):
    return max([project.get('minimumDuration', 1)] +
               [c['start'] + c['duration'] for _, c in clips(project, enabled=False)])


def migrate(project, project_file=None):
    if project.get('schemaVersion') == 2:
        return copy.deepcopy(project)
    from project import timed_shots, resolve, duration as old_duration
    p = copy.deepcopy(project)
    tracks = [dict(id='track-video-1', name='영상 1', kind='video', enabled=True, clips=[]),
              dict(id='track-voice-1', name='나레이션', kind='audio', enabled=True, clips=[]),
              dict(id='track-text-1', name='자막', kind='subtitle', enabled=True, clips=[]),
              dict(id='track-music-1', name='배경음악', kind='audio', enabled=True, clips=[])]
    for i, (start, shot) in enumerate(timed_shots(project)):
        tracks[0]['clips'].append(dict(id=f'video-{i}', title=shot['title'], start=start,
            duration=shot['duration'], sourceIn=project['video']['offset']+shot['sourceIn'],
            path=project['video']['path'], volume=1, padEnd=project['video'].get('padEnd', True),
            credits=shot.get('credits', False)))
        for j, cue in enumerate(shot['cues']):
            length=cue['duration']
            if project_file:
                try:
                    import soundfile as sf
                    length=sf.info(str(resolve(project_file, cue['path']))).duration
                except (OSError, RuntimeError):
                    pass
            tracks[1]['clips'].append(dict(id=f'voice-{i}-{j}', title=cue['text'],
                start=start+cue['start'], duration=length, sourceIn=0, path=cue['path'],
                volume=cue.get('volume', 1), role='narration', loop=False))
            tracks[2]['clips'].append(dict(id=f'text-{i}-{j}', title=cue['text'],
                text=cue['text'], start=start+cue['start'], duration=cue['duration']))
    tracks[3]['clips'].append(dict(id='music-bed', title='배경음악', start=0,
        duration=old_duration(project), sourceIn=0, path=project['music']['path'],
        volume=1, role='music', loop=True))
    p.pop('shots', None)
    p.update(schemaVersion=2, tracks=tracks, minimumDuration=1)
    return p


def validate(project, project_file, files=False):
    from project import resolve
    def number(value, name, lo=0, hi=3600):
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not lo <= value <= hi:
            raise ValueError(f'{name}: {lo}~{hi} 범위의 숫자가 필요합니다.')
    tracks=project.get('tracks', [])
    if len(tracks)>32:
        raise ValueError('트랙은 최대 32개까지 지원합니다.')
    number(project.get('minimumDuration', 1), '최소 영상 길이', .04)
    number(project['narrationVolume'], '나레이션 볼륨', 0, 2)
    number(project['video']['volume'], '영상 원음 볼륨', 0, 1)
    number(project['music']['volume'], '음악 볼륨', 0, 1)
    number(project['music']['creditsMultiplier'], '크레딧 배율', 0, 4)
    for key in ('fadeIn', 'fadeOut', 'creditsRamp'):
        number(project['music'].get(key, 2), key)
    if project['export']['height'] not in (720, 1080) or project['export']['fps'] not in (24, 30, 60):
        raise ValueError('지원하지 않는 출력 해상도/프레임입니다.')
    asset_ids=set()
    for asset in project.get('assets', []):
        if asset.get('kind') not in ('video', 'audio') or not isinstance(asset.get('path'), str) or not asset['path']:
            raise ValueError('영상·음성 에셋에는 종류와 원본 경로가 필요합니다.')
        if not isinstance(asset.get('name'), str) or not asset['name']:
            raise ValueError('에셋 이름이 필요합니다.')
        if asset.get('id') in asset_ids:
            raise ValueError('에셋 ID가 중복됩니다.')
        asset_ids.add(asset.get('id'))
        if 'duration' in asset:
            number(asset['duration'], '에셋 원본 길이', 0, 86400)
        if 'defaultVolume' in asset:
            number(asset['defaultVolume'], '에셋 기본 음량', 0, 2)
        if asset['kind']=='audio' and asset.get('role','narration') not in ('narration','music','effect'):
            raise ValueError('음원 종류가 올바르지 않습니다.')
        if 'loop' in asset and not isinstance(asset['loop'], bool):
            raise ValueError('에셋 반복 설정이 올바르지 않습니다.')
    ids=set(); count=0
    for track in tracks:
        if track['id'] in ids or track['kind'] not in ('video','audio','subtitle'):
            raise ValueError('중복 트랙 ID 또는 지원하지 않는 트랙 종류입니다.')
        ids.add(track['id']); end=0
        for c in sorted(track['clips'], key=lambda c:c['start']):
            count+=1
            if c['id'] in ids:
                raise ValueError('클립 ID가 중복됩니다.')
            ids.add(c['id'])
            number(c['start'], '클립 시작'); number(c['duration'], '클립 길이', .04)
            if track['kind']=='video' and c['start']<end-1e-6:
                raise ValueError(f"{track['name']}: 같은 영상 트랙의 클립이 겹칩니다.")
            end=max(end,c['start']+c['duration'])
            if track['kind']!='subtitle':
                number(c['sourceIn'], '원본 시작', 0, 86400)
                number(c.get('volume', 1), '클립 음량', 0, 2)
                if not isinstance(c.get('path'),str) or not c['path']:
                    raise ValueError('클립 원본 경로가 필요합니다.')
                if files and not resolve(project_file,c['path']).is_file():
                    raise ValueError('원본 파일을 찾을 수 없습니다: '+c['path'])
            elif not isinstance(c.get('text'),str):
                raise ValueError('자막 텍스트가 필요합니다.')
    if count>1000 or duration(project)>3600:
        raise ValueError('최대 1000클립·1시간까지 지원합니다.')


def video_segments(project):
    """위 트랙 우선. 겹침 경계를 분할하여 미리보기와 동일하게 출력한다."""
    videos=list(clips(project,'video'))
    points=sorted({0,duration(project)} | {v for _,c in videos for v in (c['start'],c['start']+c['duration'])})
    result=[]
    for start,end in zip(points,points[1:]):
        active=next((c for _,c in videos if c['start']<=start+1e-7 and c['start']+c['duration']>start+1e-7),None)
        if result and result[-1][2] is active:
            result[-1]=(result[-1][0],end,active)
        else:
            result.append((start,end,active))
    return result
