# Unity Architecture Plan — Road Safety Reconstruction Game

> **⚠️ SUPERSEDED BY [`BUILD_PLAN.md`](BUILD_PLAN.md).**
> That document is the authoritative plan and reflects the final agreed design
> (car + pedestrian, night, post-crash free roam, dialogue-triggered POV replays).
> This file is kept because its background explanations of Cinemachine, NavMesh,
> determinism and the audit of the original scripts are still accurate and useful.
> Where the two disagree, `BUILD_PLAN.md` wins.

Written 30 July 2026. Target: I3E / STLD / 3RT deliverables due 14 Aug 2026.
This document covers **only the Unity side** (mostly I3E, with notes where STLD/3RT touch it).

---

## Part 0 — Audit of the current project

### Environment
| Thing | Value | Note |
|---|---|---|
| Unity | 6000.3.13f1 | Unity 6.3 |
| Render pipeline | URP 17.3 | fine |
| Cinemachine | 3.1.2 | **already installed** — this is your camera system |
| AI Navigation | 2.0.12 | NavMesh, satisfies "Unity Navigation" rubric |
| Timeline | 1.8.12 | installed, but you likely won't need it (see Part 2) |
| Input System | 1.19 | Active Input Handling = **Both**, so old `Input.*` still works |
| Tags | `NPC`, `Vehicle`, `CinemachineTarget` | need more |
| Layers | `Pedestrian` (layer 6) | need `Interactable`, `Vehicle`, `Environment` |

### Existing scripts — what to keep, what to change

**`VehicleController.cs`** — waypoint follower + forward sensor raycast.
- Keep: the flattened-Y trick (`flatTargetPosition`) — that was a smart fix.
- **Problem:** the sensor brakes whenever anything tagged `NPC` or `Vehicle` is in front. Your entire game is *a car hitting a pedestrian*. This script guarantees the car stops and the crash never happens.
- **Problem:** `currentWaypointIndex = 0` on overflow makes it loop forever. Your incident is a one-shot 8-second event, not a loop.
- **Fix:** this becomes **two separate scripts**. `AmbientVehicle` (keep the sensor + loop — for background traffic that must never crash) and `IncidentVehicle` (no sensor; brakes only when its FSM decides to, based on intervention flags).

**`NPCBrain.cs`** — NavMeshAgent + `Random.insideUnitSphere` roaming.
- Keep as-is, rename to `AmbientPedestrian`. It is genuinely good for background crowd and it satisfies the NavMesh rubric.
- **Never use it for your victim pedestrian.** `Random` = a different result every playthrough. Your victim must arrive at the same spot at the same time, every single run.
- Small bug: `agent.isStopped` is set every frame in `Update`. Move it into the state transition so it's set once.

**`PlayerRaycaster.cs`** — centre-screen ray.
- **Problem:** hard-coded to `InteractableEvidence`. You will have at least five kinds of clickable thing (evidence, intervention target, POV trigger, car door, exit button). You'd end up with five `GetComponent<>` checks.
- **Fix:** introduce an `IInteractable` interface (Part 3.5). One ray, many behaviours.
- **Problem:** only raycasts from centre of screen. Inside the car you want a **mouse cursor**. Needs two aim modes.
- **Problem:** no layer mask — the ray hits the ground, your own collider, everything. Add a `LayerMask`.

**`InteractableEvidence.cs`** — material swap.
- **Problem:** `objRenderer.material = highlightMaterial` only handles a single-material single-renderer object. Your car has many renderers and many materials. Swapping `.material` also silently creates a new material instance each time (memory leak over a long session).
- **Fix:** a separate `Highlighter` component that caches `Renderer[]` and each renderer's original `materials` array, and restores them exactly.

### What's missing entirely
No prefabs. No phase manager. No time control. No camera director. No evidence tracking. No scoring. No UI. No reset system. `MainScene` is empty; the work is in `TestScene`.

### Housekeeping flags
- `Assets/Pack_FREE_Cars` is **untracked in git** and has no licence record. 3RT rubric requires credit + licence for external assets. Create `Docs/CREDITS.md` now and commit both.
- STLD tutor said don't use ProBuilder, but your scenes contain `pb_Mesh-*` objects. **ProBuilder is fine for greybox/blockout** that you later replace with Maya FBX. Just don't ship ProBuilder meshes as final art. Confirm this reading with the tutor.
- Nothing is a prefab. Fix early — prefabs are how four people work in one Unity project without destroying each other's work in git.

---

## Part 1 — The one idea that makes this whole game possible

Read this part twice. Everything else follows from it.

Your game shows **the same 8 seconds, four times**: bird's-eye, driver POV, pedestrian POV, and then a slow-motion version the player walks around in. Then the player changes something and it plays a **fifth** time with a different ending.

For that to work, the incident must be **deterministic**: given the same starting conditions, it produces *bit-for-bit the same* motion every time. Same positions, same timings, same impact point.

Most beginner Unity code is *not* deterministic, because it depends on:
- `Random.*` — different every run
- `NavMeshAgent` local avoidance — agents nudge each other unpredictably
- `Rigidbody` physics — floating-point + collision ordering means real physics drifts
- reactive raycast sensors — "brake if something is in front" depends on exactly when frames land
- variable frame rate — `speed * Time.deltaTime` accumulating over a laggy frame

Your current `VehicleController` has three of those five problems.

### The solution: a scenario clock and a fixed step

Instead of every actor reading `Time.deltaTime` and doing its own thing, you build:

1. **One `ScenarioRunner`** that owns a float called `ScenarioTime`, starting at 0.
2. It advances that clock in **fixed steps** of exactly `1/60` second — never a variable delta.
3. Every incident actor (car, pedestrian, traffic light) registers with the runner and gets ticked by it. **They never use `Update()` for movement.**
4. Motion is **position from distance**, not "add a bit each frame": a `ScenarioPath` you can ask "where am I after travelling 12.4 metres along this path?"

### Why this is magic

Because the sim is deterministic and steppable, you get all of this **for free**, with no extra systems:

- **Replay from another camera:** `ResetScenario()`, switch camera, `Play()`. Identical incident, new angle.
- **"Rewind to 3 seconds before impact":** `SeekTo(impactTime - 3f)`. Implementation is 4 lines — reset to zero, then call `StepOnce()` in a `while` loop until you reach the target time. 8 seconds of scenario = 480 iterations of simple maths = instant, in one frame, invisible to the player. **You do not need to record and reverse anything.** This is the "fake rewind" your notes mention, and this is how you fake it.
- **Slow motion:** feed the accumulator `Time.unscaledDeltaTime * 0.2f`. The world crawls.
- **Pause for investigation:** `IsPlaying = false`.
- **Branching outcomes:** the FSMs read intervention flags at their decision points.

### The second half of the trick: slow-mo must not slow the player

The obvious move is `Time.timeScale = 0.2f`. **Don't.** That slows *everything*, including the player's own walking and mouse-look, which feels terrible — and your core gameplay is "walk around while the world crawls."

Instead:
- The `ScenarioRunner` uses `Time.unscaledDeltaTime * TimeScale`. Only the incident slows down.
- The player's `FirstPersonController` keeps using normal `Time.deltaTime`. Full speed.
- Set `animator.speed = TimeScale` on incident actors so their walk cycles slow too.
- Ambient NavMesh NPCs: set `agent.speed = baseSpeed * TimeScale` and `animator.speed = TimeScale`.

Result: crisp player control, world in slow motion. Exactly the feel you're describing.

### What this means for how you build the crash

**Do not simulate the crash. Choreograph it.** You author the car's path and speed and the pedestrian's path and speed so that they *arrive at the same place at the same time*. Physics does not cause your crash — your numbers do. A trigger collider on the impact point *detects* it so you can play effects, but the collision is a foregone conclusion.

This is how real games do set-piece moments. It is not cheating; it is the only way to hit a deadline.

---

## Part 2 — Timeline vs. scripting: pick scripting

Your old notes suggest Unity Timeline for the POV sequences. My recommendation: **don't**, for the main incident.

| | Timeline | ScenarioRunner (scripted) |
|---|---|---|
| Authoring in editor | visual, nice | numbers in the Inspector |
| Deterministic | yes | yes |
| **Branching on player choices** | painful — you need separate timelines per outcome, and outcomes multiply | trivial — an `if` in a state machine |
| Reset | fiddly | one method |
| Counts as I3E "FSM / reactive AI" work | **no** | **yes** |

You have two victims × several possible interventions each. Timeline means authoring every combination by hand. Scripting means one `if`. And the I3E rubric explicitly grades FSMs and reactive NPCs — a Timeline asset earns you nothing there.

**Use Timeline only for**: the intro/briefing cutscene, and camera moves during POV replays if you want a fancy dolly shot. Never for actor logic.

---

## Part 3 — The systems you need

Listed in **dependency order** — build them in this order, because each depends on the ones above it. Twelve systems. Nine are small.

```
                     ┌─────────────────────┐
                     │  ScenarioDirector   │  the phase FSM: which mode are we in
                     └──────────┬──────────┘
             ┌──────────────┬───┴──────┬──────────────┐
             ▼              ▼          ▼              ▼
      ScenarioRunner  CameraDirector  UIManager   ScoreManager
      (the clock)     (Cinemachine)   (HUD)       (grade+debrief)
             │                                        ▲
    ┌────────┴────────┐                               │
    ▼                 ▼                        ┌──────┴───────┐
ScenarioActor    ScenarioPath              EvidenceLedger  InterventionState
(base class)     (geometry)                 (what found)   (what changed)
    │                                             ▲              ▲
    ├── IncidentVehicle  (FSM) ───reads──────────────────────────┤
    ├── PedestrianVictim (FSM) ───reads──────────────────────────┤
    ├── BystanderNPC     (FSM)                    │              │
    └── GuideNPC         (FSM)                    │              │
                                                  │              │
                              PlayerInteractor ───┴──────────────┘
                              (raycast) → IInteractable
                                            ├── EvidenceItem
                                            ├── InterventionTarget
                                            ├── PovTrigger
                                            └── CarEntryPoint
```

### 3.1 `ScenarioRunner` — the clock (build first)

**Job:** own `ScenarioTime`, step it in fixed increments, tick all registered actors, reset them, seek to any time.

```csharp
// Assets/_Project/Scripts/Scenario/ScenarioRunner.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the deterministic clock that drives the incident. All incident actors
/// are ticked from here in fixed steps so the same crash happens every replay.
/// </summary>
public class ScenarioRunner : MonoBehaviour
{
    [Tooltip("Fixed simulation step. Never change at runtime.")]
    [SerializeField] private float step = 1f / 60f;

    /// <summary>Seconds elapsed inside the incident. 0 = start of the scenario.</summary>
    public float ScenarioTime { get; private set; }

    /// <summary>1 = real speed, 0.2 = slow motion. Does not affect the player.</summary>
    public float TimeScale { get; set; } = 1f;

    public bool IsPlaying { get; private set; }

    private readonly List<ScenarioActor> actors = new List<ScenarioActor>();
    private float accumulator;

    public void Register(ScenarioActor a)
    {
        if (!actors.Contains(a)) actors.Add(a);
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // unscaledDeltaTime so Time.timeScale never interferes
        accumulator += Time.unscaledDeltaTime * TimeScale;

        // catch up in whole fixed steps; leftover carries to next frame
        int guard = 0;
        while (accumulator >= step && guard++ < 20)
        {
            accumulator -= step;
            StepOnce();
        }
    }

    private void StepOnce()
    {
        ScenarioTime += step;
        for (int i = 0; i < actors.Count; i++)
            actors[i].Tick(step, ScenarioTime);
    }

    public void Play()  { IsPlaying = true;  }
    public void Pause() { IsPlaying = false; }

    /// <summary>Puts every actor back to its authored starting state and zeroes the clock.</summary>
    public void ResetScenario()
    {
        IsPlaying = false;
        ScenarioTime = 0f;
        accumulator = 0f;
        for (int i = 0; i < actors.Count; i++) actors[i].ResetToStart();
    }

    /// <summary>
    /// Fake rewind. Resets, then fast-forwards the simulation in fixed steps
    /// until the clock reaches targetTime. Completes inside a single frame.
    /// </summary>
    public void SeekTo(float targetTime)
    {
        ResetScenario();
        int guard = 0;
        while (ScenarioTime < targetTime && guard++ < 100000) StepOnce();
    }

    /// <summary>Applies TimeScale to Animators / NavMeshAgents that Unity drives itself.</summary>
    public void PushVisualTimeScale()
    {
        for (int i = 0; i < actors.Count; i++) actors[i].ApplyVisualTimeScale(TimeScale);
    }
}
```

The `guard` counters are cheap insurance so a bad value can never hang Unity in an infinite `while`. Get in the habit.

### 3.2 `ScenarioActor` — base class for everything in the incident

**Job:** remember where it started, restore itself, receive ticks.

```csharp
using UnityEngine;

/// <summary>Base class for anything that takes part in the replayable incident.</summary>
public abstract class ScenarioActor : MonoBehaviour
{
    protected Vector3 startPosition;
    protected Quaternion startRotation;
    protected Animator animator;

    protected virtual void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // find the runner and register. Fine for a project this size.
        FindFirstObjectByType<ScenarioRunner>().Register(this);
    }

    /// <summary>Called once per fixed simulation step. Do all movement here, never in Update.</summary>
    /// <param name="dt">Always the same fixed step, e.g. 1/60.</param>
    /// <param name="now">Seconds since the scenario started.</param>
    public abstract void Tick(float dt, float now);

    /// <summary>Return to authored starting state. Override to also reset FSM state, flags, meshes.</summary>
    public virtual void ResetToStart()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    public virtual void ApplyVisualTimeScale(float scale)
    {
        if (animator != null) animator.speed = scale;
    }
}
```

`FindFirstObjectByType<T>()` is the Unity 6 replacement for the deprecated `FindObjectOfType<T>()`.

### 3.3 `ScenarioPath` — deterministic movement geometry

**Job:** hold a list of waypoints, and answer *"where am I after travelling D metres?"*

This is the key difference from your current `MoveTowards` code. `MoveTowards` only knows "step towards the next point". A path knows its own arc length, so you can jump to any point along it instantly — which is what makes seeking possible.

```csharp
using UnityEngine;

/// <summary>A polyline the incident actors travel along, addressable by distance.</summary>
public class ScenarioPath : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    private float[] cumulative;   // cumulative[i] = distance from start to points[i]
    public float TotalLength { get; private set; }

    private void Awake() => Rebuild();

    public void Rebuild()
    {
        cumulative = new float[points.Length];
        cumulative[0] = 0f;
        for (int i = 1; i < points.Length; i++)
            cumulative[i] = cumulative[i - 1] +
                            Vector3.Distance(points[i - 1].position, points[i].position);
        TotalLength = cumulative[points.Length - 1];
    }

    /// <summary>Position and facing direction at a given distance along the path.</summary>
    public Vector3 Evaluate(float distance, out Vector3 forward)
    {
        distance = Mathf.Clamp(distance, 0f, TotalLength);

        for (int i = 1; i < points.Length; i++)
        {
            if (distance > cumulative[i]) continue;

            float segLength = cumulative[i] - cumulative[i - 1];
            float t = segLength > 0.0001f ? (distance - cumulative[i - 1]) / segLength : 0f;
            Vector3 a = points[i - 1].position;
            Vector3 b = points[i].position;
            forward = (b - a).normalized;
            return Vector3.Lerp(a, b, t);
        }

        forward = transform.forward;
        return points[points.Length - 1].position;
    }

    // Draws the route in the editor so you can see it without pressing Play.
    private void OnDrawGizmos()
    {
        if (points == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 1; i < points.Length; i++)
            if (points[i - 1] != null && points[i] != null)
                Gizmos.DrawLine(points[i - 1].position, points[i].position);
    }
}
```

An actor then holds a `distanceTravelled` float, adds `speed * dt` to it each tick, and asks the path where that is. Reset = `distanceTravelled = 0`.

### 3.4 The four FSMs

The I3E rubric wants **each team member to plan and implement one FSM**, plus FSM diagrams in the ReadMe. Here is the pattern to use — a plain `enum` + `switch`, with a `Go()` helper so entering a state is a single place you can hook.

Don't reach for fancy state-class hierarchies. Four people each need to understand and defend their own FSM in the VIVA. `enum` + `switch` is readable, diagrammable, and gradeable.

```csharp
using UnityEngine;

/// <summary>The pedestrian who gets hit. Deterministic path; outcome depends on interventions.</summary>
public class PedestrianVictim : ScenarioActor
{
    public enum State { Walking, Distracted, SteppingOut, Alerted, Struck, Aftermath }

    [SerializeField] private ScenarioPath path;
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float distractedAt = 2.0f;   // scenario seconds
    [SerializeField] private float stepsOutAt   = 5.5f;
    [SerializeField] private InterventionState interventions;

    [SerializeField] private State state;     // visible in Inspector for debugging
    private float distance;
    private float timeInState;

    public override void Tick(float dt, float now)
    {
        timeInState += dt;

        switch (state)
        {
            case State.Walking:
                Move(walkSpeed, dt);
                if (now >= distractedAt && !interventions.Has(Intervention.PedestrianPhoneStowed))
                    Go(State.Distracted);
                break;

            case State.Distracted:
                Move(walkSpeed, dt);                 // head down, doesn't look up
                if (now >= stepsOutAt) Go(State.SteppingOut);
                break;

            case State.SteppingOut:
                Move(walkSpeed, dt);
                // THE DECISION POINT — this is where the game branches
                if (interventions.Has(Intervention.PedestrianWarned) ||
                    interventions.Has(Intervention.PedestrianHeadphonesRemoved))
                    Go(State.Alerted);
                break;

            case State.Alerted:
                Move(0f, dt);                        // stops at the kerb — no collision
                break;

            case State.Struck:
                Move(0f, dt);
                if (timeInState > 2f) Go(State.Aftermath);
                break;

            case State.Aftermath:
                break;
        }
    }

    private void Move(float speed, float dt)
    {
        distance += speed * dt;
        Vector3 pos = path.Evaluate(distance, out Vector3 fwd);
        transform.position = pos;
        if (fwd.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(fwd);
        if (animator != null) animator.SetFloat("Speed", speed);
    }

    private void Go(State next)
    {
        state = next;
        timeInState = 0f;
        switch (next)   // OnEnter behaviour
        {
            case State.Distracted: animator.SetBool("OnPhone", true);  break;
            case State.Alerted:    animator.SetBool("OnPhone", false);
                                   animator.SetTrigger("LookUp");     break;
            case State.Struck:     animator.SetTrigger("Hit");        break;
        }
    }

    public void NotifyStruck() => Go(State.Struck);   // called by the impact trigger

    public override void ResetToStart()
    {
        base.ResetToStart();
        distance = 0f;
        timeInState = 0f;
        state = State.Walking;
        if (animator != null) { animator.Rebind(); animator.Update(0f); }
    }
}
```

**Note the shape of it.** States advance on *scenario time* (`now >= 5.5f`) so it's repeatable, but they *branch* on intervention flags. That's the whole game mechanic in one `if`.

`animator.Rebind()` resets the Animator to its default state and clears parameters — essential for replay, and a thing almost nobody knows about.

**The four FSMs and who owns them:**

| Owner | FSM | States | Notes |
|---|---|---|---|
| **Adam** | `PedestrianVictim` | Walking → Distracted → SteppingOut → (Alerted \| Struck) → Aftermath | as above |
| **Darryl** | `IncidentVehicle` | Cruising → Approaching → (Braking \| Unaware) → (Stopped \| Impact) → Aftermath | same shape; brakes only if `DriverWarned`/`DriverPhoneStowed` |
| **Marcus** | `BystanderNPC` | Idle → NoticedIncident → MovingToScene → CallingHelp → Return | reacts to a `ScenarioDirector` event; can use NavMeshAgent |
| **Isaiah** | `GuideNPC` | Idle → FollowPlayer → LeadToObjective → WaitForPlayer → Explain | **the dynamic NPC** the tutor asked for; uses NavMeshAgent + player distance |

Write each as a `.cs` file with the same skeleton. **Draw each as a diagram** (draw.io or Figma, boxes + labelled arrows) — that's a ReadMe deliverable, and drawing it *before* coding will save you hours.

`BystanderNPC` and `GuideNPC` do **not** need to be `ScenarioActor`s — they aren't part of the deterministic incident, so they can use `Update()` and NavMeshAgent freely. Only the crash participants must be deterministic.

### 3.5 `IInteractable` + `PlayerInteractor` — the raycast layer

The single most valuable refactor in this document. An **interface** is a contract: "anything that implements `IInteractable` promises to have these members." The raycaster then talks to the contract, not to specific classes, so you can add new interactable kinds forever without touching the raycaster.

```csharp
/// <summary>Anything the player can look at and activate.</summary>
public interface IInteractable
{
    /// <summary>Text shown in the HUD, e.g. "Examine dashcam".</summary>
    string Prompt { get; }

    /// <summary>False greys it out — e.g. interventions are locked until the Intervene phase.</summary>
    bool IsAvailable { get; }

    void OnFocusEnter();
    void OnFocusExit();
    void OnInteract();
}
```

```csharp
using UnityEngine;

/// <summary>Casts a ray each frame and routes focus/interact events to IInteractable.</summary>
public class PlayerInteractor : MonoBehaviour
{
    public enum AimMode { Crosshair, MouseCursor }

    [SerializeField] private Camera activeCamera;
    [SerializeField] private LayerMask interactableMask;   // set to the Interactable layer only
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private AimMode aimMode = AimMode.Crosshair;

    private IInteractable focused;

    public void SetCamera(Camera cam)   => activeCamera = cam;
    public void SetAimMode(AimMode m)   => aimMode = m;

    private void Update()
    {
        if (activeCamera == null) return;

        Ray ray = aimMode == AimMode.Crosshair
            ? activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : activeCamera.ScreenPointToRay(Input.mousePosition);

        IInteractable hitTarget = null;

        // QueryTriggerInteraction.Collide so trigger colliders can also be clickable
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance,
                            interactableMask, QueryTriggerInteraction.Collide))
        {
            // GetComponentInParent so a child collider on a big prop still resolves
            var candidate = hit.collider.GetComponentInParent<IInteractable>();
            if (candidate != null && candidate.IsAvailable) hitTarget = candidate;
        }

        if (!ReferenceEquals(hitTarget, focused))
        {
            focused?.OnFocusExit();
            focused = hitTarget;
            focused?.OnFocusEnter();
            HudPrompt.Instance.Show(focused?.Prompt);
        }

        if (focused != null && Input.GetMouseButtonDown(0)) focused.OnInteract();
    }
}
```

Two things worth understanding here:
- `interactableMask` — put every clickable object on an `Interactable` layer. Without this the ray hits the road, walls, the player's own capsule, and you fight it all week.
- `GetComponentInParent<IInteractable>()` — Unity's `GetComponent` works with interfaces. This means the collider can be a child of the object holding the script, which is how you'll want to set up cars and props.

Implementations:

| Class | `OnInteract()` does |
|---|---|
| `EvidenceItem` | records itself in `EvidenceLedger`, shows an info panel |
| `InterventionTarget` | sets a flag in `InterventionState` (only during Intervene phase) |
| `PovTrigger` | asks `ScenarioDirector` to play a POV replay |
| `CarEntryPoint` | asks `ScenarioDirector` to enter the car interior |
| `ExitHotspot` | leaves the car interior |

Plus the shared visual helper:

```csharp
using UnityEngine;

/// <summary>Swaps every renderer's materials to a highlight material and restores them exactly.</summary>
public class Highlighter : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;

    private Renderer[] renderers;
    private Material[][] originals;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originals = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            originals[i] = renderers[i].sharedMaterials;   // sharedMaterials: no instance leak
    }

    public void SetHighlight(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (on)
            {
                var fill = new Material[originals[i].Length];
                for (int m = 0; m < fill.Length; m++) fill[m] = highlightMaterial;
                renderers[i].sharedMaterials = fill;
            }
            else
            {
                renderers[i].sharedMaterials = originals[i];
            }
        }
    }
}
```

### 3.6 `EvidenceLedger` and `InterventionState` — the game's memory

Two tiny classes that hold "what has the player figured out" and "what has the player changed". Everything else reads from them.

```csharp
/// <summary>Every clue in the scenario. Add entries here as you author evidence.</summary>
public enum EvidenceId
{
    DriverPhoneInLap,
    DriverOpenDrinkBottle,
    SpeedometerReading,
    HeadlightsOff,
    WornBrakePedal,
    PedestrianLookingAtPhone,
    PedestrianHeadphones,
    ObscuredCrossingSign,
    FadedRoadMarkings
}

/// <summary>Changes the player can make during the intervention phase.</summary>
public enum Intervention
{
    DriverPhoneStowed,
    DriverWarned,
    HeadlightsOn,
    SpeedReduced,
    PedestrianPhoneStowed,
    PedestrianHeadphonesRemoved,
    PedestrianWarned
}
```

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>Records which clues the player has discovered. Survives scenario resets.</summary>
public class EvidenceLedger : MonoBehaviour
{
    private readonly HashSet<EvidenceId> found = new HashSet<EvidenceId>();

    public bool Has(EvidenceId id) => found.Contains(id);
    public int Count => found.Count;
    public IEnumerable<EvidenceId> All => found;

    /// <summary>Returns true only the first time a given clue is found.</summary>
    public bool Record(EvidenceId id) => found.Add(id);
}
```

`InterventionState` is the same shape with `Intervention`, plus a `ClearAll()` you call when the player retries.

A `HashSet` is a collection that ignores duplicates — `Add` returns `false` if the item was already there, which is exactly the "don't score the same clue twice" behaviour you want, for free.

### 3.7 `ScenarioDirector` — the phase FSM

**Job:** the top-level state machine deciding *what mode the game is in*, and orchestrating camera + player + clock for each. This is your fifth FSM (Adam's second, effectively) and the spine of the whole game.

```
Briefing        → intro, controls, objective
Observe         → bird's-eye, incident plays at 1x, player has no control
FreeRoam        → player walks the scene, raycasts clues, can trigger POVs
    ├─ PovReplay      → sub-mode: scripted camera on driver or pedestrian
    └─ CarInterior    → sub-mode: inside the car, mouse cursor, clickable evidence
Intervene       → SeekTo(impact-3s), TimeScale = 0.2, interventions unlocked
Resolve         → TimeScale back to 1, play out, watch the new outcome
Debrief         → score + explanation + retry
```

Per phase it needs to set: which Cinemachine camera is live; whether `FirstPersonController` is enabled; whether the cursor is locked; `runner.TimeScale`; whether `InterventionTarget`s are available; which HUD panel is showing.

Write it as a `switch` in an `EnterPhase(Phase p)` method. Every phase change goes through that one method — that's what stops "the cursor is invisible and I can't click anything" bugs.

```csharp
private void EnterPhase(Phase next)
{
    phase = next;
    switch (next)
    {
        case Phase.Observe:
            runner.ResetScenario();
            runner.TimeScale = 1f;
            cameras.Activate(CameraId.BirdsEye);
            player.SetControlEnabled(false);
            Cursor.lockState = CursorLockMode.Locked;
            runner.Play();
            break;

        case Phase.FreeRoam:
            runner.Pause();
            cameras.Activate(CameraId.PlayerFirstPerson);
            player.SetControlEnabled(true);
            interactor.SetAimMode(PlayerInteractor.AimMode.Crosshair);
            break;

        case Phase.CarInterior:
            cameras.Activate(CameraId.CarInterior);
            player.SetControlEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            interactor.SetCamera(cameras.Get(CameraId.CarInterior));
            interactor.SetAimMode(PlayerInteractor.AimMode.MouseCursor);
            break;

        case Phase.Intervene:
            runner.SeekTo(impactTime - 3f);   // the fake rewind
            runner.TimeScale = 0.2f;
            runner.PushVisualTimeScale();
            cameras.Activate(CameraId.PlayerFirstPerson);
            player.SetControlEnabled(true);
            interventionsUnlocked = true;
            runner.Play();
            break;
        // ...
    }
}
```

### 3.8 `CameraDirector` — Cinemachine

Cinemachine 3 works like this: **one `CinemachineBrain` on your Main Camera**, and many `CinemachineCamera` GameObjects in the scene. Whichever active `CinemachineCamera` has the **highest Priority** wins, and the Brain blends the real camera to it.

So switching views = changing which one is highest priority. You get free smooth blends between views as a bonus.

Cameras to create:
| Name | Setup |
|---|---|
| `CAM_BirdsEye` | high above the junction, rotated to look down. No Follow target. |
| `CAM_PlayerFP` | Follow + LookAt the player's `PlayerCameraRoot`, `CinemachinePanTilt` + `CinemachineRotationComposer` (Starter Assets sets this up for you) |
| `CAM_DriverPOV` | child of the driver's head bone, or Follow the head with a `HardLockToTarget` |
| `CAM_PedestrianPOV` | same, on the pedestrian |
| `CAM_CarInterior` | positioned inside the interior diorama, with a `CinemachinePanTilt` with **clamped** pan/tilt ranges so the player can look around the dash but not out the back |

```csharp
public enum CameraId { BirdsEye, PlayerFirstPerson, DriverPov, PedestrianPov, CarInterior }

public class CameraDirector : MonoBehaviour
{
    [System.Serializable]
    public struct Entry { public CameraId id; public CinemachineCamera cam; }

    [SerializeField] private Entry[] entries;

    public void Activate(CameraId id)
    {
        foreach (var e in entries)
            e.cam.Priority = (e.id == id) ? 20 : 0;
    }

    public Camera Get(CameraId id) => Camera.main;  // Brain lives on Main Camera
}
```

Note `Get()` returns `Camera.main` — with Cinemachine there is only ever **one real Unity Camera**. The virtual cameras just tell it where to be. Your raycasts always come from `Camera.main`. Beginners lose a day to this.

### 3.9 The car interior — how to actually do it

Your car pack has **no interiors**. Standard solution: the **diorama trick**.

1. Build a separate `CarInteriorRig` GameObject: a small modelled interior set (dashboard, steering wheel, two seats, windscreen). It doesn't need to be a whole car — just what the camera can see.
2. Park it somewhere the player can never reach — e.g. at `(0, -200, 0)`, or 500 m off the edge of the map.
3. Put `CAM_CarInterior` inside it, plus `EvidenceItem`s on the phone, the open drink bottle, the speedometer, the headlight switch, the seatbelt buckle.
4. Optionally put a large curved image plane outside the windscreen showing the street, or a `Camera` rendering to a `RenderTexture` for a live view. **Do the image plane first** — a second camera costs performance and time.
5. `CarEntryPoint` on the real car's door → `ScenarioDirector.EnterPhase(Phase.CarInterior)` → camera cuts inside, player controller disabled, mouse cursor unlocked, raycaster switches to `MouseCursor` mode.
6. `Esc` or an exit hotspot returns to `FreeRoam`.

This is how virtually every game does vehicle interiors and inspection close-ups. It is dramatically cheaper than making a real interior on a driveable car, and it means the interior can be lit and dressed independently — which helps your 3RT lighting evidence too.

### 3.10 Impact detection and damage

Because your crash is choreographed, physics only needs to *notice* it:

```csharp
using UnityEngine;

/// <summary>Trigger volume on the vehicle front that reports contact with the victim.</summary>
[RequireComponent(typeof(Collider))]
public class ImpactSensor : MonoBehaviour
{
    [SerializeField] private ScenarioDirector director;
    private bool alreadyFired;

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyFired) return;
        var victim = other.GetComponentInParent<PedestrianVictim>();
        if (victim == null) return;

        alreadyFired = true;
        victim.NotifyStruck();
        director.OnImpact();
    }

    public void ResetSensor() => alreadyFired = false;
}
```

For the visual damage, follow the tutor's advice: **swap the intact mesh for a damaged one.** A `DamageSwapper` with two child GameObjects, `Intact` and `Damaged`, and `SetActive` toggles. Add a couple of loose `Rigidbody` props (a wing mirror, a hubcap, the pedestrian's phone) that you `AddForce` on impact — that's your genuine "Unity Physics mechanics" rubric evidence, and it's five lines.

Don't attempt vehicle deformation or ragdoll. Both eat days and can't be reset cleanly.

### 3.11 `ScoreManager` and the debrief

At the end, compare what the player found and changed against the authored truth:

```csharp
[System.Serializable]
public class EvidenceInfo
{
    public EvidenceId id;
    public string title;
    [TextArea] public string explanation;
    public bool isContributingFactor;   // false = red herring
    public int points;
}
```

Put a `List<EvidenceInfo>` on `ScoreManager` and fill it in the Inspector. Score:

```
+ points        for each contributing factor found
+ 0             for each red herring found (no penalty; encourage looking)
+ bigger points for each correct intervention applied
- small penalty for each unnecessary intervention
+ outcome bonus if the collision was prevented
```

Then build the debrief text by walking the same list — for each contributing factor, print title + explanation and whether the player caught it. **The debrief is your entire educational payload**, and it's the thing that wins Delta Challenge marks. It's also almost free once the ledger exists.

### 3.12 UI

Keep it minimal — Unity UI (uGUI) canvases, no fancy systems:
- `HudPrompt` — a crosshair + a text line ("[LMB] Examine dashcam")
- `EvidencePanel` — title, description, a photo
- `EvidenceListPanel` — toggled with Tab, shows what you've found
- `PhaseBanner` — "OBSERVE" / "INVESTIGATE" / "INTERVENE — 3.0s to impact"
- `DebriefPanel` — score, grade, factor-by-factor breakdown, Retry button

Make `HudPrompt` a simple singleton (`public static HudPrompt Instance`) so anything can call it. Singletons are frowned on in big codebases; in a 4-week student project they save you real time. Use them for the handful of true globals only (HUD, director, runner).

---

## Part 4 — Folder and scene layout

```
Assets/
  _Project/                    ← underscore sorts your own work to the top
    Scripts/
      Scenario/    ScenarioRunner, ScenarioActor, ScenarioPath, ScenarioDirector
      Actors/      PedestrianVictim, IncidentVehicle, BystanderNPC, GuideNPC,
                   AmbientPedestrian, AmbientVehicle
      Interaction/ IInteractable, PlayerInteractor, Highlighter, EvidenceItem,
                   InterventionTarget, PovTrigger, CarEntryPoint
      Data/        EvidenceId, Intervention, EvidenceLedger, InterventionState, EvidenceInfo
      Cameras/     CameraDirector
      Scoring/     ScoreManager
      UI/          HudPrompt, EvidencePanel, DebriefPanel, PhaseBanner
    Prefabs/
    Materials/
    Art/           ← Marcus's Maya FBX + trim sheets land here
  Scenes/
    Main.unity            ← Darryl owns. Nobody else edits.
    Sandbox_Adam.unity    ← per-person test scenes. Edit freely.
    Sandbox_Isaiah.unity
```

**Scene hierarchy in `Main.unity`:**

```
--- SYSTEMS ---            (empty GameObject, all managers as children)
    ScenarioRunner
    ScenarioDirector
    CameraDirector
    EvidenceLedger
    InterventionState
    ScoreManager
--- CAMERAS ---
    Main Camera (CinemachineBrain)
    CAM_BirdsEye / CAM_PlayerFP / CAM_DriverPOV / CAM_PedestrianPOV / CAM_CarInterior
--- PLAYER ---
    PlayerCapsule (FirstPersonController + PlayerInteractor)
--- INCIDENT ---
    Path_Vehicle   (waypoint children)
    Path_Pedestrian
    Vehicle_Incident (IncidentVehicle, ImpactSensor, DamageSwapper)
    Pedestrian_Victim (PedestrianVictim)
--- AMBIENT ---
    NavMeshSurface, ambient pedestrian instances, ambient vehicles
--- EVIDENCE ---
    all EvidenceItem / InterventionTarget objects, grouped
--- INTERIOR_DIORAMA ---    (parked at y = -200)
--- ENVIRONMENT ---         (Darryl's level, static-flagged for lightmaps)
--- UI ---
```

Objects named `--- SYSTEMS ---` are a common trick — they're just empty GameObjects used as visual dividers so a 300-object hierarchy stays readable.

---

## Part 5 — Rubric mapping (I3E)

| Requirement | Where it's satisfied |
|---|---|
| Reactive NPCs | FSMs read `InterventionState` and change outcome; `GuideNPC` reacts to player distance; ambient NPCs react to the incident |
| FSM-based behaviour, one per member | `PedestrianVictim` (Adam), `IncidentVehicle` (Darryl), `BystanderNPC` (Marcus), `GuideNPC` (Isaiah) — plus `ScenarioDirector` phase FSM |
| Unity Navigation | `AmbientPedestrian` + `GuideNPC` on NavMeshAgents; `NavMeshSurface` baked over the level |
| Raycasting | `PlayerInteractor` (both aim modes); `AmbientVehicle` sensor |
| Unity Physics | `ImpactSensor` trigger; `Rigidbody` debris with `AddForce`; character collider/`CharacterController` |
| XML comments | `/// <summary>` on every public class and method — see examples above |
| File headers | see Part 7 |
| Version control | GitHub already set up at `adamjcc/LarpLarpSahur` — everyone must have visible commits |
| ReadMe: controls, bugs, FSM diagrams, answers, credits | write as you go, not on 13 Aug |

**3RT touchpoints in Unity:** static flags on all environment meshes, mixed lighting + baked lightmaps, post-processing volume (more than Bloom — add Vignette, Colour Adjustments, Tonemapping, Depth of Field for the interior), 4 VFX (impact particles, scan/highlight effect, slow-mo screen effect, prevented-collision success effect), no duplicate texture sets.

**STLD touchpoints:** the FreeRoam loop is your circulation design — the compact loop from incident point → car → pedestrian clues → environment clues → intervention start. Sightlines matter mechanically here, not just aesthetically: what the bird's-eye view shows and the driver POV doesn't *is* the level design doing narrative work. Say that in the GDD; it's a strong point.

---

## Part 6 — Build order

Do these strictly in order. Each step ends with something you can press Play and see. **Never build two steps at once.**

**Step 1 — Scaffolding (½ day).** Create the folder structure. Write `ScenarioRunner`, `ScenarioActor`, `ScenarioPath`. Make a test scene with a cube as a `ScenarioActor` following a 4-waypoint path. Add temporary keys: `P` plays, `R` resets, `1` seeks to 3.0s. **Success test:** press `R` then `P` ten times — the cube reaches the same position at the same time every single time.

**Step 2 — The choreographed crash (1 day).** Two paths. `Vehicle_Incident` and `Pedestrian_Victim` as cubes/capsules with the two FSMs, no animation yet. Tune speeds and timestamps until they collide. Add `ImpactSensor`. Log the impact time to Console; hard-code that as `impactTime`. **Success test:** collision happens at the same `ScenarioTime` every run, ±0.05s.

**Step 3 — Cameras (½ day).** `CinemachineBrain` on Main Camera. Five virtual cameras. `CameraDirector`. Number keys switch views while the incident plays. **Success test:** you can watch the same crash from bird's-eye and from the driver's head.

**Step 4 — Phase FSM (1 day).** `ScenarioDirector`. Wire: Observe plays it once → FreeRoam gives player control → a key jumps to Intervene → Resolve → Debrief. Placeholder UI text for the phase name. **Success test:** you can walk the whole loop start to finish with no interactions yet.

**Step 5 — Interaction (1 day).** `IInteractable`, refactor `PlayerInteractor`, `Highlighter`, `EvidenceItem`, `EvidenceLedger`, `HudPrompt`. Create an `Interactable` layer and put three test cubes on it. **Success test:** looking at a cube highlights it and shows a prompt; clicking records it and Console prints the running count.

**Step 6 — Intervention branching (1 day).** `InterventionState`, `InterventionTarget`. Put one on the pedestrian ("tell them to look up") and one on the car ("warn the driver"). Unlock them only in the Intervene phase. **Success test:** intervene → collision prevented. Do nothing → collision happens. Both reachable from the same playthrough via Retry.

**This is your vertical slice.** If Step 6 works, the game exists and everything after is content. Get here first — before art, before animation, before more NPCs.

**Step 7 — Animation (1 day).** Build your own Animator Controller (the Hodaart one is bool-driven; you want a float `Speed` with a blend tree between Idle and Walk, plus `OnPhone` bool and `Hit`/`LookUp` triggers). Download the specific Mixamo animations you need — walking, looking at phone, being hit, sitting in a car, talking on phone. They retarget onto the Hodaart Humanoid rig for free. **Success test:** replay works after `animator.Rebind()`.

**Step 8 — Car interior (1 day).** Diorama, `CAM_CarInterior` with clamped pan/tilt, `CarEntryPoint`, mouse-cursor aim mode, 4–5 evidence items inside.

**Step 9 — POV replays (½ day).** `PovTrigger` interactables that `ResetScenario()`, activate the POV camera, and `Play()`.

**Step 10 — Scoring + debrief (1 day).** `ScoreManager`, `EvidenceInfo` list, `DebriefPanel`. Write the real educational text.

**Step 11 — Content + polish.** More evidence. `BystanderNPC` and `GuideNPC`. Ambient crowd. VFX. Lighting bake. Post-processing.

**Step 12 — Documentation + build.** ReadMe, FSM diagrams, controls table, known bugs, credits. Build early and often — a first build on 12 Aug is how projects die.

---

## Part 7 — Conventions the rubric grades

**File header** — top of every script you write:

```csharp
/*
 * File:    PedestrianVictim.cs
 * Author:  Adam <student id>
 * Date:    2026-07-31
 * Purpose: FSM for the pedestrian involved in the incident. Follows a
 *          deterministic path and branches to a safe or struck outcome
 *          depending on which interventions the player applied.
 */
```

**XML comments** — `///` above every public class, method, and non-obvious field. Typing `///` above a method in Visual Studio auto-generates the tags. `<summary>` for what it does, `<param name="x">` per parameter, `<returns>` if it returns something.

**Naming** — `PascalCase` for classes/methods/properties, `camelCase` for private fields, `PascalCase` for public fields the Inspector shows. Prefer `[SerializeField] private float speed;` over `public float speed;` — the Inspector still shows it, but other scripts can't secretly change it.

---

## Part 8 — Traps to avoid

1. **Don't use `Time.timeScale` for slow motion.** It slows your player too. Use the runner's own `TimeScale`.
2. **Don't let physics cause the crash.** Choreograph it.
3. **No `Random` and no NavMesh avoidance on the incident actors.** They must be deterministic.
4. **`Camera.main` is the only real camera** when using Cinemachine. Raycast from it.
5. **Use a `LayerMask` on every raycast.** Without one you will hit the ground and the player's own collider.
6. **Call `animator.Rebind()` on reset**, or the second replay looks wrong.
7. **Everything becomes a prefab.** Loose scene objects are unmergeable in git — the fastest way for four people to lose a day's work.
8. **One person edits `Main.unity`** (Darryl). Everyone else works in `Sandbox_<Name>.unity` and hands over prefabs.
9. **Commit `.meta` files.** They carry the links between assets. Missing metas = every reference in the project breaks.
10. **Rename and move assets inside Unity**, never in Windows Explorer, or the `.meta` link breaks.
11. **Bake the NavMesh after any level change**, and re-bake before every build.
12. **Record licences now** for Hodaart and Pack_FREE_Cars. Both 3RT and I3E rubrics require credit.

---

## Part 9 — Open questions to settle with your group this week

1. Is the scenario locked as **car vs. pedestrian** (matching what you have) rather than the cyclist version in the older notes?
2. **Daytime with an obscured sightline**, or **night with headlights off**? Night is easier to art-direct and makes "headlights off" a strong, readable clue — but is harder to light well.
3. Does the player begin in a training room, or straight in the scene? A room costs a scene transition but gives 3RT its modular interior.
4. Confirm with the I3E tutor: does "four NPCs each" mean four *instances* or four *behaviours*? (Instances is achievable; behaviours is not.)
5. Confirm with the STLD tutor that ProBuilder is acceptable for greybox that gets replaced.
6. How many interventions must the player make for the "prevented" outcome? Recommend: **two of four correct**, and no more than one unnecessary one.
