"""Shared vector primitives, stable GUIDs and Unity YAML inspection helpers."""
from pathlib import Path
import copy
import hashlib
import json
import re
import xml.sax.saxutils as xml
import yaml
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
INK, CREAM, MUTED = '#784126', '#FFF1D2', '#A16843'
COLORS = dict(blue='#8ACDDD', mint='#A9CE79', gold='#FFD166', coral='#FFA25A', violet='#C7ACE0', pink='#F6A6BB')

def guid(path):
    return hashlib.md5(('CatNook:' + str(path).replace('\\', '/')).encode()).hexdigest()

def meta(path, content='DefaultImporter:\n  externalObjects: {}\n'):
    p = ROOT / path
    m = Path(str(p) + '.meta')
    if not m.exists():
        m.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\n' + content, encoding='utf-8')
    return re.search(r'^guid: (\w+)', m.read_text(), re.M)[1]

class Vector:
    def __init__(self, name, w, h):
        self.name, self.w, self.h = name, w, h
        self.im = Image.new('RGBA', (w * 3, h * 3))
        self.svg = []

    def paint(self, shape, coords, color, opacity=1, radius=0):
        layer = Image.new('RGBA', self.im.size)
        d = ImageDraw.Draw(layer)
        rgb = tuple(bytes.fromhex(color.lstrip('#'))) + (round(opacity * 255),)
        if shape == 'rect':
            x, y, w, h = coords
            d.rounded_rectangle(tuple(round(a*3) for a in (x, y, x+w, y+h)), radius=radius*3, fill=rgb)
            self.svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{color}" opacity="{opacity}"/>')
        elif shape == 'ellipse':
            cx, cy, rx, ry = coords
            d.ellipse(tuple(round(a*3) for a in (cx-rx, cy-ry, cx+rx, cy+ry)), fill=rgb)
            self.svg.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{color}" opacity="{opacity}"/>')
        else:
            d.polygon([(round(x*3), round(y*3)) for x,y in coords], fill=rgb)
            self.svg.append(f'<polygon points="{" ".join(f"{x},{y}" for x,y in coords)}" fill="{color}" opacity="{opacity}"/>')
        self.im = Image.alpha_composite(self.im, layer)

    def rect(self, x,y,w,h,r,c,opacity=1): self.paint('rect', (x,y,w,h), c, opacity, r)
    def ellipse(self,x,y,rx,ry,c,opacity=1): self.paint('ellipse',(x,y,rx,ry),c,opacity)
    def paw(self,cx,cy,s,c,opacity=1):
        self.ellipse(cx, cy+s*.2, s*.45,s*.34,c,opacity)
        for i in range(3): self.ellipse(cx+(i-1)*s*.4,cy-s*(.48 if i==1 else .32),s*.16,s*.21,c,opacity)

    def save(self, border=(0,0,0,0)):
        raise NotImplementedError('Use a kit-specific vector exporter.')

class Dumper(yaml.SafeDumper): pass
def dictionary(dumper,value):
    flow = bool(value) and set(value) <= {'fileID','guid','type','x','y','z','w','r','g','b','a'}
    return dumper.represent_mapping('tag:yaml.org,2002:map',value,flow_style=flow)
Dumper.add_representer(dict,dictionary)
def ref(fid=0): return {'fileID':fid}
def sprite(name): return {'fileID':21300000,'guid':guid(f'Assets/Art/CatNookWarm/{name}.png'),'type':3}
def color(hex):
    r,g,b=bytes.fromhex(hex.lstrip('#')); return dict(r=r/255,g=g/255,b=b/255,a=1)

class Scene:
    def __init__(self,path):
        self.path=path; self.docs={}; self.next=8000000000
        text=(ROOT/path).read_text()
        self.prefix=text.split('--- !u!')[0]
        for m in re.finditer(r'--- !u!(\d+) &(\d+)([^\n]*)\n(.*?)(?=--- !u!|\Z)',text,re.S):
            self.docs[int(m[2])]=dict(type=int(m[1]),suffix=m[3],data=yaml.safe_load(m[4]),raw=m[0])
        self.next=max([self.next]+[i for i in self.docs if 8000000000 <= i < 9000000000])
        self.original=copy.deepcopy(self.docs)
    def data(self,id): return next(iter(self.docs[id]['data'].values()))
    def add(self,type,data,suffix=''):
        self.next+=1; self.docs[self.next]=dict(type=type,suffix=suffix,data=data); return self.next
    def named(self,name):
        return next(k for k,v in self.docs.items() if v['type']==1 and self.data(k).get('m_Name')==name)
    def component(self,go,field):
        return next(r['component']['fileID'] for r in self.data(go)['m_Component'] if field in self.data(r['component']['fileID']))
    def rect(self,go): return self.component(go,'m_AnchoredPosition')
    def image(self,go): return self.component(go,'m_Sprite')
    def label(self,go): return self.component(go,'m_text')
    def clone(self,template,name,parent,pos,size,kind):
        original_go=self.named(template); gd=copy.deepcopy(self.data(original_go))
        go=self.add(1,{'GameObject':gd}); gd['m_Name']=name; gd['m_IsActive']=1; gd['m_Component']=[]
        ids={}
        for r in self.data(original_go)['m_Component']:
            old=r['component']['fileID']; d=copy.deepcopy(self.docs[old]['data']); body=next(iter(d.values()))
            if self.docs[old]['type']==114 and kind not in body and 'm_text' not in body: continue
            if kind=='m_Sprite' and 'm_text' in body: continue
            body['m_GameObject']=ref(go)
            new=self.add(self.docs[old]['type'],d); gd['m_Component'].append({'component':ref(new)}); ids[old]=new
            if 'm_Children' in body: body['m_Children']=[]; body['m_Father']=ref(parent)
        rect=self.rect(go); self.data(parent)['m_Children'].append(ref(rect))
        self.layout(go,pos,size)
        return go
    def layout(self,go,pos,size):
        d=self.data(self.rect(go)); d.update(m_AnchoredPosition=dict(zip(('x','y'),pos)),m_SizeDelta=dict(zip(('x','y'),size)),m_AnchorMin={'x':.5,'y':.5},m_AnchorMax={'x':.5,'y':.5},m_Pivot={'x':.5,'y':.5},m_LocalScale={'x':1,'y':1,'z':1})
    def new_image(self,name,parent,asset,pos,size,sliced=False):
        go=self.clone('BG',name,parent,pos,size,'m_Sprite'); d=self.data(self.image(go))
        d.update(m_Sprite=sprite(asset),m_Color=color('#FFFFFF'),m_RaycastTarget=0,m_Type=int(sliced),m_PreserveAspect=0)
        return go
    def new_label(self,name,parent,text,pos,size=32,hex=CREAM):
        template='Score' if self.path.endswith('Game.unity') else 'Label'
        go=self.clone(template,name,parent,pos,(800,110),'m_text'); self.style_label(go,text,pos,size,hex); return go
    def style_label(self,go,text,pos,size,hex):
        d=self.data(self.label(go)); d.update(m_text=text,m_fontSize=size,m_fontSizeBase=size,m_fontColor=color(hex),m_RaycastTarget=0,m_fontStyle=1,m_HorizontalAlignment=2,m_VerticalAlignment=512)
        self.data(self.rect(go))['m_AnchoredPosition']=dict(zip(('x','y'),pos))
    def style_button(self,name,text,asset,pos,size):
        go=self.named(name); self.layout(go,pos,size)
        d=self.data(self.image(go)); d.update(m_Sprite=sprite(asset),m_Color=color('#FFFFFF'),m_Type=1,m_RaycastTarget=1)
        for child in self.data(self.rect(go))['m_Children']:
            child_go=self.data(child['fileID'])['m_GameObject']['fileID']; self.data(child_go)['m_IsActive']=0
        label=self.new_label('CatButtonLabel',self.rect(go),text,(20,4),34 if size[1]>110 else 26,INK)
        self.layout(label,(20,4),(size[0]-100,size[1]-20))
        button=self.data(self.component(go,'m_OnClick'))
        button['m_Transition']=1; button['m_Colors'].update(m_NormalColor=color('#FFFFFF'),m_HighlightedColor=color('#FFF7EB'),m_PressedColor=color('#C7D6D9'),m_DisabledColor=dict(r=.55,g=.6,b=.62,a=.7),m_FadeDuration=.08)
        button['m_TargetGraphic']=ref(self.image(go))
        return go,label
    def script(self,go,path,fields):
        data=dict(m_ObjectHideFlags=0,m_CorrespondingSourceObject=ref(),m_PrefabInstance=ref(),m_PrefabAsset=ref(),m_GameObject=ref(go),m_Enabled=1,m_EditorHideFlags=0,m_Script={'fileID':11500000,'guid':guid(path),'type':3},m_Name='',m_EditorClassIdentifier='')
        data.update(fields); id=self.add(114,{'MonoBehaviour':data}); self.data(go)['m_Component'].append({'component':ref(id)})
    def save(self):
        chunks=[self.prefix]
        for id,doc in self.docs.items():
            if id in self.original and doc['data']==self.original[id]['data']: chunks.append(doc['raw']); continue
            chunks.append(f'--- !u!{doc["type"]} &{id}{doc["suffix"]}\n'+yaml.dump(doc['data'],Dumper=Dumper,sort_keys=False,width=200,allow_unicode=True))
        (ROOT/self.path).write_text(''.join(chunks),encoding='utf-8')

