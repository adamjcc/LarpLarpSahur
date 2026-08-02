# BUILD PLAN — Road Safety Crash Reconstruction (Unity)

**This is the authoritative plan.** `UNITY_ARCHITECTURE_PLAN.md` is kept for its background
explanations, but where the two disagree, *this* document wins.

Written 30 July 2026 · Unity 6000.3.13f1 · URP 17.3 · Cinemachine 3.1.2

---

# TABLE OF CONTENTS

- [Part A — What the game is](#part-a--what-the-game-is)
- [Part B — The three ideas that make it work](#part-b--the-three-ideas-that-make-it-work)
- [Part C — Numbers you must confirm](#part-c--numbers-you-must-confirm)
- [Part D — Every script, and what talks to what](#part-d--every-script-and-what-talks-to-what)
- [Part E — Scene and prefab layout](#part-e--scene-and-prefab-layout)
- [Part F — Build order: 8 parts](#part-f--build-order-8-parts)
- [Part G — Known traps, per part](#part-g--known-traps-per-part)
- [Part H — Rubric checklist](#part-h--rubric-checklist)

---

# PART A — What the game is

## A.1 The story

Night. A quiet road with a pedestrian crossing near a campus. A student walks toward
the crossing looking down at her phone, wearing noise-cancelling headphones. A car
approaches too fast with its headlights off. They meet at the crossing.

The player is a crash analyst in a VR reconstruction. They watch it happen, investigate
the frozen aftermath, hear both people's accounts, see through both people's eyes, then
get sent back to five seconds before impact to try to change it.

## A.2 The four hazards

| # | Whose | What | Where you interact with it | What changes |
|---|---|---|---|---|
| 1 | Pedestrian | Looking down at phone | **Inside her POV** (she's holding it) | She pockets it and looks up |
| 2 | Pedestrian | Noise-cancelling headphones | **From outside** — aim at her head | She pulls them off |
| 3 | Driver | Headlights off | **Inside his POV** — dash button | Headlights come on, road lights up |
| 4 | Driver | Speeding | **Inside his POV** — brake pedal | Car decelerates |
| *5* | *Driver* | *Open drink bottle* | *Inside his POV* | *OPTIONAL — build last, off by default* |

**Red herrings** (clickable, give a description, change nothing):
her umbrella, the car's wiper stalk, the car radio.

## A.3 The phase flow

```
OBSERVE          Bird's-eye. The whole crash plays at normal speed.
                 Buttons: [Replay]  [Next →]
   ↓
FREE ROAM        Clock frozen 2s after impact. You walk the aftermath.
                 · Aim at her head    → headphones description
                 · Aim at her phone   → description
                 · Aim at her body    → dialogue → [See what she saw] → POV REPLAY
                 · Aim at the car     → you sit in the PASSENGER SEAT
                       → dialogue with driver → [See what he saw] → POV REPLAY
                       → look around the interior, aim at dash items → descriptions
                 · (later) a stray dog sniffs out things worth looking at
                 When ready: [Begin Intervention]
   ↓
INTERVENE        Clock jumps to impact − 5s and runs at 0.1× speed.
                 You get 50 real seconds. Countdown on screen.
                 · Walk up to her (within 12m) → E → you're in HER eyes
                       → click the phone → she pockets it
                 · Aim at her head from outside → E → headphones off
                 · Walk up to the car (within 12m) → E → you're in HIS eyes
                       → click headlight button, click brake pedal
                 Ends when all 4 are addressed, or the clock reaches impact.
   ↓
RESOLVE          Replays the same 5 seconds at normal speed with your changes
                 baked in, from a cinematic camera. You watch what you caused.
   ↓
DEBRIEF          Score, grade, and a factor-by-factor explanation. [Retry]
```

**The training room with the police officer is a separate Unity scene, added last.**
Nothing in the code above changes when it arrives.

---

# PART B — The three ideas that make it work

If you understand only three things from this document, make it these. Everything
in Part F is just carefully implementing them.

## B.1 One clock drives the whole incident

Normally in Unity, every object does its own thing inside `Update()`, using
`Time.deltaTime` — the number of seconds since the last frame. That number changes
every frame depending on how fast your computer is running. So two objects moving
"at the same speed" will end up in slightly different places every time you press Play.

For a normal game, nobody notices. For **your** game it's fatal, because you show the
same crash five times and it has to land in the same place every time.

So instead:

- One script — `ScenarioRunner` — owns a number called `ScenarioTime` that starts at 0.
- It moves that number forward in **fixed jumps of exactly 1/60 of a second**. Never a
  variable amount. Always 0.01666...
- Every object involved in the crash registers itself with the runner. The runner calls
  a method called `Tick()` on each of them, once per jump.
- **Those objects never use `Update()` to move.** They only move when the runner ticks them.

Because the maths is identical every run, the crash is identical every run.

### What this buys you, for free

| You want | You write |
|---|---|
| Replay from another camera | `ResetScenario(); Play();` |
| Freeze the aftermath | `SeekTo(impactTime + 2f); Pause();` |
| Rewind to 5s before impact | `SeekTo(impactTime - 5f);` |
| Slow motion | `TimeScale = 0.1f;` |

**`SeekTo` is not a rewind system.** It resets everything to zero, then calls the tick
method over and over in a tight loop until the clock reaches the time you asked for.
Five seconds of scenario = 300 loops of simple maths. It happens inside a single frame,
faster than the screen can refresh. The player sees an instant cut.

This is why you do not need to record anything, and do not need to reverse any animation.

## B.2 Slow motion must not slow the player down

Unity has a global setting called `Time.timeScale`. Set it to 0.1 and everything runs at
one tenth speed. It is the obvious tool for slow motion, and it is **wrong for your game**,
because your entire Intervene phase is *"the world crawls while you walk around normally."*
`Time.timeScale` would slow your walking and your mouse to a crawl too. It would feel awful.

So:

- `ScenarioRunner` feeds itself `Time.unscaledDeltaTime * TimeScale`.
  `unscaledDeltaTime` is real seconds, ignoring `Time.timeScale` entirely.
- Only things the runner ticks slow down. The player is untouched.
- You also set `animator.speed = TimeScale` on the crash actors so their walk cycles slow
  with them, otherwise they'd moonwalk.

`Time.timeScale` stays at 1.0 for the entire game. Never touch it.

## B.3 The crash is choreographed, not simulated

You are **not** going to make a car with realistic physics and hope it hits someone. You are
going to author two paths and two speeds so that the car and the pedestrian arrive at the
same spot at the same moment. Your numbers cause the crash, not physics.

A trigger collider on the car's front bumper *notices* the contact so you can play the
effects — but by then the collision was already guaranteed by arithmetic.

This is not a shortcut or a cheat. It is how every scripted moment in every game is made,
and it is the only version of this that finishes by 14 August.

### What this rules out, on purpose

- No `Random` anywhere near the crash actors.
- No `NavMeshAgent` on the crash actors. NavMeshAgents shove each other out of the way
  (it's called local avoidance) and that nudging is unpredictable. **NavMesh is still used**
  — for the background crowd and the dog — just never for the car or the victim.
- No `Rigidbody` driving the car. Rigidbodies drift.
- No reactive "brake if something is in front of me" sensor on the crash car. Your current
  `VehicleController` has exactly this, and it is why it will never hit anyone.

---

# PART C — Numbers you must confirm

**Read this before we start Part 2.** These are recommendations with reasoning. All of them
are Inspector fields you can change without touching code — but the level layout depends on
them, so Darryl needs them settled early.

## C.0 You do not place the actors by hand

This is the important bit, and it removes almost all the guesswork.

You drop **one empty GameObject called `ImpactMarker`** at the spot where the crash happens.
Each actor then works out its own starting position like this:

```
how far along my path is the ImpactMarker?          →  say 118.0 m
how far do I travel in impactTime seconds?          →  13.9 m/s × 7 s = 97.3 m
so I start at                                       →  118.0 − 97.3 = 20.7 m along my path
```

Move the marker, change the car's speed, redraw the road — everything re-solves on its own.

**And if the road is too short, Unity tells you.** If that subtraction comes out negative,
the Console prints: `Path_Vehicle is 12.4 m too short before the ImpactMarker.` No guessing,
no cars spawning in mid-air.

## C.1 The core timing

| Setting | Recommended | Why |
|---|---|---|
| `impactTime` | **7.0 s** | Enough for the bird's-eye to show the car approach, turn, and hit. Purely how much lead-in the opening shot has — change it freely. |
| Total sim length | **10.0 s** | 7 s to impact + 3 s of aftermath settling. |
| Free-roam snapshot | **impact + 2.0 s** | Car stopped, she's back on her feet, dust settled. |
| Intervene lead time | **3.0 s** | Shortened from 5 s so the road can be shorter. See C.2. |
| Intervene `TimeScale` | **0.06×** | 3 scenario seconds ÷ 0.06 = **50 real seconds** of play. |
| POV replay window | **impact − 5.0 s → impact + 1.0 s** | 6 seconds. Long enough to read the mistake, short enough not to drag. |

**The on-screen countdown shows scenario seconds, not real seconds.** It reads
`3.00 → 0.00`, to two decimal places, crawling downward. At 0.06× the second decimal changes
about six times a second, so it visibly ticks instead of looking frozen. The player never
sees the number 50 — that's just how long it takes in the real world.

## C.2 Speeds and distances

| Setting | Recommended | Why |
|---|---|---|
| Posted speed limit | **40 km/h** | Night, residential, near a crossing. Makes speeding legible on the speedometer. |
| Car speed | **50 km/h = 13.9 m/s** | A real urban speed, clearly over a 40 limit, and it keeps the road short. |
| Pedestrian walk | **1.2 m/s** | Normal walking is ~1.4. Phone users walk measurably slower — a real research finding, worth stating in the debrief. |
| Player walk | 4.0 m/s (Starter Assets default) | Already set. Don't change it yet. |
| Road carriageway | **7.0 m** (2 lanes × 3.5 m) | Standard. |
| NPC interact range | **12 m** | Your call, from earlier. |
| Prop interact range | **3.5 m** | Phones, buttons, headphones. |

### How much road you actually need

Only one stretch of road has a hard length requirement: **the straight run between the end of
the turn and the impact point.** That's where the whole Intervene phase happens.

```
straight road needed  =  car speed  ×  intervene lead time
```

| Lead time | TimeScale | Real seconds of play | Straight needed after the turn |
|---|---|---|---|
| **3.0 s** | **0.06** | **50 s** | **~42 m**  ← recommended |
| 3.5 s | 0.07 | 50 s | ~49 m |
| 4.0 s | 0.08 | 50 s | ~56 m |
| 5.0 s | 0.10 | 50 s | ~70 m |

Every row gives the same 50 seconds of play (`realSeconds = leadTime ÷ TimeScale`). Pick the
row that matches the road Darryl can actually build.

**Design rule from the turn:** the car must *finish* its turn at least `leadTime` before
impact. Otherwise the Intervene phase opens with the car mid-corner, and a car cornering at
50 km/h looks ridiculous. Everything before the straight is only ever seen in the bird's-eye
shot, so it can be as long or short as looks good.

**You don't have to run 42 m.** The car is coming toward you at 0.83 m/s (13.9 × 0.06) while
you walk at 4, so you close the gap together in about **9 real seconds** out of your 50.
Rough budget: 2 s to reach her, 12 s in her POV, 9 s to meet the car, 15 s in his POV = 38 s,
with 12 s of slack. If playtesting feels tight, drop `TimeScale` to 0.05 and you get 60 s.

**Optional, for later:** the car can take a speed *curve* instead of one constant — slower
through the corner, accelerating down the straight. It's an `AnimationCurve` field you draw
in the Inspector. I'll leave the hook in from Part 2 but default it off; adding it changes
nothing else.

## C.3 The scene geometry

The crash sits at the world origin so the numbers stay readable. Matching your sketch, the
car comes in along the top road heading **−X**, turns south, then runs **−Z** to the impact.

```
        ══════════════════════════════════════════  main road
                                       ↰   ◄── CAR enters here
                                       │
                                       │   ← the turn completes ~42 m before impact
                                       │
                                       │      side road, 7 m wide
                                       │
                                       ▼
        ═══════════════════════════════█═══════  ◄── IMPACT POINT = (0, 0, 0)
                                       ▲            put ImpactMarker here
                                       │
                                  pedestrian walking toward it
```

Exact waypoint coordinates come from Darryl's layout — you no longer need to compute them,
because of C.0. All you have to guarantee is that **each path is long enough before the
marker**, and Unity will tell you if it isn't.

Player spawn for Free Roam and Intervene: on the pavement roughly **6 m from the impact
point**, facing the direction the car comes from. Close to her, so the first thing you find
is the pedestrian, and the car arrives as a growing threat.

## C.4 Outcome grading — AGREED RULE

There are only **two endings to author**, and everything else is per-hazard dressing on top
of them:

> **The collision is avoided only when ALL FOUR hazards are fixed.**
> **Every hazard the player fixes always shows its effect, crash or no crash.**

| Hazards fixed | What happens on screen | Grade |
|---|---|---|
| 4 / 4 | She stops at the kerb. Car brakes with headlights on. **No contact.** | **Excellent** |
| 3 / 4 | **Crash still happens**, but carrying whatever the player did fix. | **Pass** |
| 0–2 / 4 | **Crash still happens**, carrying whatever the player did fix. | **Try again** |

So a player who fixed only the car's hazards watches the crash happen **with the headlights
on and the car moving noticeably slower** — and she still walks out in front of it, because
she never looked up. A player who fixed only her hazards watches the same crash with **no
phone and no headphones on her** — and it still happens, because he was speeding in the dark.

That is a far better lesson than a pass/fail score, and it's the reason this rule is better
than the count-threshold I originally proposed. Both people had to change.

### The trap this created, and how it's solved

"Braking makes the car slower but it still crashes" is not free. A slower car covers less
ground, so it would arrive **late** and miss her by accident.

The fix is that `PathScenarioActor` back-solves the start position from the actor's **actual**
speed. A braked car simply starts closer to the junction and still reaches the marker at
exactly `impactTime`. **Braking changes the severity, never the timing.**

Practically: `IncidentVehicle.PlannedSpeedToImpact` returns `brakedSpeed` when the player has
applied that intervention, and the base class does the rest.

> **⚠️ CONFIRM BEFORE PART 2:** every number in C.1, C.2 and C.4. Especially `impactTime`,
> car speed, and whether 100 m of approach road is realistic for Darryl's layout. If the road
> has to be shorter, we lower the car speed and/or the lead time and everything else follows.

---

# PART D — Every script, and what talks to what

## D.1 The dependency picture

Read this top to bottom. Each row only knows about the rows above it.

```
LAYER 1 — THE CLOCK  (knows about nothing)
   ScenarioRunner ......... owns ScenarioTime, ticks actors, resets, seeks
   ScenarioActor .......... abstract base class every crash object inherits from
   ScenarioPath ........... a line of waypoints you can query by distance

LAYER 2 — THE MEMORY  (knows about nothing)
   InterventionState ...... which of the 4 hazards has the player fixed?
   EvidenceLedger ......... which hazards has the player examined?

LAYER 3 — THE ACTORS  (know about layers 1 and 2)
   PedestrianVictim ....... FSM. Walks a path. Reads InterventionState to decide.
   IncidentVehicle ........ FSM. Drives a path. Reads InterventionState to decide.
   ImpactSensor ........... trigger on the bumper, reports contact
   DamageSwapper .......... swaps intact mesh for damaged mesh
   VehicleLights .......... turns the headlights on/off

LAYER 4 — THE VIEW  (knows about layer 1)
   CameraDirector ......... one method: Activate(CameraId)
   PovLook ................ mouse-look with clamps, for POV cameras
   FollowPositionOnly ..... helper: copies a bone's position but not its rotation

LAYER 5 — THE INPUT  (knows about layers 2 and 4)
   IInteractable .......... the contract: Prompt / IsAvailable / Focus / Interact
   PlayerInteractor ....... one raycast, routes to whatever it hits
   Highlighter ............ swaps materials to show "you can click this"
   HazardInteractable ..... a hazard or red herring. Implements IInteractable.
   NpcSubject ............. "talk to me / enter my eyes". Implements IInteractable.

LAYER 6 — THE CONDUCTOR  (knows about everything)
   ScenarioDirector ....... the phase FSM. The spine of the whole game.

LAYER 7 — THE SURFACE  (talked to by the director)
   HudPrompt / PhaseHud / DialogueUI / DebriefUI
   ScoreManager
```

## D.2 What each script actually does

### `ScenarioRunner`
Holds `ScenarioTime`, `TimeScale`, `IsPlaying`. Methods: `Play()`, `Pause()`,
`ResetScenario()`, `SeekTo(float t)`, `Register(ScenarioActor)`.
It never knows what an actor *is* — only that it has a `Tick()`.

### `ScenarioActor` *(abstract — you never put this on a GameObject directly)*
An **abstract class** is a template other scripts inherit from. It says "anything that
takes part in the crash must be able to do these things." It records where the object
started (so `ResetToStart()` can put it back) and declares `Tick(dt, now)` which its
children must fill in.

### `ScenarioPath`
A component you put on an empty GameObject with waypoint children. It measures its own
total length on startup, and answers one question: **"if I've travelled 34.2 metres along
this path, where am I and which way am I facing?"**

This is the crucial difference from your current waypoint code. Your `VehicleController`
only knows "step toward the next point". A path that knows its own length lets you jump to
any point instantly — which is what makes `SeekTo` possible.

### `InterventionState`
A list of which hazards the player has fixed. Two rules that matter:
- **It survives `ResetScenario()`.** If it reset, the Resolve replay would undo all your work.
- The actors ask it questions like `Has(Intervention.HeadlightsOn)`.

### `PedestrianVictim` *(FSM — Adam owns this)*
States: `Walking → Distracted → SteppingOut → (Alerted | Struck) → Aftermath`.

Transitions fire on **scenario time** (`if (now >= 5.08f)`) so they're repeatable, and
**branch on intervention flags** (`if (interventions.CorrectCount >= 3)`) so the player
can change the ending. That's the entire game mechanic, in one `if`.

### `IncidentVehicle` *(FSM — Darryl owns this)*
States: `Cruising → Approaching → (Braking | Unaware) → (Stopped | Impact) → Aftermath`.
Same shape. Note it has **no obstacle sensor** — that's the whole point.

### `IInteractable` *(an interface)*
An **interface** is a contract. It lists methods a class promises to have, but contains no
code itself. `PlayerInteractor` talks only to the contract, so you can invent ten new kinds
of clickable object and never touch the raycaster again.

### `PlayerInteractor`
Casts one ray per frame from `Camera.main`, filtered by a **LayerMask** so it can only hit
things on the `Interactable` layer. Finds the closest `IInteractable`, tells the old one it
lost focus and the new one it gained focus, and forwards left-clicks.

### `HazardInteractable`
One component for all eight clickable hazards and red herrings. Fields:
- `hazardId` — which hazard this is
- `access` — `FromPovOnly` / `FromOutsideOnly` / `Both`
- `isRedHerring`
- `freeRoamDescription` — the vague, no-spoiler line shown in Free Roam
- `debriefExplanation` — the real explanation shown at the end
- `UnityEvent onApplied` — hook up "turn the headlights on" in the Inspector, no code

Its behaviour changes by phase: in **Free Roam** clicking shows the description and records
that you examined it. In **Intervene** clicking applies the intervention and fires the event.

The `access` field is what makes your headphones design work: headphones are
`FromOutsideOnly`, her phone and every dashboard item are `FromPovOnly`, the umbrella is `Both`.

### `NpcSubject`
Sits on the pedestrian's body collider and on the car. In **Free Roam** it opens dialogue.
In **Intervene** it puts you in that person's POV. Same script, two behaviours, chosen by
asking the director what phase we're in.

### `ScenarioDirector`
The phase FSM. Everything funnels through one method, `EnterPhase(Phase next)`, which sets
**all seven** of these every single time:

1. Which camera is live
2. Is the player's `FirstPersonController` enabled
3. Cursor locked or free
4. `runner.TimeScale`
5. `runner.IsPlaying`
6. Which HUD panel is showing
7. Which interactables are available

The reason every phase change goes through one method is that it is the *only* way to avoid
the classic bug where you end up with an invisible cursor, a frozen player, and no idea which
of six scripts turned what off.

---

# PART E — Scene and prefab layout

## E.1 Folders

```
Assets/
  _OLD_SCRIPTS/          ← your four current scripts, parked here, still compiling
  _Project/
    Scripts/
      Scenario/     ScenarioRunner  ScenarioActor  ScenarioPath  ScenarioDirector
      Actors/       PedestrianVictim  IncidentVehicle  ImpactSensor
                    DamageSwapper  VehicleLights  WitnessLook
      Data/         HazardId  Intervention  InterventionState  EvidenceLedger
      Interaction/  IInteractable  PlayerInteractor  Highlighter
                    HazardInteractable  NpcSubject
      Cameras/      CameraDirector  PovLook  FollowPositionOnly
      UI/           HudPrompt  PhaseHud  DialogueUI  DebriefUI
      Scoring/      ScoreManager
      Debug/        ScenarioDebugKeys
    Prefabs/
    Art/            ← Marcus's FBX lands here
  Scenes/
    Scene_Incident.unity     ← the game. Darryl owns it.
    Sandbox_Adam.unity       ← your playground. Break whatever you like.
    Scene_Training.unity     ← added in Part 8
```

The underscore on `_Project` and `_OLD_SCRIPTS` makes them sort to the top of the Project
window, above all the imported asset folders.

## E.2 Tags and layers to create

**Layers** (Edit → Project Settings → Tags and Layers):

| Layer | For |
|---|---|
| `Interactable` | every collider the interaction raycast is allowed to hit |
| `Pedestrian` | already exists (layer 6) — physics colliders on people |
| `Vehicle` | the car's physics colliders |
| `Environment` | road, buildings, props |

**Tags:** `NPC` and `Vehicle` already exist. Add `Player` if it isn't set on your capsule.

**Physics collision matrix: skip it.** It was an optimisation, not a requirement — trigger
colliders never physically push anything anyway. If we ever see a trigger firing twice, I'll
filter it in code instead. Cross it off your list.

## E.3 The pedestrian prefab

### First — the rule for what becomes its own GameObject

A **GameObject** is a container. **Components** (a mesh, a collider, a script) are bolted onto
it. **Parenting** means when the parent moves, the child moves with it. So:

> **If the thing already has a mesh you can see** — headphones, phone, umbrella, a dashboard
> button — put the collider and the scripts **directly on that same GameObject**. One object,
> not two.
>
> **Only when there is nothing to see** do you create a separate empty GameObject: the
> "talk to her" volume around her body, the big volume around the car, and camera anchors.

On the pedestrian that means exactly **two** objects you create by hand. Everything else is
components added to something that already exists.

```
Pedestrian_Victim                      ← PREFAB ROOT (you create). layer: Pedestrian
│      PedestrianVictim.cs
│      CapsuleCollider       ← physics body, so the car's ImpactSensor can detect her
│      Animator
│
├── Character 03             ← the Hodaart model, dragged in as-is
│   └── mixamorig:Hips
│       ├── ... → mixamorig:Head
│       │   ├── Headphones           ← MESH. Add to it: SphereCollider (isTrigger),
│       │   │                           HazardInteractable (Headphones, FromOutsideOnly),
│       │   │                           Highlighter.        layer: Interactable
│       │   └── POV_Locked           ← ✦ EMPTY you create. Rotates with her head.
│       │                               Used for the locked free-roam replay.
│       └── ... → mixamorig:RightHand
│           ├── Phone                ← MESH. Add: BoxCollider (isTrigger),
│           │                           HazardInteractable (Phone, FromPovOnly),
│           │                           Highlighter.        layer: Interactable
│           └── Umbrella             ← MESH. Same treatment. Red herring, access = Both.
│
├── Interact_Body            ← ✦ EMPTY you create. Nothing to see, so it needs its own object.
│      CapsuleCollider (isTrigger) — from her FEET to her NECK. Must NOT enclose the head.
│      NpcSubject (subject = Pedestrian)
│      Highlighter           layer: Interactable
│
└── POV_Free                 ← ✦ EMPTY you create. FollowPositionOnly → target = head bone.
    └── CAM_PedestrianPOV_Free       CinemachineCamera + PovLook
```

**The collider trick that makes your headphones idea work:** `Physics.Raycast` returns the
**nearest** hit. Her body capsule stops at the neck; her head has its own small sphere. So
aiming at her head hits the sphere (→ headphones) and aiming anywhere else hits the capsule
(→ talk to her). If the capsule is left at its default full height it swallows the head and
you will never be able to click the headphones.

**Why two POV anchors:**
- `POV_Locked` is a child of the head **bone**, so it turns as she turns. Used for the
  free-roam replay — the camera is stuck looking down at her phone and **you cannot look up
  at the car.** That is the whole narrative point of the shot.
- `POV_Free` copies only her head *position*, never her rotation, so mouse-look on top of it
  is smooth. Used during Intervene, where you need to look down at the phone and back up.
  Parent a look-around camera straight to an animated bone and the animation fights the
  mouse — it's unusable.

**The collider trick that makes your headphones idea work:** the body capsule stops at the
neck; the head has its own sphere. `Physics.Raycast` returns the **nearest** hit, so aiming
at her head hits the sphere (headphones) and aiming anywhere else hits the capsule (talk to
her). Get the capsule height wrong and you'll never be able to click the headphones — this
is the #1 thing to check in Part 5.

**Why two POV anchors:**
- `POV_Locked` is a child of the head bone, so it rotates as she moves. Used for the Free
  Roam replay — the camera is stuck looking down at her phone and **you cannot look away**.
  That's the entire narrative point.
- `POV_Free` copies only her head *position*, never her rotation, so mouse-look on top of it
  is smooth and predictable. Used during Intervene, when you need to look down at the phone
  and then back up.

## E.4 The car prefab

```
Vehicle_Incident                         layer: Vehicle
│   IncidentVehicle.cs
│   DamageSwapper.cs
│
├── Body_Intact       (Pack_FREE_Cars mesh — swap for Marcus's later)
├── Body_Damaged      (disabled at start)
├── Interior          ← YOUR MODEL goes here. Placeholder box for now.
│   ├── Dashboard, seats, steering wheel
│   ├── Interact_HeadlightButton   Interactable, FromPovOnly, hazard
│   ├── Interact_BrakePedal        Interactable, FromPovOnly, hazard
│   ├── Interact_DrinkBottle       Interactable, FromPovOnly, hazard (disabled by default)
│   ├── Interact_WiperStalk        Interactable, FromPovOnly, RED HERRING
│   └── Interact_Radio             Interactable, FromPovOnly, RED HERRING
├── Driver (Hodaart character, seated pose)
├── Headlights (2 × Spot Light, disabled at start)   ← VehicleLights.cs
├── ImpactSensor      BoxCollider (isTrigger) on the front bumper
├── Interact_Car      layer: Interactable, big trigger box around the car
│                     NpcSubject (subject = Driver)
├── POV_DriverLocked  (empty at eye height, fixed forward)   ← CinemachineCamera
├── POV_DriverFree    (same position)  CinemachineCamera + PovLook (clamped)
└── POV_Passenger     (empty in the passenger seat)  CinemachineCamera + PovLook (clamped)
```

Everything car-related lives in one prefab. That means Adam can build and test the whole
car in `Sandbox_Adam.unity` and hand Darryl a single prefab to drop into the real scene.
This is the main thing that stops you from fighting over `Scene_Incident.unity` in git.

**The driver is a child of the car** — so he moves with it automatically, and his POV camera
is rock-steady with no head bob. That solves the nausea problem for free.

## E.5 The scene

```
=== SYSTEMS ===
    ScenarioRunner · ScenarioDirector · CameraDirector
    InterventionState · EvidenceLedger · ScoreManager
=== CAMERAS ===
    Main Camera  (CinemachineBrain — this is the ONLY real camera in the game)
    CAM_BirdsEye · CAM_PlayerFP · CAM_Resolve
=== PLAYER ===
    PlayerCapsule (CharacterController, FirstPersonController, PlayerInteractor)
      └── PlayerCameraRoot
=== INCIDENT ===
    Path_Vehicle    (waypoint children)
    Path_Pedestrian (waypoint children)
    Vehicle_Incident   ← prefab
    Pedestrian_Victim  ← prefab
=== AMBIENT ===
    NavMeshSurface · ambient pedestrians · dog (Part 8)
=== ENVIRONMENT ===
    Darryl's level. All static-flagged for lightmap baking.
=== UI ===
    Canvas → HudPrompt · PhaseHud · DialoguePanel · DebriefPanel
```

> **The single most important Cinemachine fact:** there is only **one real Unity `Camera`**
> in the whole game, the one with `CinemachineBrain` on it. Everything named `CAM_*` is a
> `CinemachineCamera`, which is not a camera — it's a set of instructions telling the real
> camera where to stand. **All raycasts come from `Camera.main`.** Beginners lose an entire
> day to this.

---

# PART F — Build order: 8 parts

Each part ends with something you can press Play and verify. **Do not start a part until the
previous part's test passes.** I'll write the scripts; each part lists exactly what you do in
the Unity editor.

---

## PART 1 — The clock *(~45 min)*

**Goal:** prove that a cube can move along a path and land in the identical spot on every replay.

**Scripts I write:** `ScenarioRunner`, `ScenarioActor`, `ScenarioPath`, `ScenarioDebugKeys`,
plus a throwaway `TestActor` that just follows a path.

**What you do in Unity:**
1. Open `Sandbox_Adam.unity` (create it: File → New Scene → Basic URP → save as that name).
2. Create empty GameObject `SYSTEMS`. Add `ScenarioRunner` and `ScenarioDebugKeys` to it.
3. Create empty `Path_Test`. Add `ScenarioPath`.
4. Create 4 empty children under it, name them `WP0`–`WP3`, drag them apart in the Scene view.
5. Select `Path_Test` → in the Inspector, set `Points` size to 4 → drag WP0–WP3 into the slots.
   **In order.** Out of order = a zigzag path.
6. Create a Cube. Add `TestActor`. Drag `Path_Test` into its `Path` field. Set `Speed` to 5.

**Test:** Press Play. Press `P` to play, `R` to reset, `1` to seek to 3.0 s.
- ✅ Cube moves smoothly along the cyan gizmo line.
- ✅ Press `R` then `P` ten times — it reaches the same place at the same clock reading every time.
- ✅ Press `1` — it jumps instantly to exactly where it was 3 seconds in. No visible loading.

---

## PART 2 — The crash *(~2 hours)*

**Goal:** a capsule and a box collide at exactly the same scenario time every single run.

**Scripts I write:** `PedestrianVictim`, `IncidentVehicle`, `ImpactSensor`, `Intervention`,
`InterventionState`, `HazardId`.

**What you do in Unity:**
1. Create an empty called `ImpactMarker` at (0, 0, 0). This is the single point everything
   else is measured from.
2. Build `Path_Vehicle` — waypoints coming down the side road, **through** the marker and a
   bit past it. Add the corner if you want; the turn is just two more waypoints.
3. Build `Path_Pedestrian` — waypoints crossing the road, through the marker and past it.
4. A Capsule for her (`PedestrianVictim` + CapsuleCollider), a Cube for the car
   (`IncidentVehicle` + a child cube with a trigger BoxCollider + `ImpactSensor`).
   **No character models yet. Grey shapes only.** Art comes much later.
5. Set the speeds and `impactTime` from the Part C table. **You do not position the actors** —
   they place themselves. If a path is too short, the Console will say so by exactly how much.

**Test:**
- ✅ The impact time printed in the Console is the same to two decimal places, ten runs in a row.
- ✅ `SeekTo(impact + 2)` gives you a frozen aftermath: car stopped past the crossing,
  capsule beside it.
- ✅ Manually tick a couple of interventions in the `InterventionState` Inspector, press `R`
  then `P` — **no collision**. This is the branching working.

---

## PART 3 — Cameras and phases *(~2 hours)*

**Goal:** walk the entire game loop, start to finish, with keyboard shortcuts and no content.

**Scripts I write:** `CameraDirector`, `PovLook`, `FollowPositionOnly`, `ScenarioDirector`,
a placeholder `PhaseHud`.

**What you do in Unity:**
1. Main Camera → Add Component → `CinemachineBrain`.
2. Create the `CAM_*` objects (GameObject → Cinemachine → Cinemachine Camera). Position them.
3. Drag the Starter Assets `PlayerCapsule` prefab in. Position at (6, 0, −6).
4. `CAM_PlayerFP` → Tracking Target = `PlayerCameraRoot`, Body = Hard Lock To Target,
   Aim = Same As Follow Target.
5. On `CameraDirector`, fill the array: one row per camera, pick the `CameraId` from the
   dropdown and drag the matching `CAM_*` object in.
6. On `ScenarioDirector`, drag in the runner, the camera director, the player capsule.

**Test:** press `1`–`7` to jump between phases.
- ✅ Observe: bird's-eye, crash plays, you can't move.
- ✅ Free Roam: you're on the ground, walking, the crash is frozen mid-aftermath.
- ✅ Intervene: it cuts to 5s before impact, everything crawls, **you still walk at normal speed.**
- ✅ Cursor is locked when it should be and free when it should be — every single time.

> **This is the moment the game becomes real.** Everything after this is filling it in.

---

## PART 4 — Interaction *(~2 hours)*

**Goal:** look at a thing, see its name, click it, something happens.

**Scripts I write:** `IInteractable`, `PlayerInteractor`, `Highlighter`, `HazardInteractable`,
`NpcSubject`, `HudPrompt`.

**What you do in Unity:**
1. Create the `Interactable` layer.
2. Make a bright emissive `M_Highlight` material (URP/Lit, Emission on, orange).
3. Put three test cubes on the `Interactable` layer with `HazardInteractable` + `Highlighter`.
4. `PlayerInteractor` on the PlayerCapsule → set `Interactable Mask` to **only** `Interactable`.
5. Canvas → a crosshair Image and a TextMeshPro text at the bottom. `HudPrompt` on the Canvas.

**Test:**
- ✅ Looking at a cube makes it glow and shows "Examine phone" at the bottom.
- ✅ Looking away removes both — and the cube goes back to its *exact* original material.
- ✅ Clicking prints to the Console.
- ✅ Looking at the floor or a wall does nothing at all. (If it does, your mask is wrong.)

---

## PART 5 — Free Roam content *(~3 hours)*

**Goal:** the full investigation loop with real characters.

**Scripts I write:** `DialogueSequence`, `DialogueUI`, `WitnessLook`, plus the passenger-seat
and POV-replay flows in the director.

**What you do in Unity:**
1. Swap the grey capsule for a Hodaart character. Swap the cube for a `Pack_FREE_Cars` car.
2. Build the pedestrian prefab exactly as in section E.3 — **especially the two colliders**.
3. Build the car prefab as in E.4, with a placeholder box interior.
4. Type the dialogue lines into the `DialogueSequence` arrays in the Inspector.
5. Write the no-spoiler descriptions for all 8 hazards. *(These matter — "her phone is
   unlocked and the screen is still on" tells the player something without handing them the
   answer.)*

**Test:**
- ✅ Aim at her head → "Headphones" + description. Aim at her chest → "Talk to her".
  **This is the collider test. Get it working before moving on.**
- ✅ Dialogue advances, and the last page offers "See what she saw".
- ✅ That replay is locked on her phone screen — you physically cannot look up at the car.
- ✅ Aiming at the car puts you in the passenger seat facing the driver; you can look around
  the dash; Esc puts you back outside standing where you were.

---

## PART 6 — Intervene *(~2 hours)*

**Goal:** the actual game.

**Scripts I write:** POV entry/exit, phase-aware `IsAvailable`, countdown HUD, hazard counter.

**What you do in Unity:**
1. Wire the `onApplied` UnityEvents in the Inspector — e.g. headlight button → `VehicleLights.TurnOn()`.
2. Set every hazard's `access` field correctly (this is where the design lives).
3. Add the countdown and "HAZARDS 0 / 4" to the Canvas.

**Test:**
- ✅ Walk to her, press E, you're in her eyes, look down, click the phone — she pockets it.
- ✅ Exit, aim at her head from outside, click — headphones come off.
- ✅ Walk toward the car (it's coming at you), press E, click the headlight button — **the road
  lights up.** Click the brake — the car visibly slows.
- ✅ All four → the counter hits 4/4 and it auto-advances.
- ✅ Do nothing → the countdown hits zero and the crash happens again.

---

## PART 7 — Resolve, Debrief, Score *(~2 hours)*

**Scripts I write:** `ScoreManager`, `DebriefUI`, `HazardInfo`, the Resolve phase.

Resolve is nearly free: seek back to `impact − 5`, set `TimeScale = 1`, activate `CAM_Resolve`,
play. Because `InterventionState` survives the reset, your changes are baked in from the start
of the replay — the headlights are on the whole way, she never had the phone out. That's the
"what if" shot.

**Test:**
- ✅ Fix 4 → watch her stop at the kerb, car slides past → Excellent.
- ✅ Fix 2 → watch the crash happen anyway → Try again.
- ✅ Retry resets everything and puts you back at Observe.

---

## PART 8 — Content and polish

In priority order. Stop wherever the calendar says stop:
1. Real animations (see below)
2. Ambient NavMesh pedestrians (rubric requirement — reuse your `NPCBrain`, renamed)
3. Night lighting and post-processing
4. Marcus's modelled car interior replacing the placeholder box
5. VFX (impact, highlight scan, slow-mo screen effect, prevention success)
6. `Scene_Training` with the police officer
7. **The dog.** Genuinely last.

### Animations — only three downloads needed

You already have Idle and Walking from Hodaart. The rig is **Humanoid on a Mixamo skeleton**,
so anything from mixamo.com retargets straight onto it. Download these three as FBX for Unity,
**without skin**:

| Mixamo search | For | Used in |
|---|---|---|
| "Texting While Walking" | her, before impact | Walking / Distracted states |
| "Stumble Backwards" or "Hit Reaction" | the moment of impact | Struck state |
| "Sitting Idle" | the driver | the whole game |

Then build your own Animator Controller — the Hodaart one is bool-driven (`Walking` as a
`bool`), which is a weak setup. Yours wants a `Speed` **float** with a blend tree between Idle
and Walk, plus an `OnPhone` bool and a `Hit` trigger. I'll walk you through it in Part 8.

Everything else (talking, reacting, looking around) is a nice-to-have you add if there's time.

---

# PART G — Known traps, per part

These are the specific things that will go wrong. I'm listing them now so that when they
happen you recognise them in thirty seconds instead of an evening.

## Part 1
- **Waypoints assigned out of order** → the path zigzags. Check the cyan gizmo.
- **`ScenarioPath.Rebuild()` runs in `Awake`** → if you move a waypoint during Play, the path
  doesn't update. That's intended; move them in Edit mode.

## Part 2
- **The car spawns in mid-air past the end of the road.** Expected — see the equation in C.2.
  Extend the road or lower the speed.
- **The impact never fires.** Almost always one of: the sensor collider isn't `isTrigger`;
  neither object has a `Rigidbody` (Unity needs at least one for trigger events — put a
  **kinematic** Rigidbody on the car); or the layer collision matrix has them switched off.
- **The impact fires twice.** Guard it with a `bool alreadyFired`.
- **Impact time drifts between runs.** Something is using `Update()` or `Time.deltaTime`
  instead of the runner's tick. Find it.

## Part 3
- **`using Unity.Cinemachine;` not `using Cinemachine;`.** The namespace changed in Cinemachine 3.
  Version 2 tutorials on YouTube will not compile against your project.
- **`CinemachineVirtualCamera` doesn't exist** in CM3. The class is `CinemachineCamera`.
- **Two cameras both at Priority 10** → whichever loads last wins, and it looks random. The
  director sets the winner to 20 and everything else to 0.
- **Camera blends between phases feel sluggish.** Set the Brain's Default Blend to **Cut** for
  now. Add blends at the end, deliberately, where they help.
- **The cursor.** Free Roam wants it locked and hidden; dialogue wants it visible. Every phase
  must set `Cursor.lockState` **and** `Cursor.visible`. Setting only one is the classic bug.
- **Starter Assets keeps re-locking the cursor.** `FirstPersonController` has a
  `LockCameraPosition` field and grabs the cursor in `Start`. Disable the whole component when
  you're not in Free Roam, not just the movement.

## Part 4
- **The ray hits the ground / your own capsule.** Your `LayerMask` is wrong, or the interaction
  colliders aren't on the `Interactable` layer.
- **Trigger colliders are invisible to raycasts** unless you pass
  `QueryTriggerInteraction.Collide`. I'll include it; don't remove it.
- **The highlight material never comes back.** You must cache `sharedMaterials` (the array)
  per renderer. Using `.material` instead creates a fresh instance every time and slowly
  leaks memory.
- **`GetComponent<IInteractable>()` returns null** even though the script is there — because
  the collider is on a *child*. Use `GetComponentInParent`.

## Part 5
- **You can never click the headphones.** The body capsule is too tall and is swallowing the
  head. Shrink it to end at the neck. **Turn on Gizmos and look at the wireframes.**
- **The POV camera is inside her skull, or spins wildly.** `POV_Locked` must be a child of the
  head *bone*, not the root. `POV_Free` must use `FollowPositionOnly` — if you parent it to the
  bone directly, animation rotation and mouse-look fight each other and it's unusable.
- **Her phone doesn't move with her hand.** It must be a child of the right-hand *bone*, not of
  the prefab root.
- **Exiting the passenger seat drops you somewhere odd.** Store the player's transform before
  you disable the controller; restore it after. And disabling a `CharacterController` is the
  only reliable way to teleport one — setting `transform.position` while it's enabled gets
  overridden.

## Part 6
- **You run out of time constantly.** Drop `TimeScale` to 0.08. One number.
- **The brake pedal does nothing visible.** The car needs a `deceleration` that's strong enough
  to read at 0.1× — tune it until you can *see* it slow.
- **Clicking a hazard applies it twice.** `HashSet.Add` returns `false` the second time; use
  that return value to decide whether to fire the effect.
- **Hazards are clickable during Free Roam when they shouldn't be.** `IsAvailable` must check
  the current phase *and* the `access` mode.

## Part 7
- **The Resolve replay ignores your interventions.** `ResetScenario()` is clearing
  `InterventionState`. It must not. Only the Retry button clears it.
- **The headlights are off at the start of the Resolve replay.** Each actor's `ResetToStart()`
  needs to read `InterventionState` and set its starting visual state accordingly.

## Always
- **Never move or rename assets in Windows Explorer.** Do it inside Unity, or the `.meta` file
  loses its link and every reference in your scene breaks. *(I'll move your old scripts with
  their `.meta` files together, which is safe.)*
- **Commit `.meta` files.** They're the glue.
- **Only Darryl edits `Scene_Incident.unity`.** Everyone else works in a sandbox and hands over
  prefabs. Unity scene files cannot be merged in git — two people editing one is a guaranteed
  lost afternoon.

---

# PART H — Rubric checklist

| I3E requirement | Satisfied by | Part |
|---|---|---|
| Reactive NPCs | FSMs branch on `InterventionState`; the whole ending changes | 2, 6 |
| FSM behaviour, one per member | `PedestrianVictim` (Adam), `IncidentVehicle` (Darryl), plus `ScenarioDirector`'s phase FSM. Marcus and Isaiah still need `BystanderNPC` and `GuideNPC`/dog | 2, 8 |
| Unity Navigation | Ambient crowd + the dog on `NavMeshAgent` (your `NPCBrain` already does this) | 8 |
| Raycasting | `PlayerInteractor` — every single interaction in the game | 4 |
| Unity Physics | `ImpactSensor` trigger, kinematic Rigidbody on the car, Rigidbody debris on impact, `CharacterController` on the player | 2, 8 |
| XML comments + file headers | Added in a pass before submission — *not now, per your instruction* | — |
| Version control | Already live at `adamjcc/LarpLarpSahur`. **Everyone needs visible commits.** | — |
| ReadMe: controls, bugs, FSM diagrams, answers, credits | Write as you go. Draw the FSM diagrams *before* coding each FSM. | — |

**3RT hooks:** night lighting + baked lightmaps, post-processing beyond Bloom (Vignette,
Tonemapping, Colour Adjustments, and Depth of Field inside the car), 4 VFX (impact,
highlight scan, slow-mo screen effect, prevention success), Marcus's modelled interior.

**STLD hooks:** the free-roam investigation loop *is* your circulation design. And your
sightlines do narrative work — what the bird's-eye camera shows that the driver's POV
cannot is level design carrying the lesson. Say exactly that in the GDD; it's a strong point.

**Delta Challenge:** the brief is *"pedestrians using phones while crossing, reduced situational
awareness, persuade youngsters to change behaviour."* Hazards 1 and 2 are literally that brief.
The debrief text is where you earn those marks — write it as if a first-year student is reading
it, and make her walking speed (1.2 m/s vs a normal 1.4) one of the stated findings.

---

# WHAT I NEED FROM YOU BEFORE PART 2

1. Pick a row from the road-length table in **C.2**. The recommended row needs about **42 m
   of straight road** between the end of the car's turn and the impact point. Check that
   against Darryl's layout.
2. Confirm the outcome rule in **C.4** (4/4 = prevented, 3/4 = near miss, ≤2 = crash).
3. Tell me which Hodaart character is the pedestrian and which is the driver (they're
   numbered 01–10), so the prefabs get built with the right ones from the start.

Parts 0 and 1 don't depend on any of that, so those go ahead now.
