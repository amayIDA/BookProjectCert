using UnityEngine;

/// <summary>
/// Sits on a button in a scene page panel. Edit the image and the label on this object.
/// </summary>
public class BookControl : MonoBehaviour
{
    public BookScreens.ButtonAction action = BookScreens.ButtonAction.OpenPage;
    [Tooltip("Page this button opens. The first page is 0.")]
    public int page;

    public void Click()
    {
        BookScreens screens = FindAnyObjectByType<BookScreens>();
        if (screens == null)
            return;
        screens.PressButton(action, page, gameObject.name);
    }
}
