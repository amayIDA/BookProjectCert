# Book Animation Kit

Everything that makes the InXNext book kiosk feel the way it does, as a Unity package:

* **Motion Studio** (`Assets/MotionStudio`): the animation components (words, lines, glow, icons, cards).
* **Screen Kit** (`Assets/BookPageCurl/Editor/ScreenKit`): build a new animated screen from a short script.
* **Book page curl** (`Assets/BookPageCurl/Scripts`): the page turn, the spread, the Home riffle.
* **Animated icons** (`AnimatedIcon.cs`, `Shaders/VideoIcon.shader`): transparent video icons that play once or loop and drift.
* **Looping video backgrounds** (`LoopVideo.cs`).
* **ANIMATION_GUIDELINES.md**: the rules. Give this file to an AI (or a new teammate) and it knows how to animate in this style.

## Requirements

* Unity 6 (6000.3 or newer). Other 6000.x versions should work.
* Packages: Unity UI (`com.unity.ugui`, includes TextMeshPro) and Input System (`com.unity.inputsystem`). Both come with a new Unity 6 project; switch the Active Input Handling to *Both* or *Input System Package*.
* Video: the `Video` built-in module (on by default).
* Render pipeline: works with URP and the built-in pipeline (the UI shader is a plain UI shader).

## Two packages

| Package | What it is | Use it when |
|---|---|---|
| `BookAnimationKit_Core.unitypackage` | Code, shaders, fonts, prefabs, icons, backgrounds, docs. No scene. | You want the animation system inside your own project. |
| `BookAnimationKit_FullSample.unitypackage` | Core plus the finished scene (`Assets/Scenes/BookSplitScene.unity`) and every page, photo and image. | You want the whole kiosk exactly as it is, to start a new project from. |

## Quick start

1. Import the package (Assets > Import Package > Custom Package).
2. Full sample: open `Assets/Scenes/BookSplitScene.unity`, set Display 1 and Display 2 (see below) and press Play.
3. Core: read `ANIMATION_GUIDELINES.md`, then open `Assets/BookPageCurl/Editor/ScreenKit/SCREEN_KIT.md` and copy `MainPage4.cs` to make your first screen.

## Displays

Display 1 shows the spread (left and right pages together, camera `SpreadCamera`). Display 2 shows the main page (camera `SingleCamera`). In the Game view choose Display 1 / Display 2; in a build both displays are activated by the `DisplaySetup` object. Both are designed for 1024x768 and scale with the screen.

## Menu items (Book menu)

* `Book/Screens/*`: build the main pages, the Begin Here lists, scenario details, question pages, closing pages.
* `Book/Icons/Apply Animated Icons`: swap picture icons for the animated video icons.
* `Book/Setup/*`: spread and video background setup.

## Replacing the content

* Question and programme text: the table at the top of `MainQuestionPages.cs`, then `Book/Screens/Build Question Pages`.
* Photos: `Assets/UI/QuestionBackground` (named `Asset 114` to `128`, in question order).
* Icons: replace the WebM files in `Assets/UI/Animated Icons/Web` (full size and `_small` copy, with alpha) and re-run `Apply Animated Icons`.
* Backgrounds: `Assets/UI/Back_Book_Loop.mp4` (spread) and `Back_Front_Screen_Loop.mp4` (main).
