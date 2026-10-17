# Motion Studio

Ordered, soft, precise motion for any Unity UI. No dependencies, nothing to install except the folder.

* Fade, slide, scale, rotate and color change for any UI object
* Text word by word, character by character or line by line
* **Order and interval**: choose what appears first and how much time lies between items
* **Card reveal**: the width of a sprite becomes a line, the line opens in both directions into the card, or the card grows out of another object
* **Shaped glow**: a soft light in the shape of any sprite or png, with blur, brightness, a moving gradient and color cycling, inside a mask or free
* **Icon composer**: give it a png, it builds the picture from pieces
* Everything can be previewed in the editor without pressing Play
* One-line code API (`Motion.FadeIn(...)`) and an AI agent guide

Works with Unity UI (uGUI), Screen Space Overlay, Camera and World Space canvases. Legacy `Text` and TextMeshPro are supported.

---

## 1. Install

**Folder:** copy the `MotionStudio` folder into your project's `Assets` folder.

**Unity package:** `Tools > Motion Studio > Export Package (plugin only)` creates a `.unitypackage` you can import into other projects. Use "with demo" to include the demo pictures and builder.

**UPM:** the folder also contains a `package.json`, so it can be copied into `Packages/` or used from a git URL.

First import creates `MotionGlow.mat` next to the glow shader automatically. If glows ever look wrong, run `Tools > Motion Studio > Repair Resources`.

---

## 2. Try the demo

`Tools > Motion Studio > Create Demo In Current Scene` builds a full demo canvas in the open scene. Everything in it is a normal scene object. Press Play, or select the canvas and use the **Preview** buttons of the Motion Sequence component.

---

## 3. Quick start (two minutes)

1. Lay your UI out in the **final** position, exactly as it should look when everything has arrived.
2. Select an object (a text, an image, a panel) and choose `GameObject > Motion Studio > Add Motion Item To Selection`.
3. In the Motion Item choose **Slide in from** = Left, tick **Fade**. For a text set **Split** = Words.
4. Select the parent object (for example the canvas) and add the component **Motion Sequence** (`Add Component > Motion Studio > Motion Sequence`).
5. Press **Collect Children**. All animations below the object are listed in order.
6. Set **Interval** (seconds between items) and press **Play** in the Preview area.

That's it. Press Play in Unity and the sequence runs on start.

---

## 4. The idea of the recorded pose

Motion Studio animates **towards where you placed the object in the scene**. You never type target positions. The first time an item is prepared it **records** its position, scale, rotation, opacity and color. Change your layout later? Press **Record Rest Pose** on the item.

Because of this:
* do not animate the same object's position, scale or alpha from other scripts at the same time
* layout groups: place the object, then record. Animated objects inside layout groups may be repositioned by the group, animate the group's child content instead

---

## 5. Order and interval

The **Motion Sequence** component is the conductor.

| Setting | What it does |
| --- | --- |
| Entries | The list of animations, top plays first. Drag to reorder. |
| Interval | Seconds between items. |
| Spacing | **StartToStart**: next item starts `interval` after the previous one *started* (flowing, overlapping). **AfterPrevious**: next item starts `interval` after the previous one *finished* (strictly one after another). |
| Group Same Order | Items with the **same Order number** start at the same moment. |
| Order (per item) | The number shown in the list. Lower plays first. Change it in the list or on the item itself. |
| Offset (per entry) | Extra seconds for one entry only. Negative values start it earlier. |
| Delay (per item) | The item's own wait before it starts. |
| Play On Start / Start Delay / Loop / Speed | Playback. |
| Hide Until Played | Items stay hidden until their turn so nothing flashes. |

Buttons: **Collect Children** (fills the list from the hierarchy, sorted by Order), **Sort by Order**, **Number by List** (writes 1, 2, 3 ... into the items in the order of the list).

From code:

```csharp
sequence.Play();         // from the start
sequence.PlayReverse();  // exit animation: plays backwards
sequence.Complete();     // jump to the end
sequence.ResetToStart(); // everything hidden again
```

---

## 6. Motion Item (any UI object, also text)

| Group | Setting | Meaning |
| --- | --- | --- |
| Timing | Duration, Ease | Length and curve. `AppleOut` is a long soft deceleration, `AppleInOut` is calm in and out, `SpringOut` overshoots a little. |
| Fade | Fade, Fade From, **Fade Duration**, Fade Ease | Opacity at the start (the whole object including children, through a CanvasGroup). Fade Duration is the time the fade takes on its own, 0 = three quarters of Duration. |
| Slide in from | Direction, Distance, Custom Offset | None, Left, Right, Top, Bottom, the four corners or Custom. *Left* means the object comes **from** the left. |
| Scale / Rotate | Scale From, Rotate From | Start scale (1 = normal) and start angle. |
| Color change | Change Color, **Color From** (initial), **Color To** (final), Color Duration, Color Ease | The color moves from the initial to the final color while arriving. |
| Text pieces | Split, Split Order, Piece Stagger, Piece Duration | Whole, Words, Characters or Lines. Order: Forward, Reverse, FromCenter, ToCenter, Random. Stagger = seconds between pieces. |
| After arriving | Loop Color, Float | Gentle color cycle (a gradient) and a slow floating movement once the item has arrived. |

Text notes:
* **TextMeshPro**: every piece fades, moves, scales and changes color on its own. (Import "TMP Essential Resources" once so TextMeshPro text can render at all.)
* **Legacy Text**: pieces fade and change color. Moving and scaling single words needs TextMeshPro, so use TMP for that effect.
* Rich text tags stay with their word.

---

## 7. Motion Card

Add **Motion Card** to any Image (also sliced and rounded sprites), RawImage or Text.

**Line origin**
1. A thin line the width of the card draws out from the centre in both directions (*Draw Line First*).
2. The line rests (*Line Hold*).
3. The card opens up and down from the line (*Expand Duration*, *Expand Ease*).
4. **Line Axis** = Vertical makes the line as tall as the card and opens to the left and right.

The line has its own color (**Line Colors** is a gradient over the whole reveal, so the line can change color), an optional soft glow (**Line Glow**) and can fade away while the card opens (**Fade Line**).

**Fade and color of the card itself:** tick **Fade** to fade the whole card in (**Fade From**, **Fade Duration**, **Fade Ease**). Tick **Change Color** to move the card from its **initial** (Color From) to its **final** color (Color To) while it opens (**Color Duration**, **Color Ease**).

**From object origin**: set **Origin = From Object** and drag any UI object into **From Object**. The card starts with that object's position and size and grows into its own. The source can fade away (**Fade Source**).

Children of the card (icon, texts, ...) are revealed together with it (*Clip Children*, uses a RectMask2D).

---

## 8. Glow (shaped light)

Create with `GameObject > Motion Studio > Glow`. Two components:

* **Motion Glow Graphic** is the light itself
  * **Shape**: any sprite or png. Its alpha is the shape (tick *Shape From Brightness* for black and white pictures)
  * **Blur**, **Brightness**, **Softness** (pulls the light to the middle or spreads it)
  * **Color A / Color B**, **Gradient Angle**, **Gradient Offset**, **Scroll Speed** (moving gradient), **Repeat Gradient**
  * **Additive** glows on dark backgrounds, off gives normal alpha
  * the tint of the light is the component color, its alpha is the opacity
* **Motion Glow** animates it
  * grows from a part of the size you set (**Grow From**), fades and brightens in, comes into focus (**Extra Blur From**)
  * **Fade From / Fade Duration / Fade Ease** control the fade, **Change Color** with **Color From** (initial) and **Color To** (final) moves the tint of the light while it grows
  * keeps moving afterwards: **Cycle Colors** (a gradient palette), **Spin**, **Breathe**, **Drift**

**Inside a sprite (masked):** put the glow under an Image that has a **Mask** component (turn off *Show Mask Graphic* to keep the light only). The light stays inside the shape of that sprite. **Free:** just don't mask it. A RectMask2D also works.

Tips: use a plain white shape with some empty margin around it. Blur reaches beyond the shape and the mesh is extended automatically for it. Use sprites with *Sprite Mode = Single* (not packed into an atlas).

---

## 9. Icon Composer

Add **Motion Icon Composer** to an Image or RawImage that shows an icon or png (white icons on transparent background look best, tint them with the Image color).

* **Columns / Rows**: size of the pieces
* **Style**: Assemble (pieces fly in and turn), Pixel (pieces pop up like pixels), Slide, Dissolve
* **Start From**: Scatter, Left, Right, Top, Bottom, Radial
* **Wave**: the order the pieces appear: FromCenter, ToCenter, LeftToRight, RightToLeft, TopToBottom, BottomToTop, Diagonal, Random
* **Duration**, **Piece Share** (small = sharp wave, large = soft overlap), **Distance**, **Spin From**, **Scale From**, **Randomness**, **Seed**
* **Fade Duration / Fade Ease**: how long each piece takes to fade in
* **Change Color**, **Color From** (initial), **Color To** (final), **Color Duration**: every piece moves from the initial to the final color as it arrives. The picture ends in the final color.
* At the end the original picture takes over, so there are no seams.

If the texture is **Read/Write enabled**, empty pieces are skipped automatically.

---

## 10. Editor preview

Every component has a **Preview** area: Play, Pause, Reset and a time slider. The Motion Sequence has a **timeline** with a bar per item and a play head. Preview works in Edit mode, nothing needs to run. When the preview stops (or you save the scene, enter Play mode or recompile) everything returns to the designed state, so nothing is saved half animated.

`Tools > Motion Studio > Stop Editor Preview` returns everything to rest at any time.

---

## 11. Code

```csharp
using MotionStudio;

MotionSequence seq = Motion.Sequence(canvas.gameObject, interval: 0.2f);
seq.AddTo(Motion.FadeIn(logo, MotionDirection.Top, 60f));
seq.AddTo(Motion.Words(title, MotionDirection.Bottom));
seq.AddTo(Motion.Card(panel));
seq.AddTo(Motion.Compose(icon, ComposeStyle.Assemble));
seq.AddTo(Motion.Glow(light));
seq.Play();
```

`AddTo` also gives each item the next Order number.

---

## 12. Troubleshooting

| Problem | Fix |
| --- | --- |
| Items flash before their turn | Make sure *Hide Until Played* is on in the sequence. |
| Item ends in the wrong place | Move it where you want it and press **Record Rest Pose**. |
| Word by word text does not move | Legacy Text can only fade and tint. Use TextMeshPro for movement. |
| Card shows nothing | The card needs a size (width and height above 1) and a graphic (Image, RawImage or Text). |
| Glow is a square or has hard edges | Use a shape with transparent margin around it and Sprite Mode = Single. |
| Glow is invisible | Check Brightness and Color alpha. Additive glow needs a dark background. |
| Nothing plays in the build | The glow shader and material sit in `Runtime/Resources/MotionStudio`. Keep that folder. |

---

## 13. Files

```
MotionStudio/
  Runtime/    the components (own assembly MotionStudio.Runtime)
  Runtime/TMP optional TextMeshPro support (compiles only when TMP exists)
  Editor/     inspectors, preview, menus, demo builder
  Documentation/
  Demo/       demo sprites (safe to delete)
```
