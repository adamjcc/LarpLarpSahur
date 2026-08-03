using UnityEngine;

/// TEMPORARY on-screen display for Part 3, drawn with OnGUI so it needs zero setup.
/// A real Canvas with proper text replaces this in Part 5. Delete it then.
public class PhaseHudPlaceholder : MonoBehaviour
{
    [SerializeField] private ScenarioDirector director;
    [SerializeField] private ScenarioRunner runner;
    [SerializeField] private ScenarioSettings settings;
    [SerializeField] private InterventionState interventions;

    [SerializeField] private bool show = true;
    [SerializeField] private int fontSize = 26;

    private GUIStyle big;
    private GUIStyle huge;

    private void Awake()
    {
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
        if (runner == null) runner = FindFirstObjectByType<ScenarioRunner>();
        if (settings == null) settings = FindFirstObjectByType<ScenarioSettings>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
    }

    private void OnGUI()
    {
        if (!show || director == null) return;
        BuildStyles();

        float w = Screen.width;

        // Sits BELOW the ScenarioDebugKeys readout so the two don't overlap.
        // Untick Show On Screen there once you don't need it.
        const float topOffset = 140f;

        // ---- phase name, centred ----
        GUI.Label(new Rect(0, topOffset, w, 46), director.Phase.ToString().ToUpper(), huge);

        // ---- prompt for what to press ----
        string hint = director.Phase switch
        {
            GamePhase.Observe       => "[ENTER] continue      [Z] replay from the top",
            GamePhase.FreeRoam      => "WASD walk      [C] her POV      [V] his POV      " +
                                       "[B] passenger seat      [ENTER] begin intervention",
            GamePhase.PovReplay     => "watching...",
            GamePhase.PassengerSeat => "[V] see what he saw      [Q] get out",
            GamePhase.Intervene     => "WASD walk — fix the hazards before impact",
            GamePhase.Resolve       => "watching the outcome...",
            GamePhase.Debrief       => "[ENTER] try again",
            _ => ""
        };
        GUI.Label(new Rect(0, topOffset + 50, w, 36), hint, big);

        // ---- countdown, only during Intervene ----
        if (director.Phase == GamePhase.Intervene && settings != null)
        {
            // SCENARIO seconds, not real seconds. Two decimals so it visibly ticks
            // even though it is crawling at 0.06x.
            float remaining = Mathf.Max(0f, director.TimeToImpact);

            GUIStyle timer = new GUIStyle(huge) { fontSize = fontSize + 20 };
            timer.normal.textColor = remaining < 1f ? Color.red : Color.yellow;

            GUI.Label(new Rect(0, Screen.height - 130, w, 70),
                      $"{remaining:F2}", timer);
            GUI.Label(new Rect(0, Screen.height - 66, w, 34),
                      "SECONDS TO IMPACT", big);
        }

        // ---- hazard counter, during Intervene and Debrief ----
        if (interventions != null &&
            (director.Phase == GamePhase.Intervene || director.Phase == GamePhase.Debrief))
        {
            GUI.Label(new Rect(20, Screen.height - 50, 500, 34),
                      $"HAZARDS FIXED   {interventions.CorrectCount} / {interventions.RequiredCount}",
                      big);
        }
    }

    private void BuildStyles()
    {
        if (big != null) return;

        big = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        huge = new GUIStyle(big)
        {
            fontSize = fontSize + 12,
            fontStyle = FontStyle.Bold
        };
    }
}
