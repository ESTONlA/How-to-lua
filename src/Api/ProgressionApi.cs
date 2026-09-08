using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class ProgressionApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        Table world = api.Get("world").Table;
        world.Set("islands", DynValue.NewCallback((c, a) => LuaValues.From(script, GameAccess.WorldReady(host) && OnlineIslandManager.Instance
            ? Enumerable.Range(0, Math.Max(0, IslandManager.TotalIslands - 1)).Select(i => (object)new Dictionary<string, object>
            { ["index"] = i, ["unlocked"] = i <= OnlineIslandManager.MaxIslandUnlocked, ["current"] = i == OnlineIslandManager.CurIsland }).ToArray() : Array.Empty<object>())));
        world.Set("travel", DynValue.NewCallback((c, a) =>
        {
            int index = LuaArguments.Integer(a[0], "island", 0, 254);
            if (!Ready(host) || index >= IslandManager.TotalIslands - 1 || index == OnlineIslandManager.CurIsland || !BoatManager.Boat || !host.TravelBudget.Take(Time.unscaledTime, 1, 2)) return DynValue.False;
            OnlineIslandManager.TpToSpecificIsland((byte)index);
            return DynValue.True;
        }));
        world.Set("unlock_island", DynValue.NewCallback((c, a) =>
        {
            int index = LuaArguments.Integer(a[0], "island", 0, 254);
            if (!Ready(host) || index >= IslandManager.TotalIslands - 1) return DynValue.False;
            OnlineIslandManager.Instance.UnlockIsland((byte)index);
            return DynValue.True;
        }));
        world.Set("water_at", DynValue.NewCallback((c, a) =>
        {
            Vector3 position = GameAccess.Position(a, 0);
            if (!GameAccess.WorldReady(host)) return DynValue.Nil;
            var info = WaterManager.GetWaterInfo(position);
            return LuaValues.From(script, new Dictionary<string, object> { ["underwater"] = info.Key, ["depth"] = info.Value, ["height"] = position.y + info.Value });
        }));
        world.Set("rules", DynValue.NewCallback((c, a) => LuaValues.From(script, GameAccess.WorldReady(host) ? new Dictionary<string, object>
        {
            ["headshot_multiplier"] = GameInfo.HeadShotDamageMulti, ["max_item_velocity"] = GameInfo.MaxItemVel,
            ["max_creature_velocity"] = GameInfo.MaxAliveCreatureVel, ["player_kill_force"] = GameInfo.PlayerKillForce,
            ["creature_death_force_multiplier"] = GameInfo.PlayerDeathForceMultiFromCreature,
            ["boat_projectile_force"] = GameInfo.BoatProjectileForce,
            ["fish_velocity_damage_min"] = GameInfo.DefaultMinMaxFishVelForDamage.x,
            ["fish_velocity_damage_max"] = GameInfo.DefaultMinMaxFishVelForDamage.y,
            ["fish_velocity_damage_multiplier"] = GameInfo.DefaultVelDamageMulti, ["seed"] = GameInfo.Seed
        } : null)));
    }
    private static bool Ready(LuaHost host) => GameAccess.WorldReady(host) && OnlineIslandManager.Instance && OnlineIslandManager.Instance.IsServerInitialized;
}
