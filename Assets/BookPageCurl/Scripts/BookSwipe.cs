using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Swipe left to turn to the next page, swipe right to turn back.
/// </summary>
[RequireComponent(typeof(Image))]
public class BookSwipe : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Book book;
    public Book mirror;
    public BookSwipe linkedSwipe;
    public BookSwipe[] alsoFlip;
    /// <summary>Called with the swipe direction before a swipe turns the page. Return true to handle the turn elsewhere.</summary>
    public System.Func<bool, bool> interceptSwipe;
    /// <summary>When this returns true the book ignores touches (for example while the pages riffle back to the beginning).</summary>
    public System.Func<bool> blocked;
    public int firstPage;
    public AudioClip flipSound;
    public AudioClip flipBackSound;
    public float flipDuration = 0.85f;
    public int animationFrames = 28;

    AudioSource audioSource;
    bool tracking;
    bool flipping;

    public bool IsFlipping
    {
        get { return flipping; }
    }
    bool holdingCorner;
    bool holdNext;
    int pointerId;
    Vector2 startPosition;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (flipping || book == null || (blocked != null && blocked()))
            return;
        tracking = true;
        pointerId = eventData.pointerId;
        startPosition = eventData.position;
        BeginCornerHold(PressedOnRightPage(eventData));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!tracking || eventData.pointerId != pointerId || !holdingCorner || flipping)
            return;

        Vector2 delta = eventData.position - startPosition;
        float towardFlip = holdNext ? -delta.x : delta.x;
        float amount = Mathf.Clamp01(towardFlip / SwipeThreshold());
        float peel = Mathf.Lerp(0.16f, 0.34f, amount);
        ApplyCorner(holdNext, peel);
        ForEachLinked(other => other.ApplyLinkedCorner(holdNext, peel));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!tracking || eventData.pointerId != pointerId)
            return;
        tracking = false;

        Vector2 delta = eventData.position - startPosition;
        float threshold = SwipeThreshold();
        bool swiped = Mathf.Abs(delta.x) >= threshold && Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
        if (swiped)
        {
            EndCornerHold(false);
            if (interceptSwipe != null && interceptSwipe(delta.x < 0f))
                return;
            BeginFlip(delta.x < 0f);
            return;
        }

        EndCornerHold(true);
    }

    bool PressedOnRightPage(PointerEventData eventData)
    {
        var rect = (RectTransform)transform;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local))
            return true;
        return local.x >= 0f;
    }

    void BeginCornerHold(bool next)
    {
        if (!CanFlip(next))
            return;

        holdingCorner = true;
        holdNext = next;
        ApplyCorner(next, 0.16f);
        ForEachLinked(other => other.BeginLinkedCornerHold(next));
    }

    public void BeginLinkedCornerHold(bool next)
    {
        if (!CanFlip(next))
            return;

        holdingCorner = true;
        holdNext = next;
        ApplyCorner(next, 0.16f);
    }

    public void ApplyLinkedCorner(bool next, float amount)
    {
        if (!holdingCorner)
            return;
        ApplyCorner(next, amount);
    }

    void EndCornerHold(bool release)
    {
        if (holdingCorner)
        {
            holdingCorner = false;
            if (release && book != null)
                book.ReleasePage();
        }

        ForEachLinked(other => other.EndLinkedCornerHold(release));
    }

    public void EndLinkedCornerHold(bool release)
    {
        if (!holdingCorner)
            return;

        holdingCorner = false;
        if (release && book != null)
            book.ReleasePage();
    }

    void ApplyCorner(bool next, float amount)
    {
        if (book == null)
            return;

        Vector3 corner = next ? book.EndBottomRight : book.EndBottomLeft;
        if (Mathf.Abs(corner.x) < 1f)
            return;

        float pageWidth = Mathf.Abs(book.EndBottomRight.x);
        float liftX = pageWidth * Mathf.Lerp(0.14f, 0.32f, amount);
        float liftY = book.Height * Mathf.Lerp(0.1f, 0.22f, amount);
        Vector3 point = next
            ? new Vector3(corner.x - liftX, corner.y + liftY, 0f)
            : new Vector3(corner.x + liftX, corner.y + liftY, 0f);

        if (next)
            book.DragRightPageToPoint(point);
        else
            book.DragLeftPageToPoint(point);
    }

    /// <summary>Stops a turn that is in progress. The book must be snapped to a page afterwards.</summary>
    public void CancelFlip()
    {
        StopAllCoroutines();
        flipping = false;
        tracking = false;
        holdingCorner = false;
    }

    public void BeginFlip(bool next)
    {
        if (!CanFlip(next))
            return;

        PlayFlipSound(next);
        StartCoroutine(FlipRoutine(next));
        ForEachLinked(other => other.BeginLinkedFlip(next));
    }

    void ForEachLinked(System.Action<BookSwipe> action)
    {
        if (linkedSwipe != null)
            action(linkedSwipe);
        if (alsoFlip == null)
            return;
        for (int i = 0; i < alsoFlip.Length; i++)
        {
            BookSwipe other = alsoFlip[i];
            if (other != null && other != linkedSwipe)
                action(other);
        }
    }

    void PlayFlipSound(bool next)
    {
        AudioClip clip = next ? flipSound : flipBackSound;
        if (clip == null || audioSource == null)
            return;
        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();
        audioSource.PlayOneShot(clip);
    }

    public void BeginLinkedFlip(bool next)
    {
        if (!CanFlip(next))
            return;

        StartCoroutine(FlipRoutine(next));
    }

    bool CanFlip(bool next)
    {
        if (flipping || book == null || book.bookPages == null)
            return false;
        if (next)
            return book.currentPage + 2 < book.TotalPageCount;
        return book.currentPage > firstPage;
    }

    float SwipeThreshold()
    {
        var rect = (RectTransform)transform;
        return Mathf.Clamp(rect.rect.width * 0.12f, 30f, 120f);
    }

    IEnumerator FlipRoutine(bool next)
    {
        if (book == null)
            yield break;

        float halfSpan = (book.EndBottomRight.x - book.EndBottomLeft.x) * 0.5f * 0.9f;
        if (halfSpan < 1f)
            yield break;

        flipping = true;
        book.followPointer = false;
        if (mirror != null)
            mirror.followPointer = false;

        float centerX = (book.EndBottomRight.x + book.EndBottomLeft.x) * 0.5f;
        float height = Mathf.Abs(book.EndBottomRight.y) * 0.9f;
        float startX = next ? centerX + halfSpan : centerX - halfSpan;
        float direction = next ? -1f : 1f;

        Vector3 point = CurlPoint(startX, centerX, halfSpan, height);
        if (next)
        {
            book.DragRightPageToPoint(point);
            if (mirror != null)
                mirror.DragRightPageToPoint(point);
        }
        else
        {
            book.DragLeftPageToPoint(point);
            if (mirror != null)
                mirror.DragLeftPageToPoint(point);
        }

        // The sheet is moved by time, not by a fixed number of steps, so it is smooth on any frame rate.
        // It peels off slowly, speeds up across the middle and settles gently on the other side.
        float elapsed = 0f;
        float duration = Mathf.Max(0.2f, flipDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
            float x = startX + direction * eased * halfSpan * 2f;
            point = CurlPoint(x, centerX, halfSpan, height);
            if (next)
            {
                book.UpdateBookRTLToPoint(point);
                if (mirror != null)
                    mirror.UpdateBookRTLToPoint(point);
            }
            else
            {
                book.UpdateBookLTRToPoint(point);
                if (mirror != null)
                    mirror.UpdateBookLTRToPoint(point);
            }

            yield return null;
        }

        book.ReleasePage();
        if (mirror != null)
            mirror.ReleasePage();
        yield return new WaitForSeconds(0.3f);
        flipping = false;
    }

    static Vector3 CurlPoint(float x, float centerX, float halfSpan, float height)
    {
        float y = (-height / (halfSpan * halfSpan)) * (x - centerX) * (x - centerX);
        return new Vector3(x, y, 0f);
    }
}
