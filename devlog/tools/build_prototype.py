"""Unity 실행 없이 프로토타입의 현재 asmdef 참조를 맞춰 C# 빌드한다."""
from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
UNITY=ROOT/'impls/unity/Afterlife'

def main():
    project=UNITY/'Afterlife.Prototype.ModuleValidation.csproj'
    if not project.exists():
        raise SystemExit('기존 Afterlife.Prototype.ModuleValidation.csproj가 필요합니다.')
    runtime=UNITY/'Assets/_AfterlifePrototype/Runtime'
    assembly=json.loads((runtime/'Afterlife.Prototype.asmdef').read_text(encoding='utf-8-sig'))
    tree=ET.parse(project)
    root=tree.getroot()
    refs=set(assembly['references'])|{'Moonstone'}
    for group in root.findall('ItemGroup'):
        for item in list(group):
            if item.tag=='Compile' or (item.tag=='Reference' and item.get('Include') in refs):
                group.remove(item)
    group=ET.SubElement(root,'ItemGroup')
    for source in sorted(runtime.rglob('*.cs')):
        ET.SubElement(group,'Compile',{'Include':source.relative_to(UNITY).as_posix()})
    for name in assembly['references']:
        path=Path('Temp/bin/Devlog')/(name+'.dll') if name.startswith('Afterlife.Devlog.') else Path('Library/ScriptAssemblies')/(name+'.dll')
        if not (UNITY/path).exists():
            raise SystemExit(f'참조 DLL 없음: {path}. Devlog는 build_modules.py를 먼저 실행합니다.')
        item=ET.SubElement(group,'Reference',{'Include':name})
        ET.SubElement(item,'HintPath').text=path.as_posix()
    ET.indent(tree)
    tree.write(project,encoding='utf-8',xml_declaration=False)
    result=subprocess.run(['dotnet','build',project.name,'--no-restore','--nologo','-v','minimal'],cwd=UNITY,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    log=result.stdout.decode('utf-8',errors='replace').replace('\r\n','\n')
    (ROOT/'devlog/records/build-logs/Afterlife.Prototype.txt').write_text(log,encoding='utf-8')
    print('Afterlife.Prototype', 'PASS' if result.returncode==0 else 'FAIL')
    if result.returncode:
        print(log.encode('ascii','backslashreplace').decode())
        raise SystemExit(result.returncode)

if __name__=='__main__':
    main()
