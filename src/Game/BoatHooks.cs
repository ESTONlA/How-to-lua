using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class BoatHooks
{
    [HarmonyPatch(typeof(Boat), "OnDriverChange")]
    private static class DriverPatch
    {
        private static void Postfix(Player prev, Player next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boat_driver_changed", () => new object[] { GameSnapshots.PlayerInfo(prev), GameSnapshots.PlayerInfo(next) });
        }
    }

    [HarmonyPatch(typeof(Boat), "OnMotorChange")]
    private static class MotorPatch
    {
        private static void Postfix(byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boat_motor_changed", () => new object[] { (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(Boat), "OnSkinChange")]
    private static class SkinPatch
    {
        private static void Postfix(byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boat_skin_changed", () => new object[] { (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(Boat), "OnBoatRadarChange")]
    private static class RadarPatch
    {
        private static void Postfix(bool prev, bool next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("boat_radar_changed", () => new object[] { prev, next });
        }
    }
}

