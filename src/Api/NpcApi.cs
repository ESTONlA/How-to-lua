using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoonSharp.Interpreter;

namespace HowToLua;

[LuaBridge]
internal static class NpcApi
{
    private static readonly FieldInfo Registry = AccessTools.Field(typeof(NPCManager), "_idToNpc");
    private static Dictionary<byte, NPC> Npcs => (Dictionary<byte, NPC>)Registry.GetValue(null);
    private static NPC Get(LuaHost host, DynValue value)
    {
        byte id = (byte)LuaArguments.Integer(value, "NPC ID", 0, 255);
        return Ready(host) && Npcs.ContainsKey(id) ? NPCManager.IDToNpc(id) : null;
    }
    private static bool Ready(LuaHost host) => GameAccess.WorldReady(host) && NPCManager.Instance && NPCManager.Instance.IsServerInitialized;
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var npcs = new Table(script);
        npcs.Set("list", DynValue.NewCallback((c, a) => LuaValues.From(script, Ready(host) ? Npcs.Values.Where(n => n).OrderBy(n => n.ID).Select(n => (object)Info(n)).ToArray() : Array.Empty<object>())));
        npcs.Set("get", DynValue.NewCallback((c, a) => LuaValues.From(script, Info(Get(host, a[0])))));
        npcs.Set("quests", DynValue.NewCallback((c, a) =>
        {
            NPC npc = Get(host, a[0]);
            return LuaValues.From(script, npc ? npc.Quests.Select((quest, index) => (object)Quest(quest, index, npc)).ToArray() : Array.Empty<object>());
        }));
        npcs.Set("progression", DynValue.NewCallback((c, a) => LuaValues.From(script, Ready(host) ? new Dictionary<string, object>
            { ["grill_unlocked"] = NPCManager.Instance.GrillUnlocked, ["final_boss_killed"] = NPCManager.Instance.FinalBossKilled } : null)));
        npcs.Set("unlock_grill", DynValue.NewCallback((c, a) =>
        {
            if (!Ready(host)) return DynValue.False;
            NPCManager.UnlockGrill();
            return DynValue.True;
        }));
        api.Set("npcs", DynValue.NewTable(npcs));
    }
    private static Dictionary<string, object> Info(NPC npc)
    {
        if (!npc) return null;
        return new()
        {
            ["id"] = (int)npc.ID, ["name"] = npc.name, ["position"] = GameSnapshots.Position(npc.transform.position),
            ["mouth_position"] = npc.MouthPosForItems ? GameSnapshots.Position(npc.MouthPosForItems.position) : null,
            ["quest_count"] = npc.Quests.Count, ["holding_item"] = NPCManager.Instance.NpcIsHoldingItem(npc.ID)
        };
    }
    private static Dictionary<string, object> Quest(NPCQuest quest, int index, NPC npc)
    {
        if (!quest) return new() { ["index"] = index, ["available"] = false };
        return new()
        {
            ["index"] = index, ["available"] = true, ["name"] = quest.name, ["type"] = quest.Type.ToString(),
            ["total_items"] = (int)quest.TotalItems, ["progress"] = (int)npc.GetServerProgression((byte)index),
            ["only_creatures"] = quest.OnlyCreatures, ["island_to_unlock"] = (int)quest.IslandToUnlock,
            ["bait_reward"] = quest.BaitToReceive ? (object)(int)GameInfo.GetIndexOfBait(quest.BaitToReceive) : null,
            ["item_ids"] = (quest.QuestItems ?? new List<Item>()).Where(i => i).Select(i => (object)(int)i.ID).ToArray()
        };
    }
}
