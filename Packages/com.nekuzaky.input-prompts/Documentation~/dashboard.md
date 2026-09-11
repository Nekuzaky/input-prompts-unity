# The dashboard

`Tools > Input Prompts > Dashboard` holds the whole configuration of the package: what to import,
where to write it, what the runtime should do, and what the result looks like.

Settings live in `ProjectSettings/InputPromptsSettings.asset`, outside `Assets/`. Commit that file
and the whole team imports the pack the same way.

## Header

The pill states what the package can do right now:

| Pill | Meaning |
|---|---|
| `Pack not found` | The pack folder does not exist. Nothing can be generated. |
| `Not generated yet` | The pack is there, but no database has been written. Prompts will stay empty. |
| `N sets ready` | The database exists and holds N device families. |

**Demo** builds the demo canvas. **Generate** runs the import.

## Source

What to read, and how.

| Field | Effect |
|---|---|
| **Pack folder** | Root of the Kenney pack, the folder holding `Keyboard & Mouse`, `Xbox Series`, and the rest. |
| **Variant** | `Default` for the 1x PNGs, `Double` for the 2x ones. Use `Double` for large UI or high DPI. |
| **Outline icons** | Uses the `_outline` variant of every icon that has one. |
| **Coloured face buttons** | Xbox A/B/X/Y and PlayStation cross/circle/square/triangle in brand colours instead of monochrome. |
| **Fix texture settings** | Sets every PNG of the used folders to Sprite, no mipmaps, clamped, alpha is transparency. One reimport for the whole pack. |
| **TMP sprite assets** | Builds one TextMeshPro sprite asset per family, packing its icons into an atlas, for inline `<sprite>` tags. |

## Output

Where to write, and what the runtime defaults are.

| Field | Effect |
|---|---|
| **Prompt sets** | Folder for the generated assets. The database goes in a `Resources` sub-folder so the runtime can load it without a reference. |
| **Demo prefab** | Folder for the icon prefab the demo canvas needs. |
| **Unknown gamepads** | Family used for a controller matching no set. Xbox is the safe default on PC. |
| **Startup style** | Family shown before the player has touched anything. |

**Ping database** selects the generated database in the Project window. **Reload** re-reads
everything from disk, useful after generating from the command line.

## Runtime

These three write into the database, and the service reads them at startup. They are the defaults;
code can still change them at any time.

| Toggle | Off (default) | On |
|---|---|---|
| **Pointer motion switches style** | Moving the mouse leaves the prompts on the gamepad. A click still switches. | Any mouse movement switches back to mouse and keyboard icons. |
| **Use keyboard layout labels** | — (on by default) | `<Keyboard>/w` draws the key the player actually has under that finger: Z on AZERTY, W on QWERTY. |
| **Prefer exact device** | An action bound to both a key and a mouse button keeps one icon. | The prompt follows the exact device last used, flipping between the key and the mouse button. |

## Devices

One row per family: a checkbox to include it in the generation, the layouts it answers for, and how
many icons its set holds. `to generate` means the folder is there but no set was written yet;
`folder?` means the pack has no folder for that family.

Clicking a row selects it for the preview.

## Preview

The icons actually present in the selected set, so a missing mapping is visible rather than
discovered in play mode. A red tile means the key resolved to nothing.

**Force this style** makes every prompt in the editor draw that family, whatever is plugged in.
**Follow device** gives control back to the active device.

## Report

What the last generation produced, line by line, including every icon it could not find:

```
[KeyboardMouse] 134 icons
[Xbox] 50 icons
...
    missing: buttonsouth -> xbox_button_a.png
```

A missing line means the name table points at a file the pack does not have. See
[extending.md](extending.md).

## Generating without the window

```bash
Unity -batchmode -quit -nographics -projectPath . \
      -executeMethod Nekuzaky.InputPrompts.Editor.InputPromptGenerator.GenerateWithDefaults
```

It uses the stored settings, which is why they live in `ProjectSettings`: a build machine generates
exactly what a developer's machine does.
