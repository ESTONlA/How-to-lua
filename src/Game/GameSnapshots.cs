using System.Collections.Generic;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class GameSnapshots
{
    internal static int Balance => MoneyManager.Instance && MoneyManager.Instance.IsServerInitialized ? MoneyManager.Instance._money.Value : MoneyManager.Money;
    internal static Dictionary<string, object> Position(Vector3 pos) => new() { ["x"] = pos.x, ["y"] = pos.y, ["z"] = pos.z };
    internal static Dictionary<string, object> PlayerInfo(Player player)
    {
        if (!player) return null;
        return new()
        {
            ["name"] = player.SteamName ?? "Unknown", ["steam_id"] = player.SteamID.ToString(),
            ["client_id"] = player.Owner?.ClientId.ToString(), ["health"] = player.Vitals ? player.Vitals.Health : 0,
            ["fullness"] = player.Vitals ? player.Vitals.Fullness : 0, ["afk"] = player.IsAfk,
            ["poison"] = player.Vitals ? player.Vitals._syncedPoison.Value : 0,
            ["fire"] = player.Vitals ? player.Vitals._syncedFire.Value : 0,
            ["position"] = Position(player.Transform ? player.Transform.position : player.transform.position)
        };
    }
    internal static Dictionary<string, object> ItemInfo(Item item)
    {
        if (!item) return null;
        return new()
        {
            ["id"] = (int)item.ID, ["name"] = Name(item), ["worth"] = item.TotalWorth,
            ["network_id"] = item.NetworkObject ? item.NetworkObject.ObjectId.ToString() : null,
            ["position"] = Position(item.transform.position), ["is_fish"] = item is Fish,
            ["boss_type"] = item is Creature creature ? creature.BossType.ToString() : "None"
        };
    }
    internal static string Name(Item item)
    {
        if (!item) return "Unknown";
        try { return item.GetName(); } catch { return item.name; }
    }
    internal static Dictionary<string, object> WorldInfo() => new()
    {
        ["is_host"] = Server.Instance && Server.Instance.IsServerInitialized,
        ["island"] = OnlineIslandManager.Instance ? (int)OnlineIslandManager.CurIsland : -1,
        ["loading"] = IslandManager.IsLoading, ["initialized"] = IslandManager.IsInitialized,
        ["save_name"] = SaveManager.CurServerSave?.Name,
        ["difficulty"] = SaveManager.CurServerSave?.Difficulty.ToString(),
        ["money"] = Balance
    };
}
