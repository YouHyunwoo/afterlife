from pathlib import Path
import xml.etree.ElementTree as E,json,copy
r=Path(__file__).resolve().parents[2]
asms={}
for f in (r/'Assets').rglob('*.asmdef'):
 try: d=json.loads(f.read_text(encoding='utf-8-sig'));asms[d['name']]=(f,d)
 except:pass
template=E.parse(r/'Afterlife.Devlog.Day01.Editor.csproj').getroot()
allrefs=template.findall('ItemGroup/Reference')
def generate(name):
 if name in done or name not in asms:return
 done.add(name);f,d=asms[name];tree=copy.deepcopy(template)
 for group in tree.findall('ItemGroup'):
  for e in list(group):
   if e.tag in ['Compile','ProjectReference','Reference']:group.remove(e)
 for prop in tree.findall('PropertyGroup'):
  for e in prop:
   if e.tag=='AssemblyName':e.text=name
   if e.tag=='RootNamespace':e.text=d.get('rootNamespace',name)
   if e.tag=='OutputPath':e.text='Temp/SceneFeatureValidation/bin/'
 ig=E.SubElement(tree,'ItemGroup')
 for ref in allrefs:
  if not d.get("noEngineReferences") or ref.attrib.get("Include","").split(",")[0].startswith(("System", "Microsoft", "mscorlib", "netstandard")):ig.append(copy.deepcopy(ref))
 for src in f.parent.rglob('*.cs'):
  if any(x!=f and x.parent in src.parents for x in f.parent.rglob('*.asmdef')):continue
  E.SubElement(ig,'Compile',Include=str(src.relative_to(r)))
 for ref in d.get('references',[]):
  if ref in asms:generate(ref);E.SubElement(ig,'ProjectReference',Include=ref+'.FeatureValidation.csproj')
  elif (r/'Library/ScriptAssemblies'/(ref+'.dll')).exists():
   elem=E.SubElement(ig,'Reference',Include=ref);E.SubElement(elem,'HintPath').text='Library/ScriptAssemblies/'+ref+'.dll'
 props=E.SubElement(tree,'PropertyGroup');E.SubElement(props,'SkipResolvePackageAssets').text='true';E.SubElement(props,'GenerateDependencyFile').text='false'
 E.ElementTree(tree).write(r/(name+'.FeatureValidation.csproj'),encoding='utf-8')
done=set()
for name in ['Afterlife.SceneFeatures.Input','Afterlife.Devlog.SceneFeatureMigration.Editor','Afterlife.SceneFeatures.Editor','Afterlife.Devlog.Day01.Editor','Afterlife.Devlog.Day02.Editor','Afterlife.Prototype.Editor']:generate(name)
print('Generated',len(done),'validation projects')

if __name__ == '__main__':
 import subprocess
 for target in ['Afterlife.Devlog.SceneFeatureMigration.Editor','Afterlife.Prototype.Editor','Afterlife.SceneFeatures.Input']:
  subprocess.run(['dotnet','build',str(r/(target+'.FeatureValidation.csproj')),'--no-restore','-v:q'],cwd=r,check=True)
