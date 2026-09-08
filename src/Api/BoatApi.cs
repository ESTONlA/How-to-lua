using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MoonSharp.Interpreter;
using UnityEngine;

namespace HowToLua;

[LuaBridge]
internal static class BoatApi
{
    private static readonly FieldInfo Motors = AccessTools.Field(typeof(Boat), "_motors");
    private static Boat Get(LuaHost host) => host.IsHost && BoatManager.Boat && BoatManager.Boat.IsServerInitialized ? BoatManager.Boat : null;
    internal static void Register(Script script, Table api, LuaHost host)
    {
        var boat = new Table(script);
        boat.Set("info", DynValue.NewCallback((c, a) => LuaValues.From(script, Info(Get(host)))));
        boat.Set("unlock", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            if (!target) return DynValue.False;
            target.UnlockBoat();
            return DynValue.True;
        }));
        boat.Set("unlock_radar", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            if (!target) return DynValue.False;
            target.UnlockBoatRadar();
            return DynValue.True;
        }));
        boat.Set("upgrade_motor", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            int index = LuaArguments.Integer(a[0], "motor", 0, 255);
            if (!target || index >= ((ICollection)Motors.GetValue(target)).Count || index < target.MotorIndex) return DynValue.False;
            target.SetMotor((byte)index);
            return DynValue.True;
        }));
        boat.Set("set_skin", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            int index = LuaArguments.Integer(a[0], "skin", 0, 255);
            if (!target || !target.SkinPreset || index >= target.SkinPreset.Skins.Count) return DynValue.False;
            target.ServerSetSkin((byte)index);
            return DynValue.True;
        }));
        boat.Set("eject_driver", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            if (!target || !target.Driver) return DynValue.False;
            target.TrySetDriver(null);
            return DynValue.True;
        }));
        boat.Set("return_to_spawn", DynValue.NewCallback((c, a) =>
        {
            Boat target = Get(host);
            if (!target || !target.HiddenPhysicsRig || !BoatManager.Instance || !GameAccess.WorldReady(host)) return DynValue.False;
            target.TrySetDriver(null);
            BoatManager.Instance.TryMoveBoat(SpawnManager.BoatSpawnPos, SpawnManager.BoatSpawnRot);
            return DynValue.True;
        }));
        api.Set("boat", DynValue.NewTable(boat));
    }
    internal static Dictionary<string, object> Info(Boat boat)
    {
        if (!boat) return null;
        Transform visual = boat.VisualBoat ? boat.VisualBoat : boat.transform;
        return new()
        {
            ["position"] = GameSnapshots.Position(visual.position), ["rotation"] = GameSnapshots.Position(visual.eulerAngles),
            ["velocity"] = GameSnapshots.Position(boat.Velocity), ["angular_velocity"] = GameSnapshots.Position(boat.AngularVelocity),
            ["speed"] = boat.VelocityMag, ["unlocked"] = boat.BoatUnlocked, ["radar_unlocked"] = boat.BoatRadarUnlocked,
            ["motor"] = (int)boat.MotorIndex, ["motor_count"] = ((ICollection)Motors.GetValue(boat)).Count,
            ["skin"] = (int)boat.CurSkin, ["skin_count"] = boat.SkinPreset ? boat.SkinPreset.Skins.Count : 0,
            ["driver"] = GameSnapshots.PlayerInfo(boat.Driver), ["driver_position"] = boat.DriverPos ? GameSnapshots.Position(boat.DriverPos.position) : null
        };
    }
}
