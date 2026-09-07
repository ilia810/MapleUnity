"""Compile selected, unmodified HeavenClient methods and record normal, drop-through and climbing movement.
Usage: python Tools/Generate-HeavenMovementTrace.py --source C:/HeavenClient/MapleStory-Client
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
parser.add_argument("--attack-only", action="store_true", help="Generate only attack movement traces.")
parser.add_argument("--swim-only", action="store_true", help="Generate only underwater movement traces.")
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
out = root / "Temp/HeavenMovementReference"
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
source += "\nstruct Physics { void move_normal(PhysicsObject&) const; void move_swimming(PhysicsObject&) const; };\n"
source += function(physics, "void Physics::move_normal") + "\n"
source += function(physics, "void Physics::move_swimming") + "\n"
source += """
namespace KeyAction { enum Id { LEFT,RIGHT,DOWN,UP,JUMP }; }
namespace EquipStat { enum Id { SPEED,JUMP }; }
struct Char {
    enum State { STAND,WALK,FALL,PRONE,LADDER,ROPE,SWIM };
    State state=STAND; int facing=0;
    void set_state(State s) { state=s; }
    void set_direction(bool right) { facing=right?1:-1; }
};
template<class T> struct Optional {
    const T* value;
    Optional(std::nullptr_t=nullptr):value(nullptr) {}
    Optional(const T& item):value(&item) {}
    explicit operator bool() const { return value!=nullptr; }
    const T* operator->() const { return value; }
};
class Ladder {
    int16_t x,y1,y2;
    bool ladder;
public:
    int id;
    Ladder(int id,int x,int top,int bottom,bool isLadder):x(x),y1(top),y2(bottom),ladder(isLadder),id(id) {}
    bool is_ladder() const;
    bool inrange(Point<int16_t>,bool) const;
    bool felloff(int16_t,bool) const;
    int16_t get_x() const;
};
struct Player : Char {
    PhysicsObject object;
    PhysicsObject& phobj=object;
    Optional<const Ladder> ladder;
    TimedBool climb_cooldown;
    bool attacking=false;
    bool keys[5]={};
    struct Stats {
        int speed=100,jump=120;
        int get_total(EquipStat::Id id) const { return id==EquipStat::SPEED?speed:jump; }
    } stats;
    PhysicsObject& get_phobj() { return object; }
    const PhysicsObject& get_phobj() const { return object; }
    bool is_attacking() const;
    bool underwater=false;
    bool is_underwater() const { return underwater; }
    bool is_key_down(KeyAction::Id k) const { return keys[k]; }
    void set_direction(bool right);
    void set_state(Char::State s);
    void set_ladder(Optional<const Ladder>);
    Optional<const Ladder> get_ladder() const;
    void set_climb_cooldown();
    bool can_climb();
    float get_flyforce() const;
    float get_climbforce() const;
    float get_walkforce() const;
    float get_jumpforce() const;
};
struct PlayerState {
    void play_jumpsound() const {}
    bool haswalkinput(const Player&) const;
    bool hasleftinput(const Player&) const;
    bool hasrightinput(const Player&) const;
    virtual void initialize(Player&) const {}
    virtual void update(Player&) const {}
    virtual void update_state(Player&) const {}
    virtual void send_action(Player&,KeyAction::Id,bool) const {}
};
struct PlayerNullState:PlayerState { void update_state(Player&) const override; };
struct PlayerStandState:PlayerState {
    void initialize(Player&) const;
    void update(Player&) const override;
    void update_state(Player&) const override;
    void send_action(Player&,KeyAction::Id,bool) const override;
};
struct PlayerWalkState:PlayerStandState {
    void initialize(Player&) const;
    void send_action(Player&,KeyAction::Id,bool) const override;
    void update(Player&) const override;
    void update_state(Player&) const override;
};
struct PlayerFallState:PlayerState {
    void initialize(Player&) const;
    void update(Player&) const override;
    void update_state(Player&) const override;
};
struct PlayerProneState:PlayerState {
    void update(Player&) const override;
    void send_action(Player&,KeyAction::Id,bool) const override;
};
struct PlayerClimbState:PlayerState {
    void initialize(Player&) const;
    void update(Player&) const override;
    void update_state(Player&) const override;
    void cancel_ladder(Player&) const;
};
struct PlayerFlyState:PlayerState {
    void initialize(Player&) const;
    void send_action(Player&,KeyAction::Id,bool) const override;
    void update(Player&) const override;
    void update_state(Player&) const override;
};
const PlayerState* get_state(Char::State s) {
    static PlayerStandState stand; static PlayerWalkState walk; static PlayerFallState fall;
    static PlayerProneState prone; static PlayerClimbState climb; static PlayerFlyState swim;
    const PlayerState* lookup[]={&stand,&walk,&fall,&prone,&climb,&climb,&swim};
    return s<=Char::SWIM ? lookup[s] : nullptr;
}
"""
player = read("Character/Player.cpp")
for sig in ["void Player::set_direction", "void Player::set_state", "bool Player::is_attacking", "float Player::get_walkforce", "float Player::get_jumpforce", "float Player::get_climbforce",
            "void Player::set_ladder", "Optional<const Ladder> Player::get_ladder",
            "void Player::set_climb_cooldown", "bool Player::can_climb", "float Player::get_flyforce"]:
    source += function(player, sig) + "\n"
states = read("Character/PlayerStates.cpp")
for sig in ["void PlayerNullState::update_state", "bool PlayerState::haswalkinput", "bool PlayerState::hasleftinput",
            "bool PlayerState::hasrightinput", "void PlayerStandState::send_action",
            "void PlayerStandState::update(", "void PlayerStandState::update_state",
            "void PlayerWalkState::send_action", "void PlayerWalkState::update(", "void PlayerWalkState::update_state",
            "void PlayerFallState::update(", "void PlayerFallState::update_state",
            "void PlayerProneState::update(", "void PlayerProneState::send_action",
            "void PlayerStandState::initialize", "void PlayerWalkState::initialize", "void PlayerFallState::initialize",
            "void PlayerClimbState::initialize", "void PlayerClimbState::update(",
            "void PlayerClimbState::update_state", "void PlayerClimbState::cancel_ladder",
            "void PlayerFlyState::initialize", "void PlayerFlyState::send_action", "void PlayerFlyState::update(", "void PlayerFlyState::update_state"]:
    source += function(states, sig) + "\n"
mapinfo = read("Gameplay/MapleMap/MapInfo.cpp")
for sig in ["bool Ladder::is_ladder", "bool Ladder::inrange", "bool Ladder::felloff", "int16_t Ladder::get_x"]:
    source += function(mapinfo, sig) + "\n"
source += """
struct TraceTree:FootholdTree {
    void bounds() {
        double lx=30000,rx=-30000,ty=30000,by=-30000;
        for (const auto& item:footholds) {
            lx=std::min(lx,double(item.second.l())); rx=std::max(rx,double(item.second.r()));
            ty=std::min(ty,double(item.second.t())); by=std::max(by,double(item.second.b()));
        }
        if(ty>=by){ty=-1000;by=0;}
        if(lx>=rx){lx=-1000;rx=1000;}
        walls={int32_t(lx+25),int32_t(rx-25)};
        borders={int32_t(ty-300),int32_t(by+100)};
    }
};
}
int traversal();
int terrain();
int attacks();
int swimming();
int main(int argc,char** argv) {
    if(argc>1 && std::string(argv[1])=="swimming") return swimming();
    if(argc>1) return std::string(argv[1])=="attacks" ? attacks() : std::string(argv[1])=="terrain" ? terrain() : traversal();
    using namespace ms;
    std::cout << "scenario,tick,x,y,hspeed,vspeed,grounded,foothold,stance\\n" << std::setprecision(17);
    for(const std::string name: {"walk_right","walk_left","walk_speed140","jump100","jump120",
          "moving_jump_brake","air_input","fast_fall","slope_down","slope_up","ledge","wall"}) {
        TraceTree tree;
        Player p;
        bool slope=name=="slope_down"||name=="slope_up", ledge=name=="ledge", wall=name=="wall";
        tree.add_foothold(Foothold(1,1,-1000,0,(slope||ledge||wall)?100:1000,0,0,(slope||wall)?2:0));
        if(slope) tree.add_foothold(Foothold(2,1,100,0,300,name=="slope_up"?-100:100,1,0));
        if(wall) tree.add_foothold(Foothold(2,1,100,-100,100,0,1,0));
        tree.add_foothold(Foothold(3,1,-1000,400,1000,400));
        tree.bounds();
        p.object.set_x(slope||ledge||wall?90:0);
        p.object.set_y(0); p.object.fhid=1;
        if(name=="air_input"||name=="fast_fall") {
            p.object.set_y(-200); p.object.onground=false; p.state=Char::FALL;
            if(name=="fast_fall") p.object.vspeed=12;
        }
        if(name=="walk_speed140") p.stats.speed=140;
        if(name=="jump100") p.stats.jump=100;
        PlayerStandState stand; PlayerWalkState walk; PlayerFallState fall; PlayerProneState prone;
        const PlayerState* lookup[]={&stand,&walk,&fall,&prone};
        Physics physics;
        for(int tick=1;tick<=160;++tick) {
            p.keys[KeyAction::LEFT]=name=="walk_left"?tick<=40:name=="moving_jump_brake"&&tick>21;
            p.keys[KeyAction::RIGHT]=name=="walk_right"||name=="walk_speed140"?tick<=40:
                name=="moving_jump_brake"?tick<=21:slope||ledge||wall||name=="air_input";
            bool jump=(name=="jump100"||name=="jump120")?tick==1:name=="moving_jump_brake"&&tick==21;
            if(jump) lookup[p.state]->send_action(p,KeyAction::JUMP,true);
            const PlayerState* state=lookup[p.state];
            state->update(p);
            tree.update_fh(p.object);
            physics.move_normal(p.object);
            tree.limit_movement(p.object);
            p.object.move();
            state->update_state(p);
            auto& o=p.object;
            std::cout << name << ',' << tick << ',' << o.crnt_x() << ',' << o.crnt_y()
              << ',' << o.hspeed << ',' << o.vspeed << ',' << o.onground << ',' << o.fhid << ',' << p.state << '\\n';
        }
    }
}
"""
source += r"""
int traversal() {
    using namespace ms;
    std::cout << "scenario,tick,x,y,hspeed,vspeed,grounded,foothold,stance,can_drop,can_climb,ladder,facing,left,right,up,down,jump,jump_edge,speed,jump_power\n" << std::setprecision(17);
    for(const std::string name: {"drop_prone","drop_walk","drop_simultaneous","drop_599","drop_600","drop_close","drop_bottom",
        "climb_up","climb_down","climb_rope","climb_pause","climb_speed140","climb_jump_left","climb_jump_right",
        "climb_jump_both","climb_jump_alone","climb_jump_held","climb_cooldown","climb_range_inside","climb_range_outside"}) {
        bool climbing=name.find("climb_")==0;
        bool jumpCase=name.find("climb_jump_")==0;
        int lower=name=="drop_599"?599:name=="drop_600"?600:name=="drop_close"?8:400;
        int top=jumpCase?-400:-80;
        TraceTree tree;
        tree.add_foothold(Foothold(1,1,-1000,0,1000,0));
        tree.add_foothold(Foothold(3,1,-1000,lower,1000,lower));
        if(climbing) tree.add_foothold(Foothold(2,1,-1000,top,1000,top));
        tree.bounds();
        Player p;
        p.object.set_x(climbing ? (name=="climb_range_inside"?10.49:name=="climb_range_outside"?10.5:7) : 0);
        p.object.set_y(jumpCase?-40:name=="climb_down"?top:name=="drop_bottom"?lower:0);
        p.object.fhid=name=="climb_down"?2:name=="drop_bottom"?3:1;
        p.object.onground=!jumpCase;
        if(jumpCase) p.set_state(Char::FALL);
        if(name=="climb_speed140") p.stats.speed=140;
        Ladder ladder(1,0,top,0,name!="climb_rope");
        PlayerStandState stand; PlayerWalkState walk; PlayerFallState fall; PlayerProneState prone; PlayerClimbState climb;
        const PlayerState* lookup[]={&stand,&walk,&fall,&prone,&climb,&climb};
        Physics physics;
        bool previousJump=false;
        for(int tick=1;tick<=260;++tick) {
            bool left=false,right=false,up=false,down=false,jump=false;
            if(!climbing) {
                down=true;
                right=name=="drop_walk" && tick<=10;
                jump=tick==(name=="drop_simultaneous"?1:3);
            } else {
                up=name!="climb_down";
                down=name=="climb_down";
                if(name=="climb_pause") {
                    up=tick<=20 || (tick>=41 && tick<=60);
                    down=tick>=41;
                }
                if(name=="climb_cooldown" && tick>=90) {up=false;down=true;}
                if(jumpCase) {
                    jump=tick>=(name=="climb_jump_held"?1:10);
                    left=(name=="climb_jump_left"||name=="climb_jump_both") && tick>=10 && tick<=12;
                    right=(name=="climb_jump_right"||name=="climb_jump_both"||name=="climb_jump_held") && tick>=10 && tick<=12;
                }
            }
            p.keys[KeyAction::LEFT]=left; p.keys[KeyAction::RIGHT]=right;
            p.keys[KeyAction::UP]=up; p.keys[KeyAction::DOWN]=down;
            bool edge=jump&&!previousJump;
            if(edge) lookup[p.state]->send_action(p,KeyAction::JUMP,true);
            p.keys[KeyAction::JUMP]=jump;
            previousJump=jump;
            const PlayerState* state=lookup[p.state];
            state->update(p);
            tree.update_fh(p.object);
            if(p.object.type==PhysicsObject::NORMAL) {physics.move_normal(p.object);tree.limit_movement(p.object);}
            p.object.move();
            state->update_state(p);
            p.climb_cooldown.update();
            // Stage checks after Player::update, upwards first only when Down is released.
            if(climbing && !p.get_ladder() && p.can_climb() && (up||down)) {
                if(ladder.inrange(p.object.get_position(),up&&!down)) p.set_ladder(ladder);
            }
            auto& o=p.object;
            std::cout << name << ',' << tick << ',' << o.crnt_x() << ',' << o.crnt_y()
                << ',' << o.hspeed << ',' << o.vspeed << ',' << o.onground << ',' << o.fhid << ',' << p.state
                << ',' << o.enablejd << ',' << p.can_climb() << ',' << (p.get_ladder()?p.get_ladder()->id:0) << ',' << p.facing
                << ',' << left << ',' << right << ',' << up << ',' << down << ',' << jump << ',' << edge
                << ',' << p.stats.speed << ',' << p.stats.jump << '\n';
        }
    }
    return 0;
}
"""
source += r"""
int terrain() {
    using namespace ms;
    std::cout << "scenario,tick,x,y,hspeed,vspeed,grounded,foothold,stance,layer,left,right,down,jump\n" << std::setprecision(17);
    for(const std::string name: {"overlap_fall","overlap_jump","overlap_walk","overlap_ledge","overlap_drop",
        "tie","tie_reverse","tie_rehash","crossing_fall","crossing_walk","negative_column",
        "wall_other_layer","ceiling","left_boundary","right_boundary","high_map"}) {
        TraceTree tree;
        bool crossing=name.find("crossing_")==0, tied=name.find("tie")==0;
        int baseY=name=="high_map"?4500:0;
        Foothold base(1,1,-200,crossing?-100:baseY,name=="wall_other_layer"?100:200,crossing?100:baseY,0,name=="wall_other_layer"?2:0);
        Foothold upper(2,4,crossing?-200:-100,crossing?100:tied?0:-80,crossing?200:100,crossing?-100:tied?0:-80);
        if(name=="tie_reverse") tree.add_foothold(upper);
        if(name=="negative_column") tree.add_foothold(Foothold(1,1,-200,0,-100,0));
        else tree.add_foothold(base);
        if(name=="negative_column") tree.add_foothold(Foothold(2,4,-99,-40,100,-40));
        else if(name=="wall_other_layer") tree.add_foothold(Foothold(2,4,100,-80,100,0,1,0));
        else if(name!="tie_reverse" && (name.find("overlap_")==0 || crossing || tied)) tree.add_foothold(upper);
        tree.add_foothold(Foothold(3,6,-200,baseY+400,200,baseY+400));
        if(name=="tie_rehash") for(int id=4;id<=24;id++) tree.add_foothold(Foothold(id,id%7,-100,0,100,0));
        tree.bounds();
        Player p;
        bool airborne=name=="overlap_fall"||tied||name=="crossing_fall"||name=="negative_column"||name=="ceiling";
        double x=name=="overlap_walk"?-90:name=="overlap_ledge"||name=="wall_other_layer"?90:
            name=="crossing_fall"?-60:name=="crossing_walk"?-40:name=="negative_column"?-99.75:
            name=="left_boundary"?-170:name=="right_boundary"?170:0;
        double y=name=="overlap_ledge"||name=="overlap_drop"?-80:name=="crossing_walk"?-20:
            name=="crossing_fall"?-180:name=="ceiling"?-295:airborne?-120:baseY;
        p.object.set_x(x); p.object.set_y(y); p.object.onground=!airborne;
        p.object.fhid=airborne?0:name=="overlap_ledge"||name=="overlap_drop"?2:1;
        if(airborne) p.set_state(Char::FALL);
        if(name=="ceiling") p.object.vspeed=-10;
        PlayerStandState stand; PlayerWalkState walk; PlayerFallState fall; PlayerProneState prone;
        const PlayerState* lookup[]={&stand,&walk,&fall,&prone};
        Physics physics;
        for(int tick=1;tick<=160;tick++) {
            bool left=name=="left_boundary";
            bool right=name=="overlap_walk"||name=="overlap_ledge"||name=="crossing_walk"||name=="wall_other_layer"||name=="right_boundary";
            bool down=name=="overlap_drop";
            bool jump=(name=="overlap_jump"||name=="high_map")?tick==2:down&&tick==3;
            p.keys[KeyAction::LEFT]=left; p.keys[KeyAction::RIGHT]=right; p.keys[KeyAction::DOWN]=down;
            if(jump) lookup[p.state]->send_action(p,KeyAction::JUMP,true);
            const PlayerState* state=lookup[p.state];
            state->update(p); tree.update_fh(p.object); physics.move_normal(p.object);
            tree.limit_movement(p.object); p.object.move(); state->update_state(p);
            auto& o=p.object;
            std::cout << name << ',' << tick << ',' << o.crnt_x() << ',' << o.crnt_y()
                << ',' << o.hspeed << ',' << o.vspeed << ',' << o.onground << ',' << o.fhid << ',' << p.state
                << ',' << int(o.fhlayer) << ',' << left << ',' << right << ',' << down << ',' << jump << '\n';
        }
    }
    return 0;
}
"""
source += (root / "Tools/HeavenAttackTrace.cpp").read_text(encoding="utf-8")
source += (root / "Tools/HeavenSwimTrace.cpp").read_text(encoding="utf-8")
cpp = out / "reference.cpp"
cpp.write_text(source, encoding="utf-8")
exe = out / "reference.exe"
subprocess.run([args.compiler, "-std=c++17", "-O0", str(cpp), "-o", str(exe)], check=True)
if args.swim_only:
    result = subprocess.run([str(exe), "swimming"], capture_output=True, text=True, check=True)
    target = root / "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenSwimming.csv"
    target.write_text(result.stdout, encoding="utf-8")
    manifest = {
        "source": str(args.source), "source_sha256": used,
        "method": "Unmodified Player Fly/Fall/Stand/Walk/Prone/Climb/Null states, physics, footholds and ladder methods; headless Char/Player adapter and synthetic water flag.",
        "attack_clock": "Optional synthetic attack ticks 21 through 95, matching unarmed 600ms motion; same update/completion/Stage order as attack reference.",
        "excluded": ["NX loading", "audio", "network", "animation rendering", "non-water flying", "custom oscillation watchdogs", "server/retail physics"],
        "trace_sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
        "harness_sha256": hashlib.sha256((root / "Tools/HeavenSwimTrace.cpp").read_bytes()).hexdigest(),
        "rows": len(result.stdout.splitlines()) - 1
    }
    (root / "Tools/HeavenSwimTrace-provenance.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print("Recorded", manifest["rows"], "C++ swimming ticks:", target)
    raise SystemExit(0)
if args.attack_only:
    result = subprocess.run([str(exe), "attacks"], capture_output=True, text=True, check=True)
    target = root / "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenAttackMovement.csv"
    target.write_text(result.stdout, encoding="utf-8")
    manifest = {
        "source": str(args.source), "source_sha256": used,
        "method": "Unmodified source PlayerStates, Player state/direction guards, NullState, physics, footholds and ladder methods; headless Char/Player adapters.",
        "attack_clock": "Synthetic attack from tick 21 through 95 (75 x 8 ms), matching the local unarmed motion. Physics then attack completion, NullState, then Stage ladder entry.",
        "contact": "Inject source Player::damage normal knockback at tick 30 in knockback case; damage formula tested separately.",
        "excluded": ["NX", "audio", "network", "animation rendering", "swimming", "flying", "portals", "custom oscillation watchdogs"],
        "trace_sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
        "harness_sha256": hashlib.sha256((root / "Tools/HeavenAttackTrace.cpp").read_bytes()).hexdigest(),
        "rows": len(result.stdout.splitlines()) - 1
    }
    (root / "Tools/HeavenAttackTrace-provenance.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print("Recorded", manifest["rows"], "C++ attack movement ticks:", target)
    raise SystemExit(0)
result = subprocess.run([str(exe)], capture_output=True, text=True, check=True)
target = root / "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenMovement.csv"
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(result.stdout, encoding="utf-8")
traversal = subprocess.run([str(exe), "traversal"], capture_output=True, text=True, check=True)
traversal_target = target.with_name("HeavenTraversal.csv")
traversal_target.write_text(traversal.stdout, encoding="utf-8")
terrain_result = subprocess.run([str(exe), "terrain"], capture_output=True, text=True, check=True)
terrain_target = target.with_name("HeavenTerrain.csv")
terrain_target.write_text(terrain_result.stdout, encoding="utf-8")
manifest = {
    "source": str(args.source), "source_sha256": used,
    "method": "Unmodified selected C++ methods; headless Player adapter; synthetic footholds; 8 ms ticks.",
    "excluded": ["NX loading", "audio", "animation", "network", "custom oscillation watchdogs",
                 "swimming", "flying"],
    "trace_sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
    "rows": len(result.stdout.splitlines()) - 1,
    "traversal_trace_sha256": hashlib.sha256(traversal_target.read_bytes()).hexdigest(),
    "traversal_rows": len(traversal.stdout.splitlines()) - 1,
    "terrain_trace_sha256": hashlib.sha256(terrain_target.read_bytes()).hexdigest(),
    "terrain_rows": len(terrain_result.stdout.splitlines()) - 1,
    "terrain_adapter": "Actual source lookup/contact/layer rules; synthetic overlapping surfaces and boundaries. Empty/null foothold behavior is tested separately as an intentional correction.",
    "traversal_adapter": "Actual Ladder, TimedBool, ClimbState and Player ladder/force methods; Stage entry order reproduced without portals/combat."
}
(root / "Tools/HeavenMovementTrace-provenance.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
print("Recorded", manifest["rows"], "C++ ticks:", target)

print("Recorded", manifest["traversal_rows"], "C++ traversal ticks:", traversal_target)

print("Recorded", manifest["terrain_rows"], "C++ terrain ticks:", terrain_target)
