using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MoonSharp.Interpreter;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HowToLua;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("How to Fish.exe")]
public sealed class HowToLuaPlugin : BaseUnityPlugin
{
    private const string PluginGuid = "estonia.howtofish.howtolua";
    private const string PluginName = "How to Lua";
    private const string PluginVersion = "0.1.0";
    private const string Author = "Estonia";
    private static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static HowToLuaPlugin _instance;

    private readonly Dictionary<string, LuaMod> _mods = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LuaCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Fish> _hookedFish = new();
    private readonly HashSet<Creature> _killedCreatures = new();
    private readonly Dictionary<string, string> _foldersById = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loading = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Player, string[]> _players = new();
    private float _nextPlayerPoll;
    private bool _actionMode;
    private readonly List<Button> _actionRows = new();
    internal readonly List<LuaAction> Actions = new();

    private Harmony _harmony;
    private ConfigEntry<bool> _autoLoad;
    private string _modsPath;
    private PauseManager _pauseManager;
    private GameObject _mainScreen;
    private GameObject _optionsScreen;
    private GameObject _serverSettingsScreen;
    private GameObject _menuButton;
    private GameObject _panel;
    private GameObject _buttonTemplate;
    private TextMeshProUGUI _textTemplate;
    private TextMeshProUGUI _modsStatus;
    private TextMeshProUGUI _footerStatus;
    private int _page;
    private float _nextUiUpdate;
    private bool _wasHost;

    private bool IsHost => Server.Instance && Server.Instance.IsServerInitialized;
    internal static HowToLuaPlugin Instance => _instance;
    internal ManualLogSource FrameworkLogger => Logger;

    private void Awake()
    {
        _instance = this;
        _autoLoad = Config.Bind("General", "AutoLoadMods", true, "Loads all valid Lua mods when How to Lua starts.");
        _modsPath = Path.Combine(Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath, "mods");
        Directory.CreateDirectory(_modsPath);

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(HowToLuaPlugin).Assembly);
        if (_autoLoad.Value)
        {
            ReloadMods();
        }

        Logger.LogInfo($"{PluginName} {PluginVersion} by {Author}. Lua mods: {_modsPath}");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        if (_panel)
        {
            Destroy(_panel);
        }

        if (_menuButton)
        {
            Destroy(_menuButton);
        }
    }

    private void Update()
    {
        if (_wasHost != IsHost)
        {
            _wasHost = IsHost;
            _hookedFish.Clear();
            _killedCreatures.Clear();
        }
        EnsurePauseUi();
        TickTimers();
        PollPlayers();
        RefreshUi();
    }

    internal void ReloadMods()
    {
        _mods.Clear();
        _commands.Clear();
        Actions.Clear();
        _foldersById.Clear();
        _loading.Clear();
        Directory.CreateDirectory(_modsPath);
        foreach (string folder in Directory.GetDirectories(_modsPath).OrderBy(x => x))
        {
            try
            {
                LuaManifest manifest = ReadManifest(folder);
                if (!LuaExecution.ValidId(manifest.id)) throw new IOException("Invalid manifest id.");
                if (_foldersById.ContainsKey(manifest.id)) throw new IOException("Duplicate manifest id: " + manifest.id);
                _foldersById.Add(manifest.id, folder);
            }
            catch (Exception ex) { Logger.LogError($"Cannot read '{folder}': {ex.Message}"); }
        }
        foreach (string folder in _foldersById.Values.ToArray())
        {
            try { LoadMod(folder); }
            catch (Exception ex) { Logger.LogError($"Cannot load '{folder}': {ex.Message}"); }
        }

        Logger.LogInfo($"Loaded {_mods.Values.Count(x => x.Loaded)} Lua mod(s) from {_modsPath}.");
    }

    private void LoadMod(string folder)
    {
        if (!_loading.Add(folder)) throw new IOException("Lua dependency cycle at " + folder);
        try { LoadModCore(folder); }
        finally { _loading.Remove(folder); }
    }

    private static LuaManifest ReadManifest(string folder)
    {
        string file = Path.Combine(folder, "manifest.json");
        if (new FileInfo(file).Length > 16384) throw new IOException("Manifest exceeds 16 KiB.");
        LuaManifest manifest = new LuaManifest();
        JsonUtility.FromJsonOverwrite(File.ReadAllText(file), manifest);
        return manifest;
    }

    private void LoadModCore(string folder)
    {
        string manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        LuaManifest manifest;
        try
        {
            if (new FileInfo(manifestPath).Length > 16384) throw new IOException("Manifest exceeds 16 KiB.");
            manifest = new LuaManifest();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(manifestPath), manifest);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to read Lua manifest '{manifestPath}': {ex.Message}");
            return;
        }

        if (manifest == null || !LuaExecution.ValidId(manifest.id) || string.IsNullOrWhiteSpace(manifest.name))
        {
            Logger.LogError($"Lua mod at '{folder}' needs a manifest id and name.");
            return;
        }

        manifest.entry = string.IsNullOrWhiteSpace(manifest.entry) ? "main.lua" : manifest.entry;
        string entryPath = LuaExecution.EntryPath(folder, manifest.entry);

        if (_mods.ContainsKey(manifest.id))
        {
            return;
        }

        foreach (string dependency in manifest.dependencies ?? Array.Empty<string>())
        {
            if (!LuaExecution.ValidId(dependency) || !_foldersById.TryGetValue(dependency, out string dependencyFolder))
                throw new IOException($"{manifest.id} requires missing mod '{dependency}'.");
            if (!_mods.ContainsKey(dependency)) LoadMod(dependencyFolder);
            if (!_mods.TryGetValue(dependency, out LuaMod required) || !required.Loaded)
                throw new IOException($"{manifest.id} requires working mod '{dependency}'.");
        }

        LuaMod mod = new LuaMod(manifest, folder, Path.Combine(Paths.ConfigPath, $"HowToLua.{SafeFileName(manifest.id)}.cfg"));
        _mods.Add(manifest.id, mod);
        try
        {
            mod.CreateScript(this);
            LuaExecution.Run(mod.Script, mod.Script.LoadString(File.ReadAllText(entryPath), null, manifest.id + "/" + manifest.entry));
            mod.Loaded = string.IsNullOrWhiteSpace(mod.Error);
            if (mod.Loaded)
            {
                mod.InvokeOptional("on_load");
            }

            if (mod.Loaded)
            {
                Logger.LogInfo($"Loaded Lua mod: {manifest.name} ({manifest.id}) {manifest.version}");
            }
        }
        catch (Exception ex)
        {
            mod.SetError(ex.Message);
        }
    }

    internal void RegisterEvent(LuaMod mod, string eventName, DynValue callback)
    {
        if (!IsCallable(callback) || !new[] { "fish_hooked", "creature_killed", "boss_killed", "server_command", "player_joined", "player_left" }.Contains(eventName))
        {
            throw new ScriptRuntimeException("htf.on requires a supported event name and a Lua function.");
        }

        mod.Events[eventName.Trim().ToLowerInvariant()] = callback;
    }

    internal void DisableMod(LuaMod mod)
    {
        foreach (string name in _commands.Where(x => x.Value.Mod == mod).Select(x => x.Key).ToArray())
            _commands.Remove(name);
        mod.Events.Clear();
        mod.Timers.Clear();
        Actions.RemoveAll(x => x.Mod == mod);
        foreach (LuaMod dependent in _mods.Values.Where(x => x.Loaded && (x.Manifest.dependencies ?? Array.Empty<string>()).Contains(mod.Manifest.id)).ToArray())
            dependent.SetError("Dependency failed: " + mod.Manifest.id);
    }

    internal void RegisterAction(LuaMod mod, string label, DynValue callback)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length > 48 || !IsCallable(callback) || Actions.Count(x => x.Mod == mod) >= 16)
            throw new ScriptRuntimeException("Button requires a label (1-48 characters) and function; maximum 16 per mod.");
        Actions.Add(new LuaAction(mod, label, callback));
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
            Emit("player_joined", DynValue.NewString(info[0]), DynValue.NewString(info[1]));
        }
        foreach (Player player in _players.Keys.Where(x => !current.Contains(x)).ToArray())
        {
            string[] info = _players[player];
            _players.Remove(player);
            Emit("player_left", DynValue.NewString(info[0]), DynValue.NewString(info[1]));
        }
        _hookedFish.RemoveWhere(x => !x);
        _killedCreatures.RemoveWhere(x => !x);
    }

    internal void RegisterCommand(LuaMod mod, string name, DynValue callback)
    {
        name = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsCallable(callback) || !IsValidCommand(name))
        {
            throw new ScriptRuntimeException("htf.command requires a command name and a Lua function.");
        }

        if (_commands.ContainsKey(name))
        {
            throw new ScriptRuntimeException($"Command /{name} is already registered.");
        }

        _commands.Add(name, new LuaCommand(mod, callback));
    }

    internal void AddTimer(LuaMod mod, float seconds, bool repeat, DynValue callback)
    {
        if (!IsCallable(callback) || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0.05f || seconds > 86400f || mod.Timers.Count >= 64)
        {
            throw new ScriptRuntimeException("Timer requires 0.05-86400 seconds and a Lua function; maximum 64 timers per mod.");
        }

        mod.Timers.Add(new LuaTimer(Mathf.Max(0.05f, seconds), repeat, callback));
    }

    internal void Emit(string eventName, params DynValue[] arguments)
    {
        foreach (LuaMod mod in _mods.Values.ToArray())
        {
            if (!mod.Loaded || mod.Manifest.hostOnly && !IsHost || !mod.Events.TryGetValue(eventName, out DynValue callback))
            {
                continue;
            }

            mod.Call(callback, arguments);
        }
    }

    private void TickTimers()
    {
        float now = Time.unscaledTime;
        foreach (LuaMod mod in _mods.Values.ToArray())
        {
            if (!mod.Loaded || mod.Manifest.hostOnly && !IsHost)
            {
                continue;
            }

            foreach (LuaTimer timer in mod.Timers.ToArray())
            {
                if (!mod.Loaded) break;
                if (now < timer.NextRun)
                {
                    continue;
                }

                if (timer.Repeat)
                {
                    timer.NextRun = now + timer.Interval;
                }
                else
                {
                    mod.Timers.Remove(timer);
                }
                mod.Call(timer.Callback);
            }
        }
    }

    internal bool TryRunCommand(string rawCommand)
    {
        if (!IsHost || string.IsNullOrWhiteSpace(rawCommand) || rawCommand[0] != '/')
        {
            return false;
        }

        string[] parts = rawCommand.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !_commands.TryGetValue(parts[0].ToLowerInvariant(), out LuaCommand command) || !command.Mod.Loaded)
        {
            return false;
        }

        Table args = new Table(command.Mod.Script);
        for (int i = 1; i < parts.Length; i++)
        {
            args.Set(i, DynValue.NewString(parts[i]));
        }

        command.Mod.Call(command.Callback, DynValue.NewTable(args));
        foreach (LuaMod listener in _mods.Values.ToArray())
        {
            if (!listener.Loaded || !listener.Events.TryGetValue("server_command", out DynValue callback)) continue;
            Table ownArgs = new Table(listener.Script);
            for (int i = 1; i < parts.Length; i++) ownArgs.Set(i, DynValue.NewString(parts[i]));
            listener.Call(callback, DynValue.NewString(parts[0].ToLowerInvariant()), DynValue.NewTable(ownArgs));
        }
        return true;
    }

    internal void OnFishHooked(Fish fish)
    {
        if (!IsHost || !fish || !_hookedFish.Add(fish))
        {
            return;
        }

        Emit("fish_hooked", DynValue.NewString(SafeName(fish)));
    }

    internal void OnCreatureDied(Creature creature)
    {
        if (!IsHost || !creature || !_killedCreatures.Add(creature))
        {
            return;
        }

        string name = SafeName(creature);
        Emit("creature_killed", DynValue.NewString(name));
        if (creature.BossType != BossType.None)
        {
            Emit("boss_killed", DynValue.NewString(name));
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

    private void EnsurePauseUi()
    {
        PauseManager current = GetPauseManager();
        if (!current || current == _pauseManager && _menuButton && _panel)
        {
            return;
        }

        _pauseManager = current;
        _mainScreen = GetPrivateField<GameObject>(_pauseManager, "_mainScreen");
        _optionsScreen = GetPrivateField<GameObject>(_pauseManager, "_optionsScreen");
        _serverSettingsScreen = GetPrivateField<GameObject>(_pauseManager, "_serverSettingsScreen");
        if (!_mainScreen)
        {
            return;
        }

        _buttonTemplate = FindButtonTemplate();
        _textTemplate = _buttonTemplate ? _buttonTemplate.GetComponentInChildren<TextMeshProUGUI>(true) : FindTextTemplate();
        BuildPauseButton();
        BuildPanel();
    }

    private void BuildPauseButton()
    {
        if (_menuButton)
        {
            Destroy(_menuButton);
        }

        Transform parent = _buttonTemplate ? _buttonTemplate.transform.parent : _mainScreen.transform;
        _menuButton = CreateNativeButton(parent, "Lua Mods", OpenPanel);
        _menuButton.name = "HowToLua_OpenButton";
        _menuButton.transform.SetAsLastSibling();
    }

    private void BuildPanel()
    {
        if (_panel)
        {
            Destroy(_panel);
        }

        Transform parent = _serverSettingsScreen ? _serverSettingsScreen.transform.parent : _mainScreen.transform.parent;
        _panel = new GameObject("HowToLua_Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(parent, false);
        _panel.transform.SetAsLastSibling();
        _panel.SetActive(false);
        GameObject[] screens = GetPrivateField<GameObject[]>(_pauseManager, "_disabledScreensOnPause");
        if (screens != null)
            typeof(PauseManager).GetField("_disabledScreensOnPause", PrivateInstance)?.SetValue(_pauseManager, screens.Where(x => x).Concat(new[] { _panel }).ToArray());
        RectTransform rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 560f);
        rect.anchoredPosition = new Vector2(0f, -6f);
        _panel.GetComponent<Image>().color = new Color(0.25f, 0.43f, 0.56f, 0.78f);
        AddBorder(_panel.transform, 2f, Color.white);

        TextMeshProUGUI title = CreateText(_panel.transform, "How to Lua", 39f, 0f, 224f, 820f, 58f);
        title.alignment = TextAlignmentOptions.Center;
        TextMeshProUGUI subtitle = CreateText(_panel.transform, "LUA MOD FRAMEWORK", 16f, 0f, 182f, 820f, 30f);
        subtitle.alignment = TextAlignmentOptions.Center;
        _modsStatus = CreateText(_panel.transform, string.Empty, 18f, 0f, 45f, 760f, 220f);
        _modsStatus.alignment = TextAlignmentOptions.TopLeft;
        _footerStatus = CreateText(_panel.transform, string.Empty, 14f, 0f, -155f, 760f, 32f);
        _footerStatus.alignment = TextAlignmentOptions.Center;
        _actionRows.Clear();
        for (int i = 0; i < 4; i++)
        {
            int row = i;
            CreatePanelButton("Mod Action", 0f, 115f - i * 55f, 700f, 44f, () => RunAction(row));
            _actionRows.Add(_panel.transform.GetChild(_panel.transform.childCount - 1).GetComponent<Button>());
        }
        CreatePanelButton("Previous", -200f, -105f, 220f, 40f, () => _page = Math.Max(0, _page - 1));
        CreatePanelButton("Next", 200f, -105f, 220f, 40f, () => _page++);
        CreatePanelButton("Reload Lua Mods", -270f, -215f, 240f, 50f, ReloadMods);
        CreatePanelButton("Mods / Actions", 0f, -215f, 240f, 50f, () => { _actionMode = !_actionMode; _page = 0; });
        CreatePanelButton("Back", 270f, -215f, 240f, 50f, ClosePanel);
    }

    private void RunAction(int row)
    {
        LuaAction action = Actions.Where(x => x.Mod.Loaded).Skip(_page * 4 + row).FirstOrDefault();
        if (action != null && (!action.Mod.Manifest.hostOnly || IsHost)) action.Mod.Call(action.Callback);
    }

    private void CreatePanelButton(string label, float x, float y, float width, float height, Action action)
    {
        GameObject button = CreateNativeButton(_panel.transform, label, action);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout)
        {
            layout.ignoreLayout = true;
        }
    }

    private GameObject CreateNativeButton(Transform parent, string label, Action action)
    {
        GameObject buttonObject;
        if (_buttonTemplate)
        {
            buttonObject = Instantiate(_buttonTemplate, parent);
            buttonObject.SetActive(false);
            RemoveLocalization(buttonObject);
            Button templateButton = _buttonTemplate.GetComponent<Button>();
            foreach (Button oldButton in buttonObject.GetComponentsInChildren<Button>(true))
            {
                DestroyImmediate(oldButton);
            }

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Graphic>() ?? buttonObject.GetComponentInChildren<Graphic>(true);
            if (templateButton) { button.colors = templateButton.colors; button.transition = templateButton.transition; }
            button.onClick.AddListener(new UnityAction(action));
            ForceButtonText(buttonObject, label);
            buttonObject.SetActive(true);
            return buttonObject;
        }

        buttonObject = new GameObject("HowToLua_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.06f, 0.12f, 0.74f);
        AddBorder(buttonObject.transform, 2f, Color.white);
        buttonObject.GetComponent<Button>().onClick.AddListener(new UnityAction(action));
        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 18f, 0f, 0f, 220f, 40f);
        text.alignment = TextAlignmentOptions.Center;
        return buttonObject;
    }

    private TextMeshProUGUI CreateText(Transform parent, string value, float size, float x, float y, float width, float height)
    {
        GameObject textObject = new GameObject("HowToLua_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (_textTemplate)
        {
            text.font = _textTemplate.font;
            text.material = _textTemplate.material;
            text.color = _textTemplate.color;
        }
        else
        {
            text.color = Color.white;
        }

        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = size;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private void RefreshUi()
    {
        if (Time.unscaledTime < _nextUiUpdate) return;
        _nextUiUpdate = Time.unscaledTime + 0.25f;
        if (_menuButton)
        {
            ForceButtonText(_menuButton, "Lua Mods");
        }

        if (_panel && _panel.activeSelf && !PauseManager.IsPaused)
        {
            ClosePanel();
        }

        if (!_panel || !_panel.activeInHierarchy)
        {
            return;
        }

        int pages = Math.Max(1, ((_actionMode ? Actions.Count : _mods.Count) + 3) / 4);
        _page = Math.Min(_page, pages - 1);
        List<string> lines = new List<string> { $"{_mods.Count} mods     Page {_page + 1}/{pages}" };
        if (_mods.Count == 0)
        {
            lines.Add("\nNo Lua mods found. Create a folder with manifest.json and main.lua.");
        }
        else
        {
            foreach (LuaMod mod in _mods.Values.OrderBy(x => x.Manifest.name).Skip(_page * 4).Take(4))
            {
                string state = mod.Loaded ? "LOADED" : "ERROR";
                string suffix = mod.Loaded ? $"v{mod.Manifest.version}" : ShortError(mod.Error);
                lines.Add($"\n{state}   {mod.Manifest.name}   {suffix}");
            }
        }

        SetText(_modsStatus, string.Join("\n", lines));
        _modsStatus.gameObject.SetActive(!_actionMode);
        for (int i = 0; i < _actionRows.Count; i++)
        {
            LuaAction action = Actions.Where(x => x.Mod.Loaded).Skip(_page * 4 + i).FirstOrDefault();
            _actionRows[i].gameObject.SetActive(_actionMode && action != null);
            if (action == null) continue;
            ForceButtonText(_actionRows[i].gameObject, action.Mod.Manifest.name + ": " + action.Label);
            _actionRows[i].interactable = !action.Mod.Manifest.hostOnly || IsHost;
        }
        SetText(_footerStatus, IsHost ? "Host mode: Lua commands and game events are active." : "Client mode: host-only Lua mods are idle.");
    }

    private void OpenPanel()
    {
        if (!_panel)
        {
            return;
        }

        _mainScreen?.SetActive(false);
        _optionsScreen?.SetActive(false);
        _serverSettingsScreen?.SetActive(false);
        _panel.SetActive(true);
        PlayerCamera.ToggleMouse(true);
    }

    private void ClosePanel()
    {
        _panel?.SetActive(false);
        if (PauseManager.IsPaused && _mainScreen && !MainMenuManager.IsInMenu)
        {
            _mainScreen.SetActive(true);
        }
    }

    private GameObject FindButtonTemplate()
    {
        Button button = _mainScreen ? _mainScreen.GetComponentInChildren<Button>(true) : null;
        return button ? button.gameObject : null;
    }

    private static PauseManager GetPauseManager()
    {
        return typeof(PauseManager).GetField("_instance", PrivateStatic)?.GetValue(null) as PauseManager;
    }

    private static T GetPrivateField<T>(object instance, string field) where T : class
    {
        return instance == null ? null : instance.GetType().GetField(field, PrivateInstance)?.GetValue(instance) as T;
    }

    private static TextMeshProUGUI FindTextTemplate()
    {
        return Resources.FindObjectsOfTypeAll<TextMeshProUGUI>().FirstOrDefault();
    }

    private static void ForceButtonText(GameObject button, string label)
    {
        foreach (TextMeshProUGUI text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.gameObject.SetActive(true);
        }
    }

    private static void RemoveLocalization(GameObject button)
    {
        foreach (Component component in button.GetComponentsInChildren<Component>(true))
        {
            string name = component?.GetType().FullName ?? string.Empty;
            if (name.IndexOf("Localiz", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                DestroyImmediate(component);
            }
        }
    }

    private static void AddBorder(Transform parent, float thickness, Color color)
    {
        AddBorderPiece(parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), color);
        AddBorderPiece(parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness), color);
        AddBorderPiece(parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f), color);
        AddBorderPiece(parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), color);
    }

    private static void AddBorderPiece(Transform parent, Vector2 min, Vector2 max, Vector2 size, Color color)
    {
        GameObject piece = new GameObject("Border", typeof(RectTransform), typeof(Image));
        piece.transform.SetParent(parent, false);
        Image image = piece.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = piece.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
    }

    private static bool IsCallable(DynValue value)
    {
        return value != null && value.Type == DataType.Function;
    }

    private static bool IsValidCommand(string command)
    {
        return command.Length > 0 && command.Length <= 32 && command.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    private static string SafeFileName(string value)
    {
        return new string(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
    }

    private static string ShortError(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown error" : value.Replace('\r', ' ').Replace('\n', ' ').Substring(0, Mathf.Min(70, value.Length));
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text)
        {
            text.text = value;
        }
    }

    [HarmonyPatch(typeof(Item), "OnAttachedRodChange")]
    private static class FishHookPatch
    {
        private static void Postfix(Item __instance, FishingRod prev, FishingRod next, bool asServer)
        {
            if (asServer && !prev && next && __instance is Fish fish)
            {
                _instance?.OnFishHooked(fish);
            }
        }
    }

    [HarmonyPatch(typeof(Creature), "OnHealthChange")]
    private static class CreatureDeathPatch
    {
        private static void Postfix(Creature __instance, int prev, int next, bool asServer)
        {
            if (asServer && prev > 0 && next <= 0) _instance?.OnCreatureDied(__instance);
        }
    }

    [HarmonyPatch(typeof(ChatManager), "SendTypedMessage")]
    private static class ChatCommandPatch
    {
        private static readonly FieldInfo InputField = typeof(ChatManager).GetField("_chatInputField", PrivateInstance);

        private static bool Prefix(ChatManager __instance)
        {
            TMP_InputField input = InputField?.GetValue(__instance) as TMP_InputField;
            if (!input || !_instance || !_instance.TryRunCommand(input.text))
            {
                return true;
            }

            input.text = string.Empty;
            return false;
        }
    }
}

internal sealed class LuaMod
{
    private readonly ConfigFile _data;
    internal readonly Dictionary<string, DynValue> Events = new(StringComparer.OrdinalIgnoreCase);
    internal readonly List<LuaTimer> Timers = new();
    internal readonly LuaManifest Manifest;
    internal readonly string Folder;
    internal Script Script;
    internal bool Loaded;
    internal string Error;

    internal LuaMod(LuaManifest manifest, string folder, string dataPath)
    {
        Manifest = manifest;
        Folder = folder;
        _data = new ConfigFile(dataPath, true);
    }

    internal void CreateScript(HowToLuaPlugin framework)
    {
        Script = LuaExecution.CreateScript();
        Script.Options.DebugPrint = text => framework.FrameworkLogger.LogInfo("[" + Manifest.id + "] " + text);
        Table api = new Table(Script);
        api.Set("button", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterAction(this, args[0].CastToString(), args[1]);
            return DynValue.Nil;
        }));
        api.Set("on", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterEvent(this, args.Count > 0 ? args[0].CastToString() : string.Empty, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("command", DynValue.NewCallback((context, args) =>
        {
            framework.RegisterCommand(this, args.Count > 0 ? args[0].CastToString() : string.Empty, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("after", DynValue.NewCallback((context, args) =>
        {
            framework.AddTimer(this, args.Count > 0 ? (float)args[0].CastToNumber() : 0f, false, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("every", DynValue.NewCallback((context, args) =>
        {
            framework.AddTimer(this, args.Count > 0 ? (float)args[0].CastToNumber() : 0f, true, args.Count > 1 ? args[1] : DynValue.Nil);
            return DynValue.Nil;
        }));
        api.Set("chat", DynValue.NewCallback((context, args) =>
        {
            if (Server.Instance && Server.Instance.IsServerInitialized && args.Count > 0)
            {
                string message = "[" + Manifest.name + "] " + args[0].ToPrintString();
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
                int grant = (int)Math.Min(Math.Round(amount), (long)int.MaxValue - MoneyManager.Money);
                if (grant > 0) MoneyManager.AddMoney(grant, Player.LocalPlayer);
                return DynValue.True;
            }
            return DynValue.False;
        }));
        api.Set("get_data", DynValue.NewCallback((context, args) =>
        {
            string key = args.Count > 0 ? args[0].CastToString() : string.Empty;
            string fallback = args.Count > 1 ? args[1].ToPrintString() : string.Empty;
            return DynValue.NewString(_data.Bind("Data", SafeKey(key), fallback).Value);
        }));
        api.Set("set_data", DynValue.NewCallback((context, args) =>
        {
            if (args.Count > 1)
            {
                string value = args[1].ToPrintString();
                if (value.Length > 4096) throw new ScriptRuntimeException("Data value exceeds 4096 characters.");
                _data.Bind("Data", SafeKey(args[0].CastToString()), string.Empty).Value = value;
                _data.Save();
            }
            return DynValue.Nil;
        }));
        api.Set("is_host", DynValue.NewCallback((context, args) => DynValue.NewBoolean(Server.Instance && Server.Instance.IsServerInitialized)));
        api.Set("log", DynValue.NewCallback((context, args) =>
        {
            framework.FrameworkLogger.LogInfo("[" + Manifest.id + "] " + (args.Count > 0 ? args[0].ToPrintString() : string.Empty));
            return DynValue.Nil;
        }));
        Script.Globals.Set("htf", DynValue.NewTable(api));
    }

    internal void InvokeOptional(string name)
    {
        DynValue callback = Script.Globals.Get(name);
        if (callback != null && (callback.Type == DataType.Function || callback.Type == DataType.ClrFunction))
        {
            Call(callback);
        }
    }

    internal void Call(DynValue callback, params DynValue[] args)
    {
        try
        {
            LuaExecution.Run(Script, callback, args);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    internal void SetError(string message)
    {
        Error = message;
        Loaded = false;
        HowToLuaPlugin.Instance.DisableMod(this);
        HowToLuaPlugin.Instance.FrameworkLogger.LogError("Lua mod '" + Manifest.id + "': " + message);
    }

    private static string SafeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || !value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            throw new ScriptRuntimeException("Data keys require 1-64 letters, digits, underscores or hyphens.");
        return value;
    }
}

[Serializable]
internal sealed class LuaManifest
{
    public string id = string.Empty;
    public string name = string.Empty;
    public string version = "0.1.0";
    public string author = "Unknown";
    public string entry = "main.lua";
    public bool hostOnly = true;
    public string[] dependencies = Array.Empty<string>();
}

internal sealed class LuaAction
{
    internal readonly LuaMod Mod;
    internal readonly string Label;
    internal readonly DynValue Callback;
    internal LuaAction(LuaMod mod, string label, DynValue callback) { Mod = mod; Label = label; Callback = callback; }
}

internal sealed class LuaTimer
{
    internal readonly float Interval;
    internal readonly bool Repeat;
    internal readonly DynValue Callback;
    internal float NextRun;

    internal LuaTimer(float interval, bool repeat, DynValue callback)
    {
        Interval = interval;
        Repeat = repeat;
        Callback = callback;
        NextRun = Time.unscaledTime + interval;
    }
}

internal sealed class LuaCommand
{
    internal readonly LuaMod Mod;
    internal readonly DynValue Callback;

    internal LuaCommand(LuaMod mod, DynValue callback)
    {
        Mod = mod;
        Callback = callback;
    }
}
