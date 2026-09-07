using System;
using MoonSharp.Interpreter;

namespace HowToLua;

internal static class CoreApi
{
    internal static void Register(LuaMod mod, LuaHost framework)
    {
        mod.Script.Options.DebugPrint = text => framework.FrameworkLogger.LogInfo("[" + mod.Manifest.id + "] " + text);
        Table api = new Table(mod.Script);
        api.Set("button", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterAction(mod, args[0].CastToString(), args[1]);
            return DynValue.Nil;
        }));
        api.Set("on", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterEvent(mod, args.Count > 0 ? args[0].CastToString() : string.Empty, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("command", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterCommand(mod, args.Count > 0 ? args[0].CastToString() : string.Empty, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("after", DynValue.NewCallback((context, args) =>
        {
            framework.AddTimer(mod, args.Count > 0 ? (float)args[0].CastToNumber() : 0f, false, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("every", DynValue.NewCallback((context, args) =>
        {
            framework.AddTimer(mod, args.Count > 0 ? (float)args[0].CastToNumber() : 0f, true, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("chat", DynValue.NewCallback((context, args) =>
        {
            if (Server.Instance && Server.Instance.IsServerInitialized && args.Count > 0)
            {
                string message = "[" + mod.Manifest.name + "] " + args[0].ToPrintString();
                if (message.Length > 200) throw new ScriptRuntimeException("Chat message exceeds 200 characters including prefix.");
                Server.Instance.SendChatMessage(message, null);
            }
            return DynValue.Nil;
        }));
        api.Set("money", DynValue.NewCallback((context, args) =>
        {
            if (Server.Instance && Server.Instance.IsServerInitialized && Player.LocalPlayer && args.Count > 0)
            {
                double amount = args[0].CastToNumber() ?? double.NaN;
                if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0 || amount > 1000000)
                    throw new ScriptRuntimeException("Money must be a number between 0 and 1000000.");
                if (!MoneyManager.Instance || !MoneyManager.Instance.IsServerInitialized) return DynValue.False;
                int grant = (int)Math.Min(Math.Round(amount), (long)int.MaxValue - GameSnapshots.Balance);
                if (grant > 0) MoneyManager.AddMoney(grant, Player.LocalPlayer);
                return DynValue.True;
            }
            return DynValue.False;
        }));
        api.Set("get_data", DynValue.NewCallback((context, args) =>
        {
            string key = args.Count > 0 ? args[0].CastToString() : string.Empty;
            string fallback = args.Count > 1 ? args[1].ToPrintString() : string.Empty;
            return DynValue.NewString(mod.Data.Bind("Data", LuaMod.SafeKey(key), fallback).Value);
        }));
        api.Set("set_data", DynValue.NewCallback((context, args) =>
        {
            if (args.Count > 1)
            {
                string value = args[1].ToPrintString();
                if (value.Length > 4096) throw new ScriptRuntimeException("Data value exceeds 4096 characters.");
                mod.Data.Bind("Data", LuaMod.SafeKey(args[0].CastToString()), string.Empty).Value = value;
                mod.Data.Save();
            }
            return DynValue.Nil;
        }));
        api.Set("is_host", DynValue.NewCallback((context, args) => DynValue.NewBoolean(Server.Instance && Server.Instance.IsServerInitialized)));
        api.Set("log", DynValue.NewCallback((context, args) =>
        {
            framework.FrameworkLogger.LogInfo("[" + mod.Manifest.id + "] " + (args.Count > 0 ? args[0].ToPrintString() : string.Empty));
            return DynValue.Nil;
        }));
        mod.Script.Globals.Set("htf", DynValue.NewTable(api));
        GameApi.Register(mod, framework);
    }
}
