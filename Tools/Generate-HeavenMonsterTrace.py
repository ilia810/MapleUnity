"""Compile selected, unmodified HeavenClient methods and record ground monster movement.
Usage: python Tools/Generate-HeavenMonsterTrace.py --source C:/HeavenClient/MapleStory-Client
Requires g++ on PATH. No NX, window, network, or running game is needed.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("--source", required=True, type=Path)
parser.add_argument("--compiler", default="g++")
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
out = root / "Temp/HeavenMonsterReference"
out.mkdir(parents=True, exist_ok=True)
used = {}

def read(path):
    data = (args.source / path).read_bytes()
    used[path] = hashlib.sha256(data).hexdigest()
    return data.decode("utf-8-sig")

def function(text, signature):
    start = text.index(signature)
    brace = text.index("{", start)
    depth = 1
    end = brace + 1
    while depth:
        if text[end] == "{": depth += 1
        elif text[end] == "}": depth -= 1
        end += 1
    return text[start:end]

def header(path):
    return re.sub(r"^\s*#.*$", "", read(path), flags=re.M)

source = """
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <iostream>
#include <iomanip>
#include <string>
#include <unordered_map>
#include <vector>
namespace nl { struct node { double x() const; double y() const; }; }
namespace ms {
template<class T> constexpr T clamp_value(T x,T a,T b) { return x<a?a:x>b?b:x; }
namespace Constants { constexpr uint16_t TIMESTEP = 8; }
}
"""
constants = read("Constants.h")
assert re.search(r"TIMESTEP\s*=\s*8\s*;", constants)
for path in ["Util/TimedBool.h", "Util/Lerp.h", "Template/Interpolated.h", "Template/Range.h",
             "Template/Point.h", "Gameplay/Physics/PhysicsObject.h",
             "Gameplay/Physics/Foothold.h", "Gameplay/Physics/FootholdTree.h"]:
    source += header(path)

fh = read("Gameplay/Physics/Foothold.cpp")
fh = re.sub(r"^\s*#.*$", "", fh, flags=re.M)
fh = fh.replace(function(fh, "Foothold::Foothold(nl::node"), "")
source += fh
tree = read("Gameplay/Physics/FootholdTree.cpp")
source += "\nnamespace ms {\n"
for sig in ["FootholdTree::FootholdTree()", "void FootholdTree::limit_movement",
            "void FootholdTree::update_fh", "const Foothold& FootholdTree::get_fh",
            "double FootholdTree::get_wall", "double FootholdTree::get_edge",
            "uint16_t FootholdTree::get_fhid_below", "void FootholdTree::add_foothold"]:
    source += function(tree, sig) + "\n"

physics = read("Gameplay/Physics/Physics.cpp")
source += "\n".join(re.findall(r"const double \w+ = [^;]+;", physics))
source += "\nstruct Physics { void move_normal(PhysicsObject&) const; };\n"
source += function(physics, "void Physics::move_normal") + "\n"

mob = read("Gameplay/MapleMap/Mob.cpp")
update = function(mob, "int8_t Mob::update")
move = update[update.index("case Stance::MOVE:"):update.index("case Stance::HIT:")]
hit = update[update.index("case Stance::HIT:"):update.index("case Stance::JUMP:")]
source += """
double source_force(float speed, bool flip, bool hit, bool ground) {
    struct { double hforce=0,vforce=0; bool onground; } phobj;
    phobj.onground=ground;
    bool canfly=false,canmove=true;
    float flyspeed=0;
    enum FlyDirection { STRAIGHT,UPWARDS,DOWNWARDS }; auto flydirection=STRAIGHT;
    enum class Stance { MOVE,HIT }; auto stance=hit?Stance::HIT:Stance::MOVE;
    switch(stance) {
""" + move + hit + """
    }
    return phobj.hforce;
}
struct TraceTree:FootholdTree {
 void bounds() {
  int lx=30000,rx=-30000,ty=30000,by=-30000;
  for(const auto& i:footholds) {
   lx=std::min(lx,int(i.second.l()));rx=std::max(rx,int(i.second.r()));
   ty=std::min(ty,int(i.second.t()));by=std::max(by,int(i.second.b()));
  }
  walls={lx+25,rx-25};borders={ty-300,by+100};
 }
};
}
int main() {
 using namespace ms;
 std::cout << "scenario,tick,x,y,hspeed,vspeed,grounded,foothold,layer,right,hit\\n" << std::setprecision(17);
 for(const std::string name:{"walk_right","walk_left","slow","fast","edge","slope_down","slope_up","wall","hit_right","hit_left"}) {
  bool slope=name=="slope_up"||name=="slope_down", shortfloor=slope||name=="wall"||name=="edge";
  TraceTree tree;
  tree.add_foothold(Foothold(1,2,-200,0,shortfloor?100:300,0,0,(slope||name=="wall")?2:0));
  if(slope) tree.add_foothold(Foothold(2,4,100,0,300,name=="slope_up"?-100:100,1,0));
  if(name=="wall") tree.add_foothold(Foothold(2,4,100,-100,100,0,1,0));
  tree.add_foothold(Foothold(3,6,-200,400,500,400));tree.bounds();
  PhysicsObject p; p.set_x(shortfloor?90:0);p.set_y(-1);p.onground=false;p.fhid=1;
  p.set_flag(PhysicsObject::Flag::TURNATEDGES);
  bool flip=name!="walk_left"; int remaining=0;
  float speed=(name=="slow"?-50:name=="fast"?5:-20);speed+=100;speed*=.001f;
  Physics physics;
  for(int tick=1;tick<=500;tick++) {
   if(!p.is_flag_set(PhysicsObject::Flag::TURNATEDGES)) {flip=!flip;remaining=0;p.set_flag(PhysicsObject::Flag::TURNATEDGES);}
   if(tick==50 && name.rfind("hit_",0)==0){flip=name=="hit_left";remaining=31;}
   p.hforce=source_force(speed,flip,remaining>0,p.onground);
   tree.update_fh(p);physics.move_normal(p);tree.limit_movement(p);p.move();
   if(remaining>0)remaining--;
   std::cout<<name<<','<<tick<<','<<p.crnt_x()<<','<<p.crnt_y()<<','<<p.hspeed<<','<<p.vspeed<<','<<p.onground<<','<<p.fhid<<','<<int(p.fhlayer)<<','<<flip<<','<<(remaining>0)<<'\\n';
  }
 }
}
"""
cpp=out/"monster-reference.cpp"
cpp.write_text(source,encoding="utf-8")
exe=out/"monster-reference.exe"
subprocess.run([args.compiler,"-std=c++17","-O0",str(cpp),"-o",str(exe)],check=True)
result=subprocess.run([str(exe)],capture_output=True,text=True,check=True)
target=root/"Assets/Scripts/Tests/GameLogic/Fixtures/HeavenMonsters.csv"
target.write_text(result.stdout,encoding="utf-8")
manifest={"source":str(args.source),"source_sha256":used,
 "method":"Unmodified C++ Physics/FootholdTree methods and Mob MOVE/HIT switch branches; controlled walking/hit adapter; 8 ms ticks.",
 "excluded":["random AI/next_move", "flying", "network", "NX loading", "animation", "custom oscillation watchdogs"],
 "rows":len(result.stdout.splitlines())-1,"trace_sha256":hashlib.sha256(target.read_bytes()).hexdigest()}
(root/"Tools/HeavenMonsterTrace-provenance.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
print("Recorded",manifest["rows"],"C++ monster ticks:",target)
