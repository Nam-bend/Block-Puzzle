from pathlib import Path
import json
import subprocess

root=Path(__file__).resolve().parents[1]
work=root/'Temp/CatRenderChecks'; work.mkdir(parents=True,exist_ok=True)
dotnet=Path('C:/Program Files/dotnet')
framework=sorted((dotnet/'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net10.0'))[-1]
runtime=sorted((dotnet/'shared/Microsoft.NETCore.App').glob('10.*'))[-1].name
output=work/'CatRenderChecks.dll'
lines=['-nologo','-nostdlib+','-target:exe','-nowarn:0649',f'-out:"{output}"']
lines.extend(f'-r:"{p}"' for p in framework.glob('*.dll'))
lines.extend(f'"{root/p}"' for p in ('Assets/Scripts/BlockBoard.cs','Assets/Scripts/BlockCatPose.cs','Assets/Scripts/BlockGridView.cs','tools/CatRenderChecks.cs'))
rsp=work/'checks.rsp'; rsp.write_text('\n'.join(lines))
subprocess.run(['dotnet',str(dotnet/'sdk/10.0.400/Roslyn/bincore/csc.dll'),'@'+str(rsp)],check=True)
output.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net10.0','framework':{'name':'Microsoft.NETCore.App','version':runtime}}}))
subprocess.run(['dotnet',str(output),str(root/'tools/warm_cat_catalog.json')],check=True)
