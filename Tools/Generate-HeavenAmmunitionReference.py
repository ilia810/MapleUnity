"""Compile the original inventory ammunition selection and attack-delay methods.
python Tools/Generate-HeavenAmmunitionReference.py --source C:/HeavenClient/MapleStory-Client
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
out = root / 'Temp/HeavenAmmunitionReference'
out.mkdir(parents=True, exist_ok=True)
used = {}
def read(path):
    data = (args.source/path).read_bytes(); used[path] = hashlib.sha256(data).hexdigest()
    return data.decode('utf-8-sig')
def function(text, signature):
    start = text.index(signature); brace = text.index('{', start); end, depth = brace+1, 1
    while depth:
        if text[end] == '{': depth += 1
        elif text[end] == '}': depth -= 1
        end += 1
    return text[start:end]

source = '#include <cstdint>\n#include <map>\n#include <unordered_map>\n#include <iostream>\n#include <iomanip>\n#include <cstddef>\nnamespace ms {\n'
source += 'namespace Weapon {'+re.search(r'enum Type\s*\{.*?\};',read('Character/Inventory/Weapon.h'),re.S)[0]+'}\n'
source += 'namespace InventoryType {'+re.search(r'enum Id\s*:\s*int8_t\s*\{.*?\};',read('Character/Inventory/InventoryType.h'),re.S)[0]+'}\n'
source += r'''
namespace EquipStat {enum Id { WATK };}
struct Equip { int get_stat(EquipStat::Id)const{return 0;} };
struct BulletData {
    int id; static BulletData get(int id){return {id};}
    int get_watk()const {return id/1000==2070?15+2*(id%1000):id/1000==2330?10:id%1000;}
};
struct Inventory {
    struct Slot {int unique_id,item_id,count;bool cash=false;};
    std::map<InventoryType::Id,std::map<int16_t,Slot>> inventories;
    std::unordered_map<int,Equip> equips;
    std::unordered_map<EquipStat::Id,int> totalstats;
    int16_t bulletslot=0;
    int get_bulletid()const {return bulletslot?inventories.at(InventoryType::USE).at(bulletslot).item_id:0;}
    void recalc_stats(Weapon::Type);
};
struct Afterimage {uint8_t get_first_frame()const{return 0;}};
struct Look {uint16_t get_attackdelay(size_t,uint8_t)const{return 240;}};
struct Char {int8_t speed=5;Afterimage afterimage;Look look;
int8_t get_integer_attackspeed()const{return speed;} float get_real_attackspeed()const;uint16_t get_attackdelay(size_t)const;};
'''
source += function(read('Character/Inventory/Inventory.cpp'),'void Inventory::recalc_stats(')+'\n'
char = read('Character/Char.cpp')
source += function(char,'float Char::get_real_attackspeed(')+'\n'+function(char,'uint16_t Char::get_attackdelay(')+'\n'
read('Gameplay/Combat/RegularAttack.cpp'); read('Character/Look/BodyDrawInfo.h'); read('Character/Look/CharLook.cpp')
source += r'''
}
int main(int argc,char**) {
using namespace ms;std::cout<<std::setprecision(17);
if(argc>1){std::cout<<"speed,delay,advance\n";for(int speed=0;speed<=15;speed++){Char c;c.speed=speed;std::cout<<speed<<','<<c.get_attackdelay(0)<<','<<int(uint16_t(8*c.get_real_attackspeed()))<<'\n';}return 0;}
std::cout<<"weapon,scenario,selected,attack_bonus\n";
for(int weapon:{0,130,137,145,146,147,148,149})for(int variant=0;variant<6;variant++){
 Inventory i;auto& slots=i.inventories[InventoryType::USE];
 slots[1]={1,2000000,20};slots[3]={2,2060001,7};slots[4]={3,2060000,9};slots[5]={4,2061000,8};slots[8]={5,2070001,2};slots[10]={6,2070000,9};slots[11]={7,2330000,1};
 if(variant==1){slots[3].count=0;slots[8].count=0;slots[11].count=0;}
 if(variant==2){slots.erase(3);slots[2]={8,2060000,2};slots[7]={9,2070000,3};}
 if(variant==3){slots.clear();}
 if(variant==4){slots[2]={10,2060001,0};slots[6]={11,2070001,0};}
 if(variant==5){slots.erase(3);slots.erase(4);slots.erase(5);slots.erase(8);slots.erase(10);slots.erase(11);}
 i.recalc_stats(Weapon::Type(weapon));std::cout<<weapon<<','<<variant<<','<<i.get_bulletid()<<','<<i.totalstats[EquipStat::WATK]<<'\n';
}}
'''
cpp, exe = out/'reference.cpp', out/'reference.exe'
cpp.write_text(source,encoding='utf-8')
subprocess.run([args.compiler,'-std=c++17','-O0',str(cpp),'-o',str(exe)],check=True)
fixtures = {}
for name, flags in [('HeavenAmmunition',[]),('HeavenGunDelay',['gun'])]:
    result = subprocess.run([str(exe)]+flags,capture_output=True,text=True,check=True)
    target=root/('Assets/Scripts/Tests/GameLogic/Fixtures/'+name+'.csv');target.write_text(result.stdout,encoding='utf-8')
    fixtures[name]={'rows':len(result.stdout.splitlines())-1,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
    print(name,fixtures[name]['rows'],'cases')
(root/'Tools/HeavenAmmunitionReference-provenance.json').write_text(json.dumps({
    'source':str(args.source),'source_sha256':used,'fixtures':fixtures,
    'method':'Unmodified Inventory::recalc_stats and Char::get_real_attackspeed/get_attackdelay.',
    'adapter':'Controlled ordered USE slots and BulletData attack values; no equipped items. The look returns the 240 ms first handgun attack marker independently verified from Character.nx.',
    'excluded':['Full inventory capacity/stack instances','Network consumption','Rendering','Ranged skill rules']},indent=2)+'\n',encoding='utf-8')
