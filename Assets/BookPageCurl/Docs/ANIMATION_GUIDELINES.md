# Animation guidelines

These rules are the style of the whole product. Follow them for every new screen. If a rule and a request disagree, ask.

## 1. The feeling

Calm, premium, confident. Things arrive together, each with its own small motion, and settle softly. Nothing pops, nothing bounces, nothing is slow for the sake of it. A screen is finished in about four seconds, then it rests.

## 2. The timeline (Beat)

Every screen uses the same beats, seconds from the moment the screen is shown. Never type a delay by hand; use the `Beat` constants in `ScreenKit.cs`.

| Seconds | What happens |
|---|---|
| 0.0 | Eyebrow rule is drawn |
| 0.7 | Eyebrow label slides in |
| 0.72 | Headline, word by word, blue settling to white |
| 1.0 | A line sweeps across the card |
| 2.2 | The line opens into the box (up first, the lower half 0.15 s later) |
| 2.6 | The glow fades in |
| 3.0 | Content flows: icons turn in, words rise in with a soft blur, dividers grow |
| end | The footer follows the last piece of content |

Content lanes: several columns can read at once, each lane starting about 0.35 s after the one before.

## 3. Motion vocabulary

| Use | Preset (`Fx`) | Behaviour |
|---|---|---|
| Any text | `Words` | Word by word, rising from below, soft blur that clears. **All** word-by-word text has the blur. |
| Headline | `Headline` | From the right, blue to white |
| Small label | `Eyebrow` | Fades in sliding from the right |
| Icon (picture) | `Icon` | Fades, grows from 50% and turns in |
| Lines | `Sweep`, `WipeLeft`, `WipeUp`, `WipeOut` | Lines are drawn, never faded in |
| Box | `CardFrame` | A line that opens into a box |
| Light | `Glow` | Rises from the bottom edge of its row and is clipped by the row |
| Anything else | `Fade` | Fade with a short slide |

## 4. Page turns (the book)

* A turn is moved by time with a soft start and end (smootherstep), about 0.9 s on the spread.
* The sheet is a sheet of paper: only the white page turns. The frame and the background video around it stay and keep playing.
* After a row button (At A Glance, Seal Of Approval, Get In Touch) the main page changes but the spread does not turn. Only Begin Here lets the spread turn.
* Home leafs back through the book: up to 5 quick backward turns on the spread and the main pages back at the same time (0.3 s each), never a jump.
* Pressing the same button again changes nothing, except Begin Here, which replays its page.

## 5. Icons

* Small icons play their video **once** when they appear and hold the last frame.
* Large icons **loop** and drift slowly up and down (`SoftBob`, about 14 units over 4.6 s).
* Icons are transparent video (WebM, alpha). `AnimatedIcon` + `BookUI/VideoIcon` shader paint them in any tint, so the same video is navy on the book and white on the dark screen. The shader thickens thin lines on small icons.
* An icon is never missing: until its video is ready it shows its still picture, and page copies made for a page turn always use the still.

## 6. Backgrounds

Looping video behind the spread and the main page (`LoopVideo`). Cover-crop to the screen shape. Never pause it, never cover it with a static picture.

## 7. How to build a screen

1. Copy `Editor/ScreenKit/MainPage4.cs`, rename the class and menu item, change the page name.
2. Describe the screen as a list in reading order: `s.Header`, `s.Card`, `s.Glow`, `s.Read` (words), `s.Icon`, `s.HRule`, `s.VRule`, `s.Footer`. Positions are fractions (0 to 1) of the parent, so nothing depends on pixel size.
3. `s.Finish()`. Run the menu item. It rebuilds the page's `Sheet`, so it can be re-run any time.
4. Check in Play mode: activate the page and `Scrub` its `MotionSequence`. At the end the screen must look like the design.

## 8. Traps (these cost time once, do not repeat them)

* **Record the rest pose after any edit.** Animations animate *towards* the pose recorded in the scene. Edit a position or colour, then call `Record()`, or the old values come back at Play.
* **An animated item remembers its colour.** If you swap a picture for a tinted icon, set the item's `restColor` to the tint or the page start paints it the old colour.
* **RectMask2D does not clip its own graphic.** Use a clip parent, a filled Image, or a container.
* **TextMeshPro words only animate in Play mode.** In edit mode every word shows.
* **Spread halves must sit on the spread plane** (z = 0 under the camera), or they draw smaller than the screen.
* **Canvas children must be at z = 0** with a perspective camera.
* **Pages are found by name** (`Page 8`, `Question 2-1`, `Closing`), never by position, so pages can be added or removed.
* **Never run a page builder over a page that was tuned by hand.** Use the copy tools (`Copy Left Page 9 Layout To Pages 10-13`) instead.
* **Video decode needs real time.** In an unfocused editor videos do not advance; judge video in a focused Game view.

## 9. Words

Short, sentence-case descriptions; uppercase for titles and eyebrows with wide letter spacing (about 18 for eyebrows, 4 to 8 for titles). Two-part titles: first part white, second part accent `#8C9BFF`.
