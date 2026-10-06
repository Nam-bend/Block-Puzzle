#target photoshop
// Review-only Photoshop composition. Does not change any Unity scene or live art reference.
(function(){
    var base=new File($.fileName).parent.parent,art=new Folder(base+'/Assets/Art/CatNookWarm'),out=new Folder(base+'/docs/design/Warm');out.create();
    var f=new File(base+'/tools/warm_cat_catalog.json');f.open('r');var cats=eval('('+f.read()+')');f.close();
    var cache={},units=app.preferences.rulerUnits,dialogs=app.displayDialogs;
    app.preferences.rulerUnits=Units.PIXELS;app.displayDialogs=DialogModes.NO;
    function rgb(hex){var c=new SolidColor();c.rgb.hexValue=hex;return c;}
    function group(parent,name){var g=parent.layerSets.add();g.name=name;return g;}
    function source(name){if(!cache[name])cache[name]=app.open(new File(art+'/'+name+'.png'));return cache[name];}
    function image(doc,parent,name,x,y,w,h,cover){
        var src=source(name);app.activeDocument=src;src.selection.deselect();
        var sw=src.width.as('px'),sh=src.height.as('px'),ab=src.activeLayer.bounds,ox=Number(ab[0]),oy=Number(ab[1]);
        var sx=w/sw,sy=h/sh;if(cover){sx=sy=Math.max(sx,sy);x+=(w-sw*sx)/2;y+=(h-sh*sy)/2;}
        var l=src.activeLayer.duplicate(doc,ElementPlacement.PLACEATBEGINNING);app.activeDocument=doc;l.move(parent,ElementPlacement.INSIDE);l.name=name;
        var b=l.bounds;l.resize(sx*100,sy*100,AnchorPosition.TOPLEFT);l.translate(x+ox*sx-Number(b[0]),y+oy*sy-Number(b[1]));return l;
    }
    function label(doc,parent,value,x,y,size,hex){
        app.activeDocument=doc;var l=doc.artLayers.add();l.kind=LayerKind.TEXT;l.name=value;l.move(parent,ElementPlacement.INSIDE);
        var t=l.textItem;t.contents=value;t.size=UnitValue(size,'px');t.color=rgb(hex||'784126');t.justification=Justification.CENTER;
        t.font='Arial-BoldMT';t.position=[UnitValue(x,'px'),UnitValue(y,'px')];return l;
    }
    function mask(doc,layer,rectangles){
        app.activeDocument=doc;doc.activeLayer=layer;
        for(var i=0;i<rectangles.length;i++){var r=rectangles[i];doc.selection.select([[r[0],r[1]],[r[2],r[1]],[r[2],r[3]],[r[0],r[3]]],i?SelectionType.EXTEND:SelectionType.REPLACE,0,false);}
        var d=new ActionDescriptor(),at=new ActionReference();d.putClass(charIDToTypeID('Nw  '),charIDToTypeID('Chnl'));
        at.putEnumerated(charIDToTypeID('Chnl'),charIDToTypeID('Chnl'),charIDToTypeID('Msk '));d.putReference(charIDToTypeID('At  '),at);
        d.putEnumerated(charIDToTypeID('Usng'),charIDToTypeID('UsrM'),charIDToTypeID('RvlS'));executeAction(charIDToTypeID('Mk  '),d,DialogModes.NO);doc.selection.deselect();
    }
    function pose(name,cols){for(var i=0;i<cats.length;i++)if(cats[i].source==name&&(!cols||cats[i].region.cols==cols))return cats[i];throw Error(name);}
    function cat(doc,parent,p,x,y,step){
        var g=group(parent,p.name),r=p.region;
        for(var i=0;i<p.shape.length;i++){
            var cell=p.shape[i],rect=p.rects[i],xx=x+cell.x*step,yy=y+(r.rows-1-cell.y)*step;
            var sx=step/rect.width,sy=step/rect.height,top=r.sourceHeight-rect.y-rect.height;
            var l=image(doc,g,'Cats/'+p.source,xx-rect.x*sx,yy-top*sy,r.sourceWidth*sx,r.sourceHeight*sy,false);
            l.name='Cell '+cell.x+','+cell.y;mask(doc,l,[[xx,yy,xx+step,yy+step]]);
        }
        return g;
    }
    function button(doc,parent,name,value,x,y,w,h,primary){var g=group(parent,name);image(doc,g,primary?'button_primary':'button_secondary',x,y,w,h,false);label(doc,g,value,x+w/2+18,y+h*.63,32);}
    function save(doc,name){app.activeDocument=doc;var options=new PhotoshopSaveOptions();options.layers=true;options.embedColorProfile=true;doc.saveAs(new File(out+'/'+name+'.psd'),options,true,Extension.LOWERCASE);doc.saveAs(new File(out+'/'+name+'.png'),new PNGSaveOptions(),true,Extension.LOWERCASE);doc.close(SaveOptions.DONOTSAVECHANGES);}
    function start(name){var d=app.documents.add(946,2048,72,name,NewDocumentMode.RGB,DocumentFill.TRANSPARENT);image(d,group(d,'01_Background'),'background',0,0,946,2048,true);return d;}
    function gameplay(){
        var d=start('Cat Nook Warm Gameplay'),g=group(d,'02_Score');
        cat(d,g,pose('single_calico',1),75,110,140);
        label(d,g,'CAT NOOK',535,170,39);label(d,g,'SCORE',535,260,25,'A16843');label(d,g,'1280',535,352,86);
        image(d,g,'panel',180,389,586,90,false);label(d,g,'PERSONAL BEST  2460',473,449,25);
        g=group(d,'03_Board');image(d,g,'board_frame',53,524,840,840,false);
        var placements=[['bar4_gray',4,0,6],['corner4_calico',2,5,4],['square_calico',2,0,2],['zigzag_black',3,3,0],['single_black',1,7,0],['corner3_ginger',2,5,2]],filled={};
        for(var i=0;i<placements.length;i++){var a=placements[i],p=pose(a[0],a[1]);for(var j=0;j<p.shape.length;j++)filled[(a[2]+p.shape[j].x)+','+(7-a[3]-p.shape[j].y)]=true;}
        for(var row=0;row<8;row++)for(var col=0;col<8;col++)if(!filled[col+','+row])image(d,g,'cell_empty',75+col*100,546+row*100,96,96,false);
        for(var i=0;i<placements.length;i++){var a=placements[i],p=pose(a[0],a[1]);cat(d,g,p,73+a[2]*100,544+(8-a[3]-p.region.rows)*100,100);}
        g=group(d,'04_Tray');image(d,g,'panel',38,1480,870,270,false);
        cat(d,g,pose('single_white',1),144,1556,105);cat(d,g,pose('bar5_siamese',5),350,1582,49);cat(d,g,pose('tee_tuxedo',3),670,1546,60);
        label(d,group(d,'05_Hint'),'Drag a cat. Fill a row or column.',473,1444,26,'A16843');
        g=group(d,'06_Buttons');button(d,g,'Home','HOME',98,1820,300,108,false);button(d,g,'Replay','REPLAY',548,1820,300,108,true);return d;
    }
    try{
        var d=app.documents.add(2000,2000,72,'Cat Nook Warm Cat Library',NewDocumentMode.RGB,DocumentFill.TRANSPARENT);
        d.selection.selectAll();d.selection.fill(rgb('F8D7A8'));d.selection.deselect();
        var g=group(d,'01_Identity');label(d,g,'CAT NOOK - WARM CAT COLLECTION',1000,66,42);label(d,g,'7 coats / 17 poses / 27 puzzle shapes',1000,109,25,'A16843');
        var library=group(d,'02_Cat_Library');
        for(var i=0;i<cats.length;i++){
            var p=cats[i],cx=30+(i%4)*485,cy=130+Math.floor(i/4)*350,card=group(library,p.name);
            image(d,card,'panel',cx,cy,450,320,false);var step=Math.min(120,380/p.region.cols,200/p.region.rows);
            cat(d,card,p,cx+225-p.region.cols*step/2,cy+25+(200-p.region.rows*step)/2,step);
            label(d,card,p.source,cx+225,cy+262,22);label(d,card,p.breed+' / '+p.expression+' / '+p.shape.length+' cells',cx+225,cy+299,19,'A16843');
        }
        save(d,'CatNook_Warm_Cats');
        d=start('Cat Nook Warm Menu');g=group(d,'02_Identity');cat(d,g,pose('single_calico',1),283,310,380);
        label(d,g,'CAT NOOK',473,835,90);label(d,g,'A little puzzle. A happy catnap.',473,923,29,'A16843');
        g=group(d,'03_Buttons');button(d,g,'Play',"LET'S PLAY",213,1070,520,140,true);button(d,g,'Sound','SOUND ON',303,1280,340,100,false);
        label(d,group(d,'04_Best'),'PERSONAL BEST  2460',473,1500,29);save(d,'CatNook_Warm_Menu');
        d=gameplay();save(d,'CatNook_Warm_Gameplay');
        d=start('Cat Nook Warm Game Over');g=group(d,'02_Modal');image(d,g,'panel',73,584,800,860,false);
        cat(d,g,pose('square_calico',2),353,650,120);label(d,g,'TIME FOR A CATNAP',473,969,40);
        label(d,g,'SCORE  1280',473,1055,39);label(d,g,'PERSONAL BEST  2460',473,1112,25,'A16843');
        button(d,g,'Again','PLAY AGAIN',233,1168,480,125,true);button(d,g,'Home','HOME',303,1320,340,95,false);save(d,'CatNook_Warm_GameOver');
        var report=new File(out+'/photoshop-build.txt');report.open('w');report.write('Review-only warm asset kit. Layered cat PNGs with occupied-footprint masks and editable UI text. No live Unity scene references changed. Example scores and boards are design mockups.');report.close();
    }catch(error){var log=new File(out+'/photoshop-error.txt');log.open('w');log.write('Line '+error.line+': '+error.message+'\n'+$.stack);log.close();throw error;}
    finally{for(var name in cache)try{cache[name].close(SaveOptions.DONOTSAVECHANGES);}catch(e){}app.preferences.rulerUnits=units;app.displayDialogs=dialogs;}
})();
