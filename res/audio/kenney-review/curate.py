from pathlib import Path
import sys, json, shutil, hashlib
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / '.tooling'))
import soundfile as sf
import numpy as np

# Original filenames are preserved in the manifest; candidates are not final approvals.
GROUPS = [
('01','채집','나무 채집','도끼·목재 계열 비교. 반복 작업의 기본음 후보.', [('rpg-audio','chop'),('impact-sounds','impactWood_medium_000'),('impact-sounds','impactWood_light_002')]),
('02','채집','돌 채집','채광 전용 파일을 우선 비교.', [('impact-sounds','impactMining_000'),('impact-sounds','impactMining_002'),('impact-sounds','impactMining_004')]),
('03','채집','자원 소진','전용 파괴음은 없어 강한 목재·파열음을 대체 후보로 제시.', [('impact-sounds','impactWood_heavy_000'),('sci-fi-sounds','explosionCrunch_000')]),
('04','보상','골드 획득·정산','실제 동전 계열 우선. 자주 재생할 때 길이 확인.', [('rpg-audio','handleCoins'),('rpg-audio','handleCoins2'),('casino-audio','chips-collide-1')]),
('05','건설','건물 배치','판자·목재 접촉으로 배치 확정 표현.', [('impact-sounds','impactPlank_medium_000'),('impact-sounds','impactWood_medium_002')]),
('06','건설','공사·수리 기여','가벼운 목재/금속 타격을 망치질 대용으로 비교.', [('impact-sounds','impactWood_light_000'),('impact-sounds','impactMetal_light_001'),('impact-sounds','impactPlank_medium_002')]),
('07','건설','완공·병력 생산 완료','짧은 확인음과 상승 계열 비교.', [('interface-sounds','confirmation_001'),('interface-sounds','confirmation_002'),('digital-audio','powerUp1')]),
('08','건설','연구·타워 강화','상승형 효과와 확인음을 비교. 전자음의 세계관 적합성 확인.', [('digital-audio','powerUp3'),('interface-sounds','confirmation_004'),('interface-sounds','maximize_001')]),
('09','전투','커서·근접 적중','펀치·부드러운 충격 계열을 일반 적중음으로 비교.', [('impact-sounds','impactPunch_medium_000'),('impact-sounds','impactPunch_heavy_000'),('impact-sounds','impactSoft_medium_000')]),
('10','전투','무기 휘두르기','칼날 동작 계열. 실제 적중음과 별도 역할.', [('rpg-audio','knifeSlice'),('rpg-audio','knifeSlice2'),('rpg-audio','drawKnife1')]),
('11','전투','화살 발사·명중 대체','확인한 팩에 명시적 활 발사음이 없어 대체 후보만 제시. 전용 활 소리는 후속 보강 권장.', [('rpg-audio','cloth1'),('rpg-audio','knifeSlice2'),('impact-sounds','impactWood_light_001')]),
('12','전투','마법 발사','SF/디지털 효과를 마법으로 전용할지 비교.', [('digital-audio','zap1'),('sci-fi-sounds','laserSmall_000'),('digital-audio','phaserUp1')]),
('13','전투','마법 명중·발화','전용 판타지 화염이 아닌 파열·추진 불꽃 계열 대체 후보.', [('sci-fi-sounds','explosionCrunch_002'),('sci-fi-sounds','thrusterFire_000'),('digital-audio','zap2')]),
('14','전투','적 처치·슬라임','슬라임 효과와 일반적인 충격음을 비교. 종별 전용 사망음은 아님.', [('sci-fi-sounds','slime_000'),('sci-fi-sounds','slime_001'),('impact-sounds','impactSoft_heavy_000')]),
('15','전투','치명타·강한 타격','일반 적중보다 강한 후보. 아군 손실 경고와 중복 사용은 피하기.', [('impact-sounds','impactPunch_heavy_002'),('impact-sounds','impactMetal_heavy_000')]),
('16','전투','건물 피해·붕괴','무거운 목재 충격과 파열음을 비교. 완전한 붕괴 녹음은 아님.', [('impact-sounds','impactWood_heavy_002'),('sci-fi-sounds','explosionCrunch_004')]),
('17','탐험','상자 잠금·개봉','잠금쇠와 문 여는 소리를 상자 연출에 전용하는 후보.', [('rpg-audio','metalLatch'),('rpg-audio','doorOpen_1'),('rpg-audio','creak1')]),
('18','탐험','유물 획득·유적 활성화','유리·벨 계열과 상승음을 비교.', [('interface-sounds','glass_001'),('impact-sounds','impactBell_heavy_000'),('digital-audio','powerUp7')]),
('19','UI','확인·선택·병력 명령','일상 클릭은 이 그룹에서 하나로 통일하는 안.', [('interface-sounds','click_001'),('interface-sounds','select_002'),('ui-audio','click1')]),
('20','UI','취소·닫기','메뉴 닫기·공사 취소 공통 후보.', [('interface-sounds','back_001'),('interface-sounds','close_001'),('ui-audio','switch2')]),
('21','UI','메뉴 열기·탭 전환','건설 메뉴·관리창 열기 및 설정 탭 전환.', [('interface-sounds','open_001'),('interface-sounds','switch_001'),('rpg-audio','bookOpen')]),
('22','UI','골드 부족·행동 불가','거절 시점에 한 번만 재생.', [('interface-sounds','error_001'),('interface-sounds','error_004'),('digital-audio','lowDown')]),
('23','하루·환생','황혼·웨이브 경고','벨·낮은 다중 음 후보. 전용 뿔피리 소리는 포함되지 않음.', [('impact-sounds','impactBell_heavy_003'),('interface-sounds','bong_001'),('digital-audio','lowThreeTone')]),
('24','하루·환생','하루 시작·완료 징글','피치카토/8비트/스틸 음색 비교. 곡의 감정은 직접 청음 후 배정.', [('music-jingles','jingles_PIZZI00'),('music-jingles','jingles_NES00'),('music-jingles','jingles_STEEL00')]),
('25','하루·환생','승리·패배 징글 후보','번호별 선율 비교용. 승리/패배 배정은 청음 후 결정.', [('music-jingles','jingles_PIZZI07'),('music-jingles','jingles_PIZZI10'),('music-jingles','jingles_PIZZI15')]),
('26','하루·환생','환생·재출발','공간 전환·역장 계열 대체 후보. SF 느낌이 강하면 제외.', [('digital-audio','phaseJump1'),('digital-audio','phaserDown1'),('sci-fi-sounds','forceField_000')]),
]

def main():
    packs=json.loads((ROOT/'sources.json').read_text(encoding='utf-8'))
    library=[]; index={}; errors=[]
    for pack in packs:
        for rel in pack['files']:
            p=ROOT/rel
            if p.stem.lower()=='preview': continue
            try:
                samples,rate=sf.read(p, always_2d=True)
                peak=float(np.max(np.abs(samples)))
                rms=float(np.sqrt(np.mean(samples*samples)))
                row=dict(pack=pack['pack'],name=p.name,path=rel,seconds=round(len(samples)/rate,3),sampleRate=rate,channels=samples.shape[1],peakDb=round(20*np.log10(max(peak,1e-10)),2),rmsDb=round(20*np.log10(max(rms,1e-10)),2),source=pack['page'])
                library.append(row); index[(pack['pack'],p.stem)]=row
            except Exception as e: errors.append(dict(path=rel,error=str(e)))
    groups=[]
    for num,category,title,note,choices in GROUPS:
        candidates=[]
        for i,key in enumerate(choices):
            row=dict(index[key]); cid=f'{num}{chr(65+i)}'
            folder=ROOT/'candidates'/f'{num}_{title.replace("·","_").replace("/","_")}'
            folder.mkdir(parents=True,exist_ok=True)
            ogg=folder/f'{cid}_{row["name"]}'
            shutil.copy2(ROOT/row['path'],ogg)
            samples,rate=sf.read(ogg,always_2d=True)
            peak=np.max(np.abs(samples)); gain=min(10**(12/20),10**(-6/20)/max(peak,1e-10))
            wav=ogg.with_suffix('.wav'); sf.write(wav,samples*gain,rate,subtype='PCM_16')
            row.update(id=cid,ogg=ogg.relative_to(ROOT).as_posix(),wav=wav.relative_to(ROOT).as_posix(),gainDb=round(20*np.log10(gain),2),first=i==0)
            candidates.append(row)
        groups.append(dict(id=num,category=category,title=title,note=note,candidates=candidates))
    data=dict(groups=groups,library=library,packs=packs,errors=errors)
    (ROOT/'catalog.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    template=(ROOT/'review-template.html').read_text(encoding='utf-8')
    (ROOT/'index.html').write_text(template.replace('__CATALOG__',json.dumps(data,ensure_ascii=False).replace('</','<\\/')),encoding='utf-8')
    lines=['# Kenney 효과음 청음 후보','','## 작업 목록','- [x] 공식 오디오 8개 팩 다운로드 및 원본/라이선스 보존','- [x] 개별 오디오 디코딩·길이·음량 검사','- [x] 용도별 후보 분리 및 WAV 청음본 생성','- [x] 선택/비교/내보내기 청음 페이지','- [ ] 사용자 청음 및 최종 선택','','## 기준','청각적 청취 평가를 했다고 주장하지 않습니다. Kenney 원본 파일의 의미·팩 분류와 디코딩/길이/음량 검사로 추린 1차 후보입니다. A는 용도상 먼저 비교할 후보이며 최종 추천 확정은 아닙니다.','원본 OGG는 바이트 그대로 복사했습니다. WAV는 피크 -6 dBFS 목표, 증폭 최대 +12 dB로 음량만 조절한 16-bit 청음본입니다. 컷/피치/속도 변경, 합성 없음. 체감 음량을 완전히 같게 만든 것은 아닙니다.','명시적 활/화염/붕괴/뿔피리 전용음이 부족한 용도는 대체 후보로 표시했습니다. 승패 징글의 감정 배정은 청음 후 결정합니다.','','## 사용법','index.html을 브라우저로 열거나 로컬 서버의 청음 페이지에서 재생합니다. 그룹별 A→B→C 비교, 3회 반복, 원본/WAV 전환, 선택, 메모를 지원합니다.','선택은 같은 브라우저/주소에 자동 보관됩니다. 「선택 JSON 저장」으로 확정 기록을 파일로 보관하세요. 서버나 게임에는 자동 반영되지 않습니다.','전체 원본 탭에서 미선정 소리도 재생하고 추가 후보로 표시할 수 있습니다.','','## 후보 목록','']
    for g in groups:
        lines += [f'### {g["id"]}. {g["title"]}',g['note'],'']
        for c in g['candidates']: lines += [f'- **{c["id"]}**: `{c["pack"]}/{c["name"]}` · {c["seconds"]}초 · [WAV]({c["wav"]})']
        lines += ['']
    lines += ['## 출처 및 라이선스','각 ZIP에 동봉된 License.txt를 originals 아래 보존했습니다. 모두 CC0 표기를 확인했습니다.','']
    lines += [f'- [{p["pack"]}]({p["page"]})' for p in packs]
    lines += ['','## 변경 전후','이전: 효과음 적용 지점 목록만 존재. 이후: 원본 팩, 분리 후보, 청음용 WAV, 선택 페이지 및 출처 기록 추가. 게임 코드/Unity Assets/씬은 변경하지 않았습니다.']
    (ROOT/'README.md').write_text('\n'.join(lines),encoding='utf-8')
    print(json.dumps(dict(packs=len(packs),individualAudio=len(library),groups=len(groups),candidates=sum(len(g['candidates']) for g in groups),decodeErrors=errors),ensure_ascii=False))

if __name__=='__main__': main()
