using System.Collections.Generic;
using UnityEngine;

/// Remembers which hazards the player has fixed. One of these lives on SYSTEMS.
///
/// THE RULE THAT MATTERS: this is deliberately NOT cleared by ScenarioRunner.ResetScenario().
/// If it were, the Resolve replay would undo everything the player just did. Only the Retry
/// button clears it.
public class InterventionState : MonoBehaviour
{
    [Tooltip("The hazards that must ALL be fixed to avoid the collision.\n\n" +
             "Add CarDrinkBottle here later if you build the optional 5th hazard.")]
    [SerializeField]
    private List<HazardId> requiredHazards = new List<HazardId>
    {
        HazardId.PedestrianPhone,
        HazardId.PedestrianHeadphones,
        HazardId.CarHeadlights,
        HazardId.CarSpeed
    };

    [Header("Read-only — watch this fill up while you play")]
    [SerializeField] private List<HazardId> appliedForInspector = new List<HazardId>();

    // A HashSet ignores duplicates automatically. Add() returns false if it was already
    // in there, which is exactly the "don't count the same fix twice" behaviour we want.
    private readonly HashSet<HazardId> applied = new HashSet<HazardId>();

    /// How many of the REQUIRED hazards have been fixed. Red herrings don't count.
    public int CorrectCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < requiredHazards.Count; i++)
            {
                if (applied.Contains(requiredHazards[i])) count++;
            }
            return count;
        }
    }

    /// How many are needed in total. Used by the "2 / 4" HUD counter.
    public int RequiredCount => requiredHazards.Count;

    /// The required hazards, in Inspector order. The debrief walks this to build its
    /// report, so the order you type them in is the order they're explained.
    public IReadOnlyList<HazardId> RequiredHazards => requiredHazards;

    /// Everything the player changed, including things that made no difference.
    public IEnumerable<HazardId> AllApplied => applied;

    /// True only when EVERY required hazard is fixed. This is the single condition that
    /// decides whether the collision is avoided.
    public bool AllRequiredFixed => CorrectCount >= requiredHazards.Count;

    /// Has this particular hazard been fixed? Actors call this to decide what to show.
    public bool Has(HazardId id) => applied.Contains(id);

    public bool IsRequired(HazardId id) => requiredHazards.Contains(id);

    /// Fix a hazard. Returns TRUE only the first time, so the caller knows whether to
    /// play a sound / bump the score / fire an effect.
    public bool Apply(HazardId id)
    {
        bool isNew = applied.Add(id);
        if (isNew) RefreshInspectorList();
        return isNew;
    }

    /// Wipes everything. Called by Retry, never by ResetScenario.
    public void ClearAll()
    {
        applied.Clear();
        RefreshInspectorList();
    }

    /// Copies the HashSet into a plain List purely so you can watch it in the Inspector.
    /// HashSets are invisible to Unity's Inspector; Lists are not.
    private void RefreshInspectorList()
    {
        appliedForInspector.Clear();
        foreach (HazardId id in applied) appliedForInspector.Add(id);
    }

    // ------------------------------------------------------------------
    // Testing helpers. Right-click the component header in the Inspector
    // to run these while the game is playing.
    // ------------------------------------------------------------------

    [ContextMenu("TEST — Fix all required hazards")]
    private void DebugFixAll()
    {
        for (int i = 0; i < requiredHazards.Count; i++) Apply(requiredHazards[i]);
        Debug.Log($"[InterventionState] All fixed. {CorrectCount}/{RequiredCount}");
    }

    [ContextMenu("TEST — Fix car hazards only")]
    private void DebugFixCarOnly()
    {
        Apply(HazardId.CarHeadlights);
        Apply(HazardId.CarSpeed);
        Debug.Log($"[InterventionState] Car only. {CorrectCount}/{RequiredCount}");
    }

    [ContextMenu("TEST — Fix pedestrian hazards only")]
    private void DebugFixPedestrianOnly()
    {
        Apply(HazardId.PedestrianPhone);
        Apply(HazardId.PedestrianHeadphones);
        Debug.Log($"[InterventionState] Pedestrian only. {CorrectCount}/{RequiredCount}");
    }

    [ContextMenu("TEST — Clear all")]
    private void DebugClear()
    {
        ClearAll();
        Debug.Log("[InterventionState] Cleared.");
    }
}
