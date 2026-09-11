# Components

Four components cover what a game needs on screen. They all subscribe to
`InputPromptService.PromptsChanged` while enabled, so they repaint themselves on a device change or
a rebind; nothing polls, nothing runs per frame.

---

## InputPromptIcon

`Add Component > Input Prompts > Input Prompt Icon`, or `GameObject > UI > Input Prompt Icon` to get
an `Image` and the component in one go. Requires an `Image` on the same object.

Shows one control: the key or button the action is bound to on the device in use.

| Field | Default | What it does |
|---|---|---|
| **Action** | none | The `InputActionReference` to display. Leave empty and assign `Action` from code instead. |
| **Composite part** | empty | Which part of a composite to show: `up`, `down`, `left`, `right`, or any part name of your composite. Empty means a plain, non-composite binding. |
| **Target Image** | self | The `Image` that receives the sprite. Filled by `Reset`, change it to draw into a child. |
| **Fallback Label** | none | Optional `TMP_Text`. When the pack has no icon for a control, the blank key cap is drawn and the control name is printed on it. Without a label, the cap is drawn empty. |
| **Hide When Unbound** | on | Disables the `Image` when the action has no binding at all. Off keeps the last sprite visible. |
| **Resize To Sprite Aspect** | on | Sets the RectTransform width from the sprite ratio, so wide caps such as Space or Enter keep their shape. Only applies when the height is not driven by a stretched anchor. |

```csharp
icon.Action = playerInput.actions["Jump"];   // rebuilds immediately
icon.CompositePart = "up";                   // same
string label = icon.DisplayString;           // "Space", "A", ... for a sentence next to the icon
icon.Refresh();                              // only needed after changing bindings while disabled
```

The inspector also carries a **Show icons of** dropdown. It forces a device family in edit mode so a
menu can be checked against PlayStation or Switch icons without owning the controller. It has no
effect in play mode or in a build.

---

## InputPromptGroup

`Add Component > Input Prompts > Input Prompt Group`.

Shows every part of an action at once, which is what a movement action needs: four keys on keyboard,
one stick on a gamepad. It spawns instances of an icon prefab and reuses them across rebuilds.

| Field | Default | What it does |
|---|---|---|
| **Action** | none | The `InputActionReference` to display. |
| **Icon Prefab** | none | A prefab carrying an `InputPromptIcon`. One instance per displayed part. |
| **Container** | self | Parent for the spawned icons. Put a `HorizontalLayoutGroup` on it. |

What it decides, in order:

1. The action has a plain binding for the current family → **one icon**, no part name. A gamepad
   `leftStick` lands here.
2. Otherwise, the parts of the composite that belong to the current family → **one icon per part**.
   A WASD `2DVector` lands here.
3. Nothing matched → whatever the action does have, rather than an empty row.

```csharp
group.Action = playerInput.actions["Move"];  // rebuilds
group.Rebuild();                             // force a rebuild
```

The component runs with `ExecuteAlways`, so the icons are visible while editing. Icons spawned in
edit mode carry `HideFlags.DontSave` and are destroyed when the component is disabled: they are a
preview, never scene content.

---

## InputPromptText

`Add Component > Input Prompts > Input Prompt Text`. Requires a `TMP_Text` on the same object.

Writes a sentence with control names substituted in. Use it where an icon alone would not read, or
next to an icon for the name.

| Field | Default | What it does |
|---|---|---|
| **Target** | self | The `TMP_Text` that receives the sentence. |
| **Actions** | none | The `InputActionAsset` the tokens are resolved against. |
| **Format** | `Press {Player/Jump} to jump` | The sentence. Tokens are replaced, everything else is kept. |
| **Use icons** | on | Render the icon inline with a `<sprite>` tag instead of the control name. |

Token syntax:

| Token | Resolves to |
|---|---|
| `{Player/Jump}` | the control bound to the `Jump` action of the `Player` map |
| `{Jump}` | same, when the action name is unique in the asset |
| `{Player/Move#up}` | the control bound to the `up` part of the `Move` composite |

An unknown token is left as written, so a typo shows up on screen instead of silently disappearing.

```csharp
text.Format = "Hold {Player/Sprint} to run";   // repaints
```

With **Use icons** on, a token renders as an inline `<sprite>` tag pulled from the sprite asset of the
family in use, and falls back to the control name when that family has no icon for the control. Turn
it off to always print names.

---

## Menu entries

| Menu | Effect |
|---|---|
| `Tools > Input Prompts > Dashboard` | Opens the configuration window. |
| `Tools > Input Prompts > Generate Prompt Sets` | Runs the import with the stored settings, no window. |
| `Tools > Input Prompts > Create Demo Canvas` | Builds a canvas with one row per action of the selected `.inputactions` asset, each showing its icons. Creates the icon prefab if it does not exist yet. |
| `GameObject > UI > Input Prompt Icon` | Creates a ready to use icon under the current canvas. |

---

## InputPromptRebindButton

`Add Component > Input Prompts > Input Prompt Rebind Button`.

Lets the player pick a new control for an action. It listens for the next input, applies it, resolves
duplicates and saves the result.

| Field | Default | What it does |
|---|---|---|
| **Action** | none | The `InputActionReference` to rebind. |
| **Composite part** | empty | Part of a composite to rebind, e.g. `up`. Empty for a plain binding. |
| **Icon** | child | `InputPromptIcon` showing the current control. Hidden while listening. |
| **Label** | none | Optional `TMP_Text`. Shows the listening text while waiting, the control name otherwise. |
| **Button** | self | The `Button` that starts the rebind. Found on the same object by `Reset`. |
| **Listening Text** | `Press any key` | Shown while waiting for an input. |
| **Cancel Path** | `<Keyboard>/escape` | Control that aborts the rebind. |
| **Excluded Paths** | mouse position, delta, scroll | Controls the player cannot bind to; without them the mouse wins instantly. |
| **On Duplicate** | `Swap` | What to do when the chosen control is already bound elsewhere. |
| **Save Key** | `InputPrompts.Bindings` | PlayerPrefs key the overrides are written to. Empty saves nothing. |

Duplicate policies:

| Policy | Effect |
|---|---|
| `Allow` | Two actions end up on the same control. |
| `Reject` | The new binding is dropped and the previous control comes back. |
| `Swap` | The other action takes the control this one just left, so no action is left unbound. |

Events: `m_started`, `m_completed`, `m_canceled`, `m_rejected`, all `UnityEvent`, wired in the
inspector.

```csharp
rebindButton.StartRebind();     // also called by the Button itself
rebindButton.Cancel();
rebindButton.ResetBinding();    // back to the binding of the .inputactions asset
```

Conflicts are searched **across the whole action asset**, not just the current map, so binding Jump
onto the key used by the UI Submit action is caught.

### Saving and loading

```csharp
RebindStore.Save(actions);                 // writes the overrides to PlayerPrefs
RebindStore.Load(actions);                 // reads them back and refreshes the prompts
RebindStore.Clear(actions);                // back to the defaults
```

The button saves on its own after each successful rebind. Loading is up to the game, usually at
startup, because the package does not know when your action asset is ready.
