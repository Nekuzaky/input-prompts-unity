# Changelog

Toutes les modifications notables de ce package sont listées ici.
Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le
[versionnage sémantique](https://semver.org/lang/fr/).

## [1.0.0] - 2026-09-10

### Ajouté

- `InputPromptService` : suivi du périphérique réellement utilisé, résolution d'une `InputAction`
  vers une icône, évènements `StyleChanged` et `PromptsChanged`.
- `InputPromptIcon`, `InputPromptGroup` et `InputPromptText` pour l'affichage en uGUI / TextMeshPro.
- `InputPromptSet` et `InputPromptDatabase` : bases d'icônes par famille de périphérique, avec
  chaîne de repli.
- Importer du pack Kenney Input Prompts (`Tools > Input Prompts > Importer`), génération en ligne de
  commande incluse.
- Création d'une scène de démo et d'icônes depuis le menu.
- Aperçu d'un style de manette dans l'éditeur, sans avoir à brancher le périphérique.
