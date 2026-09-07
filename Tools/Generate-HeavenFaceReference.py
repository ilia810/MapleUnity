"""Compile original facial clock/setter/lookup/interpolation with supplied face delays."""
import argparse, hashlib, json, re, subprocess, uuid
from pathlib import Path
p = argparse.ArgumentParser(); p.add_argument('--source', type=Path, required=True); p.add_argument('--compiler', default='g++'); a = p.parse_args()
root = Path(__file__).resolve().parent.parent; out = root/'Temp/HeavenFaceReference'; out.mkdir(parents=True, exist_ok=True); used = {}
def read(name):
    raw = (a.source/name).read_bytes(); used[name] = hashlib.sha256(raw).hexdigest()
    return raw.decode('utf-8-sig').replace('\r\n','\n')
def block(text, signature):
    start = text.index(signature); end = text.index('{', start)+1; depth = 1
    while depth:
        if text[end]=='{': depth += 1
        elif text[end]=='}': depth -= 1
        end += 1
    return text[start:end]
src = '#include <cstdint>\n#include <map>\n#include <iostream>\n#include <iomanip>\nnamespace ms {namespace Constants {constexpr uint16_t TIMESTEP=8;}}\n'
for name in ['Util/Lerp.h','Template/Interpolated.h','Util/TimedBool.h']:
    src += re.sub(r'^#.*$','',read(name),flags=re.M)+'\n'
look = read('Character/Look/CharLook.cpp'); face = read('Character/Look/Face.cpp'); header = read('Character/Look/Face.h')
src += '''namespace ms {
namespace Expression {enum Id {DEFAULT,BLINK,HIT,SMILE,TROUBLED,CRY,ANGRY,BEWILDERED,STUNNED,BLAZE,BOWING,CHEERS,CHU,DAM,DESPAIR,GLITTER,HOT,HUM,LOVE,OOPS,PAIN,SHINE,VOMIT,WINK,LENGTH};}
struct Face {struct Frame {uint16_t delay;};std::map<uint8_t,Frame> expressions[Expression::LENGTH];
uint8_t nextframe(Expression::Id,uint8_t)const;int16_t get_delay(Expression::Id,uint8_t)const;};
struct CharLook {Nominal<Expression::Id> expression;Nominal<uint8_t> expframe,stance,stframe;
uint16_t expelapsed=0;TimedBool expcooldown,alerted;Face* face=nullptr;
void set_expression(Expression::Id);bool update(uint16_t);};
'''
for signature in ['uint8_t Face::nextframe','int16_t Face::get_delay']: src += block(face,signature)+'\n'
src += block(look,'void CharLook::set_expression')+'\n'
update = block(look,'bool CharLook::update(uint16_t timestep)')
src += update[:update.index('\n\t\tbool aniend = false;')]+'\n bool aniend=false;\n'+update[update.index('\n\t\t// Add null check for face pointer'):]
src += r'''
}
ms::Face makeface(int id){
 ms::Face f;
 for(int exp=0;exp<ms::Expression::LENGTH;exp++){
  if(id==1)f.expressions[exp]={{0,{1}},{1,{3}},{2,{5}}};
  else if(id==2)f.expressions[exp]={{0,{65535}}};
  else if(id==3)f.expressions[exp]={{0,{100}}};
  else if(exp==ms::Expression::DEFAULT)f.expressions[exp]={{0,{2500}}};
  else if(exp==ms::Expression::BLINK)f.expressions[exp]={{0,{100}},{1,{120}},{2,{100}}};
  else f.expressions[exp]={{0,{100}},{1,{250}},{2,{170}}};
 }
 if(id==4)f.expressions[ms::Expression::BLINK].clear();
 return f;
}
int main(){
 std::cout<<"case,tick,step,command,profile,face,interpolation,expression,frame,elapsed,cooldown\n"<<std::setprecision(10);
 for(int c=0;c<12;c++){
  ms::CharLook look;int profile=c==8?1:c==9?2:c==6?4:0;auto face=makeface(profile);look.face=c==7?nullptr:&face;
  for(int tick=0;tick<1000;tick++){
   int command=-1;int step=c==1?tick%12:c==2?(tick%200<100?0:11):c==3?12:8;
   if(c==4&&(tick==10||tick==11||tick==100||tick==634||tick==635||tick==636))command=tick%2?ms::Expression::HIT:ms::Expression::SMILE;
   if(c==5&&(tick==0||tick==1||tick==2||tick==630))command=tick==0?ms::Expression::DEFAULT:tick==630?ms::Expression::CRY:ms::Expression::HIT;
   if(c==7&&tick==10)command=ms::Expression::SMILE;
   if(c==10&&tick==1)command=ms::Expression::SMILE;
   if(c==10&&tick==30){profile=3;face=makeface(profile);}
   if(c==11&&(tick==300||tick==925))command=ms::Expression::ANGRY;
   if(c==11&&tick>350&&tick<550)step=0;
   if(command>=0)look.set_expression(static_cast<ms::Expression::Id>(command));
   look.update(step);
   for(float t:{0.f,.25f,.5f,.75f,1.f})
    std::cout<<c<<','<<tick<<','<<step<<','<<command<<','<<profile<<','<<(look.face?1:0)<<','<<t<<','<<look.expression.get(t)<<','<<(int)look.expframe.get(t)<<','<<look.expelapsed<<','<<(bool)look.expcooldown<<'\n';
  }
 }
}
'''
(out/'reference.cpp').write_text(src,encoding='utf-8')
subprocess.run([a.compiler,'-std=c++17','-O0',str(out/'reference.cpp'),'-o',str(out/'reference.exe')],check=True)
result = subprocess.check_output([str(out/'reference.exe')]); fixture = root/'Assets/Scripts/Tests/GameLogic/Fixtures/HeavenFaceAnimation.csv';fixture.write_bytes(result)
meta = Path(str(fixture)+'.meta')
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
(root/'Tools/HeavenFaceReference-provenance.json').write_text(json.dumps({'source':str(a.source),'head':subprocess.check_output(['git','-C',str(a.source),'rev-parse','HEAD']).decode().strip(),'sha256':used,'rows':len(result.splitlines())-1,'fixtureSha256':hashlib.sha256(result).hexdigest(),'scope':'Unmodified CharLook zero-step, cooldown update and face branches/set_expression, Face delay/nextframe and original Nominal/TimedBool. Synthetic face delays and supplied body timesteps; excludes body/named-action playback, bitmap drawing, NX frame parsing and alerted overlay.'},indent=2))
print('Generated',len(result.splitlines())-1,'reference samples')
