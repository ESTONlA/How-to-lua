using System.Collections;
using HarmonyLib;

namespace HowToLua;

internal static class WorldHooks
{
    [HarmonyPatch(typeof(OnlineIslandManager), "OnIslandChange")]
    private static class IslandChangePatch
    {
        private static void Postfix(byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("island_changing", () => new object[] { (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(IslandManager), "ProcessRequest")]
    private static class IslandLoadedPatch
    {
        private static void Postfix(byte islandId, ref IEnumerator __result)
        {
            if (__result == null || islandId == byte.MaxValue) return;
            // Observe completion, not IsLoading, which clears before spawn/teleport setup.
            __result = ObservedRoutine.Wrap(__result, () =>
            {
                if (IslandManager.IsInitialized)
                    GameHooks.Capture("island_loaded", () => new object[] { (int)islandId });
            });
        }
    }

    [HarmonyPatch(typeof(BossManager), "InitializeBossFight")]
    private static class BossSpawnPatch
    {
        private static void Prefix(out bool __state) { __state = !BossManager.Boss; }
        private static void Postfix(Creature creature, bool __state)
        {
            if (__state && creature && BossManager.Boss == creature)
                GameHooks.Capture("boss_spawned", () => new object[] { GameSnapshots.Name(creature), GameSnapshots.ItemInfo(creature) });
        }
    }

    [HarmonyPatch(typeof(BossManager), "OnBossRemoved")]
    private static class BossDespawnPatch
    {
        private static void Prefix(out object[] __state)
        {
            __state = GameHooks.Snapshot(() => BossManager.Boss
                ? new object[] { GameSnapshots.Name(BossManager.Boss), GameSnapshots.ItemInfo(BossManager.Boss) } : null);
        }
        private static void Postfix(object[] __state) { GameHooks.Publish("boss_despawned", __state); }
    }

    [HarmonyPatch(typeof(SaveManager), "SaveServer")]
    private static class SavePatch
    {
        private static void Prefix(bool autoSave)
        {
            // SaveSystem handles I/O errors internally; this is a request, not proof of disk persistence.
            GameHooks.Capture("server_save_requested", () => SaveManager.CurServerSave != null
                ? new object[] { SaveManager.CurServerSave.Name, autoSave } : null);
        }
    }
}
