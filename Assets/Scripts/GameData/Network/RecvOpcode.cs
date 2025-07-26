namespace GameData.Network
{
    public enum RecvOpcode : short
    {
        // Connection
        PONG = 0x18,
        
        // Login
        LOGIN_PASSWORD = 0x01,
        SERVERLIST_REQUEST = 0x0B,
        CHARLIST_REQUEST = 0x05,
        CHAR_SELECT = 0x13,
        PLAYER_LOGGEDIN = 0x14,
        CHECK_CHAR_NAME = 0x15,
        CREATE_CHAR = 0x16,
        DELETE_CHAR = 0x17,
        REGISTER_PIN = 0x0A,
        REGISTER_PIC = 0x1D,
        CHAR_SELECT_WITH_PIC = 0x1E,
        
        // Channel
        CHANGE_CHANNEL = 0x27,
        PLAYER_DC = 0x0C,
        
        // Movement & Player
        CHANGE_MAP = 0x26,
        MOVE_PLAYER = 0x29,
        CLOSE_RANGE_ATTACK = 0x2C,
        RANGED_ATTACK = 0x2D,
        MAGIC_ATTACK = 0x2E,
        TAKE_DAMAGE = 0x30,
        
        // Chair
        CANCEL_CHAIR = 0x2A,
        USE_CHAIR = 0x2B,
        
        // Chat
        GENERAL_CHAT = 0x31,
        WHISPER_FIND = 0x8A,
        WHISPER = 0x89,
        MESSENGER = 0x90,
        
        // NPC & Shop
        NPC_TALK = 0x3A,
        NPC_TALK_MORE = 0x3C,
        NPC_SHOP = 0x3D,
        
        // Inventory
        ITEM_MOVE = 0x47,
        USE_ITEM = 0x48,
        USE_RETURN_SCROLL = 0x55,
        USE_UPGRADE_SCROLL = 0x56,
        USE_CASH_ITEM = 0x4F,
        USE_SKILL_BOOK = 0x52,
        USE_TELEPORT_ROCK = 0x54,
        USE_SUMMON_BAG = 0x4B,
        
        // Character
        DISTRIBUTE_AP = 0x57,
        DISTRIBUTE_AUTO_AP = 0x59,
        DISTRIBUTE_SP = 0x5A,
        CHANGE_KEYMAP = 0x7B,
        CHANGE_MAP_SPECIAL = 0x86,
        USE_INNER_PORTAL = 0x87,
        
        // Skills
        SKILL_EFFECT = 0x5C,
        CANCEL_BUFF = 0x5D,
        SPECIAL_MOVE = 0x76,
        
        // Social
        PARTY_OPERATION = 0x6A,
        DENY_PARTY_REQUEST = 0x6B,
        BUDDYLIST_MODIFY = 0x79,
        GUILD_OPERATION = 0x6F,
        DENY_GUILD_REQUEST = 0x70,
        ADMIN_COMMAND = 0x72,
        ADMIN_LOG = 0x73,
        ALLIANCE_OPERATION = 0x95,
        USE_FAMILY = 0xA0,
        
        // Monster & Combat
        MOVE_LIFE = 0x8C,
        AUTO_AGGRO = 0x8D,
        MOB_DAMAGE_MOB_FRIENDLY = 0x9A,
        MONSTER_BOMB = 0x9B,
        MOB_DAMAGE_MOB = 0x9C,
        
        // Items & Drops
        ITEM_PICKUP = 0x66,
        USE_ITEMEFFECT = 0x34,
        
        // Pets
        PET_MOVE = 0x67,
        PET_CHAT = 0x68,
        PET_COMMAND = 0x69,
        PET_LOOT = 0x6E,
        PET_AUTO_POT = 0x71,
        PET_FOOD = 0x4C,
        
        // Player Interaction
        CHAR_INFO_REQUEST = 0x41,
        PLAYER_INTERACTION = 0x77,
        
        // Trading
        USE_HIRED_MERCHANT = 0x78,
        MERCHANT_MESO = 0x3F,
        PLAYER_SHOP = 0x8F,
        
        // Cash Shop
        ENTER_CASHSHOP = 0x28,
        TOUCH_CASHSHOP = 0xC6,
        CASHSHOP_OPERATION = 0xC5,
        
        // MTS
        ENTER_MTS = 0x63,
        MTS_TAB = 0xB9,
        
        // Minigames
        HIRED_MERCHANT_REQUEST = 0x3F,
        FREDRICK_ACTION = 0x40,
        DUEY_ACTION = 0x41,
        
        // Storage
        STORAGE = 0x3E,
        
        // Reports
        REPORT = 0x88,
        
        // Quest
        QUEST_ACTION = 0x44,
        
        // Reactor
        TOUCH_REACTOR = 0x91,
        
        // Map Object
        ITEM_SORT = 0x45,
        ITEM_SORT2 = 0x46,
        MOVE_SUMMON = 0x94,
        SUMMON_ATTACK = 0x96,
        DAMAGE_SUMMON = 0x97,
        
        // Rings
        RING_ACTION = 0xA8,
        
        // Other
        FACE_EXPRESSION = 0x33,
        NOTE_ACTION = 0x42,
        USE_MOUNT_FOOD = 0x4D,
        SCRIPTED_ITEM = 0x4E,
        PARTY_SEARCH_REGISTER = 0xA4,
        PARTY_SEARCH_START = 0xA5,
        PLAYER_UPDATE = 0xAF,
        TOUCHING_CS = 0xC6,
        CASH_OPERATION = 0xC5,
        COUPON_CODE = 0xC7,
        MAPLETV = 0xC8,
        MOVE_PET = 0xCA,
        PET_CHAT_2 = 0xCB,
        PET_COMMAND_2 = 0xCC,
        PET_LOOT_2 = 0xCF,
        AUTO_PET_POT = 0xD1,
        MOVE_DRAGON = 0xD4,
        MOVE_ANDROID = 0xE8,
        UPDATE_QUEST = 0xEC,
        QUEST_ITEM = 0xED,
        GRENADE_EFFECT = 0xFE,
        SKILL_MACRO = 0xFF,
        REWARD_ITEM = 0x102,
        MAKER_SKILL = 0x108,
        REMOTE_STORE = 0x3B
    }
}