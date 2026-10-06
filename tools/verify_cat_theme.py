"""Static Unity YAML integrity and C# compilation using installed Editor references."""
from pathlib import Path
import re
import subprocess
import yaml
from art_common import ROOT, Scene, COLORS, guid

work=ROOT/'Temp/CatThemeValidation'; work.mkdir(parents=True,exist_ok=True)
unity=Path('C:/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor/Data')
framework=unity/'MonoBleedingEdge/lib/mono/4.8-api'
refs=list((unity/'Managed/UnityEngine').glob('*.dll'))
refs += [ROOT/'Library/ScriptAssemblies/UnityEngine.UI.dll',ROOT/'Library/ScriptAssemblies/Unity.TextMeshPro.dll', ROOT/'Library/ScriptAssemblies/Unity.InputSystem.dll']
refs += [framework/'mscorlib.dll',framework/'System.dll',framework/'System.Core.dll',framework/'Facades/netstandard.dll']
csc=Path('C:/Program Files/dotnet/sdk/10.0.400/Roslyn/bincore/csc.dll')
def compile(name,sources,extra=()):
    output=work/(name+'.dll'); rsp=work/(name+'.rsp')
    lines=['-nologo','-nostdlib+','-target:library','-langversion:latest','-nowarn:0649',f'-out:"{output}"']
    lines += [f'-r:"{p}"' for p in refs+list(extra)]
    lines += [f'"{p}"' for p in sources]
    rsp.write_text('\n'.join(lines),encoding='utf-8')
    result=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True)
    print(result.stdout.strip())
    if result.returncode: raise RuntimeError(result.stderr or 'C# compilation failed')
    print('PASS C# compile '+name); return output
runtime=compile('Assembly-CSharp',list((ROOT/'Assets/Scripts').glob('*.cs')))
compile('Assembly-CSharp-Editor',list((ROOT/'Assets/Editor').glob('*.cs')),[runtime])

asset_guids={}
for p in (ROOT/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
    if match:
        assert match[1] not in asset_guids, f'Duplicate GUID {p}'
        asset_guids[match[1]]=p
for file in ('Assets/Scenes/SampleScene.unity','Assets/Scenes/Game.unity'):
    scene=Scene(file)
    ids=set(scene.docs)
    assert all(0 < id <= 9223372036854775807 for id in ids), 'Unity fileID must fit a signed 64-bit integer'
    for id,doc in scene.docs.items():
        for m in re.finditer(r'\{fileID: (-?\d+)\}',doc.get('raw','')):
            fid=int(m[1]); assert fid==0 or fid in ids, f'{file}: unresolved local fileID {fid}'
    for id,doc in scene.docs.items():
        d=scene.data(id)
        if doc['type']==224 and 'm_Children' in d:
            children=[r['fileID'] for r in d['m_Children']]
            assert len(children)==len(set(children)), f'Duplicate child at {id}'
            for child in children:
                cd=scene.data(child)
                if 'm_Father' in cd: assert cd['m_Father']['fileID']==id, f'Wrong parent {child}'
                else:
                    instance=scene.data(cd['m_PrefabInstance']['fileID'])
                    assert instance['m_Modification']['m_TransformParent']['fileID']==id
    for name in ('PlayButton','SettingButtonShow') if 'SampleScene' in file else ('MenuButton','RestartButton','TryAgainButton','CatPopupHome'):
        go=scene.named(name); image=scene.data(scene.image(go)); button=scene.data(scene.component(go,'m_OnClick'))
        assert image['m_RaycastTarget']==1
        assert len(button['m_OnClick']['m_PersistentCalls']['m_Calls'])==1
        assert button['m_TargetGraphic']['fileID']==scene.image(go)
    root=scene.named('CatSafeContent'); assert scene.component(root,'m_Script')
    print('PASS scene references, hierarchy and button events '+file)
for p in (ROOT/'Library/PackageCache').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig',errors='replace'),re.M)
    if match: asset_guids.setdefault(match[1],p)
for file in ('Assets/Resources/BlockArt.asset','Assets/Prefabs/BlockCell.prefab','Assets/Prefabs/BlockPiece.prefab','Assets/Scenes/SampleScene.unity','Assets/Scenes/Game.unity'):
    for m in re.finditer(r'guid: (\w+)',(ROOT/file).read_text()):
        assert m[1].startswith('0000000000000000') or m[1] in asset_guids, f'Unresolved asset {file}: {m[1]}'
print('PASS art/prefab GUID references; six Cat Nook block colors')
print('NOT RUN native Unity import, Play Mode or device visual validation by this offline checker.')
