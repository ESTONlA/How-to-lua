using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class GameAccess
{
    internal static bool WorldReady(LuaHost host) => host.IsHost && IslandManager.IsInitialized && !IslandManager.IsLoading;
    internal static Player Player(LuaHost host, DynValue id)
    {
        string steamId = LuaArguments.SteamId(id);
        return host.IsHost ? PlayerManager.Players.FirstOrDefault(p => p && p.IsServerInitialized && !p.IsDeinitializing && p.SteamID.ToString() == steamId) : null;
    }
    internal static IEnumerable<Item> Items() => ItemManager.Items.Values.Where(i => i && i.IsServerInitialized && !i.IsDeinitializing && !i.IsDestroying).Distinct();
    internal static Item Item(LuaHost host, DynValue id)
    {
        string networkId = LuaArguments.NetworkId(id);
        return host.IsHost ? Items().FirstOrDefault(i => i.NetworkObject && i.NetworkObject.ObjectId.ToString() == networkId) : null;
    }
    internal static bool Loose(Item item) => item && !item.HasPlayerHolder && !item.IsInInventory && !item.AttachedRod && !item.BirdHolder;
    internal static bool Protected(Item item) => item && (item.IsQuestItem || item.DeadPlayer || (item is Creature creature && creature.BossType != BossType.None));
    internal static Vector3 Position(CallbackArguments args, int start) => new(
        (float)LuaArguments.Number(args[start], "x", -100000, 100000),
        (float)LuaArguments.Number(args[start + 1], "y", -100000, 100000),
        (float)LuaArguments.Number(args[start + 2], "z", -100000, 100000));
}
