"""asmdef 의존 순서로 Devlog 모듈을 dotnet build --no-restore 한다. Unity 실행 없음."""
from pathlib import Path
import json, subprocess, xml.etree.ElementTree as ET
from datetime import datetime
ROOT=Path(__file__).resolve().parents[2]
UNITY=ROOT/'impls/unity/Afterlife'
BASE=UNITY/'Afterlife.Devlog.Lighting.ModuleValidation.csproj'

def main():
    assemblies={}
    for path in (UNITY/'Assets/Devlog').rglob('*.asmdef'):
        data=json.loads(path.read_text(encoding='utf-8-sig'))
        assemblies[data['name']]=(path,data)
    done=set(); ordered=[]
    def visit(name,visiting):
        if name in done:return
        if name in visiting:raise RuntimeError('Assembly cycle: '+name)
        for ref in assemblies[name][1].get('references',[]):
            if ref in assemblies:visit(ref,visiting|{name})
        done.add(name);ordered.append(name)
    for name in sorted(assemblies):visit(name,set())
    template=BASE.read_text(encoding='utf-8-sig')
    records=[]
    for name in ordered:
        path,data=assemblies[name]
        tree=ET.fromstring(template)
        for node in tree.iter('AssemblyName'):node.text=name
        for node in tree.iter('BaseIntermediateOutputPath'):node.text='Temp/obj/Devlog/'+name+'/'
        for node in tree.iter('IntermediateOutputPath'):node.text='Temp/obj/Devlog/'+name+'/'
        for group in tree.findall('ItemGroup'):
            for item in list(group):
                if item.tag=='Compile' or (item.tag=='Reference' and item.attrib.get('Include','').startswith('Afterlife.Devlog.')):group.remove(item)
        group=ET.SubElement(tree,'ItemGroup')
        for source in sorted(path.parent.rglob('*.cs')):
            owner=max((p for p,d in assemblies.values() if p.parent==source.parent or p.parent in source.parents),key=lambda p:len(p.parts))
            if owner==path:ET.SubElement(group,'Compile',{'Include':str(source.relative_to(UNITY))})
        for ref in data.get('references',[]):
            if ref in assemblies:
                item=ET.SubElement(group,'Reference',{'Include':ref});ET.SubElement(item,'HintPath').text='Temp/bin/Devlog/'+ref+'.dll'
        if data.get('noEngineReferences'):
            for group_ in tree.findall('ItemGroup'):
                for item in list(group_):
                    if item.tag=='Reference' and item.attrib.get('Include','').startswith(('Unity','Assembly-CSharp')):group_.remove(item)
        output=UNITY/(name+'.ModuleValidation.csproj')
        ET.indent(tree);ET.ElementTree(tree).write(output,encoding='utf-8',xml_declaration=False)
        result=subprocess.run(['dotnet','build',output.name,'--no-restore','--nologo','-v','minimal'],cwd=UNITY,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
        log=result.stdout.decode('utf-8',errors='replace')
        logpath=ROOT/'devlog/records/build-logs'/(name+'.txt');logpath.parent.mkdir(parents=True,exist_ok=True);logpath.write_text(log,encoding='utf-8')
        records.append({'assembly':name,'exit_code':result.returncode,'log':str(logpath.relative_to(ROOT))})
        print(name, 'PASS' if result.returncode==0 else 'FAIL',flush=True)
        if result.returncode:
            print(log.encode('ascii','backslashreplace').decode(),flush=True)
            break
    report={'timestamp':datetime.now().isoformat(),'method':'dotnet build --no-restore','unity_play_tests':False,'assemblies':records,'all_passed':len(records)==len(ordered) and all(x['exit_code']==0 for x in records)}
    (ROOT/'devlog/records/build-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if not report['all_passed']:raise SystemExit(1)
if __name__=='__main__':main()
