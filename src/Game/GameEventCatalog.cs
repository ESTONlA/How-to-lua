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
        "player_poison_changed", "player_fire_changed"
    };
}
