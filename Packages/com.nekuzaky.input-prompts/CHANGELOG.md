# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
[Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-09-10

### Added

- `InputPromptService`: tracks the device the player actually uses, resolves an `InputAction` to an
  icon, and raises `StyleChanged` and `PromptsChanged`.
- `InputPromptIcon`, `InputPromptGroup` and `InputPromptText` for uGUI / TextMeshPro display.
- `InputPromptSet` and `InputPromptDatabase`: icon sets per device family, with a fallback chain.
- Dark themed dashboard (`Tools > Input Prompts > Dashboard`): import settings, per device sets,
  live icon preview and runtime defaults, with settings shared through ProjectSettings.
- Importer for the Kenney Input Prompts pack, command line
  generation included.
- Menu entries to create prompt icons and a demo canvas.
- Editor preview of any controller style, without plugging the device in.

### Changed

- Assemblies and namespaces are prefixed: `Nekuzaky.InputPrompts.Runtime` and
  `Nekuzaky.InputPrompts.Editor`, so they cannot collide with another package.
- The demo menu writes its prefab to the folder configured in the dashboard instead of a fixed one,
  and picks up any Input Action Asset of the project rather than a fixed file name.
- `InputPromptGroup.Action` can be assigned at runtime, like `InputPromptIcon.Action`.
- A missing prompt database now logs a warning pointing at the dashboard, instead of leaving every
  prompt silently empty.
- Minimum Unity version raised to 6000.3, the version the package is tested on.

## [1.1.0] - 2026-09-11

### Fixed

- Keyboard and mouse no longer behave like two platforms. Bindings are resolved against the whole
  device family rather than the exact device, so an action bound to both a key and a mouse button
  (`Attack` in the default action asset: `<Mouse>/leftButton` and `<Keyboard>/enter`) keeps one icon
  instead of flipping depending on the last input.

### Added

- `InputPromptService.PreferExactDevice`, mirrored by a *Prefer exact device* toggle in the
  dashboard, restores the previous behaviour for projects that want the prompt to follow the exact
  device in use.
- `InputPromptService.MatchesCurrentStyle`, the family-wide counterpart of `MatchesCurrentDevice`.

## [1.1.1] - 2026-09-11

### Fixed

- Resolving a prompt no longer re-reads the device family for every binding it walks. The style and
  its layouts are read once per lookup, and once per group rebuild, instead of once per binding of
  every prompt on screen at each device switch.

## [1.2.0] - 2026-09-11

### Changed

- Comments stripped from the source: the code carries no `//` or `///` any more. Inspector tooltips
  are kept, and the behaviour that used to be explained in comments is covered by the documentation
  and by the tests.
- Dashboard interface translated to English.

### Added

- `Documentation~`: the dashboard card by card, every component field by field, the generated assets,
  the scripting API including rebinding, and how to extend the package with new devices or another
  icon pack.

## [1.3.0] - 2026-09-11

### Changed

- Dashboard icons come from [Bootstrap Icons](https://icons.getbootstrap.com/) (MIT) instead of
  emoji. Unity imports SVG as `VectorImage`, which UI Toolkit draws natively: the icons stay crisp at
  any size and are tinted from code, where emoji could not be drawn at all by the editor font.
- Device rows use a tinted controller or keyboard icon, one colour per family.

### Removed

- The emoji probe and its plain-symbol fallback, now that no glyph depends on a system font.

## [1.3.1] - 2026-09-11

### Removed

- `circle-fill.svg` and the `DashboardGlyphs.Dot` constant, shipped in 1.3.0 without ever being used.
- `InputPromptDashboard.BadgeFor` is private again. It was public by accident, and 1.3.0 changed its
  signature, so any caller was already broken; nothing else in the package used it.

## [1.4.0] - 2026-09-11

### Added

- Inline icons in TextMeshPro. The importer builds one `TMP_SpriteAsset` per device family, packing
  the icons of that family into an atlas, and `InputPromptText` renders a token as a `<sprite>` tag
  that follows the device. It falls back to the control name when the family has no icon, and the
  whole thing can be turned off per component or in the dashboard.
- `InputPromptRebindButton`: listens for the next input, cancels on Escape, keeps the mouse out of
  the way, and shows the current control through an `InputPromptIcon`.
- `RebindConflicts`: duplicate detection across the whole action asset, with three policies —
  allow the duplicate, reject the new binding, or swap the two so no action is left unbound.
- `RebindStore`: save, load and clear binding overrides through PlayerPrefs.
- `InputPromptService.CurrentSpriteAsset` and `GetSpriteName`, plus `ControlPath.ToSpriteName`.

## [1.5.0] - 2026-09-13

### Added

- Local co-op. `InputPromptContext` holds what used to be global state: an active device, a style,
  events. `InputPromptPlayer`, placed next to a `PlayerInput`, gives that player a context fed only
  by the devices it is paired with. Icons, groups, texts and rebind buttons take an optional
  **Player**, so each half of a split screen shows its own prompts.
- `InputPromptService.ControlNameTranslator`: a `(key, name) => string` hook every display string
  goes through, for localising control names without a hard dependency on a localisation package.
- `InputPromptService.Register` and `Unregister` for contexts built by hand.

### Changed

- `InputPromptService` is now a facade over a global context. Its whole public API is unchanged and
  keeps following whoever acted last, so existing projects need no change.

## [1.6.0] - 2026-09-13

### Added

- `InputPromptPackDefinition`: folders, layouts, blank icon and `key -> file` mapping of every device
  family, as an asset. The importer follows it, so another icon pack — official console glyphs
  included — imports without code changes. **Export Kenney definition** writes the built-in table to
  an editable asset.
- `GenerationOptions`: the importer takes a plain options object, so it can run from a build script
  or a test without touching the project settings.
- Atlas compression for the TextMeshPro sprite assets: BC7 for desktop, ASTC 6x6 for mobile.

### Changed

- The Kenney table is now one definition among others, built in memory when no definition is set.
  Generation output is unchanged: same 355 icons, same six sets.
- `InputPromptGenerator.SupportedStyles` and `InputPromptSettings.FolderFor` are gone; families come
  from the definition, see `InputPromptGenerator.DescribeFamilies`.
