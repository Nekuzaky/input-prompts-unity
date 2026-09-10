# Input Prompts

Unity package that shows the right key or button icon for an `InputAction`, and swaps it on its own
when the player switches device — keyboard to gamepad, Xbox to DualSense, and back.
Icons come from the [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts) pack (CC0).

| | |
|---|---|
| Package | `com.nekuzaky.input-prompts` |
| Unity | 6000.0 or newer |
| Dependencies | Input System 1.14+, uGUI |

This repository is both **the package** (`Packages/com.nekuzaky.input-prompts`) and the **sample
project** used to develop it.

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

## 1. Generate the icon sets

`Tools > Input Prompts > Importer`

| Field | Purpose |
|---|---|
| **Pack folder** | Where the Kenney pack lives. |
| **Variant** | `Default` (1x) or `Double` (2x, for large UI). |
| **Outline icons** | Use the `_outline` variants wherever they exist. |
| **Coloured face buttons** | A/B/X/Y and cross/circle/square/triangle in their brand colours. |
| **Fix texture settings** | Set the PNGs to Sprite, no mipmaps, clamped. |
| **Unknown gamepads** | Style used for a controller that matches no set (Xbox by default). |

*Generate prompt sets* writes to `Assets/_/Database/InputPrompts/`:

- `SO_InputPromptSet_*.asset` — one icon set per device family;
- `Resources/SO_InputPromptDatabase.asset` — loaded automatically at startup, nothing to wire.

The report lists every icon it could not find (`buttonSouth -> xbox_button_a.png`). Sets stay
editable by hand afterwards: they are plain key → sprite lists.

## 2. Show a prompt

**A single icon**: `GameObject > UI > Input Prompt Icon`, then drop in an `InputActionReference`.

```csharp
icon.Action = playerInput.actions["Jump"];
```

**A composite action (WASD)**: put `InputPromptGroup` on an object with a `HorizontalLayoutGroup`,
plus an icon prefab. It spawns four icons on keyboard (Z Q S D on AZERTY) and a single one — the left
stick — on a gamepad.

**Text**: `InputPromptText` on a TextMeshPro component, with a format such as
`Press {Player/Jump} to jump`. The `{Map/Action#part}` token targets one part of a composite.

**Demo**: select your `.inputactions` asset, then `Tools > Input Prompts > Create Demo Canvas` — one
row per action with its icons.

## 3. Editor preview

The `InputPromptIcon` inspector has a *Show icons of* dropdown: force PlayStation, Switch or any
other style without plugging the controller in. Edit mode only.

---

## API

```csharp
InputPromptService.CurrentStyle;                       // device family in use
InputPromptService.StyleChanged += style => { };       // keyboard -> gamepad
InputPromptService.PromptsChanged += () => { };        // device, rebind, database swap
InputPromptService.GetSprite(action);                  // icon for the action
InputPromptService.GetDisplayString(action);           // "Space", "A", ...
InputPromptService.SetActiveDevice(device);            // local co-op: pin one player to one device
InputPromptService.PointerMotionSwitchesStyle = true;  // moving the mouse switches back to mouse icons
InputPromptService.UseKeyboardLayoutLabels = true;     // AZERTY: <Keyboard>/w draws the Z key
InputPromptService.Refresh();                          // after a rebind done by hand
```

## Adding a device

1. Add the value to `InputDeviceStyle`.
2. Fill in `KenneyNameTable` (`FolderFor`, `LayoutsFor`, `BlankFor`, and the name table).
3. Run the importer again.

Set keys are control paths without the device, lower-cased: `space`, `buttonsouth`, `leftstick/up`,
`dpad/left`, `scroll/y`.

## Details it gets right

- Nintendo face buttons are swapped: `buttonSouth` is the **B** button on a Switch controller.
- On AZERTY, `<Keyboard>/w` draws the **Z** key the player actually has under their fingers.
- Moving the mouse does not throw the prompts back to keyboard icons until something is clicked.
- A key with no icon in the pack (F13, oem…) falls back to a blank cap with its name printed on it.

## Credits

- Icons: [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts), CC0. Included in this
  repository for the sample project; the package pulls them from your own project.
- Code: MIT, see [LICENSE.md](Packages/com.nekuzaky.input-prompts/LICENSE.md).
