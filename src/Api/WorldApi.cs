using MoonSharp.Interpreter;

namespace HowToLua;

internal static class WorldApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var world = new Table(script);
        world.Set("info", DynValue.NewCallback((context, args) => LuaValues.From(script, GameSnapshots.WorldInfo())));
        world.Set("spawn_position", DynValue.NewCallback((context, args) => LuaValues.From(script,
            host.IsHost && IslandManager.IsInitialized && !IslandManager.IsLoading ? GameSnapshots.Position(SpawnManager.PlayerSpawnPos) : null)));
        world.Set("boss", DynValue.NewCallback((context, args) => LuaValues.From(script, host.IsHost ? GameSnapshots.ItemInfo(BossManager.Boss) : null)));
        api.Set("world", DynValue.NewTable(world));
        var economy = new Table(script);
        economy.Set("balance", DynValue.NewCallback((context, args) => DynValue.NewNumber(GameSnapshots.Balance)));
        api.Set("economy", DynValue.NewTable(economy));
    }
}
