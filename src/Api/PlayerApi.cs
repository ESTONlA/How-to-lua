using System;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class PlayerApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var players = new Table(script);
        players.Set("local_player", DynValue.NewCallback((c, a) => LuaValues.From(script, host.IsHost ? GameSnapshots.PlayerInfo(Player.LocalPlayer) : null)));
        players.Set("list", DynValue.NewCallback((context, args) => LuaValues.From(script,
            host.IsHost ? PlayerManager.Players.Where(IsReady).Select(p => (object)GameSnapshots.PlayerInfo(p)).ToArray() : Array.Empty<object>())));
        players.Set("get", DynValue.NewCallback((context, args) =>
        {
            string id = LuaArguments.SteamId(args[0]);
            return LuaValues.From(script, host.IsHost ? GameSnapshots.PlayerInfo(Find(id)) : null);
        }));
        players.Set("heal", DynValue.NewCallback((context, args) => Restore(host, args, true)));
        players.Set("feed", DynValue.NewCallback((context, args) => Restore(host, args, false)));
        players.Set("teleport", DynValue.NewCallback((context, args) =>
        {
            string id = LuaArguments.SteamId(args[0]);
            var position = new Vector3((float)LuaArguments.Number(args[1], "x", -100000, 100000),
                (float)LuaArguments.Number(args[2], "y", -100000, 100000),
                (float)LuaArguments.Number(args[3], "z", -100000, 100000));
            float? yaw = args.Count > 4 && !args[4].IsNil() ? (float)LuaArguments.Number(args[4], "yaw", -360, 360) : (float?)null;
            Player player = host.IsHost ? Find(id) : null;
            if (!player || player.Owner == null || !player.Owner.IsActive || !player.Vitals || player.Vitals.Health <= 0 || IslandManager.IsLoading)
                return DynValue.False;
            Transform body = player.Transform ? player.Transform : player.transform;
            player.RPCTeleport(player.Owner, position, yaw ?? body.eulerAngles.y);
            return DynValue.True;
        }));
        api.Set("players", DynValue.NewTable(players));
    }

    private static DynValue Restore(LuaHost host, CallbackArguments args, bool health)
    {
        string id = LuaArguments.SteamId(args[0]);
        int amount = (int)LuaArguments.Number(args[1], "amount", 0, 100);
        Player player = host.IsHost ? Find(id) : null;
        if (!player || !player.Vitals || !player.Vitals.IsServerInitialized || player.Vitals.Health <= 0) return DynValue.False;
        if (health) player.Vitals.Heal(amount);
        else player.Vitals.RestoreFullness(amount);
        return DynValue.True;
    }

    private static bool IsReady(Player player) => player && player.IsServerInitialized && !player.IsDeinitializing;
    private static Player Find(string id) => PlayerManager.Players.FirstOrDefault(p => IsReady(p) && p.SteamID.ToString() == id);
}
