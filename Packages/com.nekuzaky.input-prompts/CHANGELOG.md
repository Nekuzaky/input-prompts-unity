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
