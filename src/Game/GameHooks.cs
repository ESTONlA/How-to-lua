using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal sealed class GameHooks : IDisposable
{
    private static GameHooks _active;
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly LuaHost _host;
    private bool IsHost => _host.IsHost;
    private bool _wasHost;
    private readonly HashSet<Fish> _hookedFish = new();
    private readonly HashSet<Creature> _killedCreatures = new();
    private readonly Dictionary<Player, string[]> _players = new();
    private float _nextPlayerPoll;
    private readonly HashSet<string> _reportedErrors = new();
    internal GameHooks(LuaHost host) { _host = host; _active = this; }
    internal void Tick()
    {
        if (_wasHost != IsHost)
        {
            _wasHost = IsHost;
            _hookedFish.Clear();
            _killedCreatures.Clear();
            _players.Clear();
            _reportedErrors.Clear();
            _nextPlayerPoll = 0;
            if (IsHost) Capture("server_started", () => new object[] { GameSnapshots.WorldInfo() });
            else
            {
                _host.ResetSessionEvents();
                _host.Emit("server_stopped");
            }
        }
        try { PollPlayers(); }
        catch (Exception ex) { Report("player polling", ex); }
    }
    public void Dispose() { _active = null; _players.Clear(); }

    // Nothing from the bridge may throw into the game's own update or network callback.
    internal static object[] Snapshot(Func<object[]> capture)
    {
        if (_active == null || !_active.IsHost) return null;
        try { return capture(); }
        catch (Exception ex) { _active.Report("snapshot", ex); return null; }
    }
    internal static void Publish(string name, object[] values)
    {
        if (values != null) _active?._host.Emit(name, values);
    }
    internal static void Capture(string name, Func<object[]> capture) => Publish(name, Snapshot(capture));
    private void Report(string source, Exception ex)
    {
        if (_reportedErrors.Count < 32 && _reportedErrors.Add(source + ex.Message))
            _host.FrameworkLogger.LogError("Game hook " + source + ": " + ex);
    }
    private void PollPlayers()
    {
        if (Time.unscaledTime < _nextPlayerPoll) return;
        _nextPlayerPoll = Time.unscaledTime + 1f;
        if (!IsHost) { _players.Clear(); return; }
        var current = PlayerManager.Players.Where(x => x && x.SteamID != 0).ToArray();
        foreach (Player player in current)
        {
            if (_players.ContainsKey(player)) continue;
            string[] info = { player.SteamName ?? "Unknown", player.SteamID.ToString() };
            _players.Add(player, info);
            _host.Emit("player_joined", info[0], info[1], GameSnapshots.PlayerInfo(player));
        }
        foreach (Player player in _players.Keys.Where(x => !current.Contains(x)).ToArray())
        {
            string[] info = _players[player];
            _players.Remove(player);
            _host.Emit("player_left", info[0], info[1]);
        }
        _hookedFish.RemoveWhere(x => !x);
        _killedCreatures.RemoveWhere(x => !x);
    }

    internal void OnFishHooked(Fish fish)
    {
        if (!IsHost || !fish || !_hookedFish.Add(fish))
        {
            return;
        }

        Capture("fish_hooked", () => new object[] { SafeName(fish), GameSnapshots.ItemInfo(fish) });
    }

    internal void OnCreatureDied(Creature creature)
    {
        if (!IsHost || !creature || !_killedCreatures.Add(creature))
        {
            return;
        }

        string name = SafeName(creature);
        Capture("creature_killed", () => new object[] { name, GameSnapshots.ItemInfo(creature) });
        if (creature.BossType != BossType.None)
        {
            Capture("boss_killed", () => new object[] { name, GameSnapshots.ItemInfo(creature) });
        }
    }

    private static string SafeName(Creature creature)
    {
        try
        {
            return creature.GetName();
        }
        catch
        {
            return "Unknown";
        }
    }

    [HarmonyPatch(typeof(Item), "OnAttachedRodChange")]
    private static class FishHookPatch
    {
        private static void Postfix(Item __instance, FishingRod prev, FishingRod next, bool asServer)
        {
            if (asServer && prev && !next && __instance is Fish)
                Capture("fish_released", () => new object[] { GameSnapshots.Name(__instance), GameSnapshots.ItemInfo(__instance) });
            if (asServer && !prev && next && __instance is Fish fish)
            {
                _active?.OnFishHooked(fish);
            }
        }
    }

    [HarmonyPatch(typeof(Creature), "OnHealthChange")]
    private static class CreatureDeathPatch
    {
        private static void Postfix(Creature __instance, int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                Capture("creature_health_changed", () => new object[] { GameSnapshots.ItemInfo(__instance), prev, next });
            if (asServer && prev > 0 && next <= 0) _active?.OnCreatureDied(__instance);
        }
    }

    [HarmonyPatch(typeof(ChatManager), "SendTypedMessage")]
    private static class ChatCommandPatch
    {
        private static readonly FieldInfo InputField = typeof(ChatManager).GetField("_chatInputField", PrivateInstance);

        private static bool Prefix(ChatManager __instance)
        {
            TMP_InputField input = InputField?.GetValue(__instance) as TMP_InputField;
            if (!input || _active == null || !_active._host.TryRunCommand(input.text))
            {
                return true;
            }

            input.text = string.Empty;
            return false;
        }
    }
}
