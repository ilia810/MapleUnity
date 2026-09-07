"""Compile the inspected magic-defense and projectile paths with controlled inputs.
python Tools/Generate-HeavenMagicReference.py --source C:/HeavenClient/MapleStory-Client
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True, type=Path)
parser.add_argument('--compiler', default='g++')
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
out = root / 'Temp/HeavenMagicReference'
out.mkdir(parents=True, exist_ok=True)
used = {}

def read(path):
    data = (args.source / path).read_bytes()
    used[path] = hashlib.sha256(data).hexdigest()
    return data.decode('utf-8-sig')

def function(text, signature):
    start = text.index(signature)
    brace = text.index('{', start)
    end, depth = brace + 1, 1
    while depth:
        if text[end] == '{': depth += 1
        elif text[end] == '}': depth -= 1
        end += 1
    return text[start:end]

source = '''#include <algorithm>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <unordered_map>
#include <utility>
#include <vector>
namespace ms {
'''
source += 'namespace Weapon { ' + re.search(r'enum Type\s*\{.*?\};', read('Character/Inventory/Weapon.h'), re.S)[0] + ' }\n'
source += 'namespace EquipStat { ' + re.search(r'enum Id\s*\{.*?\};', read('Character/EquipStat.h'), re.S)[0] + ' }\n'
source += re.search(r'const std::unordered_map.*?\};', read('Character/StatCaps.h'), re.S)[0] + '\n'
source += '''
struct Job { int id=0; EquipStat::Id get_primary(Weapon::Type) const; EquipStat::Id get_secondary(Weapon::Type) const; };
struct CharStats {
    Job job; Weapon::Type weapontype;
    std::unordered_map<EquipStat::Id,int32_t> totalstats;
    std::unordered_map<EquipStat::Id,float> percentages;
    float mastery=0, damagepercent=0;
    int32_t mindamage=0,maxdamage=0;
    int32_t get_total(EquipStat::Id s) const { auto i=totalstats.find(s); return i==totalstats.end()?0:i->second; }
    void set_total(EquipStat::Id,int32_t);
    int32_t calculateaccuracy() const; int32_t get_primary_stat() const; int32_t get_secondary_stat() const;
    float get_multiplier() const; void close_totalstats();
};
struct Randomizer {
    std::vector<double> draws; mutable int index=0;
    template<class T> T next_real(T from,T to) const { if(from>=to) return from; return from+T(draws.at(index++))*(to-from); }
    bool below(float p) const { return next_real(0.0f,1.0f)<p; }
};
struct Mob {
    int level=0,avoid=0,wdef=0,mdef=0; Randomizer randomizer;
    float calculate_hitchance(int16_t,int32_t) const;
    double calculate_mindamage(int16_t,double,bool) const;
    double calculate_maxdamage(int16_t,double,bool) const;
    std::pair<int32_t,bool> next_damage(double,double,float,float) const;
};
'''
for path, signatures in [
    ('Character/Job.cpp', ['EquipStat::Id Job::get_primary', 'EquipStat::Id Job::get_secondary']),
    ('Character/CharStats.cpp', ['void CharStats::set_total', 'void CharStats::close_totalstats',
        'int32_t CharStats::calculateaccuracy', 'int32_t CharStats::get_primary_stat', 'int32_t CharStats::get_secondary_stat', 'float CharStats::get_multiplier']),
    ('Gameplay/MapleMap/Mob.cpp', ['float Mob::calculate_hitchance', 'double Mob::calculate_mindamage',
        'double Mob::calculate_maxdamage', 'std::pair<int32_t, bool> Mob::next_damage'])]:
    data = read(path)
    for signature in signatures: source += function(data, signature) + '\n'
read('Character/Player.cpp')
read('Util/Randomizer.h')

physics = read('Gameplay/Physics/PhysicsObject.h')
source += """
template<class T> struct Point { T a,b; Point(T x=0,T y=0):a(x),b(y){} T x()const{return a;} T y()const{return b;} };
template<class T> struct Linear { T value=0; T get()const{return value;} void set(T v){value=v;} void operator+=(T v){value+=v;} };
struct Animation { void update(){} };
struct MovingObject { Linear<double> x,y; double hspeed=0,vspeed=0;
"""
for signature in ['void move()', 'void set_x(double', 'void set_y(double', 'double crnt_x()', 'double crnt_y()', 'int32_t get_x()', 'int32_t get_y()']:
    source += function(physics,signature)+'\n'
source += """};
struct Bullet { Animation animation; MovingObject moveobj; bool flip=false;
Bullet(Animation,Point<int16_t>,bool); bool settarget(Point<int16_t>); bool update(Point<int16_t>); };
"""
bullet = read('Gameplay/Combat/Bullet.cpp')
for signature in ['Bullet::Bullet(', 'bool Bullet::settarget(', 'bool Bullet::update(']:
    source += function(bullet,signature)+'\n'
source += r"""
}
int main(int argc,char**) {
 using namespace ms;
 std::cout<<std::setprecision(17);
 if(argc>1) {
  std::cout<<"scenario,tick,originX,originY,right,targetX,targetY,liveX,x,y,arrived,flip\n";
  int scenario=0;
  for(int right:{0,1}) for(int distance:{9,39,40,125,222,400}) for(int moving:{0,1}) {
   int sign=right?1:-1,ox=-37,oy=54,tx=ox+sign*distance,ty=oy-41,live=tx;
   Bullet b({}, {int16_t(ox),int16_t(oy)}, !right); bool arrived=b.settarget({int16_t(tx),int16_t(ty)});
   for(int tick=0;tick<160;tick++) {
    if(tick) {live=tx+(moving?sign*std::min(18,tick):0);arrived=b.update({int16_t(live),int16_t(ty)});}
    std::cout<<scenario<<','<<tick<<','<<ox<<','<<oy<<','<<right<<','<<tx<<','<<ty<<','<<live<<','<<b.moveobj.crnt_x()<<','<<b.moveobj.crnt_y()<<','<<arrived<<','<<b.flip<<'\n';
    if(arrived)break;
   }
   scenario++;
  }
  return 0;
 }
 std::cout<<"weapon,int,luk,watk,matk,skill_mad,level,moblevel,avoid,wdef,mdef,hitroll,damageroll,critroll,min,max,accuracy,hitchance,min_after,max_after,damage,critical,draws\n";
 for(int weapon:{130,137,138}) for(int intel:{15,80,1200}) for(int matk:{0,500}) for(int mad:{11,55}) for(int variant=0;variant<5;variant++) {
  CharStats c; c.job.id=200;c.weapontype=Weapon::Type(weapon);
  int luk=20,watk=variant==1?99:15,level=8,moblevel=variant==2?75:1,avoid=variant==3?900:1,wdef=777,mdef=variant==4?900:3;
  c.set_total(EquipStat::STR,15);c.set_total(EquipStat::DEX,15);c.set_total(EquipStat::INT,intel);c.set_total(EquipStat::LUK,luk);
  c.set_total(EquipStat::WATK,watk);c.set_total(EquipStat::MAGIC,matk);c.set_total(EquipStat::ACC,0);c.close_totalstats();
  double hitroll=.5,damageroll=.5,critroll=variant==1?0:.5;
  Mob m;m.level=moblevel;m.avoid=avoid;m.wdef=wdef;m.mdef=mdef;m.randomizer.draws={hitroll,damageroll,critroll};
  int delta=std::max(0,moblevel-level);double lo=m.calculate_mindamage(delta,c.mindamage,true),hi=m.calculate_maxdamage(delta,c.maxdamage,true);
  float chance=m.calculate_hitchance(delta,c.get_total(EquipStat::ACC));auto hit=m.next_damage(lo,hi,chance,.05f);
  std::cout<<weapon<<','<<intel<<','<<luk<<','<<watk<<','<<matk<<','<<mad<<','<<level<<','<<moblevel<<','<<avoid<<','<<wdef<<','<<mdef<<','<<hitroll<<','<<damageroll<<','<<critroll<<','<<c.mindamage<<','<<c.maxdamage<<','<<c.get_total(EquipStat::ACC)<<','<<chance<<','<<lo<<','<<hi<<','<<hit.first<<','<<hit.second<<','<<m.randomizer.index<<'\n';
 }
}
"""
source=source.replace('#include <algorithm>','#include <algorithm>\n#include <cmath>')
cpp,exe=out/'reference.cpp',out/'reference.exe'
cpp.write_text(source,encoding='utf-8')
subprocess.run([args.compiler,'-std=c++17','-O0',str(cpp),'-o',str(exe)],check=True)
outputs={}
for name,arguments in [('HeavenMagicStats',[]),('HeavenProjectiles',['bullets'])]:
    result=subprocess.run([str(exe)]+arguments,capture_output=True,text=True,check=True)
    target=root/('Assets/Scripts/Tests/GameLogic/Fixtures/'+name+'.csv')
    target.write_text(result.stdout,encoding='utf-8')
    outputs[name]={'rows':len(result.stdout.splitlines())-1,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
    print(name,outputs[name]['rows'],'rows')
manifest={'source':str(args.source),'source_sha256':used,'fixtures':outputs,
'method':'Unmodified CharStats/Job/Mob math and Bullet constructor, settarget, update with MovingObject move/get/set methods.',
'adapter':'Synthetic stats and deterministic random draws; Point/Linear storage and no-op animation update. Magic attack preparation copies source bounds and selects magic defense. Skill mad and character MAGIC are deliberately not applied to bounds, matching the unused fields in the inspected pipeline.',
'excluded':['Retail magic formula correctness','NX import','network','animation rendering','RNG seed parity']}
read('Gameplay/Combat/Skill.cpp')
(root/'Tools/HeavenMagicReference-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
