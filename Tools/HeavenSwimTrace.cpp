// Appended to extracted source methods by Generate-HeavenMovementTrace.py.
int swimming() {
    using namespace ms;
    std::cout<<"scenario,tick,x,y,hspeed,vspeed,grounded,foothold,stance,layer,ladder,facing,left,right,up,down,jump,jump_edge,attacking,knockback,speed,jump_power\n"<<std::setprecision(17);
    for(const std::string name:{"water_idle","water_right","water_left","water_up","water_down","water_diagonal",
        "water_opposed","water_brake","water_jump_ignored","water_ground_jump","water_ground_speed","water_ledge",
        "water_wall","water_ceiling","water_land_prone","water_ladder","water_rope_jump","water_attack","water_knockback","water_knockback_steering"}) {
        bool grounded=name=="water_ground_jump"||name=="water_ground_speed"||name=="water_ledge";
        bool ladderCase=name=="water_ladder"||name=="water_rope_jump";
        TraceTree tree;
        tree.add_foothold(Foothold(1,1,-1000,0,name=="water_ledge"||name=="water_wall"?100:1000,0,0,name=="water_wall"?2:0));
        if(name=="water_wall")tree.add_foothold(Foothold(2,1,100,-200,100,0,1,0));
        tree.add_foothold(Foothold(3,4,-1000,400,1000,400));tree.bounds();
        Player p;p.underwater=true;p.object.set_x(name=="water_wall"||name=="water_ledge"?90:0);
        p.object.set_y(grounded?0:name=="water_ceiling"?-295:name=="water_land_prone"?-10:-150);
        p.object.onground=grounded;p.set_state(grounded?Char::STAND:Char::FALL);
        if(name=="water_ground_speed")p.stats.speed=140;
        if(name=="water_ceiling")p.object.vspeed=-10;
        Ladder ladder(1,0,-250,0,name!="water_rope_jump");
        Physics physics;bool previousJump=false;
        for(int tick=1;tick<=220;tick++) {
            bool left=name=="water_left" || (name=="water_opposed"&&tick>=10) || (name=="water_attack"&&tick>=22);
            bool right=name=="water_right"||name=="water_diagonal"||name=="water_ground_speed"||name=="water_ledge"||name=="water_wall" ||
                (name=="water_opposed"&&tick>=12) || (name=="water_brake"&&tick<=60) || (name=="water_rope_jump"&&tick>=20&&tick<=25) || (name=="water_attack"&&tick<=21);
            bool up=name=="water_up"||name=="water_diagonal"||name=="water_ceiling"||name=="water_knockback_steering"||
                (name=="water_opposed"&&tick>=10)||(ladderCase&&tick<=40)||(name=="water_attack"&&tick>=22);
            bool down=name=="water_down"||name=="water_land_prone"||(name=="water_opposed"&&tick>=12);
            bool jump=(name=="water_ground_jump"&&tick==1)||(name=="water_jump_ignored"&&(tick==10||tick==40||tick==80)) ||
                (name=="water_rope_jump"&&tick>=20) || (name=="water_attack"&&tick>=22);
            bool knockback=(name=="water_knockback"||name=="water_knockback_steering")&&tick==30;
            if(name=="water_attack"&&tick==21)p.attacking=true;
            bool values[]={left,right,down,up,jump};
            bool edge=jump&&!previousJump;previousJump=jump;
            for(int key=0;key<5;key++)if(p.keys[key]!=values[key]){
                get_state(p.state)->send_action(p,static_cast<KeyAction::Id>(key),values[key]);p.keys[key]=values[key];
            }
            if(knockback){p.object.hspeed=-1.5;p.object.vforce-=3.5;}
            const PlayerState* state=get_state(p.state);state->update(p);tree.update_fh(p.object);
            if(p.object.type==PhysicsObject::NORMAL)physics.move_normal(p.object);
            if(p.object.type==PhysicsObject::SWIMMING)physics.move_swimming(p.object);
            if(p.object.type!=PhysicsObject::FIXATED)tree.limit_movement(p.object);
            p.object.move();
            if(name=="water_attack"&&tick==95){p.attacking=false;PlayerNullState().update_state(p);}
            else state->update_state(p);
            p.climb_cooldown.update();
            if(ladderCase&&!p.attacking&&!p.get_ladder()&&p.can_climb()&&(up||down)&&ladder.inrange(p.object.get_position(),up&&!down))p.set_ladder(ladder);
            auto& o=p.object;
            std::cout<<name<<','<<tick<<','<<o.crnt_x()<<','<<o.crnt_y()<<','<<o.hspeed<<','<<o.vspeed<<','<<o.onground
                <<','<<o.fhid<<','<<p.state<<','<<int(o.fhlayer)<<','<<(p.get_ladder()?p.get_ladder()->id:0)<<','<<p.facing
                <<','<<left<<','<<right<<','<<up<<','<<down<<','<<jump<<','<<edge<<','<<p.attacking<<','<<knockback<<','<<p.stats.speed<<','<<p.stats.jump<<'\n';
        }
    }
    return 0;
}
