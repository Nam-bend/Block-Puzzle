"""Migrate saved scenes/prefabs to approved warm art and remove obsolete UI nodes."""
from pathlib import Path
import json
import re
import yaml
from PIL import Image
from art_common import ROOT, Scene, Dumper, guid, color, ref

FOLDER='Assets/Art/CatNookWarm'
INK='#784126';MUTED='#A16843'

def sprite(name):return dict(fileID=21300000,guid=guid(f'{FOLDER}/{name}.png'),type=3)

def remove(s,go):
    removed=set()
    def collect(node):
        if node in removed:return
        removed.add(node)
        for component in s.data(node)['m_Component']:
            cid=component['component']['fileID'];removed.add(cid)
            for child in s.data(cid).get('m_Children',[]):collect(s.data(child['fileID'])['m_GameObject']['fileID'])
    collect(go)
    for fid in removed:s.docs.pop(fid)
    def scrub(value):
        if isinstance(value,list):
            value[:]=[v for v in value if not(isinstance(v,dict) and set(v)=={'fileID'} and v['fileID'] in removed)]
            for v in value:scrub(v)
        elif isinstance(value,dict):
            if set(value)=={'fileID'} and value['fileID'] in removed:value['fileID']=0
            else:
                for v in value.values():scrub(v)
    for d in s.docs.values():scrub(d['data'])

def named(s,name):
    try:return s.named(name)
    except StopIteration:return None

def layout(s,name,pos,size=None):
    go=s.named(name)
    if size:s.layout(go,pos,size)
    else:s.data(s.rect(go))['m_AnchoredPosition']=dict(zip(('x','y'),pos))

def label(s,name,text,pos,size,muted=False):s.style_label(s.named(name),text,pos,size,MUTED if muted else INK)

def image(s,name,asset,pos,size):
    layout(s,name,pos,size);s.data(s.image(s.named(name))).update(m_Sprite=sprite(asset),m_Color=color('#FFFFFF'),m_Type=1 if asset in ('panel','board_frame','button_primary','button_secondary') else 0)

def button(s,name,asset,pos,size):
    image(s,name,asset,pos,size)
    go=s.named(name);b=s.data(s.component(go,'m_OnClick'));b['m_Colors']['m_PressedColor']=color('#E7BC90')
    for child in s.data(s.rect(go))['m_Children']:
        cg=s.data(child['fileID'])['m_GameObject']['fileID']
        if s.data(cg)['m_IsActive'] and any('m_text' in s.data(c['component']['fileID']) for c in s.data(cg)['m_Component']):
            d=s.data(s.label(cg));d.update(m_fontSize=32,m_fontSizeBase=32,m_fontColor=color(INK),m_fontStyle=1)

def main():
    # Keep importer schema independently of retired vendor textures.
    template=ROOT/'tools/texture_importer_template.yaml'
    if not template.exists():template.write_text((ROOT/'Assets/Texture/Squares/Blue.png.meta').read_text())
    import shutil
    shutil.copyfile(ROOT/FOLDER/'Cats/square_calico.png',ROOT/FOLDER/'mascot_sleep.png')
    from build_warm_assets import texture
    texture('mascot_sleep')
    catalog=json.loads((ROOT/'tools/warm_cat_catalog.json').read_text())
    old_guid_map={}
    for path in (ROOT/'Assets/Art/CatNook').rglob('*.png'):
        relative=path.relative_to(ROOT/'Assets/Art/CatNook');target=ROOT/FOLDER/relative
        if target.exists():old_guid_map[yaml.safe_load(Path(str(path)+'.meta').read_text())['guid']]=guid(f'{FOLDER}/{relative.as_posix()}')
    for path,menu in [('Assets/Scenes/SampleScene.unity',True),('Assets/Scenes/Game.unity',False)]:
        s=Scene(path)
        obsolete=('Title','Shapes','Settings','CatMenuFooter') if menu else ('TopBackgroundPanel','GameOverTitle','CatEndBadge')
        for name in obsolete:
            go=named(s,name)
            if go:remove(s,go)
        # Remove hidden legacy button labels, not the intentionally hidden end panel.
        for go,doc in list(s.docs.items()):
            if doc['type']==1 and s.data(go).get('m_Name')=='Label' and not s.data(go)['m_IsActive']:remove(s,go)
        for go,doc in s.docs.items():
            if doc['type']==114 and 'm_fontColor' in s.data(go):s.data(go)['m_fontColor']=color(INK)
        root=s.rect(s.named('CatSafeContent'))
        if menu:
            layout(s,'CatMascot',(0,524),(380,380));label(s,'CatTitle','CAT NOOK',(0,220),90)
            label(s,'CatSubtitle','A little puzzle. A happy catnap.',(0,110),29,True)
            button(s,'PlayButton','button_primary',(0,-116),(520,140));button(s,'SettingButtonShow','button_secondary',(0,-306),(340,100))
            label(s,'CatMenuBest','PERSONAL BEST  0',(0,-466),29)
        else:
            layout(s,'CatHeaderMascot',(-328,844),(140,140));label(s,'CatGameTitle','CAT NOOK',(62,874),39)
            label(s,'Score','0',(62,714),86);label(s,'BestScore','PERSONAL BEST  0',(0,584),25)
            if not named(s,'CatScoreCaption'):s.new_label('CatScoreCaption',root,'SCORE',(62,772),25,MUTED)
            if not named(s,'CatBestPanel'):
                bg=s.new_image('CatBestPanel',root,'panel',(0,590),(586,90),True)
                s.data(s.image(bg))['m_Sprite']=sprite('panel')
                children=s.data(root)['m_Children'];children.remove(ref(s.rect(bg)));children.insert(children.index(ref(s.rect(s.named('BestScore')))),ref(s.rect(bg)))
            image(s,'CatTray','panel',(0,-590),(870,270))
            label(s,'Message','Drag a cat. Fill a row or column.',(0,-420),26,True)
            button(s,'MenuButton','button_secondary',(-225,-850),(300,108));button(s,'RestartButton','button_primary',(225,-850),(300,108))
            s.data(s.image(s.named('GameOverPanel')))['m_Color']=dict(r=.35,g=.19,b=.1,a=.6)
            image(s,'CatEndMascot','mascot_sleep',(0,255),(240,240));label(s,'CatEndTitle','TIME FOR A CATNAP',(0,70),40)
            layout(s,'FinalScore',(0,-65),(680,140));label(s,'FinalScore','SCORE  0\nPERSONAL BEST  0',(0,-65),38)
            button(s,'TryAgainButton','button_primary',(0,-206),(480,125));button(s,'CatPopupHome','button_secondary',(0,-343),(340,95))
            # Keep modal over all new header decorations.
            children=s.data(root)['m_Children'];panel=ref(s.rect(s.named('GameOverPanel')));children.remove(panel);children.append(panel)
        with Image.open(ROOT/FOLDER/'background.png') as bg:ratio=bg.width/bg.height
        for doc in s.docs.values():
            body=next(iter(doc['data'].values()))
            if 'm_AspectRatio' in body:body['m_AspectRatio']=ratio
        s.save()
    # GUID substitution preserves each prefab instance and its overrides.
    for path in (ROOT/'Assets').rglob('*'):
        if path.suffix not in ('.unity','.prefab','.asset'):continue
        text=path.read_text(encoding='utf-8')
        for old,new in old_guid_map.items():text=text.replace(old,new)
        path.write_text(text,encoding='utf-8')
    artpath=ROOT/'Assets/Resources/BlockArt.asset';text=artpath.read_text();text=text.split('\n  cats:')[0]+'\n'
    poses=[{k:p[k] for k in ('name','breed','expression','shape','parts','small')} for p in catalog]
    text+=''.join('  '+line+'\n' for line in yaml.dump({'cats':poses},Dumper=Dumper,sort_keys=False,width=180).splitlines());artpath.write_text(text)
    # Any residual vendor sprite states belong to deleted/retired UI; normal art uses warm GUIDs.
    vendor_guids=[yaml.safe_load(p.read_text())['guid'] for p in (ROOT/'Assets/Texture').rglob('*.png.meta')]
    for path in (ROOT/'Assets').rglob('*'):
        if path.suffix not in ('.unity','.prefab','.asset'):continue
        text=path.read_text()
        for g in vendor_guids:text=re.sub(r'\{fileID: \d+, guid: '+g+r', type: \d+\}', '{fileID: 0}',text)
        path.write_text(text)
    print('Applied warm art, cat catalog and layout; removed legacy scene UI. Ready for reference validation before deleting old folders.')

if __name__=='__main__':main()
