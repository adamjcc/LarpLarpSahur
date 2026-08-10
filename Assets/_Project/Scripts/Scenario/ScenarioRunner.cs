using System.Collections.Generic;
using UnityEngine;

/// The clock that drives the entire crash. There is exactly ONE of these in the scene.
///
/// WHY THIS EXISTS
/// Normally every object moves itself in Update() using Time.deltaTime, which is a slightly
/// different number every frame depending on how fast the computer is running. That means
/// "the same crash" lands in a slightly different place every time you press Play.
///
/// This runner instead moves time forward in FIXED jumps of exactly 1/60 of a second, and
/// ticks every actor itself. Identical maths every run means an identical crash every run,
/// which is what lets you replay it from four cameras and rewind into it.
public class ScenarioRunner : MonoBehaviour
{
    [Header("Simulation")]
    [Tooltip("Fixed step size. Do not change this at runtime.")]
    [SerializeField] private float step = 1f / 60f;

    [Tooltip("Safety cap. SeekTo will never simulate past this many scenario seconds.")]
    [SerializeField] private float maxScenarioLength = 30f;

    /// Seconds elapsed inside the crash. 0 = the very start.
    public float ScenarioTime { get; private set; }

    /// 1 = normal speed. 0.06 = slow motion. IMPORTANT: this only slows the actors,
    /// never the player. Time.timeScale is left alone at 1 for the whole game.
    public float TimeScale { get; set; } = 1f;

    public bool IsPlaying { get; private set; }

    /// How many actors have registered. Useful for checking your setup worked.
    public int ActorCount => actors.Count;

    private readonly List<ScenarioActor> actors = new List<ScenarioActor>();

    // leftover time that wasn't a whole step yet; carried over to the next frame
    private float accumulator;

    /// Actors call this themselves in their Start(). You don't wire it up by hand.
    public void Register(ScenarioActor actor)
    {
        if (actor == null || actors.Contains(actor)) return;

        actors.Add(actor);

        // Keep the list sorted so things like the ImpactDetector always run after the
        // car and the pedestrian have moved. Sorting on every registration is fine —
        // it happens a handful of times at startup and never again.
        actors.Sort((a, b) => a.TickOrder.CompareTo(b.TickOrder));
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // unscaledDeltaTime = real seconds, ignoring Time.timeScale completely.
        // This is what keeps the player moving at full speed during slow motion.
        accumulator += Time.unscaledDeltaTime * TimeScale;

        // Run as many WHOLE steps as we've banked up. The guard stops a lag spike
        // from freezing Unity while it tries to catch up on hundreds of steps.
        const int maxStepsPerFrame = 20;
        int guard = 0;

        while (accumulator >= step && guard < maxStepsPerFrame)
        {
            accumulator -= step;
            guard++;
            StepOnce();
        }

        // After a big lag spike there may still be a huge backlog. Throw it away rather
        // than let it snowball into a permanent slow-motion catch-up.
        if (accumulator > step * maxStepsPerFrame) accumulator = 0f;
    }

    /// Advance the simulation by exactly one fixed step.
    private void StepOnce()
    {
        ScenarioTime += step;

        // plain for-loop, not foreach, so an actor can safely be added mid-loop
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null) actors[i].Tick(step, ScenarioTime);
        }
    }

    public void Play()
    {
        IsPlaying = true;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    /// Put every actor back to scenario time zero.
    /// NOTE: this deliberately does NOT clear the player's interventions. If it did,
    /// the Resolve replay would undo everything they just fixed.
    public void ResetScenario()
    {
        IsPlaying = false;
        ScenarioTime = 0f;
        accumulator = 0f;

        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null) actors[i].ResetToStart();
        }

        // Animator.Rebind() inside ResetToStart wipes the animator back to defaults, which
        // can take animator.speed with it. Re-apply, or the first replay after a reset
        // would run at full speed while the world around it crawls.
        PushVisualTimeScale();
    }

    /// THE FAKE REWIND.
    ///
    /// Resets to zero, then runs the simulation forward in fixed steps until the clock
    /// reaches targetTime. Five scenario seconds is only ~300 loops of simple maths, so
    /// this finishes inside a single frame and the player just sees an instant cut.
    ///
    /// This is why you never have to record or reverse anything.
    public void SeekTo(float targetTime)
    {
        ResetScenario();

        targetTime = Mathf.Clamp(targetTime, 0f, maxScenarioLength);

        int guard = 0;
        int maxSteps = Mathf.CeilToInt(maxScenarioLength / step) + 10;

        while (ScenarioTime < targetTime && guard < maxSteps)
        {
            guard++;
            StepOnce();
        }

        if (guard >= maxSteps)
        {
            Debug.LogWarning($"[ScenarioRunner] SeekTo({targetTime}) hit the step limit. " +
                             $"Raise Max Scenario Length (currently {maxScenarioLength}s).", this);
        }
    }

    /// Runs the whole scenario silently, in one frame, purely to find out WHEN something
    /// happens — then puts everything back as if nothing had occurred.
    ///
    /// Used to answer "what time does the crash actually land at?" before it happens, so
    /// the on-screen countdown can hit 0.00 at the moment of impact instead of a second
    /// or two after it. Same fixed-step maths as normal play, so the answer is exact.
    ///
    /// Returns the scenario time at which stopWhen first became true, or -1 if it never did.
    public float SimulateUntil(System.Func<bool> stopWhen, float maxTime)
    {
        if (stopWhen == null) return -1f;

        ResetScenario();

        float stoppedAt = -1f;
        int guard = 0;
        int maxSteps = Mathf.CeilToInt(maxTime / step) + 10;

        while (ScenarioTime < maxTime && guard < maxSteps)
        {
            guard++;
            StepOnce();

            if (stopWhen())
            {
                stoppedAt = ScenarioTime;
                break;
            }
        }

        // Leave no trace — the caller does its own SeekTo straight afterwards
        ResetScenario();
        IsPlaying = false;

        return stoppedAt;
    }

    /// Pushes TimeScale onto Animators and anything else Unity drives on its own.
    /// Call this whenever you change TimeScale.
    public void PushVisualTimeScale()
    {
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null) actors[i].ApplyVisualTimeScale(TimeScale);
        }
    }

    /// Convenience: set the speed and push it to the animators in one call.
    public void SetTimeScale(float scale)
    {
        TimeScale = scale;
        PushVisualTimeScale();
    }
}
