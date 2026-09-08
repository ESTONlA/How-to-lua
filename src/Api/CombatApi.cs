using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class CombatApi
{
    internal static void Register(Script script, Table api, LuaHost host)
    {
        Table players = api.Get("players").Table;
        players.Set("damage", DynValue.NewCallback((c, a) =>
        {
            Player player = GameAccess.Player(host, a[0]);
            int damage = LuaArguments.Integer(a[1], "damage", 0, 1000);
            if (!player || !player.Vitals || player.Vitals.Health <= 0) return DynValue.False;
            player.Vitals.TakeDamage(damage);
            return DynValue.True;
        }));
        players.Set("poison", DynValue.NewCallback((c, a) => Affect(host, a[0], false)));
        players.Set("ignite", DynValue.NewCallback((c, a) => Affect(host, a[0], true)));
        players.Set("held_item", DynValue.NewCallback((c, a) =>
        {
            Player player = GameAccess.Player(host, a[0]);
            return LuaValues.From(script, player && player.Holding ? GameSnapshots.ItemInfo(player.Holding.HeldItem) : null);
        }));
        var combat = new Table(script);
        combat.Set("weapon", DynValue.NewCallback((c, a) =>
        {
            Weapon weapon = GameAccess.Item(host, a[0]) as Weapon;
            if (!weapon || !weapon.Attachments) return DynValue.Nil;
            Attachments parts = weapon.Attachments;
            return LuaValues.From(script, new Dictionary<string, object>
            {
                ["damage"] = weapon.Damage, ["ads_fov"] = weapon.AdsFov, ["ads_speed_damping"] = weapon.AdsSpeedDamping,
                ["sight"] = (int)parts.Sight, ["barrel"] = (int)parts.BarrelAttachment, ["ammo_type"] = (int)parts.AmmoType,
                ["extended_magazine"] = parts.ExtendedMag, ["laser_sight"] = parts.LaserSight,
                ["current_upgrade"] = Bullet(parts.GetCurBulletUpgrade()), ["next_upgrade"] = Bullet(parts.GetNextBulletUpgrade())
            });
        }));
        combat.Set("melee", DynValue.NewCallback((c, a) =>
        {
            Melee melee = GameAccess.Item(host, a[0]) as Melee;
            return LuaValues.From(script, melee ? new Dictionary<string, object>
            { ["sharpness"] = (int)melee.SharpnessIndex, ["current_upgrade"] = Sharpness(melee.GetCurSharpness()), ["next_upgrade"] = Sharpness(melee.GetNextSharpnessUpgrade()) } : null);
        }));
        combat.Set("upgrade_bullets", DynValue.NewCallback((c, a) =>
        {
            Weapon weapon = GameAccess.Item(host, a[0]) as Weapon;
            if (!weapon || !weapon.Attachments || weapon.Attachments.GetNextBulletUpgrade() == null) return DynValue.False;
            weapon.Attachments.UpgradeBullets();
            return DynValue.True;
        }));
        combat.Set("sharpen", DynValue.NewCallback((c, a) =>
        {
            Melee melee = GameAccess.Item(host, a[0]) as Melee;
            if (!melee || melee.GetNextSharpnessUpgrade() == null) return DynValue.False;
            melee.UpgradeSharpness();
            return DynValue.True;
        }));
        api.Set("combat", DynValue.NewTable(combat));
    }
    private static DynValue Affect(LuaHost host, DynValue id, bool fire)
    {
        Player player = GameAccess.Player(host, id);
        if (!player || !player.Vitals || player.Vitals.Health <= 0) return DynValue.False;
        if (fire) player.Vitals.ApplyNewFire(); else player.Vitals.ApplyNewPoison();
        return DynValue.True;
    }
    private static Dictionary<string, object> Bullet(BulletUpgrade upgrade) => upgrade == null ? null : new() { ["damage"] = upgrade.Damage, ["cost"] = upgrade.Cost };
    private static Dictionary<string, object> Sharpness(SharpnessUpgrade upgrade) => upgrade == null ? null : new() { ["damage"] = upgrade.Damage, ["cost"] = upgrade.Cost };
}
