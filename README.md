# Input Prompts

Package Unity qui affiche la bonne icône de touche ou de bouton pour une `InputAction`, et la
remplace toute seule quand le joueur passe du clavier à la manette, ou d'une Xbox à une DualSense.
Les icônes proviennent du pack [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts) (CC0).

| | |
|---|---|
| Package | `com.nekuzaky.input-prompts` |
| Unity | 6000.0 ou plus récent |
| Dépendances | Input System 1.14+, uGUI |

Ce dépôt est à la fois **le package** (`Packages/com.nekuzaky.input-prompts`) et le **projet de test**
qui sert à le développer.

---

## Installation

Dans Unity : `Window > Package Manager > + > Install package from git URL...`

```
https://github.com/Nekuzaky/input-prompts-unity.git?path=Packages/com.nekuzaky.input-prompts
```

Ou directement dans `Packages/manifest.json` :

```json
"com.nekuzaky.input-prompts": "https://github.com/Nekuzaky/input-prompts-unity.git?path=Packages/com.nekuzaky.input-prompts"
```

Le package ne contient **aucun sprite** : télécharge le pack Kenney et pose-le dans ton projet
(par exemple `Assets/Sprites/input-prompts`), l'importer s'occupe du reste.

---

## 1. Générer les bases d'icônes

`Tools > Input Prompts > Importer`

| Champ | Rôle |
|---|---|
| **Pack folder** | Dossier du pack Kenney. |
| **Variant** | `Default` (1x) ou `Double` (2x, pour de la grosse UI). |
| **Outline icons** | Utilise les variantes `_outline` quand elles existent. |
| **Coloured face buttons** | Boutons A/B/X/Y et croix/rond/carré/triangle en couleurs. |
| **Fix texture settings** | Passe les PNG en Sprite, sans mipmaps, clamp. |
| **Unknown gamepads** | Style utilisé pour une manette non reconnue (Xbox par défaut). |

*Generate prompt sets* écrit dans `Assets/_/Database/InputPrompts/` :

- `SO_InputPromptSet_*.asset` — un jeu d'icônes par famille de périphérique ;
- `Resources/SO_InputPromptDatabase.asset` — la base chargée automatiquement au lancement, rien à câbler.

Le rapport liste les icônes manquantes (`buttonSouth -> xbox_button_a.png`). Les sets restent
éditables à la main : ce sont de simples listes clé → sprite.

## 2. Afficher un prompt

**Une icône** : `GameObject > UI > Input Prompt Icon`, puis glisse l'`InputActionReference`.

```csharp
icon.Action = playerInput.actions["Jump"];
```

**Une action composite (WASD)** : `InputPromptGroup` sur un objet avec un `HorizontalLayoutGroup`,
plus un prefab d'icône. Il instancie 4 icônes au clavier (Z Q S D en AZERTY) et une seule — le stick
gauche — à la manette.

**Du texte** : `InputPromptText` sur un TextMeshPro, avec un format du type
`Appuie sur {Player/Jump} pour sauter`. Le token `{Map/Action#part}` accepte une part de composite.

**Démo** : sélectionne ton `.inputactions` puis `Tools > Input Prompts > Create Demo Canvas` — une
ligne par action avec ses icônes.

## 3. Aperçu dans l'éditeur

L'inspector d'`InputPromptIcon` a un menu *Show icons of* : force l'affichage en PlayStation, Switch,
etc. sans brancher la manette. Ça ne concerne que le mode édition.

---

## API

```csharp
InputPromptService.CurrentStyle;                       // famille de périphérique en cours
InputPromptService.StyleChanged += style => { };       // clavier -> manette
InputPromptService.PromptsChanged += () => { };        // périphérique, rebind, base changée
InputPromptService.GetSprite(action);                  // icône pour l'action
InputPromptService.GetDisplayString(action);           // "Espace", "A", ...
InputPromptService.SetActiveDevice(device);            // co-op local : forcer le périphérique d'un joueur
InputPromptService.PointerMotionSwitchesStyle = true;  // bouger la souris repasse en icônes souris
InputPromptService.UseKeyboardLayoutLabels = true;     // AZERTY : <Keyboard>/w affiche la touche Z
InputPromptService.Refresh();                          // après un rebind fait à la main
```

## Ajouter un périphérique

1. Ajoute la valeur dans `InputDeviceStyle`.
2. Complète `KenneyNameTable` (`FolderFor`, `LayoutsFor`, `BlankFor`, et la table de noms).
3. Relance l'importer.

Les clés d'un set sont des chemins de contrôle sans le périphérique, en minuscules : `space`,
`buttonsouth`, `leftstick/up`, `dpad/left`, `scroll/y`.

## Détails traités

- Les faces Switch sont inversées : `buttonSouth` correspond au **B** chez Nintendo.
- En AZERTY, `<Keyboard>/w` affiche la touche **Z** que le joueur a réellement sous les doigts.
- Bouger la souris ne fait pas repasser les prompts en clavier tant qu'on n'a rien cliqué.
- Une touche sans icône dans le pack (F13, oem…) s'affiche sur un capuchon vierge avec son nom.

## Crédits

- Icônes : [Kenney — Input Prompts](https://kenney.nl/assets/input-prompts), CC0. Non redistribuées ici.
- Code : MIT, voir [LICENSE.md](Packages/com.nekuzaky.input-prompts/LICENSE.md).
