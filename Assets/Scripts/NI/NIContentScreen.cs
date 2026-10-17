using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The central screen of the Talent Wheel on the content display: hologram, headline, body views,
/// pipeline tracker and toast. Everything is a normal scene object created by Tools > National Initiative > Build Scene UI,
/// so sprites, colours, sizes and texts can be changed in the Inspector. This component only switches views and fills in text.
/// </summary>
public partial class NIContentScreen : MonoBehaviour
{
    [Header("Hologram and headline")]
    public NIParticleField field;
    public NIHeadline headline;
    public Text eyebrow;
    public RectTransform panel;
    public RectTransform bodyRoot;

    [Header("Views (only one is visible at a time)")]
    public NIView viewIdle;
    public NIView viewWho;
    public NIView viewLevel;
    public NIView viewApproach;
    public NIView viewReveal;
    public NIView viewProgramme;
    public NIView viewConsult;

    [Header("Level preview")]
    public Text levelTitle;
    public Text levelHint;

    [Header("Reveal")]
    public Text revealTitle;
    public Text revealHint;
    public Text revealHelp;
    public NIListBinder revealChips;
    public NIListBinder revealTicks;
    public NICardFlip[] cards;

    [Header("Programme")]
    public Text progLine;
    public Text progBenefitsLabel;
    public NIListBinder progRows;
    public NIListBinder progTicks;

    [Header("Consultation")]
    public RawImage qrBook;
    public RawImage qrAcademy;
    public Text codeText;
    public Text handoffText;
    public Text exploredText;

    [Header("Pipeline tracker (order: Work, Role, Sector, Development, Leader)")]
    public RectTransform pipeRoot;
    public Text pipeCount;
    public RectTransform[] pipeHolders;
    public Image[] pipeFills;
    public Image[] pipeRings;
    public Image[] pipeGlows;
    public Text[] pipeLabels;

    [Header("Toast")]
    public RectTransform toastRoot;
    public Text toastTitle;
    public Text toastSub;
    public float toastHold = 3.2f;

    NIView current;
    NIView wanted;
    Tween toastTween;
    readonly List<Tween> timers = new List<Tween>();

    public NIParticleField Field { get { return field; } }

    static readonly Color Off = new Color(0.008f, 0.016f, 0.039f, 1f);
    static readonly Color RingOff = new Color(180f / 255f, 198f / 255f, 238f / 255f, 0.45f);

    // ---------- view switching ----------

    void KillTimers()
    {
        for (int i = 0; i < timers.Count; i++)
        {
            if (timers[i] != null)
                timers[i].Kill();
        }

        timers.Clear();
    }

    NIView[] AllViews()
    {
        RepairViews();
        return new[] { viewIdle, viewWho, viewLevel, viewApproach, viewReveal, viewProgramme, viewConsult };
    }

    NIView FindView(string childName)
    {
        if (bodyRoot == null)
            return null;
        Transform child = bodyRoot.Find(childName);
        return child != null ? child.GetComponent<NIView>() : null;
    }

    /// <summary>Re-links views that were deleted and recreated or lost their reference, by looking for the View_ objects by name.</summary>
    public void RepairViews()
    {
        if (viewIdle == null) viewIdle = FindView("View_Idle");
        if (viewWho == null) viewWho = FindView("View_Who");
        if (viewLevel == null) viewLevel = FindView("View_Level");
        if (viewApproach == null) viewApproach = FindView("View_Approach");
        if (viewReveal == null) viewReveal = FindView("View_Reveal");
        if (viewProgramme == null) viewProgramme = FindView("View_Programme");
        if (viewConsult == null) viewConsult = FindView("View_Consult");
        if (current == null) current = null;
    }

    void SwitchTo(NIView next)
    {
        RepairViews();
        wanted = next;
        if (next == null)
        {
            Debug.LogWarning("National Initiative: a view of the content screen is missing. Run Tools > National Initiative > Build Scene UI to recreate it.");
            return;
        }

        if (current == null)
            current = null;

        if (current == next)
        {
            next.Show();
            return;
        }

        NIView[] all = AllViews();
        NIView leaving = current;
        current = next;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i] != next && all[i] != leaving && all[i].gameObject.activeSelf)
                all[i].gameObject.SetActive(false);
        }

        if (leaving == null || !leaving.gameObject.activeSelf)
        {
            next.Show();
            return;
        }

        leaving.Hide(() =>
        {
            if (wanted == next)
                next.Show();
        });
    }

    /// <summary>Shows a view while editing, used by the context menu entries.</summary>
    public void PreviewView(NIView view)
    {
        NIView[] all = AllViews();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].gameObject.SetActive(all[i] == view);
        }

        current = view;
        wanted = view;
        if (view != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyRoot);
    }

    [ContextMenu("Preview/Idle")] void PreviewIdle() { PreviewView(viewIdle); }
    [ContextMenu("Preview/Who we are")] void PreviewWho() { PreviewView(viewWho); }
    [ContextMenu("Preview/Level preview")] void PreviewLevel() { PreviewView(viewLevel); }
    [ContextMenu("Preview/Our approach")] void PreviewApproach() { PreviewView(viewApproach); }
    [ContextMenu("Preview/Reveal cards")] void PreviewReveal() { PreviewView(viewReveal); }
    [ContextMenu("Preview/Programme")] void PreviewProgramme() { PreviewView(viewProgramme); }
    [ContextMenu("Preview/Consultation")] void PreviewConsult() { PreviewView(viewConsult); }

    // ---------- headline ----------

    public void SetHeadline(string eyebrowText, string markup)
    {
        bool has = !string.IsNullOrEmpty(eyebrowText);
        eyebrow.gameObject.SetActive(has);
        eyebrow.text = has ? eyebrowText.ToUpperInvariant() : string.Empty;
        headline.Show(markup, 0.2f);
    }

    void SwapText(Text label, string value)
    {
        if (label == null)
            return;

        if (!Application.isPlaying || !label.gameObject.activeInHierarchy || label.text == value)
        {
            label.text = value;
            return;
        }

        label.DOKill();
        label.DOFade(0f, 0.1f).SetUpdate(true).SetLink(label.gameObject).OnComplete(() =>
        {
            label.text = value;
            label.DOFade(1f, 0.28f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(label.gameObject);
        });
    }

    // ---------- views ----------

    public void ShowIdle()
    {
        KillTimers();
        SetHeadline(null, "National ambitions. <b>Delivered by its people.</b>");
        SwitchTo(viewIdle);
    }

    public void ShowWho()
    {
        KillTimers();
        SetHeadline(null, "Who we are and <b>what we build.</b>");
        SwitchTo(viewWho);
    }

    public void ShowLevelHeadline(string level)
    {
        string text;
        switch (level)
        {
            case "gov": text = "What’s your <b>national priority?</b>"; break;
            case "priv": text = "What’s your <b>biggest challenge?</b>"; break;
            case "explore": text = "Explore our <b>programmes.</b>"; break;
            default: text = "Where do <b>you work?</b>"; break;
        }

        SetHeadline(null, text);
    }

    public void ShowPreview(string title, string hint)
    {
        KillTimers();
        RepairViews();
        if (viewLevel == null || levelTitle == null || levelHint == null)
            return;

        if (current == viewLevel && viewLevel.gameObject.activeSelf)
        {
            SwapText(levelTitle, title);
            SwapText(levelHint, hint);
            return;
        }

        levelTitle.text = title;
        levelHint.text = hint;
        SwitchTo(viewLevel);
    }

    public void ShowApproach()
    {
        KillTimers();
        SetHeadline(null, "Credentials <b>you can trust.</b>");
        SwitchTo(viewApproach);
    }

    public void ShowReveal(string crumb, NIEntry entry)
    {
        KillTimers();
        SetHeadline(crumb, "<b>" + entry.title + "</b>");
        revealTitle.text = entry.title;
        revealHint.text = entry.hint;
        revealHelp.text = entry.help;
        revealChips.Bind(entry.offer);
        revealTicks.Bind(entry.get);
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
                cards[i].Close();
        }

        SwitchTo(viewReveal);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewReveal.transform as RectTransform);
        for (int n = 0; n < cards.Length; n++)
        {
            int index = n;
            timers.Add(DOVirtual.DelayedCall(0.75f + n * 0.9f, () => OpenCard(index)).SetUpdate(true));
        }
    }

    void OpenCard(int n)
    {
        if (n < 0 || n >= cards.Length || cards[n] == null)
            return;

        cards[n].Open();
        NIAudio audio = NIAudio.Instance;
        if (audio != null)
        {
            audio.Play("flip" + n, 0.8f);
            audio.PlayVaried("asmr_tick", 0.9f + n * 0.1f, 1.0f + n * 0.12f, 1f);
        }

        if (field != null)
            field.AddRing(n == 1 ? new Color(238f / 255f, 213f / 255f, 153f / 255f) : new Color(180f / 255f, 198f / 255f, 238f / 255f), 8f + n * 2f);
    }

    public void ShowProgramme(NIProgramme p)
    {
        KillTimers();
        SetHeadline("Programme", "<b>" + p.title + "</b>");
        progLine.text = p.line;
        progRows.Bind(p.list);
        progBenefitsLabel.text = (p.id == "sector" ? "Sectors we cover" : "Benefits").ToUpperInvariant();
        progTicks.Bind(p.get);
        SwitchTo(viewProgramme);
    }

    public void ShowConsult(string code, string[] exploredTitles, string mailUrl, string handoffMessage)
    {
        KillTimers();
        SetHeadline(null, "Let’s build your <b>talent pipeline.</b>");
        qrBook.texture = QrTexture(mailUrl);
        qrAcademy.texture = QrTexture(NIData.ContactUrl);
        codeText.text = string.IsNullOrEmpty(code) ? "—" : code;
        handoffText.text = handoffMessage;
        bool has = exploredTitles != null && exploredTitles.Length > 0;
        exploredText.gameObject.SetActive(has);
        exploredText.text = has ? "You explored: " + string.Join(" · ", exploredTitles) : string.Empty;
        SwitchTo(viewConsult);
    }

    public void SetHandoffMessage(string message)
    {
        if (handoffText != null)
            handoffText.text = message;
    }

    public static Texture2D QrTexture(string content)
    {
        bool[,] modules;
        try
        {
            modules = QRCodeEncoder.Encode(content);
        }
        catch (Exception)
        {
            modules = QRCodeEncoder.Encode(NIData.ContactUrl);
        }

        int n = modules.GetLength(0);
        int quiet = 2;
        int size = n + quiet * 2;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.DontSave;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color dark = NIUI.Navy;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                if (modules[x, y])
                    pixels[(size - 1 - (y + quiet)) * size + (x + quiet)] = dark;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // ---------- pipeline tracker ----------

    public void SetPipeVisible(bool visible)
    {
        if (pipeRoot == null)
            return;

        if (!Application.isPlaying)
        {
            pipeRoot.gameObject.SetActive(visible);
            return;
        }

        if (visible == pipeRoot.gameObject.activeSelf)
            return;

        CanvasGroup group = pipeRoot.GetComponent<CanvasGroup>();
        if (group == null)
            group = pipeRoot.gameObject.AddComponent<CanvasGroup>();
        group.DOKill();
        if (visible)
        {
            pipeRoot.gameObject.SetActive(true);
            group.alpha = 0f;
            group.DOFade(1f, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(pipeRoot.gameObject);
        }
        else
        {
            group.DOFade(0f, 0.25f).SetEase(Ease.InCubic).SetUpdate(true).SetLink(pipeRoot.gameObject).OnComplete(() => pipeRoot.gameObject.SetActive(false));
        }
    }

    public void SetPipe(HashSet<string> set, IEnumerable<string> pop)
    {
        HashSet<string> popped = new HashSet<string>();
        if (pop != null)
        {
            foreach (string key in pop)
                popped.Add(key);
        }

        for (int i = 0; i < NIData.StageList.Length && i < pipeFills.Length; i++)
        {
            string id = NIData.StageList[i][0];
            bool on = set.Contains(id);
            Color fill = on ? NIUI.Gold : Off;
            Color ring = on ? NIUI.Gold : RingOff;
            Color glow = new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, on ? 0.32f : 0f);
            Color label = on ? Color.white : NIUI.Faint;

            if (Application.isPlaying)
            {
                pipeFills[i].DOColor(fill, 0.45f).SetUpdate(true).SetLink(pipeFills[i].gameObject);
                pipeRings[i].DOColor(ring, 0.45f).SetUpdate(true).SetLink(pipeRings[i].gameObject);
                pipeGlows[i].DOColor(glow, 0.7f).SetUpdate(true).SetLink(pipeGlows[i].gameObject);
                pipeLabels[i].DOColor(label, 0.45f).SetUpdate(true).SetLink(pipeLabels[i].gameObject);
                if (popped.Contains(id))
                {
                    pipeHolders[i].DOComplete();
                    pipeHolders[i].DOPunchScale(Vector3.one * 0.75f, 0.8f, 5, 0.45f).SetUpdate(true).SetLink(pipeHolders[i].gameObject);
                }
            }
            else
            {
                pipeFills[i].color = fill;
                pipeRings[i].color = ring;
                pipeGlows[i].color = glow;
                pipeLabels[i].color = label;
            }
        }

        pipeCount.text = set.Count + " of 5";
    }

    // ---------- toast ----------

    public void Toast(string title, string sub)
    {
        toastTitle.text = title;
        toastSub.text = sub ?? string.Empty;
        toastSub.gameObject.SetActive(!string.IsNullOrEmpty(sub));
        LayoutRebuilder.ForceRebuildLayoutImmediate(toastRoot);

        if (!Application.isPlaying)
            return;

        float height = Mathf.Max(NIUI.U(56f), toastRoot.rect.height);
        float hidden = height * 1.6f + NIUI.U(14f);
        float shown = -NIUI.U(14f);
        if (toastTween != null)
            toastTween.Kill();
        toastRoot.anchoredPosition = new Vector2(0f, hidden);
        Sequence s = DOTween.Sequence().SetUpdate(true).SetLink(toastRoot.gameObject);
        s.Append(toastRoot.DOAnchorPosY(shown, 0.7f).SetEase(Ease.OutBack, 1.4f));
        s.AppendInterval(toastHold);
        s.Append(toastRoot.DOAnchorPosY(hidden, 0.45f).SetEase(Ease.InBack, 1.1f));
        toastTween = s;

        if (NIAudio.Instance != null)
            NIAudio.Instance.PlayVaried("asmr_drop", 0.98f, 1.04f, 1f);
    }

    void Awake()
    {
        if (!Application.isPlaying)
            return;

        // start from a clean slate no matter which view was left open in the editor
        NIView[] all = AllViews();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].gameObject.SetActive(false);
        }

        if (toastRoot != null)
            toastRoot.anchoredPosition = new Vector2(0f, NIUI.U(200f));
    }
}
