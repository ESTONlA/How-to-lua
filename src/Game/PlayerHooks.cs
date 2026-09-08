using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class PlayerHooks
{
    [HarmonyPatch(typeof(PlayerVitals), "OnHealthChange")]
    private static class HealthPatch
    {
        private static void Postfix(Player ____player, int prev, int next, bool asServer)
        {
            if (!asServer || prev == next) return;
            GameHooks.Capture("player_health_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), prev, next });
            if (prev > 0 && next <= 0)
                GameHooks.Capture("player_died", () => new object[] { GameSnapshots.PlayerInfo(____player) });
            if (prev <= 0 && next > 0)
                GameHooks.Capture("player_revived", () => new object[] { GameSnapshots.PlayerInfo(____player) });
        }
    }

    [HarmonyPatch(typeof(PlayerVitals), "OnFullnessChange")]
    private static class FullnessPatch
    {
        private static void Postfix(Player ____player, int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("player_fullness_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), prev, next });
        }
    }

    [HarmonyPatch(typeof(PlayerVitals), "OnPoisonChange")]
    private static class PoisonPatch
    {
        private static void Postfix(Player ____player, int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("player_poison_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), prev, next });
        }
    }

    [HarmonyPatch(typeof(PlayerVitals), "OnFireChange")]
    private static class FirePatch
    {
        private static void Postfix(Player ____player, int prev, int next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("player_fire_changed", () => new object[] { GameSnapshots.PlayerInfo(____player), prev, next });
        }
    }
}
