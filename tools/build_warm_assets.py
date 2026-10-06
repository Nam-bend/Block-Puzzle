"""Build the active warm UI kit and its sprite metadata."""
from pathlib import Path
import json
import yaml
from PIL import Image
from art_common import Vector, ROOT, guid, Dumper
BREEDS=('calico','black','white','gray','ginger','tuxedo','siamese')
POSES=[
    (f'single_{breed}',breed,[(0,0)],expression)
    for breed,expression in zip(BREEDS,('curious','grumpy','surprised','sleepy','smug','playful','curious'))
]+[
    ('bar2_tuxedo','tuxedo',[(0,0),(1,0)],'sleeping'),
    ('bar3_ginger','ginger',[(0,0),(1,0),(2,0)],'content'),
    ('bar4_gray','gray',[(0,0),(1,0),(2,0),(3,0)],'relaxed'),
    ('bar5_siamese','siamese',[(0,0),(1,0),(2,0),(3,0),(4,0)],'sleepy'),
    ('square_calico','calico',[(0,0),(1,0),(0,1),(1,1)],'sleeping'),
    ('square_calico','calico',[(x,y) for y in range(3) for x in range(3)],'sleeping'),
    ('corner3_ginger','ginger',[(0,0),(0,1),(1,0)],'grooming'),
    ('corner4_calico','calico',[(0,0),(0,1),(0,2),(1,0)],'surprised'),
    ('tee_tuxedo','tuxedo',[(0,0),(1,0),(2,0),(1,1)],'playful'),
    ('zigzag_black','black',[(0,0),(1,0),(1,1),(2,1)],'mischievous'),
]

FOLDER = 'Assets/Art/CatNookWarm'
ART = ROOT / FOLDER
INK = '#784126'
CREAM = '#FFF1D2'
COLORS = dict(blue='#8ACDDD', mint='#A9CE79', gold='#FFD166', coral='#FFA25A', violet='#C7ACE0', pink='#F6A6BB')

def texture(name, border=(0,0,0,0)):
    data=yaml.safe_load((ROOT/'tools/texture_importer_template.yaml').read_text())
    data['guid']=guid(f'{FOLDER}/{name}.png'); t=data['TextureImporter']
    t.update(internalIDToNameTable=[],spriteMode=1,spriteMeshType=0,spriteBorder=dict(zip(('x','y','z','w'),border)),spritePivot={'x':.5,'y':.5},alphaIsTransparency=1,maxTextureSize=2048)
    t['spriteSheet']=dict(serializedVersion=2,sprites=[],outline=[],customData='',physicsShape=[],bones=[],spriteID='',internalID=0,vertices=[],indices=[],edges=[],weights=[],secondaryTextures=[],nameFileIdTable={})
    for platform in t['platformSettings']: platform.update(textureCompression=0,maxTextureSize=2048)
    Path(str(ART/(name+'.png'))+'.meta').write_text(yaml.dump(data,Dumper=Dumper,sort_keys=False))

class WarmVector(Vector):
    def save(self,border=(0,0,0,0)):
        # Rasterize our editable vector components; do not alter generated cat images.
        self.im.resize((self.w,self.h),Image.Resampling.LANCZOS).save(ART/(self.name+'.png'))
        (ART/'Sources'/(self.name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}">\n'+'\n'.join(self.svg)+'\n</svg>\n')
        texture(self.name,border)

def ui():
    (ART/'Sources').mkdir(parents=True,exist_ok=True)
    for name,c in COLORS.items():
        v=WarmVector('block_'+name,128,128)
        v.rect(3,5,122,120,23,INK); v.rect(6,4,116,113,21,c)
        v.rect(14,12,99,6,3,'#FFF5D9',.8); v.ellipse(20,26,5,8,'#FFFFFF',.85)
        v.paw(66,71,26,INK,.22); v.save()
    v=WarmVector('cell_empty',128,128)
    v.rect(3,3,122,122,19,'#D9AA70');v.rect(6,6,116,116,17,CREAM);v.paw(64,68,23,'#D8AC77',.52);v.save()
    v=WarmVector('cell_preview',128,128)
    v.rect(3,3,122,122,19,INK);v.rect(7,7,114,114,16,'#FFE09A');v.paw(64,68,23,'#C18749',.35);v.save()
    v=WarmVector('board_frame',256,256)
    v.rect(2,5,252,250,32,'#A86A3A');v.rect(3,2,250,248,30,'#FFCF7D')
    v.rect(7,6,242,239,27,'#FFE6AF');v.rect(13,12,230,228,23,'#DBA26A');v.save((36,36,36,36))
    v=WarmVector('panel',256,256)
    v.rect(2,5,252,250,36,'#B4774A');v.rect(3,2,250,245,34,CREAM);v.rect(10,9,236,228,29,'#FFF6DF');v.save((40,40,40,40))
    for name,face,edge in [('primary','#FFB35E','#BD7237'),('secondary','#F6B4BD','#B97179')]:
        v=WarmVector('button_'+name,256,128)
        v.rect(2,5,252,122,29,edge);v.rect(3,2,250,117,28,INK)
        v.rect(6,4,244,110,25,face);v.rect(15,11,224,5,2,'#FFF4D7',.8)
        v.ellipse(19,27,4,7,'#FFFFFF',.8);v.paw(37,66,16,INK);v.save((62,30,30,30))
    v=WarmVector('icon_paw',128,128)
    v.ellipse(64,67,59,59,'#BD7237');v.ellipse(64,63,59,59,INK)
    v.ellipse(64,63,55,55,CREAM);v.ellipse(64,62,48,48,'#FFE1A5')
    v.paw(64,66,37,'#E98772');v.save()
    if (ART/'background.png').exists():texture('background')
    if (ART/'mascot_sleep.png').exists():texture('mascot_sleep')
    for folder in (FOLDER,FOLDER+'/Sources',FOLDER+'/Cats'):
        Path(str(ROOT/folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')
    for path in (ART/'Sources').glob('*.svg'):
        Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(path.relative_to(ROOT))}\nDefaultImporter:\n  externalObjects: {{}}\n')

def cats():
    template=yaml.safe_load((ROOT/'tools/texture_importer_template.yaml').read_text())
    catalog=[];textures={}
    for name,breed,shape,expression in POSES:
        expression={'single_calico':'happy','single_black':'surprised','single_white':'joyful','single_tuxedo':'winking','zigzag_black':'surprised'}.get(name,expression)
        with Image.open(ART/'Cats'/(name+'.png')) as im:
            assert im.mode=='RGBA' and im.getchannel('A').getextrema()[0]==0
            left,top,right,bottom=im.getchannel('A').point(lambda a:255 if a>=64 else 0).getbbox()
            cols=max(x for x,y in shape)+1;rows=max(y for x,y in shape)+1
            xs=[round(left+(right-left)*i/cols) for i in range(cols+1)]
            ys=[round(top+(bottom-top)*i/rows) for i in range(rows+1)]
            # Fit the face into occupied cells instead of cutting cheeks/ears at a uniform split.
            if name=='corner4_calico':xs[1]=round(left+(right-left)*.72)
            if name=='corner3_ginger':xs[1]=round(left+(right-left)*.59)
            if name=='tee_tuxedo':
                xs[1]=round(left+(right-left)*.23);xs[2]=round(left+(right-left)*.77)
            if name=='zigzag_black':
                xs[1]=round(left+(right-left)*.26);ys[1]=round(top+(bottom-top)*.64)
            area=dict(x=left,y=top,width=right-left,height=bottom-top,xs=xs,ys=ys,sourceWidth=im.width,sourceHeight=im.height,cols=cols,rows=rows)
            records=textures.setdefault(name,[]);ids=[];rects=[]
            for x,y in shape:
                row=rows-1-y;fid=21300100+len(records)
                rect=dict(serializedVersion=2,x=xs[x],y=im.height-ys[row+1],width=xs[x+1]-xs[x],height=ys[row+1]-ys[row])
                entry=__import__('copy').deepcopy(template['TextureImporter']['spriteSheet']['sprites'][0])
                entry.update(name=f'{name}_{cols}x{rows}_{x}_{y}',rect=rect,pivot={'x':.5,'y':.5},alignment=0,spriteID=guid(f'warm:{name}:{cols}:{rows}:{x}:{y}'),internalID=fid)
                records.append(entry);ids.append(dict(fileID=fid,guid=guid(f'{FOLDER}/Cats/{name}.png'),type=3));rects.append(dict(rect))
            catalog.append(dict(name=f'{name}_{cols}x{rows}',source=name,breed=breed,expression=expression,shape=[dict(x=x,y=y) for x,y in shape],parts=ids,small=dict(fileID=21300100,guid=guid(f'{FOLDER}/Cats/single_{breed}.png'),type=3),region=area,rects=rects))
    for name,records in textures.items():
        data=__import__('copy').deepcopy(template);data['guid']=guid(f'{FOLDER}/Cats/{name}.png');t=data['TextureImporter']
        t.update(spriteMode=2,spriteMeshType=0,maxTextureSize=1024,alphaIsTransparency=1)
        t['internalIDToNameTable']=[dict(first={213:r['internalID']},second=r['name']) for r in records]
        t['spriteSheet']['sprites']=records;t['spriteSheet']['nameFileIdTable']={r['name']:r['internalID'] for r in records}
        for platform in t['platformSettings']:platform.update(textureCompression=0,maxTextureSize=1024)
        Path(str(ART/'Cats'/(name+'.png'))+'.meta').write_text(yaml.dump(data,Dumper=Dumper,sort_keys=False))
    (ROOT/'tools/warm_cat_catalog.json').write_text(json.dumps(catalog,indent=2))
    (ART/'cat_catalog.json').write_text(json.dumps(catalog,indent=2))
    print(f'Review-only warm kit: {len(textures)} cats, {len(catalog)} poses, {sum(map(len,textures.values()))} slices. Live scenes unchanged.')

if __name__=='__main__':
    ui()
    if all((ART/'Cats'/(name+'.png')).exists() for name,*_ in POSES):cats()
