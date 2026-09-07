"""Compile HeavenClient passive registry, conditions, job ancestry and stat setters.
python Tools/Generate-HeavenPassiveReference.py --source C:/HeavenClient/MapleStory-Client
"""
import argparse, hashlib, json, re, subprocess
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--source',required=True,type=Path);parser.add_argument('--compiler',default='g++');args=parser.parse_args()
root=Path(__file__).resolve().parent.parent;out=root/'Temp/HeavenPassiveReference';out.mkdir(parents=True,exist_ok=True);used={}
def read(path):
    data=(args.source/path).read_bytes();used[path]=hashlib.sha256(data).hexdigest();return data.decode('utf-8-sig')
def function(text,signature):
    start=text.index(signature);brace=text.index('{',start);end=brace+1;depth=1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]
source=r"""
#include <algorithm>
#include <cstdint>
#include <iomanip>
#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <unordered_map>
namespace nl { struct node { int32_t value=0; std::unordered_map<std::string,node> children;
    node& operator[](const char* s){return children[s];} node& operator[](const std::string& s){return children[s];} node& operator[](int32_t i){return children[std::to_string(i)];}
    operator int32_t()const{return value;} node& operator=(int32_t i){value=i;return *this;}
}; namespace nx { node Skill; } }
namespace ms {
namespace string_format { std::string extend_id(int id,int width){std::ostringstream s;s<<std::setfill('0')<<std::setw(width)<<id;return s.str();} }
"""
for name,path,pattern in [('Weapon','Character/Inventory/Weapon.h',r'enum Type\s*\{.*?\};'),('EquipStat','Character/EquipStat.h',r'enum Id\s*\{.*?\};'),('SkillId','Character/SkillId.h',r'enum Id\s*:\s*uint32_t\s*\{.*?\};')]:
    source+='namespace '+name+'{'+re.search(pattern,read(path),re.S)[0]+'}\n'
source+=re.search(r'const std::unordered_map.*?\};',read('Character/StatCaps.h'),re.S)[0]+'\n'
source+=r"""
namespace MapleStat {enum Id {HP};}
struct Job {
    enum Level {BEGINNER,FIRST,SECOND,THIRD,FOURTH};uint16_t id=0;Level level=BEGINNER;std::string name;
    static std::string get_name(uint16_t){return "";} void change_job(uint16_t);bool is_sub_job(uint16_t)const;
    bool can_use(int32_t)const;uint16_t get_subjob(Level)const;
};
struct CharStats {
    Job job;Weapon::Type weapon;int hp=100;float mastery=0,damagepercent=0,reducedamage=0;
    std::unordered_map<EquipStat::Id,int32_t> totalstats;
    int32_t get_total(EquipStat::Id s)const{auto it=totalstats.find(s);return it==totalstats.end()?0:it->second;}
    int32_t get_stat(MapleStat::Id)const{return hp;} const Job& get_job()const{return job;} Weapon::Type get_weapontype()const{return weapon;}
    void set_total(EquipStat::Id,int32_t);void add_value(EquipStat::Id,int32_t);void set_mastery(float);void set_damagepercent(float);void set_reducedamage(float);
};
"""
for path,signatures in [('Character/Job.cpp',['void Job::change_job','bool Job::is_sub_job','bool Job::can_use','uint16_t Job::get_subjob']),('Character/CharStats.cpp',['void CharStats::set_total','void CharStats::add_value','void CharStats::set_mastery','void CharStats::set_damagepercent','void CharStats::set_reducedamage'])]:
    text=read(path)
    for signature in signatures:source+=function(text,signature)+'\n'
source+='}\n'
h=read('Character/PassiveBuffs.h');source+=h[h.index('namespace ms'):]
# Forward declaration lets GCC resolve the one-weapon overload used by the unmodified two-weapon template.
source+='namespace ms {template<Weapon::Type W1> bool f_is_applicable(CharStats&,nl::node);}\n'
c=read('Character/PassiveBuffs.cpp');source+=c[c.index('namespace ms'):]
source+=r"""
int main(){using namespace ms;PassiveBuffs buffs;
std::cout<<"id,job,weapon,hp,watk,matk,acc,avoid,mastery,damagepercent,reduction,canuse\n"<<std::setprecision(17);
for(int id:{12,1100000,1100001,1200000,1200001,1300000,1300001,1120004,1220005,1320005,1320006,1000000})
for(int job:{0,100,110,111,112,120,121,122,130,131,132,200,1110})
for(int weapon:{0,130,131,132,140,141,142,143,144})
for(int hp:{49,50,51}){
CharStats s;s.job.change_job(job);s.weapon=Weapon::Type(weapon);s.hp=hp;s.set_total(EquipStat::HP,101);
std::string key=string_format::extend_id(id,7);auto& level=nl::nx::Skill[key.substr(0,3)+".img"]["skill"][key]["level"][20];
level["x"]=id==1320006?50:(id==1120004||id==1220005||id==1320005)?850:20;level["y"]=40;level["z"]=20;level["mastery"]=10;level["damage"]=100;
buffs.apply_buff(s,id,20);
std::cout<<id<<','<<job<<','<<weapon<<','<<hp<<','<<s.get_total(EquipStat::WATK)<<','<<s.get_total(EquipStat::MAGIC)<<','<<s.get_total(EquipStat::ACC)<<','<<s.get_total(EquipStat::AVOID)<<','<<s.mastery<<','<<s.damagepercent<<','<<s.reducedamage<<','<<s.job.can_use(id)<<'\n';
}}
"""
cpp=out/'reference.cpp';exe=out/'reference.exe';cpp.write_text(source,encoding='utf-8')
subprocess.run([args.compiler,'-std=c++17','-O0',str(cpp),'-o',str(exe)],check=True)
result=subprocess.run([str(exe)],capture_output=True,text=True,check=True)
fixture=root/'Assets/Scripts/Tests/GameLogic/Fixtures/HeavenPassiveStats.csv';fixture.write_text(result.stdout,encoding='utf-8')
manifest={'source':str(args.source),'source_sha256':used,'rows':len(result.stdout.splitlines())-1,'trace_sha256':hashlib.sha256(fixture.read_bytes()).hexdigest(),'method':'Unmodified PassiveBuffs registry/handlers, Job ancestry and CharStats additive/mastery/reduction setters.','adapter':'Synthetic NX level node and HP/stat container; 12 IDs, 13 jobs, 9 weapons and 3 HP thresholds.','excluded':['NX import','network skill books/AP/SP','passives absent from source registry','server damage authority']}
(root/'Tools/HeavenPassiveReference-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('Generated',manifest['rows'],'passive reference cases')
