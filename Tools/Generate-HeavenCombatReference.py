"""Compile original stat/damage methods with controlled inputs; no NX or game process.
python Tools/Generate-HeavenCombatReference.py --source C:/HeavenClient/MapleStory-Client
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
out = root / 'Temp/HeavenCombatReference'
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
source += r'''
}
int main() {
    using namespace ms;
    std::cout<<"weapon,job,str,dex,int,luk,watk,acc,level,mastery,percent,prone,moblevel,avoid,wdef,hitroll,damageroll,critroll,min,max,accuracy,hitchance,min_after,max_after,damage,critical,draws\n"<<std::setprecision(17);
    for(int weapon:{0,130,131,132,133,137,138,140,141,142,143,144,145,146,147,148,149,170})
    for(int job:{0,112,212,322,422,512,522,1110})
    for(int variant=0;variant<7;variant++) {
        CharStats c; c.job.id=job; c.weapontype=Weapon::Type(weapon);
        int str=variant==6?1200:77,dex=43,intel=123,luk=89,watk=variant==6?1200:88,acc=variant==4?0:25;
        int level=40, moblevel=variant==2?190:variant==3?20:47,avoid=variant==4?10000:12,wdef=variant==5?999:23;
        float mastery=variant==0?0:.6f, percent=variant==6?250.0f:.1f;
        bool prone=variant==1;
        c.set_total(EquipStat::STR,str);c.set_total(EquipStat::DEX,dex);c.set_total(EquipStat::INT,intel);
        c.set_total(EquipStat::LUK,luk);c.set_total(EquipStat::WATK,watk);c.set_total(EquipStat::ACC,acc);
        c.mastery=mastery;c.damagepercent=percent;c.close_totalstats();
        double minimum=c.mindamage,maximum=c.maxdamage;
        if(prone||weapon==137||weapon==138) {minimum/=10;maximum/=10;}
        Mob m;m.level=moblevel;m.avoid=avoid;m.wdef=wdef;
        double hitroll=variant==4?.99:.005,damageroll=variant==5?0:.4,critroll=variant==6?0:.01;
        m.randomizer.draws={hitroll,damageroll,critroll};
        int delta=std::max(0,m.level-level);
        double lo=m.calculate_mindamage(delta,minimum,false),hi=m.calculate_maxdamage(delta,maximum,false);
        float chance=m.calculate_hitchance(delta,c.get_total(EquipStat::ACC));
        auto hit=m.next_damage(lo,hi,chance,.05f);
        std::cout<<weapon<<','<<job<<','<<str<<','<<dex<<','<<intel<<','<<luk<<','<<watk<<','<<acc<<','<<level
            <<','<<mastery<<','<<percent<<','<<prone<<','<<moblevel<<','<<avoid<<','<<wdef<<','<<hitroll<<','<<damageroll<<','<<critroll
            <<','<<minimum<<','<<maximum<<','<<c.get_total(EquipStat::ACC)<<','<<chance<<','<<lo<<','<<hi<<','<<hit.first<<','<<hit.second<<','<<m.randomizer.index<<'\n';
    }
}
'''
cpp, exe = out / 'reference.cpp', out / 'reference.exe'
cpp.write_text(source, encoding='utf-8')
subprocess.run([args.compiler, '-std=c++17', '-O0', str(cpp), '-o', str(exe)], check=True)
result = subprocess.run([str(exe)], capture_output=True, text=True, check=True)
target = root / 'Assets/Scripts/Tests/GameLogic/Fixtures/HeavenCombatStats.csv'
target.write_text(result.stdout, encoding='utf-8')
manifest = {'source': str(args.source), 'source_sha256': used,
    'method': 'Unmodified CharStats caps/close/formulas, Job primary/secondary and Mob physical defense/hit/roll methods.',
    'adapter': 'Controlled stats, job/weapon IDs, level delta and unit random draws; reproduces Player::prepare_attack division in double precision. Does not reproduce an RNG seed/algorithm.',
    'excluded': ['NX import', 'network', 'magic/skill attacks', 'passive skill discovery', 'server-authoritative buffs and damage'],
    'rows': len(result.stdout.splitlines()) - 1, 'trace_sha256': hashlib.sha256(target.read_bytes()).hexdigest()}
(root / 'Tools/HeavenCombatReference-provenance.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
print('Recorded', manifest['rows'], 'source combat cases:', target)
