using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class ProgressionHooks
{
    [HarmonyPatch(typeof(ServerSettings), "OnDifficultyChange")]
    private static class DifficultyPatch
    {
        private static void Postfix(byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("difficulty_changed", () => new object[] { ((Difficulty)prev).ToString(), ((Difficulty)next).ToString() });
        }
    }

    [HarmonyPatch(typeof(BossManager), "OnBossMaxHpChange")]
    private static class BossHealthPatch
    {
        private static void Postfix(int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boss_max_health_changed", () => new object[] { prev, next });
        }
    }

    [HarmonyPatch(typeof(BossManager), "OnIsImmportalChange")]
    private static class BossImmortalPatch
    {
        private static void Postfix(bool prev, bool next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boss_immortality_changed", () => new object[] { prev, next });
        }
    }

    [HarmonyPatch(typeof(NPCManager), "ServerOnClientSpokeToNpc")]
    private static class NpcTalkPatch
    {
        private static void Postfix(NPCManager __instance, byte npcID)
        {
            if (__instance.IsServerInitialized)
                GameHooks.Capture("npc_talk_requested", () => new object[] { (int)npcID });
        }
    }

    [HarmonyPatch(typeof(NPCManager), "ServerSpeak")]
    private static class QuestPatch
    {
        private static void Postfix(NPCManager __instance, byte npcID, byte questIndex, byte progression, QuestLineType lineType)
        {
            if (__instance.IsServerInitialized && (lineType == QuestLineType.ItemReceived || lineType == QuestLineType.OnCompleted))
                GameHooks.Capture("npc_quest_progressed", () => new object[] { (int)npcID, (int)questIndex, (int)progression, lineType.ToString() });
        }
    }

    [HarmonyPatch(typeof(NPCManager), "OnGrillUnlockedChange")]
    private static class GrillPatch
    {
        private static void Postfix(bool prev, bool next, bool asServer)
        {
            if (asServer && !prev && next)
                GameHooks.Capture("grill_unlocked", () => new object[] {  });
        }
    }
}

