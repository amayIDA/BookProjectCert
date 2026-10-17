using System.Collections.Generic;

/// <summary>
/// Copy and structure for the Talent Wheel, taken from National_Initiatives_Talent_Wheel.html.
/// </summary>
public sealed class NIEntry
{
    public string title;
    public string hint;
    public string help;
    public string[] offer;
    public string[] get;
    public string[] stages;
}

public sealed class NIProgramme
{
    public string id;
    public string title;
    public string icon;
    public string line;
    public string[] list;
    public string[] get;
}

public sealed class NIOption
{
    public string id;
    public string title;
    public string icon;
    public int dataIndex;
}

public sealed class NILevel
{
    public string id;
    public string hub;
    public string hubSub;
    public string parent;
    public string crumb;
    public NIOption[] options;
}

public static class NIData
{
    public static readonly string[][] StageList =
    {
        new[] { "work", "Work Ready" },
        new[] { "role", "Role Ready" },
        new[] { "sector", "Sector Capable" },
        new[] { "pd", "Professional Development" },
        new[] { "leader", "Future Leader" }
    };

    public static readonly Dictionary<string, string> StageLine = new Dictionary<string, string>
    {
        { "work", "Ready for a first job." },
        { "role", "Ready for a specific role." },
        { "sector", "Specialists in priority sectors." },
        { "pd", "Short, practical courses." },
        { "leader", "Tomorrow’s leaders." }
    };

    public static readonly string[] Support =
    {
        "Coaching and mentoring", "Tutoring", "Job search support", "On-the-job training", "Placement and retention"
    };

    public const string ContactUrl = "https://inxeducation.ac.ae";
    public const string ContactMail = "info@inxeducation.ae";

    public static readonly NIEntry[] Gov =
    {
        new NIEntry { title = "Policy to delivery", hint = "Turn national goals into results.", help = "We are your delivery partner. We design and run programmes for your initiative.",
            offer = new[] { "Tell us the goal", "We build the pathway" },
            get = new[] { "Your initiative delivered end to end", "Emiratisation goals accelerated", "A model you can replicate" },
            stages = new[] { "work", "role", "sector", "pd", "leader" } },
        new NIEntry { title = "Education to employment", hint = "Connect learning to real jobs.", help = "We connect school, vocational and tertiary pathways to the skills employers need.",
            offer = new[] { "High school engagement", "Bridging programmes", "Tertiary curriculum design", "Vocational training" },
            get = new[] { "Your initiative delivered end to end", "Emiratisation goals accelerated", "A model you can replicate" },
            stages = new[] { "work" } },
        new NIEntry { title = "Workplace readiness", hint = "Get Emiratis ready for their first role.", help = "We prepare Emirati job seekers for their first role, from certification to placement.",
            offer = new[] { "Employment Readiness Certification", "Role-specific training", "Placement support" },
            get = new[] { "Emiratis ready to start work", "A smoother move into their first role", "Strong early-career foundations" },
            stages = new[] { "work", "role" } },
        new NIEntry { title = "Capability in priority sectors", hint = "Grow talent where the UAE needs it.", help = "Sector pathways, from role fundamentals to accredited expertise.",
            offer = new[] { "Financial Services", "Healthcare", "Aviation", "Tourism & Hospitality", "Real Estate" },
            get = new[] { "Capability where the UAE needs it", "Skilled Emiratis in priority sectors" },
            stages = new[] { "sector" } },
        new NIEntry { title = "Next-generation leaders", hint = "Secure tomorrow’s Emirati leaders.", help = "Bespoke leadership programmes, designed and delivered with accredited partners.",
            offer = new[] { "Bespoke leadership programmes", "Accredited partners" },
            get = new[] { "A national leadership pipeline", "Succession capability", "Next-gen Emirati leaders" },
            stages = new[] { "leader" } }
    };

    public static readonly NIEntry[] Priv =
    {
        new NIEntry { title = "Meeting Emiratisation targets", hint = "Hire Emiratis ready to perform.", help = "We supply job-ready and role-ready Emirati talent.",
            offer = new[] { "Employment Readiness Certification", "Role-specific training", "Placement support", "Retention support" },
            get = new[] { "Emirati hires ready to perform", "Faster recruitment", "Stronger retention" },
            stages = new[] { "work", "role" } },
        new NIEntry { title = "Graduates aren’t work-ready", hint = "Close the gap before day one.", help = "We prepare Emirati job seekers for work before they reach you.",
            offer = new[] { "Employment Readiness Certification, two levels" },
            get = new[] { "Faster hiring", "Better-prepared hires", "Fewer entry-level skills gaps" },
            stages = new[] { "work" } },
        new NIEntry { title = "New hires lack role skills", hint = "Train for the actual role.", help = "We train for the real role, tailored to your business.",
            offer = new[] { "Customer Experience", "Business Support", "HR", "Financial Services", "Sales & Retail" },
            get = new[] { "Faster onboarding", "Stronger role performance", "Greater Emirati capability" },
            stages = new[] { "role" } },
        new NIEntry { title = "Specialists are hard to find", hint = "Build the specialists you need.", help = "We build specialists to match your sector.",
            offer = new[] { "Financial Services", "Healthcare", "Aviation", "Tourism & Hospitality", "Real Estate" },
            get = new[] { "Capability matched to your business", "Fewer specialist skills gaps" },
            stages = new[] { "sector" } },
        new NIEntry { title = "Skills gaps in your team", hint = "Upskill the team you have.", help = "Short, practical courses for your employees.",
            offer = new[] { "Emotional Intelligence", "Conflict Resolution & Negotiation", "Finance for Non-Finance Managers", "Problem Solving & Decision Making" },
            get = new[] { "Skills used at work straight away", "Stronger individual and team performance" },
            stages = new[] { "pd" } },
        new NIEntry { title = "Leaders are hard to find", hint = "Grow leaders from within.", help = "We grow your leaders through bespoke, accredited training.",
            offer = new[] { "Strategic leadership", "Organisational performance", "Change management", "Decision making" },
            get = new[] { "A stronger talent pipeline", "Greater succession capability", "Next-gen leaders" },
            stages = new[] { "leader" } }
    };

    public static readonly NIProgramme[] Programmes =
    {
        new NIProgramme { id = "work", title = "Work Ready", icon = "work", line = "Employment Readiness Certification, in two levels.",
            list = new[] { "Level 1: Core Workforce Readiness", "Level 2: Workplace Transition Readiness" },
            get = new[] { "Faster hiring", "Better-prepared hires", "Fewer skills gaps" } },
        new NIProgramme { id = "role", title = "Role Ready", icon = "role", line = "Training for roles matched to your business.",
            list = new[] { "Customer Experience Associate", "Business Support Associate", "HR Coordinator", "Financial Services Coordinator" },
            get = new[] { "Ready to perform on day one" } },
        new NIProgramme { id = "sector", title = "Sector Capable", icon = "sectorcap", line = "From first role to accredited expertise.",
            list = new[] { "Sector Foundations: learn the sector", "Sector Practitioner: apply it on the job", "Sector Advanced Professional: specialise and accredit" },
            get = new[] { "Financial Services · Healthcare · Aviation · Tourism & Hospitality · Real Estate" } },
        new NIProgramme { id = "pd", title = "Professional Development", icon = "pd", line = "Short courses for individuals and teams.",
            list = new[] { "Emotional Intelligence", "Innovation & Creativity", "Conflict Resolution & Negotiation" },
            get = new[] { "Immediate impact", "Practical skills" } },
        new NIProgramme { id = "leader", title = "Future Leader", icon = "leader", line = "Bespoke training and certification.",
            list = new[] { "Bespoke leadership training", "Certification" },
            get = new[] { "A stronger talent pipeline", "Succession capability", "Next-gen Emirati leaders" } }
    };

    // Order of the wheel items. The numbers are indexes into the arrays above.
    static readonly int[] GovOrder = { 2, 1, 0, 4, 3 };
    static readonly int[] PrivOrder = { 0, 5, 4, 3, 2, 1 };
    static readonly int[] ProgrammeOrder = { 2, 3, 1, 0, 4 };

    static readonly Dictionary<string, NILevel> levels = Build();

    public static NILevel Level(string id)
    {
        return levels[id];
    }

    public static string StageName(string id)
    {
        for (int i = 0; i < StageList.Length; i++)
        {
            if (StageList[i][0] == id)
                return StageList[i][1];
        }

        return id;
    }

    static Dictionary<string, NILevel> Build()
    {
        var map = new Dictionary<string, NILevel>();

        map["home"] = new NILevel
        {
            id = "home", hub = "Who we are and what we build.", hubSub = "Tap to discover", parent = null, crumb = "Start",
            options = new[]
            {
                new NIOption { id = "approach", title = "Our approach", icon = "approach" },
                new NIOption { id = "consult", title = "Book a consultation", icon = "consult" },
                new NIOption { id = "sector", title = "Pick your sector", icon = "sector" }
            }
        };

        map["sector"] = new NILevel
        {
            id = "sector", hub = "Where do you work?", hubSub = "Back", parent = "home", crumb = "Pick your sector",
            options = new[]
            {
                new NIOption { id = "priv", title = "Private sector", icon = "priv" },
                new NIOption { id = "gov", title = "Government", icon = "gov" },
                new NIOption { id = "explore", title = "Just exploring", icon = "explore" }
            }
        };

        var gov = new List<NIOption>();
        for (int i = 0; i < GovOrder.Length; i++)
            gov.Add(new NIOption { id = "g" + GovOrder[i], title = Gov[GovOrder[i]].title, icon = "goal", dataIndex = GovOrder[i] });
        map["gov"] = new NILevel { id = "gov", hub = "What’s your priority?", hubSub = "Back", parent = "sector", crumb = "Government", options = gov.ToArray() };

        var priv = new List<NIOption>();
        for (int i = 0; i < PrivOrder.Length; i++)
            priv.Add(new NIOption { id = "p" + PrivOrder[i], title = Priv[PrivOrder[i]].title, icon = "goal", dataIndex = PrivOrder[i] });
        map["priv"] = new NILevel { id = "priv", hub = "What’s your challenge?", hubSub = "Back", parent = "sector", crumb = "Private sector", options = priv.ToArray() };

        var programmes = new List<NIOption>();
        for (int i = 0; i < ProgrammeOrder.Length; i++)
        {
            NIProgramme p = Programmes[ProgrammeOrder[i]];
            programmes.Add(new NIOption { id = p.id, title = p.title, icon = p.icon, dataIndex = ProgrammeOrder[i] });
        }

        map["explore"] = new NILevel { id = "explore", hub = "Pick a programme", hubSub = "Back", parent = "sector", crumb = "Just exploring", options = programmes.ToArray() };
        return map;
    }
}
