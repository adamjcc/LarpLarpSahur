using System.Collections.Generic;
using UnityEngine;

/// Remembers which hazards the player has EXAMINED (as opposed to fixed).
///
/// Two separate ideas, kept in two separate places:
///   EvidenceLedger    — "I looked at this and read what it said"    (Free Roam)
///   InterventionState — "I changed this"                            (Intervene)
///
/// The debrief uses both: it can tell the difference between a factor you never noticed and
/// one you noticed but chose not to act on. That distinction is the interesting part of the
/// feedback, so it is worth the extra ten lines.
public class EvidenceLedger : MonoBehaviour
{
    [Header("Read-only — watch this fill up while you play")]
    [SerializeField] private List<HazardId> examinedForInspector = new List<HazardId>();

    private readonly HashSet<HazardId> examined = new HashSet<HazardId>();

    public int Count => examined.Count;
    public bool Has(HazardId id) => examined.Contains(id);
    public IEnumerable<HazardId> All => examined;

    /// Returns TRUE only the first time, so the caller knows whether to award points
    /// or play a discovery sound.
    public bool Record(HazardId id)
    {
        bool isNew = examined.Add(id);
        if (isNew) RefreshInspectorList();
        return isNew;
    }

    public void ClearAll()
    {
        examined.Clear();
        RefreshInspectorList();
    }

    // HashSets are invisible in the Inspector; Lists are not. This exists purely so you
    // can watch what is happening while you playtest.
    private void RefreshInspectorList()
    {
        examinedForInspector.Clear();
        foreach (HazardId id in examined) examinedForInspector.Add(id);
    }
}
