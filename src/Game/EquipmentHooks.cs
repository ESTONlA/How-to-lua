using HarmonyLib;

namespace HowToLua;

[LuaBridge]
internal static class EquipmentHooks
{
    [HarmonyPatch(typeof(Attachments), "OnSightChange")]
    private static class SightPatch
    {
        private static void Postfix(Attachments __instance, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("weapon_sight_changed", () => new object[] { GameSnapshots.ItemInfo(__instance.Weapon), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(Attachments), "OnBarrelChange")]
    private static class BarrelPatch
    {
        private static void Postfix(Attachments __instance, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("weapon_barrel_changed", () => new object[] { GameSnapshots.ItemInfo(__instance.Weapon), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(Attachments), "OnAmmoChange")]
    private static class AmmoPatch
    {
        private static void Postfix(Attachments __instance, byte prev, byte next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("weapon_ammo_type_changed", () => new object[] { GameSnapshots.ItemInfo(__instance.Weapon), (int)prev, (int)next });
        }
    }

    [HarmonyPatch(typeof(Attachments), "OnMagChange")]
    private static class MagPatch
    {
        private static void Postfix(Attachments __instance, bool prev, bool next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("weapon_magazine_changed", () => new object[] { GameSnapshots.ItemInfo(__instance.Weapon), prev, next });
        }
    }

    [HarmonyPatch(typeof(Attachments), "OnLaserSightChange")]
    private static class LaserPatch
    {
        private static void Postfix(Attachments __instance, bool prev, bool next, bool asServer)
        {
            if (asServer && prev != next)
                GameHooks.Capture("weapon_laser_changed", () => new object[] { GameSnapshots.ItemInfo(__instance.Weapon), prev, next });
        }
    }
}

