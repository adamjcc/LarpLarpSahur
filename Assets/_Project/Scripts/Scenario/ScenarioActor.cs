using UnityEngine;

/// Base class for anything that takes part in the replayable crash.
///
/// "abstract" means you never put THIS on a GameObject. It is a template that other scripts
/// (PedestrianVictim, IncidentVehicle) inherit from. It says: anything in the crash must be
/// able to be ticked, and must be able to put itself back to the start.
///
/// THE GOLDEN RULE: a ScenarioActor never moves itself in Update(). It only moves when the
/// ScenarioRunner calls Tick(). That is the whole reason the crash is repeatable.
public abstract class ScenarioActor : MonoBehaviour
{
    protected Vector3 startPosition;
    protected Quaternion startRotation;
    protected Animator animator;

    /// "virtual" means a child script can add to this. If it does, it must call base.Awake().
    protected virtual void Awake()
    {
        // remember where the designer left this object, so ResetToStart can restore it
        startPosition = transform.position;
        startRotation = transform.rotation;

        // GetComponentInChildren finds the Animator even when it's on the model
        // one or two levels down, which is how imported characters are set up
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
        ScenarioRunner runner = FindFirstObjectByType<ScenarioRunner>();

        if (runner == null)
        {
            Debug.LogError($"[ScenarioActor] '{name}' could not find a ScenarioRunner in the " +
                           "scene. Add one to your SYSTEMS object.", this);
            return;
        }

        runner.Register(this);
        ResetToStart();
    }

    /// Called once per fixed simulation step by the ScenarioRunner.
    /// dt  = always the same fixed amount (1/60 s). Never a variable frame time.
    /// now = seconds since the scenario started.
    public abstract void Tick(float dt, float now);

    /// Put me back exactly as I was at scenario time zero.
    /// Child scripts override this to also reset their state machine, distance, animator, etc.
    public virtual void ResetToStart()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    /// Slow motion for the things Unity animates by itself (Animators, NavMeshAgents),
    /// which the ScenarioRunner cannot reach directly.
    public virtual void ApplyVisualTimeScale(float scale)
    {
        if (animator != null) animator.speed = scale;
    }
}
