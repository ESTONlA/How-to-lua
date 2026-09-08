using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace HowToLua;

[LuaBridge]
internal static class BossApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var bosses = new Table(script);
        bosses.Set("info", DynValue.NewCallback((c, a) => LuaValues.From(script, host.IsHost && BossManager.Boss ? new Dictionary<string, object>
        {
            ["item"] = GameSnapshots.ItemInfo(BossManager.Boss), ["health"] = BossManager.Boss._hp.Value,
            ["max_health"] = BossManager.BossMaxHp, ["immortal"] = BossManager.IsImmortal,
            ["spawn_tick"] = (double)BossManager.BossSpawnTick, ["leaves_tick"] = (double)BossManager.BossLeavesTick,
            ["duration_ticks"] = (double)BossManager.BossTotalTimeInTicks
        } : null)));
        bosses.Set("set_immortal", DynValue.NewCallback((c, a) =>
        {
            bool enabled = LuaArguments.Boolean(a[0], "enabled");
            if (!host.IsHost || !BossManager.Boss || BossManager.Boss.IsDead) return DynValue.False;
            BossManager.ToggleImmortal(enabled);
            return DynValue.True;
        }));
        bosses.Set("scaled_health", DynValue.NewCallback((c, a) =>
        {
            int health = LuaArguments.Integer(a[0], "base health", 1, 100000);
            float multiplier = a.Count > 1 ? (float)LuaArguments.Number(a[1], "multiplier", 0, 10) : 0.5f;
            return host.IsHost ? DynValue.NewNumber(BossManager.GetBossMaxHp(health, multiplier)) : DynValue.Nil;
        }));
        bosses.Set("scaled_damage", DynValue.NewCallback((c, a) =>
        {
            int damage = LuaArguments.Integer(a[0], "base damage", 0, 100000);
            float multiplier = a.Count > 1 ? (float)LuaArguments.Number(a[1], "multiplier", 0, 10) : 0.2f;
            return host.IsHost ? DynValue.NewNumber(BossManager.GetBossDamage(damage, multiplier)) : DynValue.Nil;
        }));
        api.Set("bosses", DynValue.NewTable(bosses));
    }
}
