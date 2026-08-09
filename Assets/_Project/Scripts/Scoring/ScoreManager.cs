using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// How well the player did.
public enum OutcomeGrade
{
    TryAgain,
    Pass,
    Good,
    Excellent
}

/// Works out the result and writes the debrief.
///
/// This is where the whole game finally says what it means, and it costs almost nothing to
/// build because the data has been accumulating since Part 4:
///
///   EvidenceLedger     — what the player EXAMINED
///   InterventionState  — what the player CHANGED
///   ImpactDetector     — whether the collision happened in the end
///
/// Keeping "examined" and "changed" separate is the point. It lets the debrief tell the
/// difference between a factor you never noticed and one you noticed and walked away from,
/// and say something different about each. A single number can't do that.
///
/// The per-hazard wording lives on the HazardInteractable components out in the scene, not
/// duplicated here — this collects them at startup. Write the text once, next to the object
/// it describes.
public class ScoreManager : MonoBehaviour
{
    [Header("Wiring — auto-found if left empty")]
    [SerializeField] private InterventionState interventions;
    [SerializeField] private EvidenceLedger ledger;
    [SerializeField] private ImpactDetector impactDetector;

    [Header("Points")]
    [SerializeField] private int pointsPerHazardFixed = 25;

    [Tooltip("Partial credit for spotting a factor even if you didn't act on it.")]
    [SerializeField] private int pointsPerHazardNoticedOnly = 5;

    [Tooltip("Cost of changing something that made no difference. 0 by default — guessing " +
             "shouldn't be punished on a first playthrough.")]
    [SerializeField] private int penaltyPerPointlessChange = 0;

    [Header("Results — filled in when the debrief opens")]
    [SerializeField] private int score;
    [SerializeField] private OutcomeGrade grade;
    [SerializeField] private bool collisionPrevented;

    public int Score => score;
    public int MaxScore => interventions != null ? interventions.RequiredCount * pointsPerHazardFixed : 100;
    public OutcomeGrade Grade => grade;
    public bool CollisionPrevented => collisionPrevented;

    public string Headline { get; private set; } = "";
    public string GradeLine { get; private set; } = "";
    public string Body { get; private set; } = "";

    // hazardId -> the component in the scene that describes it
    private readonly Dictionary<HazardId, HazardInteractable> hazardLookup =
        new Dictionary<HazardId, HazardInteractable>();

    private void Awake()
    {
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (ledger == null) ledger = FindFirstObjectByType<EvidenceLedger>();
        if (impactDetector == null) impactDetector = FindFirstObjectByType<ImpactDetector>();

        BuildHazardLookup();
    }

    /// Collects every hazard in the scene so the debrief can quote its wording.
    ///
    /// FindObjectsInactive.Include matters: a hazard the player has already fixed may have
    /// hidden itself (the phone disappears when she pockets it), and a disabled hazard
    /// would otherwise vanish from the report exactly when it's most relevant.
    private void BuildHazardLookup()
    {
        hazardLookup.Clear();

        HazardInteractable[] all = FindObjectsByType<HazardInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (HazardInteractable h in all)
        {
            if (hazardLookup.ContainsKey(h.Id))
            {
                Debug.LogWarning(
                    $"[ScoreManager] Two objects both claim to be '{h.Id}': " +
                    $"'{hazardLookup[h.Id].name}' and '{h.name}'. The debrief will use the " +
                    "first. Give one of them a different Hazard Id.", h);
                continue;
            }

            hazardLookup.Add(h.Id, h);
        }
    }

    /// Called by the director the moment the debrief opens.
    public void Compute()
    {
        if (interventions == null)
        {
            Debug.LogError("[ScoreManager] No InterventionState in the scene.", this);
            return;
        }

        int fixedCount = 0;
        int noticedOnlyCount = 0;
        score = 0;

        var wasFixed = new List<HazardId>();
        var noticedButIgnored = new List<HazardId>();
        var neverFound = new List<HazardId>();

        // ---- go through the four factors that actually mattered ----
        foreach (HazardId id in interventions.RequiredHazards)
        {
            bool changed = interventions.Has(id);
            bool examined = ledger != null && ledger.Has(id);

            if (changed)
            {
                fixedCount++;
                score += pointsPerHazardFixed;
                wasFixed.Add(id);
            }
            else if (examined)
            {
                noticedOnlyCount++;
                score += pointsPerHazardNoticedOnly;
                noticedButIgnored.Add(id);
            }
            else
            {
                neverFound.Add(id);
            }
        }

        // ---- things they changed that were never going to help ----
        var pointless = new List<HazardId>();
        foreach (HazardId id in interventions.AllApplied)
        {
            if (interventions.IsRequired(id)) continue;
            pointless.Add(id);
            score -= penaltyPerPointlessChange;
        }

        score = Mathf.Max(0, score);
        collisionPrevented = impactDetector == null || !impactDetector.HasImpacted;
        grade = GradeFor(fixedCount, interventions.RequiredCount);

        BuildText(wasFixed, noticedButIgnored, neverFound, pointless, fixedCount);
    }

    private OutcomeGrade GradeFor(int fixedCount, int required)
    {
        if (fixedCount >= required) return OutcomeGrade.Excellent;
        if (fixedCount >= required - 1) return OutcomeGrade.Good;
        if (fixedCount >= required - 2) return OutcomeGrade.Pass;
        return OutcomeGrade.TryAgain;
    }

    private void BuildText(List<HazardId> wasFixed, List<HazardId> noticedButIgnored,
                           List<HazardId> neverFound, List<HazardId> pointless, int fixedCount)
    {
        int required = interventions.RequiredCount;

        Headline = collisionPrevented
            ? "COLLISION PREVENTED"
            : "THE COLLISION STILL HAPPENED";

        GradeLine = $"{grade.ToString().ToUpper()}     {fixedCount} of {required} factors " +
                    $"addressed     {score} / {MaxScore}";

        // NOTE: every string below is deliberately plain ASCII. TextMeshPro can only draw
        // glyphs that exist in the font atlas, and the default LiberationSans SDF has no
        // tick, cross or em dash — they render as empty boxes. Coloured +, ! and x are
        // safe with any font, including whatever gets swapped in during Part 12.
        StringBuilder sb = new StringBuilder();

        // ---- the lesson, stated once, up front ----
        if (collisionPrevented)
        {
            sb.AppendLine("You found all four. Put any single one of them back and this still " +
                          "happens - that is the point. It took every one of these small " +
                          "failures lining up at the same moment.");
        }
        else
        {
            sb.AppendLine($"You addressed {fixedCount} of the {required} factors. The changes " +
                          "you made were real, and you saw them in the replay - but avoiding " +
                          "this collision needed <b>all four</b> gone.");
        }
        sb.AppendLine();

        // ---- what they got ----
        if (wasFixed.Count > 0)
        {
            sb.AppendLine("<b>YOU CHANGED</b>");
            foreach (HazardId id in wasFixed)
            {
                sb.AppendLine($"  <color=#7CE07C><b>+</b></color>  {NameOf(id)}");
            }
            sb.AppendLine();
        }

        // ---- what they saw but walked away from. The most interesting category. ----
        if (noticedButIgnored.Count > 0)
        {
            sb.AppendLine("<b>YOU SPOTTED BUT LEFT ALONE</b>");
            foreach (HazardId id in noticedButIgnored)
            {
                sb.AppendLine($"  <color=#FFC85A><b>!</b></color>  {NameOf(id)}");
                sb.AppendLine($"      {ExplanationOf(id)}");
            }
            sb.AppendLine();
        }

        // ---- what they never found ----
        if (neverFound.Count > 0)
        {
            sb.AppendLine("<b>YOU NEVER FOUND</b>");
            foreach (HazardId id in neverFound)
            {
                sb.AppendLine($"  <color=#FF7A6E><b>x</b></color>  {NameOf(id)}");
                sb.AppendLine($"      {ExplanationOf(id)}");
            }
            sb.AppendLine();
        }

        // ---- red herrings, reported without scolding ----
        if (pointless.Count > 0)
        {
            sb.AppendLine("<b>ALSO CHANGED, WITH NO EFFECT</b>");
            foreach (HazardId id in pointless)
            {
                sb.AppendLine($"  -  {NameOf(id)}");
            }
        }

        Body = sb.ToString();
    }

    private string NameOf(HazardId id)
    {
        return hazardLookup.TryGetValue(id, out HazardInteractable h) && !string.IsNullOrEmpty(h.DisplayName)
            ? h.DisplayName
            : id.ToString();
    }

    private string ExplanationOf(HazardId id)
    {
        if (hazardLookup.TryGetValue(id, out HazardInteractable h) &&
            !string.IsNullOrWhiteSpace(h.DebriefExplanation))
        {
            return h.DebriefExplanation;
        }

        // Loud on purpose — an empty explanation is the one thing that makes the debrief
        // worthless, and it is easy to forget to fill one in.
        Debug.LogWarning($"[ScoreManager] '{id}' has no Debrief Explanation written. " +
                         "Fill it in on that object's Hazard Interactable component.", this);

        return $"<color=#FF7A6E>[No debrief explanation written for {id}]</color>";
    }
}
