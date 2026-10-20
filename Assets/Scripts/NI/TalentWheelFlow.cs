using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runs the Talent Wheel from National_Initiatives_Talent_Wheel.html on the existing three display setup.
/// Display 1 is the touch dial, display 2 the large LED wheel, display 3 the content screen.
/// The screens themselves are scene objects built by Tools > National Initiative > Build Scene UI,
/// so everything can be restyled in the editor. This component only runs the logic.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class TalentWheelFlow : MonoBehaviour
{
    [Header("Scene objects")]
    public CircularMenu menu;
    public NIContentScreen content;

    [Header("Touch screen")]
    public Text crumbText;
    public Text hubText;
    public Button startAgain;
    public Button bookPill;
    public Button expertPill;
    public Button finishPill;
    public Button soundButton;
    public Text expertLabel;
    public Text soundLabel;
    public RectTransform rippleRoot;
    public RectTransform shockRoot;

    [Header("LED wheel")]
    public NILedWheel ledWheel;

    [Header("Timing")]
    [Tooltip("Seconds without a touch before the experience returns to the idle screen.")]
    public float idleTimeout = 60f;
    public float consultTimeout = 90f;

    string level = "home";
    string view = "idle";
    string code;
    string handoffStatus;
    bool handed;
    float lastActivity;
    bool ready;

    readonly HashSet<string> pipe = new HashSet<string>();
    readonly HashSet<string> seen = new HashSet<string>();
    readonly HashSet<string> sectors = new HashSet<string>();
    readonly List<string> explored = new List<string>();
    readonly List<Tween> timers = new List<Tween>();
    readonly Dictionary<Button, bool> pillState = new Dictionary<Button, bool>();

    NIAudio Audio { get { return NIAudio.Instance; } }

    /// <summary>Adds the flow to the scene's CircularMenu when nobody added it by hand.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        CircularMenu found = FindAnyObjectByType<CircularMenu>();
        if (found != null && found.GetComponent<TalentWheelFlow>() == null)
            found.gameObject.AddComponent<TalentWheelFlow>();
    }

    void Awake()
    {
        if (menu == null)
            menu = GetComponent<CircularMenu>();
        if (menu == null)
            menu = FindAnyObjectByType<CircularMenu>();
        if (menu == null)
        {
            enabled = false;
            return;
        }

        homeFocus = menu.itemInWindow;
        menu.niStyle = true;
        menu.externalContent = true;
        menu.selectHandler = OnSelect;
        menu.discoverHandler = OnDiscover;
        menu.backHandler = OnBack;
        menu.windowChanged = OnWindowChanged;
        menu.activityHandler = Activity;
        NIAudio.Ensure();
        NIPressFeedback.AnyPress += OnAnyPress;
    }

    void OnDestroy()
    {
        NIPressFeedback.AnyPress -= OnAnyPress;
    }

    static Canvas FindCanvas(string canvasName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == canvasName)
                return canvases[i];
        }

        return null;
    }

    IEnumerator Start()
    {
        // let CircularMenu create its canvases and touch dial first
        yield return null;
        yield return null;

        // fall back to building the screens when Build Scene UI was never run
        if (content == null)
        {
            Canvas canvas = FindCanvas("ContentDisplay_Canvas");
            if (canvas != null)
                content = NIContentScreen.Build(canvas.transform);
        }

        if (crumbText == null)
            NIScenery.BuildTouch(menu, this);

        WireButtons();
        lastActivity = Time.unscaledTime;
        ready = true;
        SetWheel("home", HomeFocus());
        ShowView("idle");
    }

    void WireButtons()
    {
        Hook(startAgain, StartAgain);
        Hook(bookPill, BookConsultation);
        Hook(expertPill, Handoff);
        Hook(finishPill, FinishSession);
        Hook(soundButton, ToggleSound);
    }

    static void Hook(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void Update()
    {
        if (!ready)
            return;

        if (view != "idle" && Time.unscaledTime - lastActivity > (view == "consult" ? consultTimeout : idleTimeout))
            ResetExperience();
    }

    // ---------- wheel ----------

    // The item that sits in the touch window at the start is the Item In Window value of the CircularMenu.
    int homeFocus;

    int HomeFocus()
    {
        return Mathf.Clamp(homeFocus, 0, NIData.Level("home").options.Length - 1);
    }

    void SetWheel(string levelId, int focus)
    {
        level = levelId;
        NILevel data = NIData.Level(levelId);
        if (ledWheel == null && menu != null)
            ledWheel = menu.GetComponent<NILedWheel>();
        if (ledWheel != null)
            ledWheel.SetHub(data.hub, data.hubSub);
        List<CircularMenu.CircleItem> list = new List<CircularMenu.CircleItem>();
        for (int i = 0; i < data.options.Length; i++)
        {
            Sprite icon = NIUI.Art("icon_" + data.options[i].icon);
            list.Add(new CircularMenu.CircleItem { name = data.options[i].title, buttonImage = icon, itemContext = icon });
        }

        menu.SetItems(list, focus);
        RefreshTouchUi();
    }

    int Window()
    {
        int w = menu.WindowItem;
        return w < 0 ? 0 : w;
    }

    // ---------- touch screen text and buttons ----------

    void RefreshTouchUi()
    {
        if (crumbText == null)
            return;

        NILevel data = NIData.Level(level);
        List<string> chain = new List<string>();
        string key = level;
        while (key != null)
        {
            NILevel l = NIData.Level(key);
            chain.Insert(0, l.crumb);
            key = l.parent;
        }

        SwapText(crumbText, string.Join("   ›   ", chain.ToArray()).ToUpperInvariant());
        SwapText(hubText, data.hub);

        bool idle = view == "idle";
        SetPill(startAgain, !idle && level != "home");
        SetPill(bookPill, !idle && view != "consult");
        SetPill(expertPill, view == "consult");
        SetPill(finishPill, view == "consult");
        if (expertLabel != null)
            expertLabel.text = handed ? "With our expert" : "Continue with our expert";
        if (soundLabel != null)
            soundLabel.text = Audio != null && Audio.IsOn ? "Sound on" : "Sound off";
    }

    void SwapText(Text label, string value)
    {
        if (label == null)
            return;

        if (!Application.isPlaying || label.text == value || !label.gameObject.activeInHierarchy)
        {
            label.text = value;
            return;
        }

        label.DOKill();
        label.DOFade(0f, 0.12f).SetUpdate(true).SetLink(label.gameObject).OnComplete(() =>
        {
            label.text = value;
            label.DOFade(1f, 0.35f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(label.gameObject);
        });
    }

    void SetPill(Button pill, bool show)
    {
        if (pill == null)
            return;

        bool known;
        bool seenBefore = pillState.TryGetValue(pill, out known);
        bool previous = seenBefore && known;
        pillState[pill] = show;
        if (!seenBefore && !show)
        {
            pill.gameObject.SetActive(false);
            return;
        }

        if (show == previous && pill.gameObject.activeSelf == show)
            return;

        CanvasGroup group = pill.GetComponent<CanvasGroup>();
        if (group == null)
            group = pill.gameObject.AddComponent<CanvasGroup>();

        pill.transform.DOKill();
        group.DOKill();
        if (show)
        {
            pill.gameObject.SetActive(true);
            group.alpha = 0f;
            pill.transform.localScale = Vector3.one * 0.8f;
            group.DOFade(1f, 0.4f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(pill.gameObject);
            pill.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack, 1.8f).SetUpdate(true).SetLink(pill.gameObject);
        }
        else if (pill.gameObject.activeSelf)
        {
            group.DOFade(0f, 0.18f).SetUpdate(true).SetLink(pill.gameObject).OnComplete(() =>
            {
                if (!pillState.ContainsKey(pill) || !pillState[pill])
                    pill.gameObject.SetActive(false);
            });
        }
    }

    void ToggleSound()
    {
        if (Audio == null)
            return;

        Audio.SetOn(!Audio.IsOn);
        RefreshTouchUi();
    }

    // ---------- touch feedback ----------

    void OnAnyPress(PointerEventData eventData)
    {
        if (rippleRoot == null || eventData == null)
            return;

        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rippleRoot, eventData.position, cam, out local))
            return;
        if (!rippleRoot.rect.Contains(local))
            return;

        Ripple(local);
    }

    /// <summary>Soft rings that spread from the touch point.</summary>
    void Ripple(Vector2 local)
    {
        for (int i = 0; i < 2; i++)
        {
            Image ring = NIUI.Img(rippleRoot, "Ripple", new Color(1f, 1f, 1f, i == 0 ? 0.55f : 0.3f), NIUI.Ring());
            NIUI.Center(ring.rectTransform, local, new Vector2(120f, 120f));
            ring.rectTransform.localScale = Vector3.one * 0.35f;
            float delay = i * 0.12f;
            Sequence s = DOTween.Sequence().SetUpdate(true).SetLink(ring.gameObject);
            s.Insert(delay, ring.rectTransform.DOScale(i == 0 ? 3.2f : 4.6f, 1.1f).SetEase(Ease.OutCubic));
            s.Insert(delay, ring.DOFade(0f, 1.1f).SetEase(Ease.OutQuad));
            s.OnComplete(() => Destroy(ring.gameObject));
        }
    }

    void Shock()
    {
        if (shockRoot == null || !Application.isPlaying)
            return;

        Image ring = NIUI.Img(shockRoot, "ShockRing", NIUI.Gold, NIUI.Ring());
        NIUI.Center(ring.rectTransform, Vector2.zero, new Vector2(170f, 170f));
        ring.rectTransform.localScale = Vector3.one * 0.5f;
        Sequence s = DOTween.Sequence().SetUpdate(true).SetLink(ring.gameObject);
        s.Insert(0f, ring.rectTransform.DOScale(5.5f, 1.3f).SetEase(Ease.OutExpo));
        s.Insert(0f, ring.DOFade(0f, 1.3f).SetEase(Ease.OutQuad));
        s.OnComplete(() => Destroy(ring.gameObject));
    }

    // ---------- timers and audio ----------

    void Later(float seconds, TweenCallback action)
    {
        timers.Add(DOVirtual.DelayedCall(seconds, action).SetUpdate(true));
    }

    void ClearTimers()
    {
        for (int i = 0; i < timers.Count; i++)
        {
            if (timers[i] != null)
                timers[i].Kill();
        }

        timers.Clear();
    }

    void Play(string clip, float volume = 1f)
    {
        if (Audio != null)
            Audio.Play(clip, volume);
    }

    void Soft(string clip, float pitchMin = 0.97f, float pitchMax = 1.04f, float volume = 1f)
    {
        if (Audio != null)
            Audio.PlayVaried(clip, pitchMin, pitchMax, volume);
    }

    void Activity()
    {
        lastActivity = Time.unscaledTime;
    }

    // ---------- input from the menu ----------

    void OnDiscover(int index)
    {
        Activity();
        if (view == "idle")
        {
            Wake();
            return;
        }

        Soft("asmr_tap");
        OpenOption(index);
    }

    void OnSelect(int index)
    {
        Activity();
        if (view == "idle")
        {
            Wake();
            return;
        }

        if (index == Window())
        {
            Soft("asmr_tap");
            OpenOption(index);
        }
        else
            menu.RotateToItem(index, () => { Soft("asmr_tap"); OpenOption(index); });
    }

    void OnBack()
    {
        Activity();
        if (view == "idle")
        {
            Wake();
            return;
        }

        Play("ui", 0.8f);
        Soft("asmr_swish", 0.9f, 1f, 0.7f);
        if (level == "home")
        {
            ShowView("who");
            return;
        }

        string parent = NIData.Level(level).parent;
        GoLevel(parent);
        if (parent == "home")
            ShowView("who");
    }

    void OnWindowChanged(int index)
    {
        if (!ready)
            return;

        // a small rising glass tick for every item that passes the notch
        float pitch = 0.92f + (index % 6) * 0.045f;
        Soft("asmr_tick", pitch, pitch + 0.02f, 0.9f);
        if (view == "level")
            Preview();
    }

    // ---------- state changes ----------

    void EnsureSession()
    {
        if (!string.IsNullOrEmpty(code))
            return;

        code = "NI-" + UnityEngine.Random.Range(1000, 10000);
    }

    void Wake()
    {
        EnsureSession();
        Play("place", 0.75f);
        Soft("asmr_settle");
        SetWheel("home", HomeFocus());
        Shock();
        ShowView("who");
        if (content != null)
        {
            content.Field.Burst(10f);
            content.Field.Flash(1f);
        }
    }

    void GoLevel(string next)
    {
        ClearTimers();
        SetWheel(next, 0);
        Shock();
        if (next == "gov" || next == "priv" || next == "explore")
            sectors.Add(NIData.Level(next).crumb);
        ShowView("level");
    }

    void OpenOption(int index)
    {
        EnsureSession();
        Activity();
        NILevel data = NIData.Level(level);
        if (index < 0 || index >= data.options.Length)
            return;

        NIOption option = data.options[index];
        seen.Add(level + ":" + option.id);

        if (level == "home")
        {
            if (option.id == "sector")
            {
                Play("place", 0.75f);
                Soft("asmr_swish");
                GoLevel("sector");
            }
            else
            {
                Play("connect", 0.8f);
                Soft("asmr_rise");
                ShowView(option.id);
            }
        }
        else if (level == "sector")
        {
            Play("place", 0.75f);
            Soft("asmr_swish");
            GoLevel(option.id);
        }
        else if (level == "gov" || level == "priv")
        {
            Play("reveal", 0.8f);
            Soft("asmr_chime");
            Reveal(level, option.dataIndex);
        }
        else if (level == "explore")
        {
            Play("reveal", 0.8f);
            Soft("asmr_chime");
            Programme(option.dataIndex);
        }
    }

    void ShowView(string next)
    {
        view = next;
        ClearTimers();
        RefreshTouchUi();
        if (!EnsureContent())
            return;

        content.SetPipeVisible(view != "idle");
        content.SetPipe(pipe, null);

        NIParticleField field = content.Field;
        if (view == "idle")
        {
            field.Formation("words");
            if (Audio != null)
                Audio.SetLevel(0.2f);
            content.ShowIdle();
            return;
        }

        if (Audio != null)
            Audio.SetLevel(1f);

        switch (view)
        {
            case "who":
                field.Formation("sphere", null, 0.25f);
                content.ShowWho();
                break;
            case "level":
                field.Formation("icon", level == "home" ? "sector" : level);
                content.ShowLevelHeadline(level);
                Preview();
                break;
            case "approach":
                field.Formation("icon", "approach");
                content.ShowApproach();
                break;
            case "consult":
                ConsultView();
                break;
        }
    }

    bool EnsureContent()
    {
        if (content == null)
            content = FindAnyObjectByType<NIContentScreen>();
        return content != null;
    }

    void Preview()
    {
        if (view != "level" || !EnsureContent())
            return;

        NILevel data = NIData.Level(level);
        int index = Mathf.Clamp(Window(), 0, data.options.Length - 1);
        NIOption option = data.options[index];
        string hint = string.Empty;
        if (level == "sector")
        {
            if (option.id == "gov") hint = "National priorities and Emiratisation programmes.";
            else if (option.id == "priv") hint = "Hiring, upskilling and leadership for your business.";
            else hint = "Browse all five programmes.";
        }
        else if (level == "gov")
            hint = NIData.Gov[option.dataIndex].hint;
        else if (level == "priv")
            hint = NIData.Priv[option.dataIndex].hint;
        else if (level == "explore")
            hint = NIData.Programmes[option.dataIndex].line;

        content.ShowPreview(option.title, hint);
    }

    void Reveal(string levelId, int dataIndex)
    {
        if (!EnsureContent())
            return;

        NIEntry entry = levelId == "gov" ? NIData.Gov[dataIndex] : NIData.Priv[dataIndex];
        view = "reveal";
        ClearTimers();
        RefreshTouchUi();
        explored.Add((levelId == "gov" ? "Government" : "Private sector") + ": " + entry.title);

        NIParticleField field = content.Field;
        field.Formation("icon", levelId);
        field.Burst(9f);
        field.Flash(1.2f);
        content.ShowReveal(NIData.Level(levelId).crumb, entry);
        Later(3.3f, () => AddStages(entry.stages));
    }

    void Programme(int dataIndex)
    {
        if (!EnsureContent())
            return;

        NIProgramme p = NIData.Programmes[dataIndex];
        view = "prog";
        ClearTimers();
        RefreshTouchUi();
        explored.Add("Programme: " + p.title);

        NIParticleField field = content.Field;
        field.Formation("icon", p.icon);
        field.Burst(9f);
        field.Flash(1.2f);
        content.ShowProgramme(p);
        Later(1.6f, () => AddStages(new[] { p.id }));
    }

    void ConsultView()
    {
        EnsureSession();
        content.Field.Formation("brandx", null, 0.55f);

        List<string> titles = new List<string>();
        int from = Mathf.Max(0, explored.Count - 4);
        for (int i = from; i < explored.Count; i++)
        {
            int cut = explored[i].IndexOf(": ", StringComparison.Ordinal);
            titles.Add(cut >= 0 ? explored[i].Substring(cut + 2) : explored[i]);
        }

        List<string> stageNames = new List<string>();
        foreach (string key in pipe)
            stageNames.Add(NIData.StageName(key));

        StringBuilder body = new StringBuilder();
        body.Append("Session code: ").Append(code).Append('\n');
        body.Append("Talent pipeline: ").Append(stageNames.Count > 0 ? string.Join(", ", stageNames.ToArray()) : "to discuss").Append('\n');
        if (titles.Count > 0)
            body.Append("Explored: ").Append(string.Join("; ", titles.ToArray()));

        string mail = "mailto:" + NIData.ContactMail
                      + "?subject=" + Uri.EscapeDataString("Book a consultation · National initiatives")
                      + "&body=" + Uri.EscapeDataString(body.ToString());

        content.ShowConsult(code, titles.ToArray(), mail, HandoffMessage());
    }

    string HandoffMessage()
    {
        if (handoffStatus == "followup")
            return "Thank you. We will be in touch soon.";
        if (handoffStatus == "with")
            return "You are with our expert now.";
        return "Show this code to our expert beside you.";
    }

    void AddStages(string[] keys)
    {
        if (!EnsureContent())
            return;

        List<string> fresh = new List<string>();
        for (int i = 0; i < keys.Length; i++)
        {
            if (pipe.Add(keys[i]))
                fresh.Add(keys[i]);
        }

        content.SetPipe(pipe, fresh);
        if (fresh.Count > 0)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < fresh.Count; i++)
                names.Add(NIData.StageName(fresh[i]));
            content.Toast("Added to your talent pipeline", string.Join(" · ", names.ToArray()));
            Play("place", 0.7f);
            Soft("asmr_sparkle");
            content.Field.AddRing(new Color(238f / 255f, 213f / 255f, 153f / 255f), 11f);
            content.Field.Flash(1f);
        }
    }

    // ---------- touch screen buttons ----------

    void StartAgain()
    {
        Activity();
        Play("lift", 0.8f);
        Soft("asmr_exhale", 1f, 1f, 0.8f);
        SetWheel("home", HomeFocus());
        Shock();
        ShowView("who");
    }

    void BookConsultation()
    {
        Activity();
        Play("connect", 0.8f);
        Soft("asmr_rise");
        EnsureSession();
        ShowView("consult");
    }

    void Handoff()
    {
        Activity();
        EnsureSession();
        handed = true;
        handoffStatus = handoffStatus ?? "ready";
        Play("connect", 0.8f);
        Soft("asmr_chime_low");
        content.Field.Flash(1f);
        content.Field.AddRing(new Color(238f / 255f, 213f / 255f, 153f / 255f), 11f);
        content.Toast("Your expert is ready", "Everything is linked to " + code);
        content.SetHandoffMessage(HandoffMessage());
        RefreshTouchUi();
    }

    void FinishSession()
    {
        Activity();
        ResetExperience();
    }

    void ResetExperience()
    {
        ClearTimers();
        pipe.Clear();
        seen.Clear();
        sectors.Clear();
        explored.Clear();
        code = null;
        handed = false;
        handoffStatus = null;

        Play("lift", 0.8f);
        Soft("asmr_exhale", 1f, 1f, 0.9f);
        if (content != null)
            content.Field.Scatter(6f);
        SetWheel("home", HomeFocus());
        ShowView("idle");
    }
}
