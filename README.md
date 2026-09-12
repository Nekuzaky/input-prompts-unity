# Input Prompts

[![Unity](https://img.shields.io/badge/Unity-6000.3%2B-2b3038?logo=unity&logoColor=white)](https://unity.com/releases/editor/archive)
[![Input System](https://img.shields.io/badge/Input%20System-1.14%2B-3a80e8)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html)
[![Release](https://img.shields.io/github/v/tag/Nekuzaky/input-prompts-unity?label=release&color=3ddc84)](https://github.com/Nekuzaky/input-prompts-unity/tags)
[![Tests](https://img.shields.io/badge/tests-25%20passing-3ddc84)](#tests)
[![License](https://img.shields.io/badge/license-MIT-9aa0ae)](Packages/com.nekuzaky.input-prompts/LICENSE.md)

Unity package that shows the right key or button icon for an `InputAction`, and swaps it on its own
when the player switches device — keyboard to gamepad, Xbox to DualSense, and back.
Icons come from the [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts) pack (CC0).

| | |
|---|---|
| Package | `com.nekuzaky.input-prompts` |
| Unity | 6000.3 or newer |
| Dependencies | Input System 1.14+, uGUI |

This repository is both **the package** (`Packages/com.nekuzaky.input-prompts`) and the **sample
project** used to develop it.

**Full documentation** lives in [`Documentation~`](Packages/com.nekuzaky.input-prompts/Documentation~/index.md):
the [dashboard](Packages/com.nekuzaky.input-prompts/Documentation~/dashboard.md) card by card, every
[component](Packages/com.nekuzaky.input-prompts/Documentation~/components.md) field by field, the
[generated assets](Packages/com.nekuzaky.input-prompts/Documentation~/assets.md), the
[scripting API](Packages/com.nekuzaky.input-prompts/Documentation~/scripting.md) including rebinding,
and how to [extend it](Packages/com.nekuzaky.input-prompts/Documentation~/extending.md) with your own
devices or icon pack.

---

## Installation

In Unity: `Window > Package Manager > + > Install package from git URL...`

```
https://github.com/Nekuzaky/input-prompts-unity.git?path=Packages/com.nekuzaky.input-prompts
```

Or straight into `Packages/manifest.json`:

```json
"com.nekuzaky.input-prompts": "https://github.com/Nekuzaky/input-prompts-unity.git?path=Packages/com.nekuzaky.input-prompts"
```

The package itself ships **no sprites**. Drop the Kenney pack into your project (for example
`Assets/Sprites/input-prompts`) and the importer takes care of the rest. This repository does carry a
copy of the pack under `Assets/Sprites/input-prompts`, so the sample project works right after a
clone — those files are Kenney's, under CC0.

---

## 1. The dashboard

`Tools > Input Prompts > Dashboard` — one dark window holding the whole configuration: import
settings, per device sets, a live preview of the generated icons and the runtime defaults.

| Card | What it holds |
|---|---|
| **Source** | Pack folder, `Default` (1x) or `Double` (2x) variant, `_outline` icons, coloured face buttons, texture fixing. |
| **Output** | Where the sets and the demo prefab are written, style used for unknown gamepads, style shown at startup. |
| **Runtime** | Pointer motion, keyboard layout labels and exact-device resolution, written into the database. |
| **Devices** | One row per device family: enable it, see its layouts and its icon count, click to preview. |
| **Preview** | The generated icons for the selected family, and a button to force that style in the editor. |
| **Report** | What the last generation produced, missing icons included. |

Settings live in `ProjectSettings/InputPromptsSettings.asset`, so a whole team shares the same import
configuration. On a build machine, run the import headless:

```bash
Unity -batchmode -quit -projectPath . -executeMethod Nekuzaky.InputPrompts.Editor.InputPromptGenerator.GenerateWithDefaults
```

*Generate* writes to `Assets/_/Database/InputPrompts/`:

- `SO_InputPromptSet_*.asset` — one icon set per device family;
- `Resources/SO_InputPromptDatabase.asset` — loaded automatically at startup, nothing to wire.

The Report card lists every icon it could not find (`buttonSouth -> xbox_button_a.png`). Sets stay
editable by hand afterwards: they are plain key → sprite lists.

## 2. Show a prompt

**A single icon**: `GameObject > UI > Input Prompt Icon`, then drop in an `InputActionReference`.

```csharp
icon.Action = playerInput.actions["Jump"];
```

**A composite action (WASD)**: put `InputPromptGroup` on an object with a `HorizontalLayoutGroup`,
plus an icon prefab. It spawns four icons on keyboard (Z Q S D on AZERTY) and a single one — the left
stick — on a gamepad.

**Text with inline icons**: `InputPromptText` on a TextMeshPro component, with a format such as
`Press {Player/Jump} to jump`. The token becomes the icon itself, drawn from a generated sprite
atlas, and falls back to the control name when the family has none. `{Map/Action#part}` targets one
part of a composite.

**Rebinding**: `InputPromptRebindButton` listens for the next input, applies it, resolves duplicates
across the whole action asset and saves the overrides to PlayerPrefs.

**Demo**: select your `.inputactions` asset, then `Tools > Input Prompts > Create Demo Canvas` — one
row per action with its icons.

## 3. Editor preview

The `InputPromptIcon` inspector has a *Show icons of* dropdown: force PlayStation, Switch or any
other style without plugging the controller in. Edit mode only.

---

## API

```csharp
using Nekuzaky.InputPrompts;

InputPromptService.CurrentStyle;                       // device family in use
InputPromptService.StyleChanged += style => { };       // keyboard -> gamepad
InputPromptService.PromptsChanged += () => { };        // device, rebind, database swap
InputPromptService.GetSprite(action);                  // icon for the action
InputPromptService.GetDisplayString(action);           // "Space", "A", ...
InputPromptService.SetActiveDevice(device);            // force the device the global context follows
GetComponent<InputPromptPlayer>().Context;             // per player context, for local co-op
InputPromptService.ControlNameTranslator = (key, name) => ...;  // localise control names
InputPromptService.PointerMotionSwitchesStyle = true;  // moving the mouse switches back to mouse icons
InputPromptService.UseKeyboardLayoutLabels = true;     // AZERTY: <Keyboard>/w draws the Z key
InputPromptService.PreferExactDevice = true;           // follow the exact device, not its family
InputPromptService.Refresh();                          // after a rebind done by hand
```

## Adding a device

1. Add the value to `InputDeviceStyle`.
2. Fill in `KenneyNameTable` (`FolderFor`, `LayoutsFor`, `BlankFor`, and the name table).
3. Hit *Generate* in the dashboard again.

Set keys are control paths without the device, lower-cased: `space`, `buttonsouth`, `leftstick/up`,
`dpad/left`, `scroll/y`.

## Details it gets right

- Nintendo face buttons are swapped: `buttonSouth` is the **B** button on a Switch controller.
- On AZERTY, `<Keyboard>/w` draws the **Z** key the player actually has under their fingers.
- Moving the mouse does not throw the prompts back to keyboard icons until something is clicked.
- A key with no icon in the pack (F13, oem…) falls back to a blank cap with its name printed on it.
- Keyboard and mouse are one family: an action bound to both a key and a mouse button keeps a single
  icon instead of flipping with every input. `PreferExactDevice` restores the per-device behaviour.
- Split screen works: `InputPromptPlayer` next to each `PlayerInput` gives every player prompts that
  follow only the devices paired with them.

## Tests

Twenty-five tests drive real devices through the Input System and read back what the package
resolves, instead of trusting it.

Twenty-one EditMode tests cover the service: which style a keyboard, an XInputController or an
unrecognised gamepad selects, that mouse movement alone does not steal the prompts from a gamepad
while a click does, that an action resolves to the keyboard sprite and then to the gamepad one, and
that a style change is raised once per switch rather than once per input. Three more cover the
keyboard and mouse family, one the PreferExactDevice option, and one checks that applying a binding
override moves the prompt to the new key on its own. Six more cover rebinding: duplicate detection
across maps, the swap that leaves no action unbound, and the save and load round trip. Three check
the generated TextMeshPro atlas resolves its sprites by name.

Four PlayMode tests cover the components in a scene: an icon repainting itself on a device switch
with nothing calling `Refresh`, a composite showing four keys on keyboard and a single stick on
gamepad, the blank key cap fallback, and a prompt hiding itself when the action has no binding.

```bash
Unity -batchmode -nographics -runTests -testPlatform EditMode -projectPath . -testResults edit.xml
Unity -batchmode -nographics -runTests -testPlatform PlayMode -projectPath . -testResults play.xml
```

Package tests only run when the project lists the package in the `testables` array of
`Packages/manifest.json`, which this repository does.

## Credits

- Icons: [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts), CC0. Included in this
  repository for the sample project; the package pulls them from your own project.
- Code: MIT, see [LICENSE.md](Packages/com.nekuzaky.input-prompts/LICENSE.md).
