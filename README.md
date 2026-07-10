# Configuration Manager — Input System fork (BepInEx 5 Mono)

A fork of [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (based on **v19.0**) with patches for Unity games that use the **new Input System** and where `BaseUnityPlugin` lifecycle methods do not run reliably.

**This is not the official Configuration Manager release.** For the unmodified plugin, use [upstream releases](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases).

An easy way to let users configure plugin settings in-game without writing your own GUI. Settings from any plugin's `Config.Bind(...)` entries appear automatically, including keyboard shortcuts.

## What this fork changes

Only the **BepInEx 5 Mono** build is patched. The IL2CPP / BepInEx 6 project is unchanged upstream code.

| Change | Why |
|--------|-----|
| `InputSystemHelper` | Hotkeys and key rebinding via `UnityEngine.InputSystem` instead of legacy `UnityEngine.Input` / `KeyboardShortcut.IsDown()` |
| `ConfigurationManagerMonoBehaviour` | Routes `Start`, `Update`, `LateUpdate`, and `OnGUI` through a `DontDestroyOnLoad` `MonoBehaviour` |
| Relaxed shortcut detection | Matches simple Input System polling (main key + modifiers) without requiring no other keys be held |

**Tested with:** [Hadean Tactics](https://store.steampowered.com/app/527110/Hadean_Tactics/) (BepInEx 5.4.23, Unity 2022.3 Mono).

## How to install

**Requirements:** BepInEx **5.4.20+** (Mono). This fork does **not** replace the IL2CPP / BepInEx 6 build.

1. Install [BepInEx 5](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for your game.
2. Download the latest **`BepInEx5`** release from this repository's **Releases** page (after you publish your fork).
3. Extract into your game folder (next to the existing `BepInEx` folder). The DLL should end up at:
   ```
   BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll
   ```
4. Start the game. Open the menu with the configured hotkey (default **F1**).

Hotkey and other CM options are in `BepInEx/config/com.bepis.bepinex.configurationmanager.cfg`.

### Choosing a hotkey

Avoid keys reserved by the game (e.g. F-keys used for menus). If another mod uses the same key, only one will respond. Pick a unique binding in the config file.

### Build from source

```bash
dotnet build ConfigurationManager.csproj -c Release
```

Output: `bin/BepInEx5/ConfigurationManager.dll`

**Important:** The BepInEx 5 project defines the compile symbol `Mono` (not `MONO`). Input System code is wrapped in `#if Mono`; using the wrong symbol name will silently exclude the patch.

## Known issues

- **Input System games (this fork):** Intended fix for games where the stock CM hotkey and window never appear. If problems persist, check `BepInEx/LogOutput.log` for `Input System hotkey handler started`.
- **Linux / Wine:** If no text is visible in IMGUI windows, the system may be missing `Arial.ttf`. See [this RuntimeUnityEditor issue](https://github.com/ManlyMarco/RuntimeUnityEditor/issues/55).
- **IL2CPP:** Use [upstream Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) — this fork does not patch that build.
- **Screenshot:** See the [upstream README](https://github.com/BepInEx/BepInEx.ConfigurationManager) for a preview of the UI.

## License and attribution

- **Original project:** [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) by ManlyMarco / MarC0
- **License:** [GNU LGPL v3](LICENSE) (same as upstream)
- **This fork:** Input System and lifecycle patches (2026, AZander48)

If you distribute binaries, include the `LICENSE` file and make your modified source available (e.g. this repository), per LGPL v3.

---

## How to make my mod compatible?

Configuration Manager automatically displays settings from your plugin's `Config`. Add descriptive section names, keys, descriptions, and acceptable value lists or ranges wherever possible.

In most cases you do not need to reference `ConfigurationManager.dll`. Metadata on `Config.Bind` is enough.

**Note for Input System games:** Stock `KeyboardShortcut.IsDown()` / `UnityEngine.Input` may not receive keypresses. Use `UnityEngine.InputSystem` in your own mods if you need in-game hotkeys (this fork only fixes Configuration Manager itself).

### How to make my setting into a slider?

Specify `AcceptableValueRange` when creating your setting. If the range is 0f–1f or 0–100 the slider is shown as %.

```c#
CaptureWidth = Config.Bind("Section", "Key", 1, new ConfigDescription("Description", new AcceptableValueRange<int>(0, 100)));
```

### How to make my setting into a drop-down list?

Specify `AcceptableValueList` when creating your setting. Enums are listed automatically unless you hide values via attributes.

```c#
public enum MyEnum
{
    Entry1,
    [Description("Entry2 will be shown in the combo box as this string")]
    Entry2
}
```

### How to allow user to change my keyboard shortcuts?

Add a `ConfigEntry<KeyboardShortcut>`. On games with legacy input working, check `IsDown()` in `Update`:

```c#
private ConfigEntry<KeyboardShortcut> ShowCounter { get; set; }

public Constructor()
{
    ShowCounter = Config.Bind("Hotkeys", "Show FPS counter", new KeyboardShortcut(KeyCode.U, KeyCode.LeftShift));
}

private void Update()
{
    if (ShowCounter.Value.IsDown())
    {
        // Handle the key press
    }
}
```

On Input System–only games, poll `UnityEngine.InputSystem` instead (see fork notes above).

## Overriding default Configuration Manager behavior

Pass `ConfigurationManagerAttributes` as a tag on a setting. Download the attribute class from [ConfigurationManagerAttributes.cs](ConfigurationManagerAttributes.cs) and add it to your project.

- You do not have to reference `ConfigurationManager.dll` for attributes to work.
- Keep the class name and field declarations unchanged.
- Prefer non-public class scope to avoid conflicts between plugins.

```c#
Config.Bind("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
Config.Bind("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
Config.Bind("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
```

### Custom setting drawer (per setting)

```c#
void Start()
{
    Config.Bind("Section", "Key", "Some value",
        new ConfigDescription("Desc", null, new ConfigurationManagerAttributes { CustomDrawer = MyDrawer }));
}

static void MyDrawer(BepInEx.Configuration.ConfigEntryBase entry)
{
    GUILayout.Label(entry.BoxedValue, GUILayout.ExpandWidth(true));
}
```

### Custom setting drawer (global, by type)

Requires referencing `ConfigurationManager.dll`. Only use if all users will have CM installed.

```c#
void Start()
{
    ConfigurationManager.RegisterCustomSettingDrawer(typeof(MyType), CustomDrawer);
}

static void CustomDrawer(SettingEntryBase entry)
{
    GUILayout.Label((MyType)entry.Get(), GUILayout.ExpandWidth(true));
}
```
