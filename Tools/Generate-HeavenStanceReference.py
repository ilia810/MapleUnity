"""Compile the inspected ordinary CharLook body clock and Char speed selection without NX/graphics."""
import argparse, hashlib, json, re, subprocess, uuid
from pathlib import Path
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True, type=Path)
parser.add_argument('--compiler', default='g++')
a = parser.parse_args()
root = Path(__file__).resolve().parent.parent
out = root / 'Temp/HeavenStanceReference'
out.mkdir(parents=True, exist_ok=True)
used = {}
def read(path):
    data = (a.source / path).read_bytes()
    used[path] = hashlib.sha256(data).hexdigest()
    return data.decode('utf-8-sig').replace('\r\n', '\n')
def block(text, signature):
    start = text.index(signature); brace = text.index('{', start); end = brace + 1; depth = 1
    while depth:
        if text[end] == '{': depth += 1
        elif text[end] == '}': depth -= 1
        end += 1
    return text[start:end]
src = '#include <cstdint>\n#include <cmath>\n#include <map>\n#include <iostream>\n#include <iomanip>\n'
for path in ['Util/Lerp.h', 'Template/Interpolated.h']:
    src += re.sub(r'^#.*$', '', read(path), flags=re.M) + '\n'
look = read('Character/Look/CharLook.cpp')
char = read('Character/Char.cpp')
body = read('Character/Look/BodyDrawInfo.cpp')
src += r'''
namespace ms {
namespace Stance { enum Id { NONE=-1,STAND1,STAND2,WALK1,WALK2,JUMP,PRONE,FLY,LADDER,ROPE }; }
struct Timer {void update(){};};
struct BodyDrawInfo {
 std::map<uint8_t,uint16_t> stance_delays[9];
 uint8_t nextframe(Stance::Id,uint8_t)const; uint16_t get_delay(Stance::Id,uint8_t)const;
};
struct Equips {int variant=0; Stance::Id adjust_stance(Stance::Id s)const{
 if(variant&&s==Stance::STAND1)return Stance::STAND2;
 if(variant&&s==Stance::WALK1)return Stance::WALK2;
 return s;
}};
struct CharLook {
 void* action=nullptr; Equips equips; BodyDrawInfo drawinfo;
 Nominal<Stance::Id> stance; Nominal<uint8_t> stframe,expression,expframe;
 uint16_t stelapsed=0; Timer alerted,expcooldown;
 bool update(uint16_t);void set_stance(Stance::Id);
 uint16_t get_delay(Stance::Id,uint8_t)const;uint8_t getnextframe(Stance::Id,uint8_t)const;
};
struct Char {
 enum State {STAND,WALK,FALL,PRONE,SWIM,LADDER,ROPE}; State state=STAND;
 bool attacking=false; struct {double hspeed=0,vspeed=0;} phobj;
 float get_real_attackspeed()const{return 1;} float get_stancespeed()const;
};
namespace Constants {constexpr uint16_t TIMESTEP=8;}
'''
for signature in ['uint8_t BodyDrawInfo::nextframe', 'uint16_t BodyDrawInfo::get_delay']:
    src += block(body, signature) + '\n'
for signature in ['void CharLook::set_stance', 'uint16_t CharLook::get_delay', 'uint8_t CharLook::getnextframe']:
    src += block(look, signature) + '\n'
# Retain the zero-step branch and entire action==nullptr body verbatim. The
# named-action and facial branches are outside this ordinary body clock fixture.
update = block(look, 'bool CharLook::update(uint16_t timestep)')
end_body = update.index('\n\t\telse\n\t\t{')
src += update[:end_body] + '\n return aniend;\n}\n'
src += block(char, 'float Char::get_stancespeed() const') + '\n'
conversion = char[char.index('\t\tuint16_t stancespeed = 0;'):char.index('\n\t\tafterimage.update')]
src += 'uint16_t timestep(float speed){\n' + conversion + '\nreturn stancespeed;\n}\n}\n'
src += r'''
int main(){
 std::cout<<"case,tick,stance,speed,interpolation,frame,elapsed\n"<<std::setprecision(10);
 for(int c=0;c<11;c++){
  ms::CharLook a;ms::Char ch;
  for(int st=0;st<9;st++){
   if(c==7) a.drawinfo.stance_delays[st]={{0,1},{1,3},{2,5}};
   else if(st==0||st==1)a.drawinfo.stance_delays[st]={{0,100},{1,120},{2,180}};
   else if(st==2||st==3)a.drawinfo.stance_delays[st]={{0,100},{1,100},{2,100},{3,100}};
   else if(st==4||st==5)a.drawinfo.stance_delays[st]={{0,100}};
   else a.drawinfo.stance_delays[st]={{0,100},{1,120}};
  }
  for(int tick=0;tick<600;tick++){
   ms::Stance::Id st=ms::Stance::STAND1;ch.state=ms::Char::STAND;
   if(c==1||c==2||c==3||c==7){st=ms::Stance::WALK1;ch.state=ms::Char::WALK;}
   if(c==4){st=ms::Stance::LADDER;ch.state=ms::Char::LADDER;}
   if(c==5){st=ms::Stance::ROPE;ch.state=ms::Char::ROPE;}
   if(c==6)st=ms::Stance::FLY;
   if(c==8)st=static_cast<ms::Stance::Id>((tick/37)%9);
   if(c==10){st=ms::Stance::WALK1;ch.state=ms::Char::WALK;a.equips.variant=(tick/75)%2;}
   ch.phobj.hspeed=c==1?(tick%100)*0.011:c==2?1.2-(tick%100)*0.012:
    c==3?(tick%4==0?.12499999:tick%4==1?.125:tick%4==2?.24999999:.25):.83;
   ch.phobj.vspeed=(tick%100<40?1.4:tick%100<70?0:-1);
   a.set_stance(st);float speed=ch.get_stancespeed();a.update(ms::timestep(speed));
   for(float t:{0.f,.25f,.5f,.75f,1.f})
    std::cout<<c<<','<<tick<<','<<a.stance.get()<<','<<speed<<','<<t<<','<<(int)a.stframe.get(t)<<','<<a.stelapsed<<'\n';
  }
 }
}
'''
(out / 'reference.cpp').write_text(src, encoding='utf-8')
subprocess.run([a.compiler, '-std=c++17', '-O0', str(out/'reference.cpp'), '-o', str(out/'reference.exe')], check=True)
result = subprocess.check_output([str(out/'reference.exe')])
fixture = root / 'Assets/Scripts/Tests/GameLogic/Fixtures/HeavenStanceAnimation.csv'
fixture.write_bytes(result)
meta = Path(str(fixture)+'.meta')
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
(root/'Tools/HeavenStanceReference-provenance.json').write_text(json.dumps({
    'source':str(a.source), 'head':subprocess.check_output(['git','-C',str(a.source),'rev-parse','HEAD']).decode().strip(),
    'sha256':used, 'rows':len(result.splitlines())-1,
    'fixtureSha256':hashlib.sha256(result).hexdigest(),
    'scope':'Unmodified CharLook ordinary-body/zero-step branches, set_stance and delay/frame wrappers; BodyDrawInfo delay/nextframe; Char speed selection/conversion; original Nominal. Synthetic delays, velocities and adjusted equipment stances. Excludes named actions, face/blink/alert timers, NX parsing and drawing.'
}, indent=2))
print('Generated', len(result.splitlines())-1, 'reference samples')
