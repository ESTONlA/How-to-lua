using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class ServerApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var server = new Table(script);
        server.Set("settings", DynValue.NewCallback((c, a) => LuaValues.From(script, Settings(host))));
        server.Set("set_friendly_fire", DynValue.NewCallback((c, a) =>
        {
            bool enabled = LuaArguments.Boolean(a[0], "enabled");
            if (!Ready(host)) return DynValue.False;
            ServerSettings.Instance.ToggleFriendlyFire(enabled);
            return DynValue.True;
        }));
        server.Set("set_one_shot", DynValue.NewCallback((c, a) =>
        {
            bool enabled = LuaArguments.Boolean(a[0], "enabled");
            if (!Ready(host)) return DynValue.False;
            if (ServerSettings.OneShotEnabled != enabled) ServerSettings.Instance.ToggleOneShot();
            return DynValue.True;
        }));
        server.Set("set_difficulty", DynValue.NewCallback((c, a) =>
        {
            string name = a[0].Type == DataType.String ? a[0].String.ToLowerInvariant() : "";
            Difficulty difficulty = name switch
            {
                "easy" => Difficulty.Easy, "default" => Difficulty.Default, "hard" => Difficulty.Hard,
                _ => throw new ScriptRuntimeException("Difficulty must be easy, default or hard.")
            };
            if (!Ready(host)) return DynValue.False;
            ServerSettings.Instance.SetDifficulty(difficulty);
            return DynValue.True;
        }));
        server.Set("save", DynValue.NewCallback((c, a) =>
        {
            if (!GameAccess.WorldReady(host) || SaveManager.CurServerSave == null || !host.SaveBudget.Take(Time.unscaledTime, 1, 5)) return DynValue.False;
            SaveManager.SaveServer();
            return DynValue.True;
        }));
        api.Set("server", DynValue.NewTable(server));

        Table economy = api.Get("economy").Table;
        economy.Set("can_afford", DynValue.NewCallback((c, a) =>
        {
            int amount = LuaArguments.Integer(a[0], "amount", 0, 1000000);
            return DynValue.NewBoolean(host.IsHost && MoneyManager.Instance && GameSnapshots.Balance >= amount);
        }));
        economy.Set("give", DynValue.NewCallback((c, a) => Money(host, a, true)));
        economy.Set("spend", DynValue.NewCallback((c, a) => Money(host, a, false)));
    }
    private static DynValue Money(LuaHost host, CallbackArguments args, bool give)
    {
        int amount = LuaArguments.Integer(args[0], "amount", 0, 1000000);
        Player actor = args.Count > 1 ? GameAccess.Player(host, args[1]) : Player.LocalPlayer;
        if (!host.IsHost || !actor || !MoneyManager.Instance || !MoneyManager.Instance.IsServerInitialized) return DynValue.False;
        if (give)
        {
            if ((long)GameSnapshots.Balance + amount > int.MaxValue) return DynValue.False;
            MoneyManager.AddMoney(amount, actor);
        }
        else
        {
            if (GameSnapshots.Balance < amount) return DynValue.False;
            MoneyManager.RemoveMoney(amount, actor);
        }
        return DynValue.True;
    }
    private static bool Ready(LuaHost host) => host.IsHost && ServerSettings.Instance && ServerSettings.Instance.IsServerInitialized;
    private static Dictionary<string, object> Settings(LuaHost host) => !Ready(host) ? null : new()
    {
        ["friendly_fire"] = ServerSettings.UseFriendlyFire, ["one_shot"] = ServerSettings.OneShotEnabled,
        ["difficulty"] = ServerSettings.Difficulty.ToString(), ["health_multiplier"] = ServerSettings.HealthMultiplier,
        ["damage_multiplier"] = ServerSettings.DamageMultiplier
    };
}
