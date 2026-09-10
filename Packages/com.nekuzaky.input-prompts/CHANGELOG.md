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
