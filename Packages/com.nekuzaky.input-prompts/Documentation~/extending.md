# Extending

## Pack definitions

Everything the importer knows about an icon pack lives in an `InputPromptPackDefinition` asset:
which folder holds each device family, which Input System layouts the family answers for, which
file is the blank key cap, and the `key -> file` mapping of every control. The generator reads
nothing else.

When the dashboard's **Pack definition** field is empty, a definition is built in memory from the
built-in Kenney table. Press **Export Kenney definition** to write that table to an asset you can
edit: the dashboard assigns it straight away.

| Definition field | What it holds |
|---|---|
| **Uses Variant Folders** | Each family folder holds `Default` and `Double` sub-folders, as in the Kenney pack. |
| **Extension** | File extension of the icons, dot included. |
| **Outline Suffix** | Appended to a file name to find its outline variant. |
| **Families** | One entry per device family: style, folder, layouts, blank icon, fallback to Generic, mappings. |

## An icon is missing

The Report card names it:

```
missing: buttonsouth -> xbox_button_a.png
```

Either the pack does not hold that file, or the mapping points at the wrong one. Export the Kenney
definition if you have not already, find the family, fix the `File` of that mapping, press Generate.

A mapping's **Key** is the control key ([assets.md](assets.md) lists the format). Its **File** is the
file name without extension and without the outline suffix, which the importer adds by itself.

For a one-off fix, editing the generated set asset is faster: its entries list is a plain
`key -> sprite` array. A later generation of that family overwrites it.

## A control the package does not know

Add a mapping to the family in your definition. Keys follow the Input System control paths, so
`leftstick/upleft` or `touchpadbutton` work the same way as the built-in ones.

## A new device family

1. Add a value to `InputDeviceStyle` in `Runtime/src/InputDeviceStyle.cs`. This is the only code
   change.
2. Add a family to your definition: folder, layouts, blank icon, mappings. Run
   `InputSystem.ListLayouts()` or open the Input Debugger for the exact layout name, e.g.
   `SwitchProControllerHID`.
3. Order matters: families are checked in the definition's order, so put specific ones before
   generic ones.
4. Generate.

## Another icon pack, certified console glyphs included

Point **Pack folder** at the new pack, then write a definition for its naming: one family per
device, one mapping per control. This is the route for the official glyphs Sony, Microsoft and
Nintendo ship with their SDKs, which console certification requires in place of third-party
lookalikes: the package never needs to know the files are not Kenney's.

Set **Uses Variant Folders** off when the pack keeps its icons flat, and change **Extension** when
they are not PNG.

A pack with no generator at all works too: create sets by hand with
`Assets > Create > Input Prompts > Prompt Set` and a database with
`Assets > Create > Input Prompts > Prompt Database`, placed in a `Resources` folder under the name
`SO_InputPromptDatabase`. The components know nothing about where the sprites came from.

## Atlas size

TextMeshPro atlases are uncompressed by default, about 2 MB per family. **Atlas compression** in the
dashboard writes them as BC7 for desktop builds or ASTC 6x6 for mobile builds. A format the target
GPU cannot read is decompressed at load time, which costs memory and load time, so pick the one that
matches the platform you ship.

## Icons for a device that is not plugged in

`InputPromptService.EditorPreviewStyle` forces a family in the editor. The dashboard and the icon
inspector both expose it. It is compiled out of builds.

```csharp
#if UNITY_EDITOR
InputPromptService.EditorPreviewStyle = InputDeviceStyle.PlayStation;
InputPromptService.Refresh();
#endif
```

## Local co-op

Supported through `InputPromptPlayer`, see [scripting.md](scripting.md). Each player gets its own
`InputPromptContext`, fed only by the devices its `PlayerInput` is paired with.


## Validation

`Tools > Input Prompts > Validate Action Assets`, or the **Validation** card of the dashboard, checks
every binding of an action asset against the database before a player ever sees a blank prompt.

| Report | Meaning | What to do |
|---|---|---|
| `has no icon in Xbox (key "dpad/left")` | A family that will be shown for this binding has no icon for that control. | Add the mapping to the pack definition, or bind another control. |
| `matches no device family` | No set covers the device, e.g. `<Joystick>/trigger` or `<XRController>/grip`. | Expected for devices you do not show prompts for; add a family otherwise. |
| `cannot be turned into an icon key` | The path names a usage such as `*/{Submit}` rather than a control. | Bind concrete controls where the action needs a prompt. |

Only the first kind is a defect of the prompts; the dashboard counts it apart from the others.

A set that only names the root `Gamepad` layout, like the Kenney Generic family, is not reported when
the database has a different gamepad fallback: unknown gamepads use that fallback, so the Generic set
is never what the player sees for them.

```csharp
List<InputPromptValidator.Issue> issues = InputPromptValidator.Validate(actions, database);
```

`Validate` is a plain call, so a build script can fail the build on missing icons:

```csharp
if (InputPromptValidator.Validate(actions, database).Any(issue => issue.m_kind == InputPromptValidator.IssueKind.MissingIcon))
    throw new BuildFailedException("Some bindings have no prompt icon.");
```
