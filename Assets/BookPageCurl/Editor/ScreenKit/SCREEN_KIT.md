# Screen Kit: building a new animated screen

Every screen is a short script. It lists what is on the page, in reading order, and the kit places it and animates it
with the same choreography as the reference video (`App Animation_2.mp4`). You never type a delay by hand.

## Files

| File | What it is |
|---|---|
| `ScreenKit.cs` | `Screen` (the builder), `Style` (fonts and colours), `Beat` (the video's timings), `Lane` (a column that reads top to bottom), `Fx` (the motion presets) |
| `ScreenIcons.cs` | Draws simple line icons (`alert`, `cap`, `trend`, `translate`, `clock`, `layers`) the first time they are used. Replace `Assets/BookPageCurl/Sprites/Icons/<name>.png` with real artwork and every screen updates |
| `MainPage4.cs` | The Page 4 screen. **Copy this file for each new screen** |

Runtime pieces (not editor): `MotionStudio/*`, `AxisReveal`, `CardLineToBox`, `PageIntro`.

## Make a new screen

1. Copy `MainPage4.cs`, rename the class and the `[MenuItem]` (for example `Book/Screens/Build Main Page 5`).
2. Change the page name in `Find("MainCanvas", "Page 5")`.
3. Replace the content. Run the menu item. It rebuilds the page's `Sheet` each time, so you can re-run it freely.

```csharp
Screen s = Screen.Begin(page);                       // clears the Sheet, adds MotionSequence + PageIntro
s.Header("EYEBROW", "HEADLINE LINE ONE\nLINE TWO <color=#8C9BFF>ACCENT.</color>", "side note", 0.9f);
RectTransform card = s.Card("Card", 0.07f, 0.31f, 0.93f, 0.745f);   // x0, y0, x1, y1 as fractions of the page
// Several boxes: the line is drawn at originY, the part above opens up, each divider below opens down in turn.
// header row + body + strip:  s.Card("Card", 0.07f, 0.31f, 0.93f, 0.745f, 0.79f, 0.14f);   // originY = 0.79, divider at 0.14
s.Glow(card, 0.5f, 0.5f, 0.79f, 1f, Beat.Glow);      // light rising from the bottom of a row (centre, width, rowBottom, rowTop)
Lane lane = new Lane(Beat.Content + 0.5f);           // a column that reads top to bottom
s.Read(card, lane, "Intro", "Some words…", s.style.light, 26, s.style.white, 0.03f, 0.5f, 0.3f, 0.9f);
s.HRule(card, "Rule", 0f, 1f, 0.14f, at: 4.0f);      // line wiping in from the left
s.VRule(card, "Split", 0.333f, 0.14f, 0.79f, 3.4f);  // line growing up from the bottom
s.Icon(card, "Icon", "clock", 0.9f, 0.9f, 44f, s.style.white, 3.2f);
s.Footer(6.0f);                                      // the shared contact footer
s.Finish();
```

All positions are fractions (0 to 1) of the parent, so nothing depends on pixel size.

### What plays, and when (`Beat`)

| Seconds | What |
|---|---|
| 0.0 | Eyebrow rule is drawn leftward, then its right end pulls in |
| 0.7 | Eyebrow label slides in from the right |
| 0.72 | Headline, word by word from the right, blue settling to white |
| 1.0 | A line sweeps left to right across the card |
| 2.2 | The line opens into the box, upward first, the lower half 0.15 s later |
| 2.6 | The glow fades in |
| 3.0 | Content flows: icons turn in, words rise in with a soft blur, dividers grow |
| end | The footer follows the last piece of content |

Content uses `Lane`s. Each lane keeps its own clock, so three columns can read at the same time. Start each lane a
little later than the one before (about 0.35 s) and the screen stays calm instead of everything landing at once.

### Presets (`Fx`)

`Words` (word by word, soft blur) · `Headline` · `Fade` · `Eyebrow` · `Icon` · `Sweep` · `WipeLeft` · `WipeUp` ·
`CardFrame` · `Glow`. They add the Motion Studio component and set `delay`, so they also work on objects you place by hand.

## Rules that keep it working

* Design the final screen. The components record that pose and animate towards it.
* One screen, one `Sheet` on the page. The page holds a `MotionSequence` and `PageIntro`. Do not add your own animation
  code to objects the kit animates.
* The shared footer and logo come from `Assets/BookPageCurl/Prefabs/ScreenFooter.prefab` and `ScreenLogo.prefab`. They were
  cut from Page 3 the first time and are reused after that. Edit the prefab to change every screen.
* Colours and fonts live in `Style`. Timings live in `Beat` and `Fx`.
* Preview word animation in **Play mode**. In edit mode TextMeshPro shows every word.

## Checking a screen

Enter Play mode, activate the page, then call `Scrub(seconds)` on its `MotionSequence` and render the camera. Look at
the start, the line, the box opening, the content and the end. At `TotalDuration` the screen must look like the design.

## Displays and cameras (two displays)

* **Display 1** is `SpreadCanvas` with `SpreadCamera`: the left and right pages together on one canvas, 2048x1820 inside,
  on top of `Assets/UI/TwoPages.png`. `LeftCanvas` and `RightCanvas` are now the two halves of that spread. They kept their
  names and their `PagePanels`, so every page, script and screen builder that uses them works as before.
* **Display 2** is `MainCanvas` with `SingleCamera`.
* A page turn is one `Book` over the whole spread: the right page lifts, curls across the spine and lands on the left.
  The turn is moved by time with a soft start and end (`BookSwipe.flipDuration`, 0.9 s on the spread).
* `Book/Setup/Merge Left And Right Into One Spread` did the conversion. It does nothing if the spread already exists.
