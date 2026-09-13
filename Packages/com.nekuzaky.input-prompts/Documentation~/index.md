# Input Prompts — documentation

Icons that follow the device the player is using. This folder documents every moving part of the
package: the editor window, the components you drop in a scene, the assets they read, and the
scripting API behind them.

| Page | What it covers |
|---|---|
| [dashboard.md](dashboard.md) | The editor window, card by card, and the settings file it writes. |
| [components.md](components.md) | `InputPromptIcon`, `InputPromptGroup`, `InputPromptText`, field by field. |
| [assets.md](assets.md) | `InputPromptSet` and `InputPromptDatabase`: key format, fallback chain, hand editing. |
| [scripting.md](scripting.md) | `InputPromptService`, events, rebinding, saving the player's bindings. |
| [extending.md](extending.md) | New device families, other icon packs, missing icons. |

## How it fits together

```
InputAction "Jump"
  |
  |  binding.effectivePath        "<Keyboard>/space"     you assign this in the .inputactions asset
  v
ControlPath.ToKey()               "space"                device stripped, lower-cased
  |
  v
InputPromptSet.Find("space")      keyboard_space.png     the set of the family in use
  |
  v
InputPromptIcon                   draws it in an Image
```

Two things decide what a player sees: **which binding** is picked, and **which icon** that binding
maps to. The first is the job of `InputPromptService`, driven by the device in use. The second is
the job of an `InputPromptSet`, generated once from the icon pack.

`effectivePath` is the path after binding overrides are applied, which is why rebinding needs no
support code: change the binding and the prompt follows.

## Install

```
https://github.com/Nekuzaky/input-prompts-unity.git?path=Packages/com.nekuzaky.input-prompts
```

Pin a version by appending `#v1.1.1`.

The package ships no sprites. Drop the [Kenney Input Prompts](https://kenney.nl/assets/input-prompts)
pack anywhere in `Assets/`, then generate the sets from the dashboard.

## Three steps to a prompt on screen

1. `Tools > Input Prompts > Dashboard`, point **Pack folder** at the Kenney pack, press **Generate**.
2. `GameObject > UI > Input Prompt Icon`, drop an `InputActionReference` into the **Action** field.
3. Enter play mode and switch between keyboard and controller.

## Requirements

- Unity 6000.3 or newer
- Input System 1.14 or newer, with actions defined in an `.inputactions` asset
- uGUI for the components; TextMeshPro only for `InputPromptText` and the optional fallback label

## Sample

`Window > Package Manager > Input Prompts > Samples > Basic prompts > Import` copies a scene into
`Assets/Samples`: an icon, a WASD group, a sentence with inline icons, and a rebind button with a reset,
all following the device in use. Generate the prompt database from the dashboard before pressing Play.
