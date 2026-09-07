"""Render portable artwork proofs in GIMP; these are not Unity captures."""
from pathlib import Path
from tempfile import TemporaryDirectory
import subprocess
from gi.repository import Gimp, Gio, Gegl

ROOT=Path.cwd()
PNG=Path(globals().get('PNG_DIR',ROOT/'Assets/Resources/UI/ClassicHudButtons'))
OUT=Path(globals().get('PROOF_DIR',ROOT/'Tools/ArtReferences/MenuButtons'))
OUT.mkdir(parents=True,exist_ok=True)
BASE='afe2633853c884a38c338d0561d29b5d1d250ac4'
FONT=Gimp.Font.get_by_name('Arial Bold')
Gimp.context_set_interpolation(Gimp.InterpolationType.LINEAR)
NAMES=['BtShop','BtMenu','BtShort']
STATES=['normal','mouseOver','pressed','disabled']
LABELS=['NORMAL','HOVER / FOCUS','PRESSED','DISABLED']

def canvas(w,h):
    im=Gimp.Image.new(w,h,Gimp.ImageBaseType.RGB)
    layer=Gimp.Layer.new(im,'Proof background',w,h,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL)
    im.insert_layer(layer,None,0)
    Gimp.context_set_foreground(Gegl.Color.new('#172331'))
    layer.fill(Gimp.FillType.FOREGROUND)
    return im

def label(im,txt,x,y,size=18,c='#e7f1f5'):
    layer=Gimp.TextLayer.new(im,txt,FONT,size,Gimp.Unit.pixel())
    im.insert_layer(layer,None,0)
    layer.set_color(Gegl.Color.new(c));layer.set_offsets(x,y)

def art(im,path,x,y,w=74,h=34):
    layer=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(path)))
    im.insert_layer(layer,None,0)
    layer.scale(w,h,False);layer.set_offsets(x,y)
    return layer

def save(im,name):
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(OUT/name)))
    im.delete()
    print('PROOF',name,flush=True)

with TemporaryDirectory(prefix='hud-before-') as temp:
    for name in NAMES:
        relative='Assets/Resources/UI/ClassicHudButtons/'+name+'-normal.png'
        data=subprocess.run(['git','show',BASE+':'+relative],check=True,capture_output=True).stdout
        (Path(temp)/(name+'.png')).write_bytes(data)
    im=canvas(1000,742)
    label(im,'CLASSIC HUD / ORIGINAL ART, CLEANER EFFECTS',28,25,26)
    label(im,'Original icons, caption size and colors preserved · 3× display size',28,62,18,'#a7becb')
    for row,state in enumerate(['before']+STATES):
        y=114+row*116
        label(im,'ORIGINAL' if row==0 else LABELS[row-1],28,y+38,17)
        for col,name in enumerate(NAMES):
            p=Path(temp)/(name+'.png') if state=='before' else PNG/(name+'-'+state+'.png')
            art(im,p,260+col*232,y,222,102)
    label(im,'GIMP artwork proof · runtime verification remains on Windows',28,705,16,'#a7becb')
    save(im,'gimp-cleanup-comparison.png')

im=canvas(460,302)
label(im,'ACTUAL DISPLAY SIZE',20,15,19)
label(im,'Each button is 74 × 34 pixels',20,43,14,'#a7becb')
for row,state in enumerate(STATES):
    y=79+row*50
    label(im,LABELS[row],20,y+10,13)
    for col,name in enumerate(NAMES): art(im,PNG/(name+'-'+state+'.png'),188+col*82,y)
label(im,'Linear 2:1 reduction · inspect at 100% zoom',20,278,13,'#a7becb')
save(im,'gimp-cleanup-actual-size.png')

# Context from the supplied reference, reduced by 74 / 206. This is a static
# compositing check, not a claim that the game was run on macOS.
im=canvas(872,212)
label(im,'HUD CONTEXT / ARTWORK MOCKUP',28,17,22)
label(im,'Reference screenshot with the revised normal artwork composited in',28,48,15,'#a7becb')
hud=Gimp.Image.new(812,94,Gimp.ImageBaseType.RGB)
ref=ROOT/'Tools/ArtReferences/MapleClassic/09-character-stats.png'
layer=art(hud,ref,-286,-683,1379,776)
for col,name in enumerate(NAMES): art(hud,PNG/(name+'-normal.png'),[580,655,730][col],58)
layer=Gimp.Layer.new_from_visible(hud,im,'Reference HUD with revised buttons')
im.insert_layer(layer,None,0);layer.set_offsets(28,82)
hud.delete()
label(im,'Static GIMP composite · not a Unity runtime capture',28,185,14,'#a7becb')
save(im,'gimp-cleanup-context.png')
