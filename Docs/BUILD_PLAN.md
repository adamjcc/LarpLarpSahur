# BUILD PLAN — Road Safety Crash Reconstruction (Unity)

**This is the authoritative plan.** `UNITY_ARCHITECTURE_PLAN.md` is kept for its background
explanations, but where the two disagree, *this* document wins.

> **New:** `PROJECT_CONTEXT.md` holds the settled decisions, conventions and the list of traps
> that have already cost us time. Read that first if you are picking the project up cold.
>
> **Scripts moved to `Assets/Scripts/` on 11 Aug** (out of `_Project/`). All GUIDs verified
> intact, no scene references broken. Paths in older sections of this document that say
> `_Project/Scripts/...` mean `Assets/Scripts/...`.

Written 30 July 2026 · Unity 6000.3.13f1 · URP 17.3 · Cinemachine 3.1.2

---

# TABLE OF CONTENTS

- [Part A — What the game is](#part-a--what-the-game-is)
- [Part B — The three ideas that make it work](#part-b--the-three-ideas-that-make-it-work)
- [Part C — Numbers you must confirm](#part-c--numbers-you-must-confirm)
- [Part D — Every script, and what talks to what](#part-d--every-script-and-what-talks-to-what)
- [Part E — Scene and prefab layout](#part-e--scene-and-prefab-layout)
- [Part F — Roadmap: where we are and what is left](#part-f--roadmap-where-we-are-and-what-is-left)
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
   ImpactDetector ......... deterministic distance check, NOT a trigger — see D.3
   DamageSwapper .......... swaps intact mesh for damaged mesh
   VehicleLights .......... turns the headlights on/off

LAYER 4 — THE VIEW  (knows about layer 1)
   CameraDirector ......... one method: Activate(CameraId, allowLook)
   PovLook ................ mouse-look with clamps, for POV cameras
   FollowPositionOnly ..... helper: copies a bone's position but not its rotation
   PovObstructionToggle ... F hides the steering wheel inside the driver's POV

LAYER 5 — THE INPUT  (knows about layers 2 and 4)
   GameInput .............. the only place player input is read. Lives on the player,
                            beside PlayerInput, because Send Messages only reaches
                            components on its own GameObject.
   IInteractable .......... the contract: Prompt / IsAvailable / MaxDistance / Focus / Interact
   PlayerInteractor ....... one raycast, routes to whatever it hits
   Highlighter ............ swaps materials to show "you can click this"
   HazardInteractable ..... a hazard or red herring. Implements IInteractable.
   NpcSubject ............. "talk to me / enter my eyes". Implements IInteractable.

LAYER 6 — THE CONDUCTOR  (knows about everything)
   ScenarioDirector ....... the phase FSM. The spine of the whole game.

LAYER 7 — THE SURFACE  (talked to by the director)
   UIManager .............. every panel and every piece of text. Nothing else touches UI.
   DialogueSequence ....... a conversation, authored on the NPC in the Inspector
   ScoreManager ........... reads the two ledgers, writes the debrief
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

# PART F — Roadmap: where we are and what is left

## F.0 How to read this

Each part below tells you five things:

- **Delivers** — what actually exists when it's finished
- **Why it matters** — the reason it's on the list at all
- **Owner** — whose job it is
- **Effort** — rough working hours
- **Done when** — the test that proves it works

Parts are in dependency order. Anything marked ✅ is finished and tested.

---

# COMPLETED

## ✅ Part 1 — The scenario clock

**Delivers:** `ScenarioRunner`, `ScenarioActor`, `ScenarioPath`, `PathScenarioActor`.

**Why it mattered:** the game shows the same eight seconds up to five times, so the incident has
to be repeatable to the centimetre. Normal Unity code isn't — it drifts with frame rate. The
runner advances a clock in fixed 1/60 s steps and moves every actor itself, which is what makes
rewind, replay and slow motion possible at all. Everything else in the project sits on top of it.

**Done when:** a cube reset and replayed ten times lands in the identical place at the identical
clock reading. ✅

## ✅ Part 2 — The crash

**Delivers:** `PedestrianVictim` and `IncidentVehicle` state machines, `InterventionState`,
`ImpactDetector`, `HazardId`, `ScenarioSettings`.

**Why it mattered:** this is the game. Two characters choreographed to meet at a marker, with a
single `if` at each decision point that reads what the player has fixed. That `if` is the whole
mechanic.

**Done when:** the impact time is identical every run; fixing all four hazards prevents it;
fixing only some produces a crash that visibly carries the changes. ✅

## ✅ Part 3 — Cameras and phases

**Delivers:** `CameraDirector`, `PovLook`, `FollowPositionOnly`, `PlayerRig`,
`ScenarioDirector` phase machine.

**Why it mattered:** the moment it stopped being scripts and became a game. Six camera angles,
seven phases, and one method that owns every control-state change so no phase can leave you with
an invisible cursor and a frozen player.

**Done when:** you can press Enter four times and walk the whole loop start to finish. ✅

## ✅ Part 4 — Interaction

**Delivers:** `IInteractable`, `PlayerInteractor`, `Highlighter`, `HazardInteractable`,
`NpcSubject`, `EvidenceLedger`.

**Why it mattered:** one raycast that talks to an interface rather than to specific classes,
so new clickable things never require touching the raycaster. The `Access` field
(`FromPovOnly` / `FromOutsideOnly` / `Both`) is what makes the headphones reachable from outside
and the phone only from inside her eyes.

**Done when:** aiming at her head gives headphones, her chest gives dialogue, and a
`FromPovOnly` object refuses to be changed until you're in the right view. ✅

## ✅ Part 5 — Real models

**Delivers:** `CarProper` with its `Pedals/Brake` and `Signal_Lever` wired as hazards,
Hodaart characters, headphones and phone props, POV anchors, safe animator calls.

**Why it mattered:** grey boxes proved the systems; real geometry proves the *design*. It also
surfaced two real problems — props buried inside the interaction capsule, and POV pitch limits
that couldn't reach the brake pedal.

**Done when:** every hazard is visible, highlightable and clickable from its intended view. ✅

## ✅ Part 6 — Canvas UI and dialogue

**Delivers:** `UIManager`, `DialogueSequence`, a Screen-Space Canvas at 1920×1080 reference,
and the modal system that suspends movement and the interaction ray while a panel is open.

**Why it mattered:** the two OnGUI placeholders were never shippable — no fonts, no layout
control, no styling. A real Canvas means Part 12 is pure visual work with no code changes.
Dialogue is also what gives the POV replays a reason to exist: you hear someone's account, then
ask to see it.

**Done when:** talking to her opens a panel, clicking through reaches a "See what she saw"
button, the replay plays, and control comes back correctly. ✅

## ✅ Part 7 — Scoring and debrief

**Delivers:** `ScoreManager`, the debrief screen, per-hazard explanation text.

**Why it mattered:** this is where the educational payload actually lands, and it is the
single biggest source of Delta Challenge marks. Everything before it sets up a lesson; this
is the lesson. It was also nearly free to build, because `EvidenceLedger` and
`InterventionState` had been recording since Part 4 — the debrief just reads them back.

Keeping those two separate is what makes it worth reading. The report distinguishes a factor
you **never noticed** from one you **noticed and walked away from**, and says something
different about each.

**Done when:** finishing with 2/4 names exactly which two you missed and why. ✅

## ✅ Part 8 — Input Action Asset conversion

**Delivers:** `Interact`, `Back`, `Continue` and `ToggleView` actions in
`StarterAssets.inputactions`, plus a `GameInput` component the whole project reads from.

**Why it mattered:** less than it looks for the rubric — Active Input Handling is "Both", so
the old code worked. The real wins are gamepad support, one consistent look sensitivity
instead of two, and no dependency on a project setting that could change.

**The discovery that made it worth doing.** The `Look` binding carries
`InvertVector2(invertX=false), ScaleVector2(x=0.05, y=0.05)`. Two consequences nobody would
guess:

- `invertY` defaults to **true**, so `look.y` is *already* negative when the mouse moves up.
  That is why `FirstPersonController` adds it to pitch rather than subtracting. Subtracting
  inverts it twice, which is exactly the "camera goes down when I push up" bug we hit.
- The delta is pre-scaled by 0.05, so it arrives about **half** the old `Input.GetAxis` value
  — not a tenth as first assumed. `PovLook` sensitivity wants roughly **4**.

**Still on legacy input, deliberately:** `ScenarioDebugKeys`, the director's phase-jump
letters, and `CompanionBrain`'s H key. All debug-only and stripped for the build. ✅

---

# REMAINING

## Part 9 — Character animation  *(IN PROGRESS — clips being sourced)*

**Delivers:** one custom Animator Controller, three Mixamo clips, applied to both characters.

**Why it matters:** everything currently stands in a T-pose or an idle. This is the largest
single jump in how finished the game looks, and it costs less than it appears — the rig is
Humanoid on a Mixamo skeleton, so clips retarget for free.

**What to build:**

| Parameter | Type | Drives |
|---|---|---|
| `Speed` | float | blend tree: Idle ↔ Walk |
| `OnPhone` | bool | walking-while-texting variant |
| `Hit` | trigger | the moment of impact |
| `Sit` | bool | the driver, permanently on |

**Clips to download** (mixamo.com, FBX for Unity, *without skin*):
*Texting While Walking* · *Stumble Backwards* (or any Hit Reaction) · *Sitting Idle*

**⚠️ The critical setting: `Apply Root Motion` must be OFF on every Animator.** Root motion lets
the animation drive position — which would fight the path system for control and make the
collision timing drift. This is the one setting that can quietly break Part 1's determinism.

The code is already ready: `MoveAlongPath` calls `SetAnimFloat("Speed", ...)` every tick, and
`ScenarioActor` caches which parameters exist and silently skips the rest. The moment the
controller has these parameters, the existing calls start working with **no code change**.

**Also in this part:** re-parent the phone from `PhoneHolder` to `mixamorig:RightHand` and
re-tune it against the texting animation — in Play mode, then Copy Component / Paste Component
Values.

**`HitReaction` must have NO exit transition.** She holds the last frame and stays down for
the whole playback. `Animator.Rebind()` on reset returns her to Locomotion, so nothing sticks.

### Already built for this part

Three things landed early because the animation work exposed the need for them:

- **Witness mode.** Free Roam re-simulates straight through the collision to reach the
  aftermath, so the fall animation fired again on every camera change and she kept collapsing.
  `PedestrianVictim.SetWitnessMode(true)` is now called *before* the seek and suppresses the
  `Hit` trigger, so the investigation finds her standing and able to talk. Every other phase
  keeps the fall.
- **`Free Roam Stand Point`.** An empty Transform on `PedestrianVictim`. The seek leaves her
  wherever the impact put her — usually the middle of the road — so `ApplyWitnessPlacement()`
  runs *after* the seek and moves her somewhere sensible. Order matters in both directions.
- **Two phone poses.** The texting clip and the idle clip hold her arm in completely
  different places, so one fixed offset can only look right in one of them.
  `Phone Walking Position/Rotation` and `Phone Idle Position/Rotation` on `PedestrianVictim`
  swap automatically with her state.

**Owner:** Adam + Marcus · **Effort:** ~3–4 h

**Done when:** she walks while texting, stumbles on impact, the driver sits properly — and
the impact time is still identical run to run.

## Part 10 — Vehicle detail

**Delivers:** cornering (**done**), plus wheel rotation, steering-wheel rotation, brake lights,
and the damaged-car swap.

**Why it matters:** the car takes a 90° junction in the opening shot, and a car that pivots on
the spot destroys the illusion immediately.

**Already built:** `PathScenarioActor` now has `Look Ahead Distance` and `Max Turn Rate`. Set
**4** and **90** on `VEHICLE_INCIDENT`. It looks a few metres up the path rather than at the
current segment, so it leans into the bend before reaching it. Both default to 0, so people are
unaffected.

**Why it stays flexible:** the turn is derived from the path itself, so **moving waypoints needs
no retuning**. And because it only affects rotation, never position, the collision timing is
untouched — you can tune it freely without re-testing determinism.

**Still to do:** spin the four wheels from `CurrentSpeed`; rotate `Steering` from the frame's
turn rate; enable `Brake_Lights` while decelerating; model a damaged body and wire
`DamageSwapper` (**both slots must stay empty until that model exists, or the car vanishes on
impact**).

**Owner:** Darryl · **Effort:** ~2 h

**Done when:** the junction reads as a real turn, and the wheels aren't skating.

## Part 11 — Extra interactables

**Delivers:** more clickable objects that can be examined and changed but count for nothing.

**Why it matters:** more than it looks. An investigation where every clickable object is a
correct answer isn't an investigation, it's a checklist. Objects that *might* have mattered are
what make the four that did feel like a discovery. They also give the debrief something to say
about judgement, not just observation.

**Already built:** `HazardId` has 14 non-contributing entries — bag, shoes, umbrella, mirror,
seatbelt, glovebox, handbrake, wipers, radio, road sign, street light, road marking, kerb ramp,
drain cover. Tick **Is Red Herring** on the component and the player is told plainly: *"You
changed X, but it played no part in this collision."* Without that message a red herring feels
identical to a real fix and the counter silently refusing to move just reads as a bug.

**The writing is the work, not the code.** Each needs an `Examine Description` that is
interesting but not misleading. A good one gives a real detail with no verdict attached: *"The
wipers are set to intermittent. It hasn't rained all week."*

**Owner:** anyone · **Effort:** ~1 h of placement, ~1 h of writing

**Done when:** there are at least six non-contributing objects across the pedestrian, the car
and the street.

### Related: the driver's obstruction toggle

`PovObstructionToggle` on `VEHICLE_INCIDENT`. The driver sits close to the wheel, so the
steering wheel and signal stalk sit between the camera and the pedals — the brake pedal
simply cannot be clicked. **F** (or gamepad Y) hides whatever is dragged into its
`Obstructions` array, and only while the driver's POV is live. It restores them automatically
on the way out, or you would climb out of the car and find the steering wheel missing for the
rest of the game.

Moving the camera back would have fixed the raycast and ruined the framing. This fixes both,
and reads as a deliberate feature: an analyst stripping away the parts of a reconstruction
that are in the way.

## Part 12 — UI polish  *(IN PROGRESS)*

### Already built

- **Phase banner stays up inside the car and inside a POV.** It is hidden completely during
  a POV *replay* so nothing sits over the shot, and during the debrief because that panel
  carries its own headline.
- **The hint and the controls are two separate lines.** `PhaseHint` says what this part of
  the game *is*; `controlsHint` lists which keys currently do something, including `[F]` only
  when the steering wheel is genuinely in the way. Splitting them stops the hint turning into
  a wall of bracketed keys.
- **Traffic lights on the banner.** Green while the incident is playing back, yellow while
  investigating, red while the countdown runs, all off during a POV replay. Only the `_ON`
  objects are touched; the `_OFF` bulbs sit underneath permanently.
- **The countdown kicks on every whole second** — a quick scale-up and a small tilt that
  alternates direction so it rocks rather than flicking the same way each time. Tunable via
  `pulseDuration`, `pulseScale` and `pulseTilt`.
- **Hazard counter split** into numbers (`2/4`) and label (`HAZARDS FOUND`).

### Still to do

**Delivers:** a TMP font asset, a colour palette, panel artwork, and fade transitions.

**Why it matters:** default Arial and flat black boxes read as unfinished no matter how good the
systems underneath are. This is the cheapest possible improvement to how the project is
*perceived*, and Part 6 built the Canvas specifically so this is pure styling with no code.

**What to do:** download a free font (Inter, Barlow and Manrope all suit this), create a **TMP
Font Asset**, and set it on every text object. Pick three or four colours and use them
consistently. Replace the flat panel Images with a 9-sliced sprite. Add `CanvasGroup` fades so
panels don't pop.

**Owner:** Isaiah (it's the same skill set as the DUX kiosk) · **Effort:** ~2–3 h

**Done when:** no default Unity font remains anywhere.

## Part 13 — Environment and atmosphere

**Delivers:** night lighting with baked lightmaps, post-processing, four VFX, ambient NavMesh
pedestrians, the guide/dog NPC, and the training-room scene.

**Why it matters:** this is where most of the 3RT marks live, and where the two remaining
required FSMs live. It's listed last because none of it blocks anything else — but the two
NPCs are a **rubric requirement**, not a nice-to-have. Every team member needs one FSM.

**Sub-items:**

| Item | Owner | Rubric |
|---|---|---|
| `BystanderNPC` FSM (NavMesh witness) | Marcus | I3E — one FSM per member |
| `GuideNPC` FSM (the stray dog) | Isaiah | I3E — one FSM per member, and the "dynamic NPC" the tutor asked for |
| Ambient NavMesh crowd | Darryl | I3E — Unity Navigation |
| Night lighting + baked lightmaps | Marcus | 3RT |
| Post-processing beyond Bloom | Marcus | 3RT |
| Four VFX | Marcus | 3RT — one per member |
| Modelled car interior | Adam | 3RT |
| Training room scene | Darryl | 3RT modular interior, STLD |

**Effort:** this is the largest remaining block by far. Start the two NPC FSMs early — they are
required, and they are small.

## Part 14 — Documentation and submission

**Delivers:** the finished ReadMe as PDF, FSM diagrams as images, credits, a tested standalone
build, and every module's deliverables.

**Why it matters:** every rubric requires it, and it is the one part that cannot be rushed on
the final day.

**Standing actions, do these now rather than later:**

- **Record the `Pack_FREE_Cars` licence.** It has no source URL and no licence recorded. Both
  I3E and 3RT require it. "It was free" is not a licence.
- **Keep `Docs/I3E_ReadMe.md` current** as features land, and clear the `🔲` markers as you go.
- **Build early and often.** A first build attempted on 12 August is how projects die.
- **Everyone needs visible Git commits.** It's assessed.

**Owner:** everyone · **Effort:** ~1 day, spread out

---

## F.1 Suggested order

Parts 7 and 9 are the two that most change how finished the game feels, and neither blocks the
other. Part 13's two NPC FSMs are a hard rubric requirement, so **start those in parallel with
whatever else is happening** — they're small, and they belong to Marcus and Isaiah, who aren't
blocked by the Unity gameplay work.

```
Adam     ──► 9 (animation) ──► 11 (extra interactables) ──► 14
Darryl   ──► 10 (vehicle)  ──► 13 ambient crowd + training room
Marcus   ──► 13 lighting / VFX / interior ──► BystanderNPC FSM
Isaiah   ──► DUX kiosk ──► 12 (UI polish) ──► GuideNPC FSM
```

**Two things that are late and shouldn't be.** `Pack_FREE_Cars` still has no recorded source
or licence, and the Bystander and Guide FSMs are a rubric requirement rather than polish.
Isaiah has started `CompanionBrain` (the dog) with `FollowingPlayer` / `LeadingToClue` states
— its `FindUnexaminedClue()` has a TODO to ask the ledger instead of cycling an array, and
`EvidenceLedger.Has(id)` and `.All` are ready for it.
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

## Parts 6–9 — traps we actually hit

These all cost real time. Every one is fixed, but the shape of them repeats.

- **Empty GameObjects inside a Canvas default to a 100×100 rect anchored to the centre.**
  A child's anchor is relative to its *parent's* rect, not the screen — so "top-centre" on a
  child of an unstretched container anchors to the top of a small box floating in the middle
  of the screen. Set every container to full stretch first.
- **TextMeshPro can only draw glyphs in its font atlas.** The default LiberationSans SDF has
  no tick, cross, middle dot or em dash. They render as empty boxes. Every runtime-generated
  string in `ScoreManager` is plain ASCII for exactly this reason.
- **`Input.GetKeyDown` stays true for the whole frame, and script execution order is
  undefined.** Closing a panel with Q also ejected you from the passenger seat; clicking
  "Leave" also re-triggered the person behind the panel. `UIManager.ModalBlockingInput` stays
  true for the rest of the frame a panel closes on, which is what fixes both.
- **`PovLook` lives on the camera, not the player**, so `SetControlEnabled(false)` never
  reached it and the camera kept turning behind an open dialogue. It now bails whenever the
  cursor is unlocked.
- **`StarterAssetsInputs` re-applies its own `cursorLocked` on window focus.** Leaving that
  out of step with the real cursor meant alt-tabbing back in silently freed the mouse.
  `PlayerRig.SetCursorLocked` keeps both in sync.
- **Unity's positive X-rotation is nose-DOWN.** A field called `minPitch = -70` was therefore
  the *up* limit, not the down one — which is why the brake pedal at 58° below the eyeline was
  unreachable behind a 45° cap. `PovLook`'s fields are named for what they do now.
- **Props hidden by `Hide When Applied` stay hidden after Retry** unless something puts them
  back. `RetryFromStart` calls `RestoreVisual()` on every hazard, searching inactive objects
  included — the ones you need are precisely the ones you cannot see.
- **A seek re-runs the whole incident inside one frame**, so anything triggered during the
  crash fires again on every camera change. That is what made the pedestrian collapse
  repeatedly, and why witness mode has to be set *before* the seek.

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

---

# APPENDIX — As-built values

Recorded from the working scene so they survive a model re-import or a lost prefab override.
**Update this whenever you re-tune something.**

## Car (`CarProper`, instance scale 0.41)

All positions are **local to `VEHICLE_INCIDENT`**, whose scale is 1. The car's own 0.41 is
already accounted for.

> ⚠️ Keep the cameras as children of **`VEHICLE_INCIDENT`**, never of `CarProper`. Parented
> under the car they would inherit the 0.41 and every number below would need dividing by it.

| Part | Position | Notes |
|---|---|---|
| `Steering` | `0.419, 1.120, 0.681` | +X confirms driver-on-the-right |
| `Pedals` / `Brake` | `0.315, 0.454, 1.086` | 45° below the driver's eyeline, 1.12 m away |
| `Front_Lights` | `0.000, 0.912, 1.784` | +Z confirms nose-forward |
| `Interior` | `0.000, 0.923, -0.151` | |
| `Windows` | `0.000, 1.595, -0.312` | roof line |
| `FR_Door` | `0.843, 1.207, 0.782` | car is ~1.69 m wide |
| `CAM_DriverPov` | `0.42, 1.25, 0.30` | rotation `0,0,0` |
| `CAM_PassengerSeat` | `-0.42, 1.25, 0.30` | rotation `0,40,0`, Max Look Right ≥ 110 |
| `FrontBumper` | `0, 0.5, 1.95` | verify visually against the nose |
| Headlights | `±0.55, 0.75, 1.85` | Spot, Range 40, Angle 50, Intensity 8 |
| `Interact_Car` | Center `0,0.8,0`, Size `2.0,1.8,4.4` | |

## Pedestrian (`Character_Pedestrian`, scale 1, ~1.7 m)

| Object | Parent | Values |
|---|---|---|
| `Headphones_Mesh` | `mixamorig:Head` | placed by hand |
| `Mobile Phone` | `mixamorig:Spine2` | pos `-2.02835, 0.11979, 0.92591` · rot `24.476, -180, 0` · scale `0.4` · BoxCollider centre `0,0.44,0` size `0.6,0.6,0.6` |
| `Interact_Body` | `PEDESTRIAN_VICTIM` | pos `0,0.75,0` · Capsule radius `0.28` height `1.35` (spans y 0.075–1.425, stops below the chin) |
| `POV_Anchor` | `PEDESTRIAN_VICTIM` | `FollowPositionOnly` → target `mixamorig:Head` |
| `CAM_PedestrianPov` | `POV_Anchor` | pos `0, 0, 0.12` · **rotation is driven at runtime, see below** |

## Runtime-driven values on `PedestrianVictim`

These are no longer authored on the object itself — the script pushes them, so editing the
Transform directly will just get overwritten.

| Field | Value | When |
|---|---|---|
| Pov Pitch / Yaw On Phone | `32` / `15` | while she is reading — head down, turned slightly right |
| Pov Pitch / Yaw Looking Up | `0` / `0` | once the phone is stowed |
| Phone Walking Position | `0.0608, -0.0066, 0.0705` | Walking · Distracted · Crossing |
| Phone Walking Rotation | `8.522, -64.605, 72.478` | " |
| Phone Idle Position | `-0.06606, 0.169283, 0.1135409` | every other state, including the Free Roam witness pose |
| Phone Idle Rotation | `27.208, -102.513, 99.772` | " |
| Phone scale | `0.4` | set once on the prefab, never touched by script |

To re-tune either phone pose: **enter Play mode**, drag it while that animation is running,
right-click Transform → **Copy Component**, exit Play → **Paste Component Values**, then copy
the numbers into the matching pair of fields. Tuning against the bind pose is what produced
the original wrong angle.

## Input

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| `Interact` | Left Mouse · E | A |
| `Back` | Q · Right Mouse | B |
| `Continue` | Enter · Numpad Enter | Start |
| `ToggleView` | F | Y |

`PovLook` **Sensitivity = 4** on all POV cameras. The `Look` binding pre-scales by 0.05, so
this is roughly double the old `Input.GetAxis` value, not a tenth.

The phone's odd local position is normal — Mixamo bones carry baked rotations and scales, so
local coordinates under `Spine2` do not read like world space. Re-parent it to
`mixamorig:RightHand` once the "Texting While Walking" clip is imported.
