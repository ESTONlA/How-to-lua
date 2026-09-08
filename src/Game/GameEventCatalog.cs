using System.Collections.Generic;

namespace HowToLua;

internal static class GameEventCatalog
{
    internal static readonly HashSet<string> Names = new()
    {
        "fish_hooked", "creature_killed", "boss_killed", "server_command", "player_joined", "player_left",
        "server_started", "server_stopped", "server_save_requested", "island_changing", "island_loaded",
        "boss_spawned", "boss_despawned", "creature_health_changed", "money_changed",
        "item_picked_up", "item_dropped", "item_sold", "fish_sold", "item_cooked", "item_skin_changed",
        "player_health_changed", "player_died", "player_revived", "player_fullness_changed",
        "player_poison_changed", "player_fire_changed",
        "item_spawned", "item_despawned", "fish_released",
        "inventory_changed", "inventory_slot_changed", "inventory_pockets_changed", "inventory_bait_changed", "inventory_bait_stock_changed",
        "boat_driver_changed", "boat_motor_changed", "boat_skin_changed", "boat_radar_changed",
        "difficulty_changed", "boss_max_health_changed", "boss_immortality_changed",
        "npc_talk_requested", "npc_quest_progressed", "grill_unlocked",
        "weapon_sight_changed", "weapon_barrel_changed", "weapon_ammo_type_changed", "weapon_magazine_changed", "weapon_laser_changed"
    };
}
