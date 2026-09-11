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
