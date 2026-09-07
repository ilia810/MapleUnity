"""Preserve the reference artwork and edit only interaction effects in GIMP 3.

Run through GIMP's Python batch interpreter from the repository root. The default
output is a review directory; checked-in sources and runtime PNGs are not touched.
"""
from pathlib import Path
from tempfile import TemporaryDirectory
import subprocess
from gi.repository import Gimp, Gio, Gegl

ROOT=Path.cwd()
OUT=Path(globals().get('OUTPUT_DIR',ROOT/'ArtSource/HudButtons/generated'))
OUT.mkdir(parents=True,exist_ok=True)
BASE='afe2633853c884a38c338d0561d29b5d1d250ac4'
W,H=148,68
R,A,D=Gimp.ChannelOps.REPLACE,Gimp.ChannelOps.ADD,Gimp.ChannelOps.SUBTRACT
Gimp.context_set_antialias(True)
Gimp.context_set_feather(False)


def new_layer(im,group,name,opacity=100):
    layer=Gimp.Layer.new(im,name,W,H,Gimp.ImageType.RGBA_IMAGE,opacity,Gimp.LayerMode.NORMAL)
    im.insert_layer(layer,group,0)
    return layer


def fill(im,layer,color):
    Gimp.context_set_foreground(Gegl.Color.new(color))
    layer.edit_fill(Gimp.FillType.FOREGROUND)
    Gimp.Selection.none(im)


def rect(im,x,y,w,h,op=R):
    im.select_rectangle(op,x,y,w,h)


def interior(im):
    im.select_round_rectangle(R,6,6,136,58,3,3)


def load_original(im,group,path,name):
    layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(path)))
    im.insert_layer(layer,group,0)
    layer.set_name(name)
    return layer


def build(name,path):
    im=Gimp.Image.new(W,H,Gimp.ImageBaseType.RGB)
    im.undo_disable()
    groups=[]
    for state,title in [
        ('normal','NORMAL · Original artwork / exact colors'),
        ('mouseOver','HOVER / FOCUS · Soft inner rim'),
        ('pressed','PRESSED · Inset bevel / +1 HUD px'),
        ('disabled','DISABLED · Original art / muted grayscale')]:
        group=Gimp.GroupLayer.new(im,title)
        im.insert_layer(group,None,0);groups.append(group)
        original=load_original(im,group,path,'01 · Original icon, caption and palette / preserved')
        if state=='mouseOver':
            light=new_layer(im,group,'02 · Narrow soft rim / 18%',18)
            im.select_round_rectangle(R,5,5,138,58,4,4)
            im.select_round_rectangle(D,6.5,6.5,135,55,3,3)
            Gimp.Selection.feather(im,.75)
            fill(im,light,'#ffffff')
            top=new_layer(im,group,'03 · Upper bevel reflection / 8%',8)
            rect(im,10,5,126,1)
            Gimp.Selection.feather(im,1)
            fill(im,top,'#ffffff')
        elif state=='pressed':
            shifted=load_original(im,group,path,'02 · Original face shifted 2 texture px right/down')
            shifted.set_offsets(2,2)
            interior(im)
            Gimp.Selection.invert(im)
            shifted.edit_clear();Gimp.Selection.none(im)
            shade=new_layer(im,group,'03 · Face depth / 8%',8)
            interior(im);fill(im,shade,'#000000')
            inset=new_layer(im,group,'04 · Inset top-left bevel / 25%',25)
            rect(im,8,5,132,2);rect(im,5,8,2,53,A)
            Gimp.Selection.feather(im,.5)
            fill(im,inset,'#000000')
            reflected=new_layer(im,group,'05 · Lower-right reflected light / 18%',18)
            rect(im,8,63,132,1);rect(im,141,9,1,53,A)
            Gimp.Selection.feather(im,.5)
            fill(im,reflected,'#ffffff')
        elif state=='disabled':
            # A live GIMP filter retains the underlying full-color reference.
            desaturate=Gimp.DrawableFilter.new(original,'gimp:desaturate','Disabled · luminance')
            config=desaturate.get_config()
            config.set_property('mode',Gimp.DesaturateMode.LUMINANCE)
            desaturate.update();original.append_filter(desaturate)
            veil=new_layer(im,group,'02 · Gentle contrast reduction / 28%',28)
            im.select_item(R,original);fill(im,veil,'#7a7a7a')
        for g in groups:g.set_visible(g==group)
        assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(OUT/(name+'-'+state+'.png'))))
        if state=='normal':
            # Keep the original PNG byte for byte, including invisible RGB at
            # fully transparent corners. GIMP's composite matches visible pixels.
            (OUT/(name+'-normal.png')).write_bytes(path.read_bytes())
        print('EXPORTED',name,state,flush=True)
    for i,g in enumerate(groups):
        im.reorder_item(g,None,i);g.set_visible(i==0)
    im.undo_enable()
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(OUT/(name+'.xcf'))))
    im.delete()

with TemporaryDirectory(prefix='hud-reference-') as temp:
    for name in ('BtShop','BtMenu','BtShort'):
        resource='Assets/Resources/UI/ClassicHudButtons/'+name+'-normal.png'
        data=subprocess.run(['git','show',BASE+':'+resource],check=True,capture_output=True).stdout
        source=Path(temp)/(name+'.png');source.write_bytes(data)
        build(name,source)
print('Saved 12 PNGs and 3 XCFs with the original raster icons and captions.',flush=True)
