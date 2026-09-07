"""Compile inspected Animation.cpp timing against supplied frame metadata (no renderer/NX dependency)."""
import argparse, hashlib, json, re, subprocess, uuid
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True,type=Path)
parser.add_argument('--compiler',default='g++')
a=parser.parse_args()
root=Path(__file__).resolve().parent.parent
out=root/'Temp/HeavenBackgroundReference'
out.mkdir(parents=True,exist_ok=True)
used={}
def read(path):
 data=(a.source/path).read_bytes();used[path]=hashlib.sha256(data).hexdigest();return data.decode('utf-8-sig')
def block(text,signature):
 start=text.index(signature);brace=text.index('{',start);end=brace+1;depth=1
 while depth:
  if text[end]=='{':depth+=1
  elif text[end]=='}':depth-=1
  end+=1
 return text[start:end]
src='#include <cstdint>\n#include <vector>\n#include <utility>\n#include <iostream>\n#include <iomanip>\n'
for path in ['Util/Lerp.h','Template/Interpolated.h']:
 src+=re.sub(r'^#.*$','',read(path),flags=re.M)+'\n'
cpp=read('Graphics/Animation.cpp')
src+=r"""
namespace ms {
struct Frame {
 uint16_t delay; std::pair<uint8_t,uint8_t> opacities; std::pair<int16_t,int16_t> scales;
 uint16_t get_delay()const{return delay;} uint8_t start_opacity()const{return opacities.first;}
 uint16_t start_scale()const{return scales.first;}
 float opcstep(uint16_t)const; float scalestep(uint16_t)const;
};
struct Animation {
 std::vector<Frame> frames; bool zigzag; Nominal<int16_t> frame; Linear<float> opacity,xyscale;
 uint16_t delay;int16_t framestep;
 const Frame& get_frame()const{return frames[frame.get()];}
 void reset();bool update(uint16_t);
};
"""
for signature in ['float Frame::opcstep','float Frame::scalestep','void Animation::reset','bool Animation::update(uint16_t']:
 src+=block(cpp,signature)+'\n'
src+=r"""
}
int main(){
 std::cout<<"case,tick,interpolation,frame,alpha,scale\n"<<std::setprecision(10);
 for(int c=0;c<8;c++){
  ms::Animation a;
  if(c<2)a.frames={{10,{255,100},{100,200}},{17,{100,255},{200,100}},{24,{255,255},{100,100}}};
  else if(c<4)a.frames={{1,{0,255},{100,0}},{3,{255,0},{0,100}},{5,{50,200},{50,150}}};
  else if(c<6)a.frames={{100,{255,0},{100,0}}};
  else a.frames={{4000,{255,170},{100,100}},{4000,{170,255},{100,100}}};
  a.zigzag=c%2;a.reset();
  for(int tick=0;tick<=1100;tick++){
   for(float t:{0.0f,0.25f,0.5f,0.75f,1.0f})
    std::cout<<c<<','<<tick<<','<<t<<','<<a.frame.get(t)<<','<<a.opacity.get(t)/255<<','<<a.xyscale.get(t)/100<<'\n';
   a.update(8);
  }
 }
}
"""
(out/'reference.cpp').write_text(src,encoding='utf-8')
subprocess.run([a.compiler,'-std=c++17','-O0',str(out/'reference.cpp'),'-o',str(out/'reference.exe')],check=True)
result=subprocess.check_output([str(out/'reference.exe')])
fixture=root/'Assets/Scripts/Tests/GameLogic/Fixtures/HeavenBackgroundAnimation.csv'
fixture.parent.mkdir(parents=True,exist_ok=True);fixture.write_bytes(result)
meta=Path(str(fixture)+'.meta')
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
(root/'Tools/HeavenBackgroundReference.provenance.json').write_text(json.dumps({'source':str(a.source),'head':subprocess.check_output(['git','-C',str(a.source),'rev-parse','HEAD']).decode().strip(),'sha256':used,'rows':len(result.splitlines())-1,'scope':'Unmodified Animation update/reset, Frame steps and Interpolated/Lerp; supplied metadata, no texture drawing or NX parsing.'},indent=2))
print('Generated',len(result.splitlines())-1,'reference samples')
