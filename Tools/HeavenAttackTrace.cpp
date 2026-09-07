// Appended by Generate-HeavenMovementTrace.py to extracted HeavenClient methods.
int attacks() {
    using namespace ms;
    std::cout << "scenario,tick,x,y,hspeed,vspeed,grounded,foothold,stance,can_drop,ladder,facing,left,right,up,down,jump,jump_edge,attacking,knockback,jump_power\n" << std::setprecision(17);
    for (const std::string name : {"stand_turn", "walk_brake", "air_turn", "landing", "ledge",
        "prone_resume", "prone_drop", "walk_drop", "jump_discard", "jump_repress",
        "knockback", "climb_held", "both_resume", "stand_prone"}) {
        TraceTree tree;
        tree.add_foothold(Foothold(1,1,-1000,0,name=="ledge"?113:1000,0));
        tree.add_foothold(Foothold(3,1,-1000,400,1000,400)); tree.bounds();
        Player p; p.object.set_x(name=="ledge"?100:0); p.object.set_y(0); p.object.fhid=1;
        if (name=="landing") {p.object.set_y(-120); p.object.onground=false; p.set_state(Char::FALL);}
        if (name=="air_turn") p.stats.jump=200;
        Ladder ladder(1,0,-200,0,true);
        Physics physics; bool previousJump=false;
        for (int tick=1;tick<=150;tick++) {
            bool moving=name=="walk_brake"||name=="walk_drop"||name=="air_turn"||name=="ledge";
            bool left=(name=="stand_turn"||name=="walk_brake"||name=="air_turn"||name=="both_resume")&&tick>=22;
            bool right=(moving&&tick<=21)||(name=="prone_resume"&&tick>=22)||(name=="both_resume"&&tick>=22);
            bool up=name=="climb_held"&&tick>=22;
            bool down=name=="prone_drop"||(name=="prone_resume"&&tick<=21)||((name=="walk_drop"||name=="stand_prone"||name=="landing")&&tick>=22);
            bool jump=(name=="air_turn"&&tick==20)||((name=="prone_drop"||name=="walk_drop")&&tick==23)||
                (name=="jump_discard"&&((tick>=22&&tick<=110)||tick==115))||(name=="jump_repress"&&(tick==22||tick==110));
            bool knockback=name=="knockback"&&tick==30;
            if (tick==21) p.attacking=true;
            p.keys[KeyAction::LEFT]=left; p.keys[KeyAction::RIGHT]=right;
            p.keys[KeyAction::UP]=up; p.keys[KeyAction::DOWN]=down;
            bool edge=jump&&!previousJump;
            if(edge) get_state(p.state)->send_action(p,KeyAction::JUMP,true);
            p.keys[KeyAction::JUMP]=jump; previousJump=jump;
            if(knockback) {p.object.hspeed=-1.5; p.object.vforce-=3.5;}
            const PlayerState* state=get_state(p.state);
            state->update(p); tree.update_fh(p.object);
            if(p.object.type==PhysicsObject::NORMAL) {physics.move_normal(p.object); tree.limit_movement(p.object);}
            p.object.move();
            if(tick==95) {p.attacking=false; PlayerNullState().update_state(p);}
            else state->update_state(p);
            p.climb_cooldown.update();
            if(name=="climb_held"&&!p.attacking&&!p.get_ladder()&&p.can_climb()&&(up||down)&&
                ladder.inrange(p.object.get_position(),up&&!down)) p.set_ladder(ladder);
            auto& o=p.object;
            std::cout<<name<<','<<tick<<','<<o.crnt_x()<<','<<o.crnt_y()<<','<<o.hspeed<<','<<o.vspeed<<','<<o.onground
                <<','<<o.fhid<<','<<p.state<<','<<o.enablejd<<','<<(p.get_ladder()?p.get_ladder()->id:0)<<','<<p.facing
                <<','<<left<<','<<right<<','<<up<<','<<down<<','<<jump<<','<<edge<<','<<p.attacking<<','<<knockback<<','<<p.stats.jump<<'\n';
        }
    }
    return 0;
}
