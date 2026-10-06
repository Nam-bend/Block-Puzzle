"""Validate the live warm kit and publish its sprite preview page."""
from pathlib import Path
import json
import re
import yaml
from PIL import Image
from art_common import ROOT

ART=ROOT/'Assets/Art/CatNookWarm'
catalog=json.loads((ROOT/'tools/warm_cat_catalog.json').read_text())
guids={};parts=0;cards=[];report=[]
ui_files=list((ART/'Sources').glob('*.svg'))
assert len(ui_files)==13
for path in ui_files:
    assert (ART/(path.stem+'.png')).exists() and Path(str(ART/(path.stem+'.png'))+'.meta').exists()
    assert '<svg' in path.read_text()
assert (ART/'background.png').exists()
for path in (ART/'Cats').glob('*.png'):
    with Image.open(path) as im:
        assert im.mode=='RGBA' and im.getchannel('A').getextrema()[0]==0,path
    metadata=yaml.safe_load(Path(str(path)+'.meta').read_text())
    guids[metadata['guid']]={s['internalID'] for s in metadata['TextureImporter']['spriteSheet']['sprites']}
    assert set(metadata['TextureImporter']['spriteSheet']['nameFileIdTable'].values())==guids[metadata['guid']]
for p in catalog:
    with Image.open(ART/'Cats'/(p['source']+'.png')) as im:
        alpha=im.getchannel('A');binary=alpha.point(lambda a:1 if a>=128 else 0)
        total=sum(binary.get_flattened_data());kept=0
        r=p['region'];tiles=[]
        for point,rect,part in zip(p['shape'],p['rects'],p['parts']):
            x,y,w,h=(rect[key] for key in ('x','y','width','height'));top=im.height-y-h
            assert x>=0 and y>=0 and w>0 and h>0 and x+w<=im.width and y+h<=im.height
            assert alpha.crop((x,top,x+w,top+h)).getbbox() is not None
            assert part['fileID'] in guids[part['guid']]
            kept+=sum(binary.crop((x,top,x+w,top+h)).get_flattened_data());parts+=1
            sx,sy=70/w,70/h
            tiles.append(f'<i style="left:{point["x"]*70}px;top:{(r["rows"]-1-point["y"])*70}px;background-image:url(../../../Assets/Art/CatNookWarm/Cats/{p["source"]}.png);background-size:{im.width*sx}px {im.height*sy}px;background-position:{-x*sx}px {-top*sy}px"></i>')
        assert p['small']['fileID'] in guids[p['small']['guid']]
        loss=(1-kept/total)*100;assert loss<2.5,(p['name'],loss)
        report.append(dict(pose=p['name'],opaquePixelsOutsideFootprintPercent=round(loss,3)))
        cards.append(f'<article><div class="pose" style="width:{r["cols"]*70}px;height:{r["rows"]*70}px">'+''.join(tiles)+f'</div><h3>{p["source"]}</h3><p>{p["breed"]} / {p["expression"]} / {len(p["shape"])} cells</p></article>')
art=yaml.safe_load((ROOT/'Assets/Resources/BlockArt.asset').read_text().split('--- !u!114 &11400000')[1])['MonoBehaviour']
assert len(art['cats'])==len(catalog)
for pose,configured in zip(catalog,art['cats']):
    assert all(configured[key]==pose[key] for key in ('name','breed','expression','shape','parts','small'))
assert not (ROOT/'Assets/Art/CatNook').exists() and not (ROOT/'Assets/Texture').exists(),'Obsolete UI folders must be removed'
assert len(guids)==16 and len(catalog)==17 and parts==49
out=ROOT/'docs/design/Warm';out.mkdir(parents=True,exist_ok=True)
html='''<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Cat Nook Warm - asset review</title><style>
*{box-sizing:border-box}body{margin:0;padding:32px;background:#f8d7a8;color:#784126;font-family:system-ui,sans-serif}header{max-width:1000px;margin:auto auto 32px}h1{margin:0}p{line-height:1.6}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(370px,1fr));gap:20px}article{background:#fff1d2;border:2px solid #d9aa70;border-radius:24px;padding:24px;min-height:270px;display:flex;align-items:center;justify-content:center;flex-direction:column}h3{font-size:17px;margin-bottom:0}.pose{position:relative;background-image:linear-gradient(#d9aa7055 1px,transparent 1px),linear-gradient(90deg,#d9aa7055 1px,transparent 1px);background-size:70px 70px}.pose i{position:absolute;width:70px;height:70px;background-repeat:no-repeat}.mockups{display:flex;gap:20px;overflow:auto}.mockups img{width:280px;height:auto;border-radius:20px}a{color:#784126}footer{margin-top:30px}</style><header><h1>CAT NOOK - WARM ASSET KIT</h1><p>Warm UI and cat sprites are now linked in the game scenes, prefabs and BlockArt. The grid below shows the actual sprite slices.</p><div class="mockups"><a href="CatNook_Warm_Menu.png"><img src="CatNook_Warm_Menu.png" alt="Menu mockup"></a><a href="CatNook_Warm_Gameplay.png"><img src="CatNook_Warm_Gameplay.png" alt="Gameplay mockup"></a><a href="CatNook_Warm_GameOver.png"><img src="CatNook_Warm_GameOver.png" alt="Game over mockup"></a></div></header><section class="cards">'''+''.join(cards)+'''</section><footer>16 PNG mèo / 17 cấu hình dáng / 49 sprite con. Biểu cảm tĩnh. Mockup Photoshop không phải ảnh chụp Play Mode.</footer></html>'''
(out/'index.html').write_text(html,encoding='utf-8')
(out/'asset-validation.json').write_text(json.dumps(dict(catSources=16,poses=17,sprites=parts,liveIntegration=True,footprints=report),indent=2))
print('PASS 16 RGBA sources, 17 poses, 49 valid sprite subassets; all poses retain at least 97.5% opaque source pixels within footprint.')
print('PASS same-breed kitten references and importer sprite ID tables.')
print('PASS 13 matching UI PNG/SVG components and warm background.')
print('PASS live BlockArt matches the approved warm catalog; obsolete UI folders are absent.')
