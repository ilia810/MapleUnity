"""Compile the inspected generic ranged-skill rules and body-action delay methods.

python Tools/Generate-HeavenRangedSkillReference.py --source C:/HeavenClient/MapleStory-Client
The four missing source attack registrations are an explicitly excluded extension.
"""
import argparse
import hashlib
import json
import re
import subprocess
import uuid
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True, type=Path)
parser.add_argument('--compiler', default='g++')
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
out = root / 'Temp/HeavenRangedSkillReference'
out.mkdir(parents=True, exist_ok=True)
used = {}


def read(path):
    data = (args.source / path).read_bytes()
    used[path] = hashlib.sha256(data).hexdigest()
    return data.decode('utf-8-sig')


def block(text, signature):
    start = text.index(signature)
    brace = text.index('{', start)
    end, depth = brace + 1, 1
    while depth:
        if text[end] == '{':
            depth += 1
        elif text[end] == '}':
            depth -= 1
        end += 1
    return text[start:end]


source = r'''
#include <algorithm>
#include <cstdint>
#include <cstddef>
#include <iostream>
#include <iomanip>
#include <map>
#include <string>
#include <unordered_map>
#include <vector>
template<class T> struct Point {T x=0,y=0;};
template<class T> struct Rectangle {
    T l,r,t,b;
    constexpr Rectangle(T left=0,T right=0,T top=0,T bottom=0):l(left),r(right),t(top),b(bottom){}
    bool empty()const{return l==r && t==b;}
    T left()const{return l;}
};
namespace nl {
struct node {
    int64_t number=0;std::string text;Point<int16_t> point;
    std::map<std::string,node> children;
    node operator[](const std::string& key)const{auto i=children.find(key);return i==children.end()?node{}:i->second;}
    node operator[](const char* key)const{return operator[](std::string(key));}
    operator int64_t()const{return number;}
    operator std::string()const{return text;}
    operator Point<int16_t>()const{return point;}
};
}
namespace ms {
'''
source += 'namespace Weapon {' + re.search(r'enum Type\s*\{.*?\};', read('Character/Inventory/Weapon.h'), re.S)[0] + '}\n'
source += 'namespace SkillId { enum { THREE_SNAILS = 1000 }; }\n'
source += 'struct SpecialMove {' + re.search(r'enum ForbidReason\s*\{.*?\};', read('Gameplay/Combat/SpecialMove.h'), re.S)[0] + '};\n'
source += block(read('Gameplay/Combat/Attack.h'), 'struct Attack\n') + ';\n'
source += 'struct SkillData {\n' + block(read('Data/SkillData.h'), 'struct Stats') + ';\n'
source += r'''
    Stats stats{1,0,0,0,1,1,1,1,0,0,1,0,0,1,{}};
    Weapon::Type required=Weapon::NONE;
    static SkillData current;
    static const SkillData& get(int32_t){return current;}
    const Stats& get_stats(int32_t)const{return stats;}
    int32_t get_masterlevel()const{return 20;}
    Weapon::Type get_required_weapon()const{return required;}
};
SkillData SkillData::current;
struct Job {bool allowed=true;bool can_use(int32_t)const{return allowed;}};
namespace Stance {
enum Id {NONE,SHOOT2,STABO1};
Id by_string(const std::string& s){return s=="shoot2"?SHOOT2:s=="stabO1"?STABO1:NONE;}
}
'''
source += block(read('Character/Look/BodyDrawInfo.h'), 'class BodyAction') + ';\n'
source += r'''
struct BodyDrawInfo {
    std::unordered_map<std::string,std::vector<uint16_t>> attack_delays;
    uint16_t get_attackdelay(std::string,size_t)const;
};
struct StanceHolder {Stance::Id get()const{return Stance::SHOOT2;}};
struct CharLook {
    const BodyAction* action=nullptr;
    std::string actionstr="doublefire";
    BodyDrawInfo drawinfo;StanceHolder stance;
    std::vector<uint16_t> ordinary_delays{170,160,120};
    uint16_t get_delay(Stance::Id,uint8_t f)const{return ordinary_delays.at(f);}
    uint16_t get_attackdelay(size_t,uint8_t)const;
    uint8_t get_stance()const{return Stance::SHOOT2;}
};
struct Afterimage {
    uint8_t first=2;
    uint8_t get_first_frame()const{return first;}
    Rectangle<int16_t> get_range()const{return {-100,-20,-40,20};}
};
struct Char {
    int8_t speed=5;int32_t skilllevel=1;
    CharLook look;Afterimage afterimage;
    int8_t get_integer_attackspeed()const{return speed;}
    int32_t get_skilllevel(int32_t)const{return skilllevel;}
    const CharLook& get_look()const{return look;}
    const Afterimage& get_afterimage()const{return afterimage;}
    float get_real_attackspeed()const;
    uint16_t get_attackdelay(size_t)const;
};
struct Skill : SpecialMove {
    int32_t skillid;bool projectile,overregular;
    Skill(int32_t id,bool ball,bool ordinary):skillid(id),projectile(ball),overregular(ordinary){}
    void apply_stats(const Char&,Attack&)const;
    SpecialMove::ForbidReason can_use(int32_t,Weapon::Type,const Job&,uint16_t,uint16_t,uint16_t)const;
};
'''
for path, signatures in [
    ('Gameplay/Combat/Skill.cpp', ['void Skill::apply_stats(', 'SpecialMove::ForbidReason Skill::can_use(']),
    ('Character/Look/BodyDrawInfo.cpp', ['uint16_t BodyDrawInfo::get_attackdelay(']),
    ('Character/Look/CharLook.cpp', ['uint16_t CharLook::get_attackdelay(']),
    ('Character/Char.cpp', ['float Char::get_real_attackspeed(', 'uint16_t Char::get_attackdelay(']),
]:
    text = read(path)
    for signature in signatures:
        source += block(text, signature) + '\n'

# These declarations select controlled metadata and storage only. They are not a
# substitute implementation of the extracted apply_stats/can_use/delay methods.
source += r'''
struct Metadata {int id,level,damage,bullets,mp,range;};
const std::vector<Metadata> metadata{
    {3001004,1,190,1,7,100},{3001004,10,220,1,10,100},{3001004,20,260,1,14,100},
    {3001005,1,92,2,10,100},{3001005,10,110,2,12,100},{3001005,20,130,2,16,100},
    {4001344,1,58,2,8,100},{4001344,10,100,2,11,100},{4001344,20,150,2,16,100},
    {5001003,1,81,2,4,245},{5001003,10,90,2,5,305},{5001003,20,110,2,7,380}
};
Skill configure(const Metadata& m) {
    SkillData::current.stats=SkillData::Stats(float(m.damage)/100,0,0,0,1,1,uint8_t(m.bullets),int16_t(m.bullets),0,m.mp,1,0,0,float(m.range)/100,{});
    // The original parser sees only Lucky Seven's weapon key: gun's NX key has a trailing space.
    SkillData::current.required=m.id==4001344?Weapon::CLAW:Weapon::NONE;
    return {m.id,m.id==3001004 || m.id==5001003,m.id!=5001003};
}
nl::node frame(const char* stance,int delay,int move=0) {
    nl::node n;n.children["action"].text=stance;n.children["delay"].number=delay;
    n.children["frame"].number=0;n.children["move"].point={int16_t(move),0};return n;
}
std::vector<BodyAction> doublefire() {
    return {BodyAction(frame("shoot2",90)),BodyAction(frame("stabO1",360,2)),BodyAction(frame("shoot2",0))};
}
void seed_markers(CharLook& look,const std::vector<BodyAction>& frames) {
    uint16_t attackdelay=0;
    for(const auto& action:frames) {
        // Same accumulator statements as BodyDrawInfo::init, with the decoded frame storage stubbed.
        if(action.isattackframe())look.drawinfo.attack_delays["doublefire"].push_back(attackdelay);
        attackdelay+=action.get_delay();
    }
}
}
int main(int argc,char** argv) {
    using namespace ms;std::cout<<std::setprecision(17);
    std::string mode=argc>1?argv[1]:"stats";
    if(mode=="stats") {
        std::cout<<"skill,level,attack_type,minimum_before,maximum_before,damage_percent,attack_count,bullet_count,incoming_bullet,has_ball,over_regular,minimum_after,maximum_after,hit_count,bullet_id,hrange,range_left,range_right,scaled_left\n";
        for(const auto& m:metadata)for(int type:{0,1,2})for(int hasammo:{0,1}) {
            Skill s=configure(m);Char c;c.skilllevel=m.level;
            int ammo=m.id==4001344?2070000:m.id==5001003?2330000:2060000;
            Attack a;a.type=Attack::Type(type);a.mindamage=12;a.maxdamage=26;
            a.bullet=hasammo?ammo:0;a.range={-400,-5,-50,50};a.critical=.05f;
            s.apply_stats(c,a);
            Rectangle<int16_t> range=a.range;const Attack& attack=a;
            // INSERT_SOURCE_SCALING_STATEMENT
            std::cout<<m.id<<','<<m.level<<','<<type<<",12,26,"<<m.damage<<",1,"<<m.bullets<<','<<(hasammo?ammo:0)<<','<<s.projectile<<','<<s.overregular<<','<<a.mindamage<<','<<a.maxdamage<<','<<int(a.hitcount)<<','<<a.bullet<<','<<a.hrange<<','<<a.range.l<<','<<a.range.r<<','<<hrange<<'\n';
        }
    } else if(mode=="costs") {
        std::cout<<"skill,metadata_level,scenario,level,job_allowed,weapon,required_weapon,hp,hp_cost,mp,mp_cost,bullets,bullet_cost,reason\n";
        for(const auto& m:metadata)for(int scenario=0;scenario<14;scenario++) {
            Skill s=configure(m);Job job;int level=m.level;
            int weapon=m.id==4001344?147:m.id==5001003?149:145;
            int hp=1,mp=m.mp,bullets=m.bullets;
            switch(scenario) {
                case 1:bullets=m.bullets-1;break;
                case 2:bullets=0;break;
                case 3:mp=m.mp-1;break;
                case 4:hp=0;break;
                case 5:job.allowed=false;break;
                case 6:level=0;break;
                case 7:level=21;break;
                case 8:weapon=146;break;
                case 9:weapon=130;bullets=0;break;
                case 10:SkillData::current.stats.bulletcost=0;bullets=0;break;
                case 11:SkillData::current.stats.bulletcost=3;bullets=2;break;
                case 12:SkillData::current.stats.bulletcost=3;bullets=3;break;
                case 13:SkillData::current.stats.hpcost=10;hp=10;mp=0;bullets=0;break;
            }
            auto& d=SkillData::current;
            auto result=s.can_use(level,Weapon::Type(weapon),job,uint16_t(hp),uint16_t(mp),uint16_t(bullets));
            std::cout<<m.id<<','<<m.level<<','<<scenario<<','<<level<<','<<job.allowed<<','<<weapon<<','<<int(d.required)<<','<<hp<<','<<d.stats.hpcost<<','<<mp<<','<<d.stats.mpcost<<','<<bullets<<','<<d.stats.bulletcost<<','<<int(result)<<'\n';
        }
    } else if(mode=="delays") {
        std::cout<<"action,speed,line,raw_delay,delay,advance\n";
        auto frames=doublefire();
        for(int action:{0,1})for(int speed=0;speed<16;speed++)for(int line=0;line<4;line++) {
            Char c;c.speed=int8_t(speed);seed_markers(c.look,frames);
            if(action==0)c.look.action=&frames[0];
            int raw=c.look.get_attackdelay(size_t(line),c.afterimage.first);
            std::cout<<(action==0?"doublefire":"ordinary")<<','<<speed<<','<<line<<','<<raw<<','<<c.get_attackdelay(size_t(line))<<','<<int(uint16_t(8*c.get_real_attackspeed()))<<'\n';
        }
    } else if(mode=="actions") {
        std::cout<<"action,frame,signed_delay,duration,attack_marker,elapsed_before,move_x,move_y\n";
        for(int action:{0,1}) {
            auto frames=action==0?doublefire():std::vector<BodyAction>{BodyAction(frame("shoot2",-240)),BodyAction(frame("stabO1",540,2)),BodyAction(frame("shoot2",0))};
            std::vector<int> raw=action==0?std::vector<int>{90,360,0}:std::vector<int>{-240,540,0};
            int elapsed=0;
            for(size_t i=0;i<frames.size();i++) {
                const auto& f=frames[i];auto move=f.get_move();
                std::cout<<(action==0?"doublefire":"handgun")<<','<<i<<','<<raw[i]<<','<<f.get_delay()<<','<<f.isattackframe()<<','<<elapsed<<','<<move.x<<','<<move.y<<'\n';
                elapsed+=f.get_delay();
            }
        }
    } else return 2;
}
'''

# Evaluate the original float product and integer cast at the combat range
# boundary, rather than reconstructing it with Python's double precision.
scaling = re.search(r'int16_t hrange = static_cast<int16_t>\(range\.left\(\) \* attack\.hrange\);',
                    read('Gameplay/Combat/Combat.cpp'))
if scaling is None:
    raise RuntimeError('Source Combat horizontal range scaling statement changed.')
source = source.replace('// INSERT_SOURCE_SCALING_STATEMENT', scaling[0])

# Record the registry/preparation/effect-selection context, even though these
# parts are intentionally not executed by this bounded generic-rule adapter.
for path in ['Data/SkillData.cpp', 'Character/Player.cpp', 'Gameplay/Combat/Combat.cpp',
             'Gameplay/Combat/SkillBullet.cpp', 'Character/SkillId.h', 'Template/TimedQueue.h']:
    read(path)
cpp, exe = out / 'reference.cpp', out / 'reference.exe'
cpp.write_text(source, encoding='utf-8')
subprocess.run([args.compiler, '-std=c++17', '-O0', str(cpp), '-o', str(exe)], check=True)
fixtures = {}
for name, mode in [('HeavenRangedSkillStats', 'stats'), ('HeavenRangedSkillCosts', 'costs'),
                   ('HeavenRangedSkillDelays', 'delays'), ('HeavenRangedSkillActions', 'actions')]:
    result = subprocess.run([str(exe), mode], capture_output=True, text=True, check=True)
    target = root / ('Assets/Scripts/Tests/GameLogic/Fixtures/' + name + '.csv')
    target.write_text(result.stdout, encoding='utf-8')
    meta = Path(str(target) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid5(uuid.NAMESPACE_URL, 'MapleUnity/Fixtures/' + name).hex + '\n', encoding='utf-8')
    fixtures[name] = {'rows': len(result.stdout.splitlines()) - 1,
                      'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}
    print(name, fixtures[name]['rows'], 'cases')

probe = root / 'Logs/ranged-skill-probe.txt'
manifest = {
    'source': str(args.source), 'source_sha256': used, 'fixtures': fixtures,
    'method': 'Unmodified Skill::apply_stats/can_use, SkillData::Stats and Attack storage declarations, BodyAction constructor/accessors, BodyDrawInfo::get_attackdelay, CharLook::get_attackdelay, Char::get_real_attackspeed/get_attackdelay; exact Combat::apply_move horizontal range scaling statement.',
    'adapter': [
        'Controlled NX values for levels1/10/20, prepared damage bounds12/26, job acceptance boolean, ordinary stance delays170/160/120 and afterimage firstframe2.',
        'Decoded named body frames are represented by a minimal nl::node stub. Marker accumulation uses the same statements as BodyDrawInfo::init with controlled storage.',
        'The SkillData registry stub directly supplies these otherwise unregistered four IDs; no claim of end-to-end source activation is made.',
        'Weapon requirements preserve the original loader result: Lucky Seven147; archer and gun NONE because archer has no weapon key and the gun key has a trailing space.',
        'Cost scenarios10/11/12 set explicit bulletConsume0/3 independently of bulletCount, and scenario13 sets a synthetic hpCon10 to verify source guard precedence.',
        'Stats fixtures invoke apply_stats directly for CLOSE/RANGED/MAGIC to show generic hit-count/range behavior. Public Player::can_use rejects prone before these helpers; that guard is outside this harness.',
        'Stats scaled_left is evaluated by the extracted int16_t(range.left() * attack.hrange) statement after Skill::apply_stats. The product rounds as C++ float before truncation, independently of Python/Mono expression evaluation.',
    ],
    'limitations': [
        'The source attack registry lacks all four IDs; local family-scoped enablement is an explicit Unity extension.',
        'No Lucky Seven-specific damage formula exists in the inspected source. These vectors verify generic source percentages, not retail Lucky Seven formula correctness.',
        'No NX importer, renderer, projectile flight, random damage roll, network debit, or local offline consumption is executed.',
        'The fixture tests Skill::can_use only; player busy/prone/climbing/cooldown and learned-skill entry guards require integration checks.',
    ],
    'reason_values': {'NONE': 0, 'WEAPONTYPE': 1, 'HPCOST': 2, 'MPCOST': 3, 'BULLETCOST': 4, 'COOLDOWN': 5, 'OTHER': 6},
}
if probe.exists():
    manifest['nx_probe_sha256'] = hashlib.sha256(probe.read_bytes()).hexdigest()
(root / 'Tools/HeavenRangedSkillReference-provenance.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
