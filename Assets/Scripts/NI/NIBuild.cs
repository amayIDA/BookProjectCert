using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Talent Wheel screens into the open scene. In the editor run it with
/// Tools > National Initiative > Build Scene UI. Everything it creates is a normal scene object.
/// </summary>
public static class NIBuild
{
    public static Canvas FindCanvas(string canvasName)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == canvasName)
                return canvases[i];
        }

        return null;
    }

    public static string BuildAll()
    {
        CircularMenu menu = Object.FindAnyObjectByType<CircularMenu>();
        if (menu == null)
            return "No CircularMenu found in the scene.";

        Canvas contentCanvas = FindCanvas("ContentDisplay_Canvas");
        if (contentCanvas == null)
            return "ContentDisplay_Canvas not found.";

        // The default UI panels that tint each display (red, green, white) would cover the new screens, so they are switched off.
        string[] canvasNames = { "TouchScreen_Canvas", "LargeLEDCircle_Canvas", "ContentDisplay_Canvas" };
        for (int c = 0; c < canvasNames.Length; c++)
        {
            Canvas canvas = FindCanvas(canvasNames[c]);
            if (canvas == null)
                continue;

            Transform panelObject = canvas.transform.Find("Panel");
            Image panelImage = panelObject != null ? panelObject.GetComponent<Image>() : null;
            if (panelImage != null && panelImage.sprite != null && panelImage.sprite.name == "Background")
                panelObject.gameObject.SetActive(false);
        }

        TalentWheelFlow flow = menu.GetComponent<TalentWheelFlow>();
        if (flow == null)
            flow = menu.gameObject.AddComponent<TalentWheelFlow>();
        flow.menu = menu;

        menu.niStyle = true;
        menu.externalContent = true;
        menu.niDisc = NIUI.Disc();
        menu.niHalo = NIUI.Halo();

        // the plain label and picture of the original menu are replaced by the new screen
        string[] oldObjects = { "MenuContentImage", "MenuContentLabel", "MenuContentSubtitle" };
        for (int i = 0; i < oldObjects.Length; i++)
        {
            Transform old = contentCanvas.transform.Find(oldObjects[i]);
            if (old != null)
                old.gameObject.SetActive(false);
        }

        flow.content = NIContentScreen.Build(contentCanvas.transform);
        flow.content.transform.SetSiblingIndex(Mathf.Min(1, contentCanvas.transform.childCount - 1));

        NIScenery.BuildTouch(menu, flow);
        NIScenery.BuildLed(menu);

        // home wheel so the buttons are visible in the Scene view before pressing Play
        NILevel home = NIData.Level("home");
        List<CircularMenu.CircleItem> items = new List<CircularMenu.CircleItem>();
        for (int i = 0; i < home.options.Length; i++)
        {
            Sprite icon = NIUI.Art("icon_" + home.options[i].icon);
            items.Add(new CircularMenu.CircleItem { name = home.options[i].title, buttonImage = icon, itemContext = icon });
        }

        menu.SetItems(items, Mathf.Clamp(menu.itemInWindow, 0, items.Count - 1));
        menu.RebuildNow();

        menu.niFace = NIUI.OptFace();
        menu.niFaceSelected = NIUI.OptFaceSel();

        // the disc of the LED wheel, drawn like the wheel of the HTML file
        flow.ledWheel = NILedWheel.Build(menu);
        flow.ledWheel.SetHub(home.hub, home.hubSub);
        menu.startAngle = 90f;
        menu.RebuildNow();

        // the icon of the item in the window sits inside the hub, above the question
        Canvas led = FindCanvas("LargeLEDCircle_Canvas");
        Transform context = led != null ? led.transform.Find("ItemContext") : null;
        if (context != null)
        {
            context.SetAsLastSibling();
            RectTransform contextRect = (RectTransform)context;
            contextRect.anchoredPosition = new Vector2(0f, flow.ledWheel.iconLift);
            Image contextImage = context.GetComponent<Image>();
            contextImage.color = new Color(1f, 1f, 1f, 0.95f);
            if (context.GetComponent<NIBreathe>() == null)
            {
                NIBreathe breathe = context.gameObject.AddComponent<NIBreathe>();
                breathe.scaleTo = 1.05f;
                breathe.period = 5f;
            }
        }

        return "Built NIScreen, touch controls and LED effects.";
    }
}
