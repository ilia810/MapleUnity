namespace GameData.Network
{
    public enum SendOpcode : short
    {
        // Connection
        PING = 0x11,

        // Login
        LOGIN_STATUS = 0x00,
        SERVERSTATUS = 0x03,
        SERVERLIST = 0x0A,
        CHARLIST = 0x0B,
        SERVER_IP = 0x0C,
        CHAR_NAME_RESPONSE = 0x0E,
        ADD_NEW_CHAR_ENTRY = 0x0F,
        DELETE_CHAR_RESPONSE = 0x10,
        CHANNEL_SELECTED = 0x14,
        RELOG_RESPONSE = 0x16,
        CHECK_PINCODE = 0x17,
        UPDATE_PINCODE = 0x18,

        // In-game
        MODIFY_INVENTORY_ITEM = 0x1A,
        UPDATE_INVENTORY_SLOT = 0x1B,
        UPDATE_STATS = 0x1C,
        GIVE_BUFF = 0x1D,
        CANCEL_BUFF = 0x1E,
        TEMP_STATS = 0x1F,
        TEMP_STATS_RESET = 0x20,
        UPDATE_SKILLS = 0x21,
        FAME_RESPONSE = 0x24,
        SHOW_STATUS_INFO = 0x25,
        SHOW_NOTES = 0x27,
        TROCK_LOCATIONS = 0x29,
        UPDATE_MOUNT = 0x3F,
        SHOW_QUEST_COMPLETION = 0x30,
        ENTRUSTED_SHOP_CHECK_RESULT = 0x31,
        USE_SKILL_BOOK = 0x32,
        REPORT_PLAYER_MSG = 0x35,
        CHAR_INFO = 0x3D,
        PARTY_OPERATION = 0x40,
        BUDDYLIST = 0x41,
        GUILD_OPERATION = 0x43,
        ALLIANCE_OPERATION = 0x44,
        SPAWN_PORTAL = 0x45,
        SERVERMESSAGE = 0x46,
        PIGMI_REWARD = 0x47,
        OWL_OF_MINERVA = 0x48,
        ENGAGE_REQUEST = 0x4A,
        ENGAGE_RESULT = 0x4B,
        YELLOW_CHAT = 0x50,
        PLAYER_NPC = 0x52,
        MONSTERBOOK_ADD = 0x54,
        MONSTERBOOK_CHANGE_COVER = 0x56,
        ENERGY_CHARGE = 0x59,
        GHOST_POINT = 0x5A,
        GHOST_STATUS = 0x5B,
        FAIRY_PEND_MSG = 0x5C,
        SEND_PEDIGREE = 0x5D,
        OPEN_FAMILY = 0x5E,
        FAMILY_MESSAGE = 0x5F,
        FAMILY_INVITE = 0x60,
        FAMILY_JUNIOR = 0x61,
        SENIOR_MESSAGE = 0x62,
        REP_INCREASE = 0x63,
        FAMILY_LOGGEDIN = 0x64,
        FAMILY_BUFF = 0x65,
        FAMILY_USE_REQUEST = 0x66,
        LEVEL_UPDATE = 0x67,
        MARRIAGE_UPDATE = 0x68,
        JOB_UPDATE = 0x69,
        SET_BUY_EQUIP_EXT = 0x6A,
        MAPLE_TV_USE_RES = 0x6E,
        AVATAR_MEGA_RESULT = 0x70,
        SET_AVATAR_MEGA = 0x71,
        CANCEL_NAME_CHANGE_RESULT = 0x73,
        CANCEL_TRANSFER_WORLD_RESULT = 0x75,
        DESTROY_SHOP_RESULT = 0x76,
        FAKE_GM_NOTICE = 0x77,
        SUCCESS_IN_USE_GACHAPON_BOX = 0x78,
        NEW_YEAR_CARD_RES = 0x79,
        RANDOM_MORPH_RES = 0x7A,
        CANCEL_NAME_CHANGE_2 = 0x7C,
        SLOT_UPDATE = 0x7D,
        FOLLOW_REQUEST = 0x7E,
        TOP_MSG = 0x7F,
        MID_MSG = 0x82,
        CLEAR_MID_MSG = 0x83,
        SPECIAL_MSG = 0x84,
        MAPLE_ADMIN_MSG = 0x85,
        CAKE_VS_PIE_MSG = 0x86,
        UPDATE_INVENTORY_SLOTS = 0x87,
        GM_POLICE = 0x88,
        TREASURE_BOX_MSG = 0x89,
        NEW_YEAR_MSG = 0x8A,
        RANDOM_MORPH_MSG = 0x8B,
        CANCEL_NAME_CHANGE = 0x8E,
        SET_EXTRA_PENDANT_SLOT = 0x8F,
        SCRIPT_PROGRESS_MESSAGE = 0x90,
        DATA_CRC_CHECK_FAILED = 0x91,
        MACRO_SYS_DATA_INIT = 0x92,

        // CField Recv
        SET_FIELD = 0x93,
        SET_ITC = 0x94,
        SET_CASH_SHOP = 0x95,

        // CField::OnPacket
        SET_BACK_EFFECT = 0x97,
        SET_MAP_OBJECT_VISIBLE = 0x98,
        CLEAR_BACK_EFFECT = 0x99,
        BLOCKED_MAP = 0x9A,
        BLOCKED_SERVER = 0x9B,
        FORCED_MAP_EQUIP = 0x9C,
        MULTICHAT = 0x9D,
        WHISPER = 0x9E,
        SPOUSE_CHAT = 0x9F,
        SUMMON_ITEM_INAVAILABLE = 0xA0,
        FIELD_EFFECT = 0xA1,
        FIELD_OBSTACLE_ONOFF = 0xA2,
        FIELD_OBSTACLE_ONOFF_LIST = 0xA3,
        FIELD_OBSTACLE_ALL_RESET = 0xA4,
        BLOW_WEATHER = 0xA5,
        PLAY_JUKEBOX = 0xA6,
        ADMIN_RESULT = 0xA7,
        OX_QUIZ = 0xA8,
        GMEVENT_INSTRUCTIONS = 0xA9,
        CLOCK = 0xAA,
        CONTI_MOVE = 0xAB,
        CONTI_STATE = 0xAC,
        ARIANT_SCOREBOARD = 0xAE,
        SET_OBJECTS_STATE = 0xB1,

        // CField::OnChatMsgRecv
        FORCED_CHAT = 0xB3,
        UPDATE_CHAR_BOX = 0xB5,
        SHOW_ITEM_UPGRADE_EFFECT = 0xB7,
        FOLLOW_EFFECT = 0xBA,
        PLAY_SOUND = 0xBD,
        OPEN_UI = 0xBF,

        // CUserPool::OnPacket
        SPAWN_PLAYER = 0xC1,
        REMOVE_PLAYER_FROM_MAP = 0xC2,

        // CUserPool::OnUserCommonPacket
        CHATTEXT = 0xC3,
        CHALKBOARD = 0xC5,
        UPDATE_CHAR_LOOK = 0xC6,
        SHOW_FOREIGN_EFFECT = 0xC7,
        GIVE_FOREIGN_BUFF = 0xC8,
        CANCEL_FOREIGN_BUFF = 0xC9,
        UPDATE_PARTYMEMBER_HP = 0xCA,
        GUILD_NAME_CHANGED = 0xCB,
        GUILD_MARK_CHANGED = 0xCC,
        THROW_GRENADE = 0xCD,
        CANCEL_CHAIR = 0xCE,
        SHOW_ITEM_EFFECT = 0xCF,
        SHOW_CHAIR = 0xD0,
        UPDATE_CHAR_CASH = 0xD1,
        CHAR_DAMAGE = 0xD3,
        FACIAL_EXPRESSION = 0xD4,
        SHOW_EFFECT = 0xD5,
        SHOW_TITLE = 0xD7,
        ANGELIC_CHANGE = 0xD8,
        SHOW_CHAIR_EFFECT = 0xDA,
        // UPDATE_MOUNT = 0xDD, // Duplicate - using 0x3F

        // CUserPool::OnUserPetPacket
        SPAWN_PET = 0xDE,
        MOVE_PET = 0xDF,
        PET_CHAT = 0xE0,
        PET_NAMECHANGE = 0xE1,
        PET_EXCEPTION_LIST = 0xE2,
        PET_COMMAND = 0xE3,

        // CUserPool::OnUserDragonPacket
        SPAWN_DRAGON = 0xE4,
        MOVE_DRAGON = 0xE5,
        REMOVE_DRAGON = 0xE6,

        // CUserPool::OnUserAndroidPacket
        // Android stuff here

        // Ox Quiz packets
        OX_QUIZ_CONTROL = 0xE7,

        // CUserPool::OnUserRemotePacket
        MOVE_PLAYER = 0xF3,
        CLOSE_RANGE_ATTACK = 0xF4,
        RANGED_ATTACK = 0xF5,
        MAGIC_ATTACK = 0xF6,
        ENERGY_ATTACK = 0xF7,
        SKILL_EFFECT = 0xF8,
        CANCEL_SKILL_EFFECT = 0xF9,
        DAMAGE_PLAYER = 0xFA,
        FACIAL_EXPRESSION_REMOTE = 0xFB,
        SHOW_ITEM_EFFECT_REMOTE = 0xFC,
        SHOW_CHAIR_REMOTE = 0xFD,
        UPDATE_CHAR_CASH_REMOTE = 0xFE,
        GIVE_BUFF_REMOTE = 0xFF,
        CANCEL_BUFF_REMOTE = 0x100,
        THROW_GRENADE_REMOTE = 0x101,
        MOVE_PET_REMOTE = 0x102,
        PET_CHAT_REMOTE = 0x103,
        PET_NAMECHANGE_REMOTE = 0x104,
        PET_COMMAND_REMOTE = 0x105,
        SPAWN_PET_REMOTE = 0x106,
        MOVE_DRAGON_REMOTE = 0x107,

        // CMobPool::OnPacket  
        SPAWN_MONSTER = 0x109,
        KILL_MONSTER = 0x10A,
        SPAWN_MONSTER_CONTROL = 0x10B,
        MOVE_MONSTER = 0x10D,
        MOVE_MONSTER_RESPONSE = 0x10E,
        APPLY_MONSTER_STATUS = 0x110,
        CANCEL_MONSTER_STATUS = 0x111,
        RESET_MONSTER_ANIMATION = 0x112,
        DAMAGE_MONSTER = 0x116,
        ARIANT_THING = 0x119,
        SHOW_MONSTER_HP = 0x11A,
        CATCH_MONSTER = 0x11B,
        CATCH_MONSTER_WITH_ITEM = 0x11C,
        SHOW_MAGNET = 0x11D,

        // CNpcPool::OnPacket
        SPAWN_NPC = 0x11E,
        REMOVE_NPC = 0x11F,
        SPAWN_NPC_REQUEST_CONTROLLER = 0x120,
        NPC_ACTION = 0x122,
        NPC_TOGGLE_VISIBLE = 0x124,
        SET_NPC_SCRIPTABLE = 0x125,

        // CDropPool::OnPacket
        DROP_ITEM_FROM_MAPOBJECT = 0x127,
        REMOVE_ITEM_FROM_MAP = 0x128,

        // CMessageBoxPool::OnPacket
        SPAWN_KITE = 0x129,
        REMOVE_KITE = 0x12A,
        SPAWN_MIST = 0x12B,
        REMOVE_MIST = 0x12C,
        SPAWN_DOOR = 0x12D,
        REMOVE_DOOR = 0x12E,

        // CReactorPool::OnPacket
        SPAWN_REACTOR = 0x133,
        DESTROY_REACTOR = 0x134,

        // snowball event
        SNOWBALL_STATE = 0x136,
        HIT_SNOWBALL = 0x137,
        SNOWBALL_MESSAGE = 0x138,
        LEFT_KNOCK_BACK = 0x139,

        // coconut event
        COCONUT_HIT = 0x13A,
        COCONUT_SCORE = 0x13B,

        // guild search event, "Who's in charge here?"
        MOVE_HEALER = 0x13C,
        PULLEY_STATE = 0x13D,

        // monster carnival
        MONSTER_CARNIVAL_START = 0x13E,
        MONSTER_CARNIVAL_OBTAINED_CP = 0x13F,
        MONSTER_CARNIVAL_PARTY_CP = 0x140,
        MONSTER_CARNIVAL_SUMMON = 0x141,
        MONSTER_CARNIVAL_MESSAGE = 0x142,
        MONSTER_CARNIVAL_DIED = 0x143,
        MONSTER_CARNIVAL_LEAVE = 0x144,

        // Chaos Zakum/Horntail
        CHAOS_ZAKUM_SHRINE = 0x146,
        CHAOS_HORNTAIL_SHRINE = 0x147,

        // Capture the Flag
        CAPTURE_FLAGS = 0x148,
        CAPTURE_POSITION = 0x149,
        CAPTURE_RESET = 0x14A,
        PINK_ZAKUM_SHRINE = 0x14B,

        // CSpaceRiftPool::OnPacket
        SPACE_RIFT_ACK = 0x14C,
        SPACE_RIFT = 0x14D,

        // CAffectedAreaPool::OnPacket
        // Affected areas here

        // CTownPortalPool::OnPacket
        SPAWN_TOWN_PORTAL = 0x14E,
        REMOVE_TOWN_PORTAL = 0x14F,

        // COpenGatePool::OnPacket
        SPAWN_OPEN_GATE = 0x150,
        REMOVE_OPEN_GATE = 0x151,

        // CMapleMapObj::OnPacket
        SPAWN_MAPOBJECT = 0x152,
        REMOVE_MAPOBJECT = 0x153,

        // CReactor
        REACTOR_HIT = 0x154,
        REACTOR_MOVE = 0x155,

        // Various
        MAP_EFFECT = 0x157,
        PVP_ICEGAGE = 0x15A,
        BLOW_WEATHER_EFFECT = 0x15E,
        GM_LOG = 0x15F,
        OX_QUIZ_QUESTION = 0x160,
        OX_QUIZ_RESULT = 0x161,
        GM_EVENT_NOTICE = 0x162,
        OPEN_GATE_CREATED = 0x165,
        OPEN_GATE_REMOVED = 0x166,
        MAKER_SKILL_RESULT = 0x169,
        BUFF_BAR = 0x16D,
        CASHSHOP_OPERATION = 0x189,
        CASHSHOP_PURCHASE_EXP = 0x18A,
        CASHSHOP_GIFT_INFO_RESULT = 0x18B,
        CASHSHOP_CHECK_NAME_CHANGE = 0x18C,
        CASHSHOP_CHECK_NAME_CHANGE_POSSIBLE_RESULT = 0x18D,
        CASHSHOP_REGISTER_NEW_CHARACTER_RESULT = 0x18E,
        CASHSHOP_CHECK_TRANSFER_WORLD_POSSIBLE_RESULT = 0x18F,
        CASHSHOP_GACHAPON_STAMPS = 0x190,
        CASHSHOP_CASH_ITEM_GACHAPON_BOX = 0x191,
        CASHSHOP_CASH_GACHAPON_OPEN_RESULT = 0x192,
        KEYMAP = 0x194,
        AUTO_HP_POT = 0x195,
        AUTO_MP_POT = 0x196,
        SEND_TV = 0x19C,
        REMOVE_TV = 0x19D,
        ENABLE_TV = 0x19E,
        MTS_OPERATION2 = 0x1A2,
        MTS_OPERATION = 0x1A3,
        MAPLELIFE_RESULT = 0x1A4,
        MAPLELIFE_ERROR = 0x1A5,
        VICIOUS_HAMMER = 0x1BC,
        VEGA_SCROLL = 0x1C2
    }
}