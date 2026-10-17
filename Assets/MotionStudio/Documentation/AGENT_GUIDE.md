# Motion Studio: guide for AI agents

Read this file completely before building animation. It is written to be unambiguous. Namespace: `MotionStudio` (runtime), `MotionStudio.Editor` (editor only). No other plugin is needed. Works on Unity UI (uGUI) only.

## 0. The mental model (5 rules)

1. **Design the final screen first.** Place every object where it should rest when the animation is finished. The components record that pose (position, scale, rotation, opacity, color) and animate **towards it**. Never type target positions.
2. **One component per effect on the object it animates.** `MotionItem` (fade/slide/scale/rotate/color/text pieces), `MotionCard` (line to card), `MotionGlow` + `MotionGlowGraphic` (shaped light), `MotionIconComposer` (icon built from pieces). All of them inherit `MotionBehaviour` and have `order`, `delay`, `label`.
3. **A `MotionSequence` plays them in order.** It sits on a parent (usually the canvas root), `Collect(true)` fills it from all `MotionBehaviour` below it sorted by `order`, equal `order` starts together, `interval` is the time between groups.
4. **Do not animate the same properties from other code** (anchoredPosition, localScale, rotation, alpha of the object, Graphic.color) while a Motion component controls them.
5. **UI only.** Objects need a `RectTransform`. Text effects need a `Text` or `TMP_Text` on the same object. A `CanvasGroup` is added automatically by `MotionItem`.

## 1. Fast recipe (editor script or runtime script)

```csharp
using MotionStudio;
using UnityEngine;

// 1. build/place the UI in its final layout (canvas, images, texts ...)
// 2. add motion
MotionSequence seq = Motion.Sequence(canvas.gameObject, interval: 0.18f);   // spacing StartToStart
seq.AddTo(Motion.FadeIn(logo,     MotionDirection.Top,    60f, 0.8f));      // order 0
seq.AddTo(Motion.Words(title,     MotionDirection.Bottom, 36f, 0.08f));     // order 1, word by word
seq.AddTo(Motion.Words(subtitle,  MotionDirection.Left,   60f, 0.05f));     // order 2
seq.AddTo(Motion.Card(panel));                                              // order 3, line opens into the card
seq.AddTo(Motion.Compose(icon, ComposeStyle.Assemble, 1.8f));               // order 4, icon from pieces
seq.AddTo(Motion.Glow(lightObject));                                        // order 5 (needs MotionGlowGraphic on it)
seq.playOnStart = true;
```

`AddTo` sets `order = number of entries so far`, so the call order is the show order. To make two things appear **together** give them the same number:
`a.order = 3; b.order = 3;`.

## 2. Component reference (exact field names)

### MotionBehaviour (all components)
| field | type | default | meaning |
|---|---|---|---|
| `order` | int | 0 | position in sequence, lower first, equal = together |
| `delay` | float | 0 | seconds before the item starts (extra to the sequence spacing) |
| `label` | string | "" | name in the timeline |

Methods: `Prepare()`, `Play(speed)`, `PlayReverse(speed)`, `Stop()`, `Evaluate(time)`, `ApplyStartState()`, `ApplyEndState()`, `ApplyRestState()`; properties `Duration`, `ContentDuration`.

### MotionItem
`duration` (0.8), `ease` (`MotionEase.AppleOut`)
`fade` (true), `fadeFrom` (0..1, 0), `fadeDuration` (seconds, 0 = 0.75 x duration), `fadeEase`
`direction` (`MotionDirection`: None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight, Custom), `distance` (80), `customOffset` (Vector2). Left = it comes **from** the left.
`scale` (bool), `scaleFrom` (0.9), `rotate` (bool), `rotateFrom` (degrees)
`changeColor` (bool), `colorFrom` (initial color), `colorTo` (final color), `colorDuration` (0 = duration), `colorEase`: color of the Graphic moves from → to
`split` (`TextSplit`: Whole, Words, Characters, Lines), `splitOrder` (`TextOrder`: Forward, Reverse, FromCenter, ToCenter, Random), `pieceStagger` (seconds between pieces, 0.07), `pieceDuration` (0 = use duration)
`loopColor` (bool), `loopGradient`, `loopSeconds`; `floatAmount`, `floatSeconds`
Recorded pose: `recorded`, `restPosition`, `restScale`, `restRotation`, `restAlpha`, `restColor`. Call `Record()` after you moved the object.

Text with pieces: TextMeshPro moves/scales/fades/tints each piece. Legacy `Text` only fades and tints (no per-piece movement). When `split != Whole`, the object-level fade/slide is replaced by the per-piece version.

### MotionCard
`origin` (`CardOrigin`: Line, FromObject), `lineAxis` (`CardLineAxis`: Horizontal = line is as wide as the card and the card opens up/down; Vertical = line as tall as the card, opens left/right)
Line: `drawLineFirst` (true), `lineDrawDuration` (0.5), `lineHold` (0.1), `lineThickness` (3), `lineEase`, `lineColors` (Gradient over the whole reveal → the line changes color), `lineGlow` (true), `lineGlowSize` (22), `fadeLine` (true)
Open: `expandDuration` (0.8), `expandEase`
Fade of the whole card: `fade` (false), `fadeFrom`, `fadeDuration` (0 = whole reveal), `fadeEase`
Color of the card graphic: `changeColor` (false), `colorFrom` (initial), `colorTo` (final), `colorDuration`, `colorEase`
From object: `fromObject` (RectTransform), `fromObjectDuration` (0.9), `fromObjectEase`, `fadeSource` (true)
`clipChildren` (true): children are revealed with the card (adds a RectMask2D).
Needs a Graphic (Image/RawImage/Text) on the same object and a size > 1.

### MotionGlowGraphic (the light) + MotionGlow (its animation, same object)
Graphic: `shape` (Sprite, alpha = shape), `shapeFromBrightness`, `blur` (0..1), `brightness`, `softness`, `additive`, `colorA`, `colorB`, `gradientAngle`, `gradientOffset`, `scrollSpeed`, `repeatGradient`. Call `PushProperties()` after changing values from code. The component `color` is a tint (alpha = opacity).
Animation: `duration` (1.6), `ease`, `growFrom` (0.2 of the set size), `fadeFrom`, `fadeDuration` (0 = 0.7 x duration), `fadeEase`, `changeColor`, `colorFrom` (initial tint), `colorTo` (final tint), `colorDuration`, `colorEase`, `brightnessFrom`, `extraBlurFrom`, `cycleColors`, `palette` (Gradient), `cycleSeconds`, `spin` (deg/s), `breathe` (0..0.5), `breatheSeconds`, `drift`, `driftSeconds`.
Masked inside a sprite: make the glow a child of an Image that has a `Mask` component. Free: no mask.
Size the glow RectTransform to the **final** size, `MotionGlow` grows it from `growFrom`.

### MotionIconComposer
`source` (Graphic, default: same object; Image with sprite or RawImage), `columns`, `rows` (2..40), `style` (`ComposeStyle`: Assemble, Pixel, Slide, Dissolve), `startFrom` (`ComposeStart`: Scatter, Left, Right, Top, Bottom, Radial), `wave` (`ComposeWave`: FromCenter, ToCenter, LeftToRight, RightToLeft, TopToBottom, BottomToTop, Diagonal, Random), `duration` (1.8), `ease`, `pieceShare` (0.1..1), `distance`, `spinFrom`, `scaleFrom`, `randomness`, `seed`, `restoreOriginalAtEnd` (true), `fadeDuration` (0 = quarter of the piece time), `fadeEase`, `changeColor`, `colorFrom` (initial), `colorTo` (final), `colorDuration`, `colorEase`.
Use a white icon on transparent background, tint with `Image.color`. `Rebuild()` re-cuts the pieces after changing the sprite or the grid.

### MotionSequence
Fields: `entries` (`List<Entry>`, Entry = `target` MotionBehaviour, `offset` float, `enabled` bool), `interval` (0.15), `spacing` (`SequenceSpacing`: StartToStart = next starts `interval` after previous *started*, AfterPrevious = after previous *finished*), `groupSameOrder` (true), `playOnStart` (true), `startDelay`, `hideUntilPlayed` (true), `loop`, `loopPause`, `speed`, `useUnscaledTime`, `onStarted`, `onCompleted` (UnityEvent).
Methods: `Collect(bool sortByOrder=true)`, `Add(target, offset)`, `Clear()`, `SortByOrder()`, `NumberEntriesByListOrder()`, `Prepare()`, `Play()`, `PlayReverse()`, `Pause()`, `Resume()`, `Stop()`, `Complete()`, `ResetToStart()`, `ResetToRest()`, `Scrub(seconds)`; properties `TotalDuration`, `IsPlaying`, `Time`.
Timing rule: groups of equal `order`, sorted ascending. Group start = previous group start + interval (StartToStart) or previous group end + interval (AfterPrevious). Entry start = group start + `entry.offset`; each item also waits its own `delay`.

### Motion (static helpers, runtime and editor)
`FadeIn(go, direction, distance, duration, delay, ease)`, `Words(textGo, direction, distance, stagger, split, delay)`, `ColorChange(go, from, to)`, `Card(go, delay)`, `CardFrom(go, fromRect, delay)`, `Compose(iconGo, style, duration, delay)`, `Glow(go, duration, delay)`, `Sequence(host, interval, spacing)`, `sequence.AddTo(behaviour, offset)`.

## 3. Recipes

**Headline with subtitle, word by word, color settling:**
```csharp
var h = Motion.Words(title, MotionDirection.Bottom, 40f, 0.09f);
var s = Motion.Words(sub, MotionDirection.Left, 60f, 0.04f);
s.changeColor = true; s.colorFrom = gold; s.colorTo = muted;   // colors of the text
```
**Card that opens from a line, content revealed with it:** put `Motion.Card(panel)` on the panel Image. Children (texts, icons) can have their own `MotionItem` for a second layer of motion.
**Card from a button/dot:** `Motion.CardFrom(panel, dot.GetComponent<RectTransform>())`. Give the dot its own `MotionItem` (order earlier than the card).
**Light that stays inside a card:** card Image → child Image (same sprite, `Mask`, *showMaskGraphic = false*, stretched) → child with `MotionGlowGraphic` + `MotionGlow`.
**Icon that builds itself, then a glow flash:** `Motion.Compose(icon, ComposeStyle.Assemble)` with order N, `Motion.Glow(flash)` with order N+1 or the same order and `delay = 1.2f`.
**Exit animation:** `sequence.PlayReverse()`.
**Replay button:** `button.onClick.AddListener(sequence.Play)`.
**Everything at once with tiny stagger:** same `order` for all, `groupSameOrder = true`, give each a different `delay` (0, 0.05, 0.1 ...).

## 4. Verify your work (editor, no Play mode)

```csharp
// show the sequence at 1.4 seconds, render the canvas to a png, put everything back
MotionStudio.Editor.MotionPreview.Scrub(sequence, 1.4f);
MotionStudio.Editor.MotionCapture.Save(canvas, "Temp/shot.png");   // works for Overlay canvases too
MotionStudio.Editor.MotionPreview.Stop();                          // always call this, then save the scene
```
`sequence.TotalDuration` tells you the length. Look at pictures at the start, middle and end. At `TotalDuration` the screen must look exactly like the design.

## 5. Checklist before you finish

* The scene is saved **after** `MotionPreview.Stop()` (nothing half animated is stored).
* Each animated object has exactly one component per effect (a `MotionItem` and a `MotionCard` on the same object are fine, two `MotionItem` are not).
* `sequence.entries.Count` equals the number of animated objects (run `Collect(true)` after adding components).
* `order` numbers describe the show order, same number = together.
* Text objects with `split != Whole` have a `Text` or `TMP_Text` on the same GameObject.
* Cards have a real size. Glow objects have a `shape` sprite.
* No other script drives the position/scale/alpha of an animated object.

## 6. Common mistakes

| Mistake | Result | Fix |
|---|---|---|
| Typing start positions | item jumps | place the **final** layout, use `direction`/`distance` |
| Moving an object after the component was added | it animates to the old place | `MotionItem.Record()` / press *Record Rest Pose* |
| `Collect` before adding components | empty list | add components, then `Collect(true)` |
| Using `Left` expecting it to leave to the left | it comes from the left | direction = where it comes from |
| Per-word slide on legacy `Text` | words only fade | use TextMeshPro |
| Glow sprite packed in an atlas or without margin | cut or hard edge | Sprite Mode Single, transparent margin |
| Layout Group repositions an animated child | wrong position | animate content inside a fixed-size container |
