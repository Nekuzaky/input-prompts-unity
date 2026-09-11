# Generated assets

Two ScriptableObjects carry everything the runtime needs. Both are generated, both stay editable by
hand afterwards.

---

## InputPromptSet

One per device family. `Assets > Create > Input Prompts > Prompt Set` creates an empty one.

| Field | What it holds |
|---|---|
| **Style** | The family this set represents, from the `InputDeviceStyle` enum. |
| **Layouts** | Input System layout names this set answers for, e.g. `XInputController`. Checked against `InputDevice.layout`, sub-layouts included: a set listing `Gamepad` matches every gamepad. |
| **Blank Sprite** | Drawn when a control has no icon of its own, with the control name printed on it by the icon's fallback label. |
| **Fallback** | Another set to look in when a key is missing here. The generator points every gamepad family at the Generic set. |
| **Entries** | The `key -> sprite` list itself. |

### Key format

A key is a control path with the device part removed, lower-cased:

| Binding path | Key |
|---|---|
| `<Keyboard>/space` | `space` |
| `<Keyboard>/leftShift` | `leftshift` |
| `<Mouse>/leftButton` | `leftbutton` |
| `<Gamepad>/buttonSouth` | `buttonsouth` |
| `<Gamepad>/leftStick/up` | `leftstick/up` |
| `<Gamepad>/dpad/left` | `dpad/left` |
| `<Mouse>/scroll/y` | `scroll/y` |

Bindings written against a usage, such as `<Gamepad>/{Submit}`, produce no key and fall through to
the blank cap.

### Editing by hand

The entries list is a plain array in the inspector. Replace the sprite on the `space` row and every
Space prompt in the game changes. Add a row for a key the generator does not know about and it is
picked up immediately.

Generation overwrites the list of a family it regenerates. To keep hand edits, uncheck that family
in the dashboard's Devices card before pressing Generate.

### Lookup order

`Find(key)` walks the chain: this set, then its fallback, then the fallback's fallback, up to eight
levels. A cycle is detected by the depth cap instead of hanging.

---

## InputPromptDatabase

One per project, written to a `Resources` folder so the service can load it with no reference to
wire. `Assets > Create > Input Prompts > Prompt Database` creates an empty one.

| Field | What it holds |
|---|---|
| **Sets** | Every family, in priority order. The first set whose layouts cover a device wins. |
| **Gamepad Fallback** | Used for a device that is a `Gamepad` but matches no set. |
| **Default Style** | Family used before any device has been touched. |
| **Pointer Motion Switches Style** | Written into `InputPromptService.PointerMotionSwitchesStyle` at startup. |
| **Use Keyboard Layout Labels** | Written into `InputPromptService.UseKeyboardLayoutLabels`. |
| **Prefer Exact Device** | Written into `InputPromptService.PreferExactDevice`. |

Order matters. A set listing `Gamepad` placed above the Xbox set would swallow every controller, so
keep the specific families first and the generic one last, which is what the generator does.

### Using a different database

```csharp
InputPromptService.Database = myOtherDatabase;   // repaints every live prompt
```

Useful to ship an alternative icon style and let the player pick it in the options.

### Where the name comes from

The service loads `Resources.Load<InputPromptDatabase>("SO_InputPromptDatabase")`. The file must
keep that name and live under a `Resources` folder, anywhere in the project. Renaming it means
loading it yourself and assigning `InputPromptService.Database`.

If no database is found, the service logs one warning pointing at the dashboard and every prompt
stays empty. It is a warning, not an error: a project can run without prompts.
