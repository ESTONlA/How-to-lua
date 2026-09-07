using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HowToLua;

using static UnityEngine.Object;

internal sealed class LuaMenu
{
    private static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly LuaHost _host;
    private IReadOnlyDictionary<string, LuaMod> _mods => _host.Mods;
    private List<LuaAction> Actions => _host.Actions;
    private bool IsHost => _host.IsHost;
    private bool _actionMode;
    private readonly List<Button> _actionRows = new();
    private PauseManager _pauseManager;
    private GameObject _mainScreen, _optionsScreen, _serverSettingsScreen, _menuButton, _panel, _buttonTemplate;
    private TextMeshProUGUI _textTemplate, _modsStatus, _footerStatus;
    private int _page;
    private float _nextUiUpdate;
    internal LuaMenu(LuaHost host) { _host = host; }
    internal void Tick() { EnsurePauseUi(); RefreshUi(); }
    private void ReloadMods() { _host.ReloadMods(); }
    internal void Dispose() { if (_panel) Destroy(_panel); if (_menuButton) Destroy(_menuButton); }
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

}
