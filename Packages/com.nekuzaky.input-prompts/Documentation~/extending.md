# Extending

## An icon is missing

The Report card names it:

```
missing: buttonsouth -> xbox_button_a.png
```

Two causes. Either the pack does not hold that file, or the name table points at the wrong one. The
table lives in `Editor/src/KenneyNameTable.cs`, one method per family:

```csharp
("buttonsouth", "xbox_button_a"),
("leftshoulder", "xbox_lb"),
```

The left side is the control key ([assets.md](assets.md) lists the format), the right side is the
file name without extension and without the `_outline` suffix, which the importer adds by itself
when the option is on.

Add the pair, press Generate again.

For a one-off fix, editing the set asset directly is faster: the entries list is a plain
`key -> sprite` array in the inspector. The trade-off is that a later generation of that family
overwrites it.

## A control the package does not know

Same table, same method. Sticks and d-pads follow a pattern and are generated in `Gamepad(prefix)`,
so adding `leftstick/upleft` for a pack that has such an icon means adding it there once, for every
controller family at the same time.

## A new device family

1. Add a value to `InputDeviceStyle` in `Runtime/src/InputDeviceStyle.cs`.
2. Fill in four things in `KenneyNameTable`:
   - `FolderFor` — the pack sub-folder holding those icons;
   - `LayoutsFor` — the Input System layouts the family answers for, e.g. `SwitchProControllerHID`.
     Run `InputSystem.ListLayouts()` or open the Input Debugger to find the exact name;
   - `BlankFor` — the icon used as a blank cap;
   - a method returning the `key -> file` table, added to the `For` switch.
3. Add the value to `InputPromptGenerator.SupportedStyles`, which also fixes its priority in the
   database: specific families first, generic last.
4. Generate.

Nothing in the runtime needs to change: it reads families from the database.

## A different icon pack

The generator is Kenney-specific, the runtime is not. Two options.

**Keep the generator.** Name your files like Kenney's and point the dashboard at your folder.
Practical only for a pack you control.

**Skip the generator.** Create the sets by hand: `Assets > Create > Input Prompts > Prompt Set`, set
the style and the layouts, fill the entries with your own sprites, then create a database with
`Assets > Create > Input Prompts > Prompt Database`, list your sets, and put it in a `Resources`
folder named `SO_InputPromptDatabase`. The components know nothing about Kenney.

A third route, for a pack that has its own naming convention: copy `KenneyNameTable` into your own
editor assembly, write the tables for your names, and call `InputPromptGenerator.Generate` with your
own settings instance.

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

## What is deliberately absent

- No UI Toolkit or SpriteRenderer components. `InputPromptService` returns plain `Sprite` objects, so
  either is a small component to write against the same API.
