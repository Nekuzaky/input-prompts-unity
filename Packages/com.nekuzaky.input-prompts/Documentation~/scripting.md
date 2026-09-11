# Scripting

Everything lives in `Nekuzaky.InputPrompts`. `InputPromptService` is a static class: one active
device for the whole UI, no instance to wire.

```csharp
using Nekuzaky.InputPrompts;
```

## Reading the current state

```csharp
InputDeviceStyle style = InputPromptService.CurrentStyle;   // KeyboardMouse, Xbox, PlayStation, ...
InputDevice device = InputPromptService.ActiveDevice;       // the device that was last actuated
InputPromptSet set = InputPromptService.CurrentSet;         // the icon set in use
```

## Reacting to changes

```csharp
void OnEnable()
{
    InputPromptService.StyleChanged += OnStyleChanged;      // keyboard -> gamepad, Xbox -> DualSense
    InputPromptService.PromptsChanged += Repaint;           // anything that can stale a prompt
}

void OnDisable()
{
    InputPromptService.StyleChanged -= OnStyleChanged;
    InputPromptService.PromptsChanged -= Repaint;
}
```

`StyleChanged` fires only when the icon family actually changes, so alternating between a keyboard
and a mouse raises nothing. `PromptsChanged` is the wider one: device change, rebind, database swap.

Always unsubscribe. These are static events; a subscriber that never detaches keeps its object alive.

## Resolving icons and names

```csharp
Sprite icon = InputPromptService.GetSprite(action);             // for the device in use
Sprite up = InputPromptService.GetSprite(action, "up");         // one part of a composite
Sprite raw = InputPromptService.GetSpriteForPath("<Gamepad>/buttonSouth");
Sprite cap = InputPromptService.GetBlankSprite();               // blank key cap of the current set

string name = InputPromptService.GetDisplayString(action);      // "Space", "A", ...
```

Lower level, when you need the binding rather than the icon:

```csharp
int index = InputPromptService.ResolveBindingIndex(action);                  // -1 when nothing fits
int strict = InputPromptService.ResolveBindingIndex(action, null, false);    // no cross-device fallback
bool ours = InputPromptService.MatchesCurrentStyle("<Keyboard>/space");
```

Resolution order, for a given action:

1. `PreferExactDevice` on → a binding for the exact device in use.
2. A binding for any layout of the current family, first one in the action wins.
3. Any binding at all, unless `allowAnyDevice` is false.

Step 2 is why binding order matters: an action bound to `<Mouse>/leftButton` then `<Keyboard>/enter`
always shows the mouse icon. Swap the two in the `.inputactions` asset to show Enter instead.

## Settings

```csharp
InputPromptService.PointerMotionSwitchesStyle = false;  // moving the mouse leaves gamepad prompts alone
InputPromptService.UseKeyboardLayoutLabels = true;      // AZERTY: <Keyboard>/w draws the Z key
InputPromptService.PreferExactDevice = false;           // keyboard and mouse resolved as one family
```

The database carries the same three values and applies them at startup, so set them in the dashboard
for defaults and in code only when a player option needs to change them live.

## Rebinding

Applying a binding override raises `InputActionChange.BoundControlsChanged`, which the service turns
into `PromptsChanged`. Live prompts repaint on their own.

```csharp
public void StartRebind(InputActionReference reference)
{
    var action = reference.action;
    action.Disable();                                   // required while rebinding

    action.PerformInteractiveRebinding()
        .WithControlsExcluding("<Mouse>/position")      // otherwise the mouse wins instantly
        .WithCancelingThrough("<Keyboard>/escape")
        .OnComplete(operation =>
        {
            operation.Dispose();
            action.Enable();
        })
        .Start();
}
```

Call `InputPromptService.Refresh()` yourself only when you change bindings while the actions are
disabled: the Input System does not always raise the event then.

## Saving the player's bindings

```csharp
PlayerPrefs.SetString("bindings", m_actions.SaveBindingOverridesAsJson());

m_actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString("bindings"));
InputPromptService.Refresh();
```

## Pinning a device

```csharp
InputPromptService.SetActiveDevice(Gamepad.all[0]);
```

This forces the device the prompts follow. It is **global**: there is one active device for the
whole UI, so it cannot give two players different prompts in local co-op.

## Lifecycle

`Initialize()` runs by itself before the first scene loads, and every component calls it defensively
on enable. `Shutdown()` detaches the Input System callbacks; it exists for tests and for code that
swaps the whole input stack at runtime.

## Path helpers

`ControlPath` is public for code that needs the same parsing as the package:

```csharp
ControlPath.ToKey("<Keyboard>/space");        // "space"
ControlPath.LayoutOf("<Gamepad>/buttonSouth") // "Gamepad"
ControlPath.MatchesDevice(path, device);      // can that device actuate that path
```
