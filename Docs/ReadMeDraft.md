# Road Ready - A

**Ngee Ann Polytechnic · Diploma in Immersive Media · Year 2.1 Integrated Project 2026**

**[DUX] Figma Wireframe · [STLD] Gameplay Walkthrough Video** \
**[DUX] Proposal Doc · [STLD] Game Design Doc**

<!-- > **🔲 BEFORE SUBMITTING — fill in every 🔲 marker in this document.**
> Then export to PDF as `I3E_<NameOfApp>_ReadMe.pdf`.
> This file is the source. Edit it here, not in the PDF. -->

|                     |                                                                   |
| ------------------- | ----------------------------------------------------------------- |
| **Team**            | Larp Larp Sahur Studios                                           |
| **Members**         | Adam, Darryl, Marcus, Isaiah                                      |
| **Engine**          | Unity 6000.3.13f1 (Unity 6.3 LTS), Universal Render Pipeline 17.3 |
| **Repository Link** | https://github.com/adamjcc/LarpLarpSahur                          |
| **Status**          | 🔲 _In development. See "Limitations and Known Issues"._          |

---

## 1. Overview

At night, on a quiet road near a campus, a student walks toward a pedestrian crossing looking
down at her phone, wearing noise-cancelling headphones. A car approaches too fast with its
headlights off. They meet at the crossing.

You are a crash detective inside a reconstruction of the incident. You watch it happen from
above. You walk through the frozen aftermath. You hear both people's accounts and see the
final seconds through each of their eyes. Then you are sent back to a few seconds before
impact — with time crawling at a sixteenth of normal speed — and given one chance to change
what happens.

The lesson the game is built to teach is that **road accidents are rarely one person's
mistake.** Four separate small failures had to line up for this collision to occur: she was
looking at her phone, she could not hear, he was speeding, and he had no headlights. Fix
three of the four and it still happens — with the headlights on, or with her phone away, but
it still happens. Only fixing all four prevents it. The game will not let you blame one
person, because the simulation itself refuses to agree with you.

**Road Ready is one half of a two-part experience.** The Unity game is the deep, slow,
personal half: ten minutes, one incident, examined properly. The companion **DUX kiosk** is
the fast public half — a walk-up screen posing short road-safety dilemmas, showing you the
consequence, and comparing your answer against everyone else who has played. Together they
cover both ends of the same behaviour: the quick gut reaction, and the long look at what that
reaction actually costs.

---

## 2. The Problem We Were Set

**Delta Challenge 2026: Guardians of Tomorrow — Track B.**

> _Background:_ A significant number of pedestrians, particularly tertiary students, use
> mobile phones while walking and crossing roads, reducing their situational awareness and
> increasing the risk of near-miss incidents and accidents.
>
> _Brief:_ Propose a project that persuades youngsters to prioritise their safety and
> encourages behavioural change.

Road safety education for this age group is usually a poster or a talk — both easy to nod
along to and then ignore. The gap is not information. Everyone already knows that looking at
your phone while crossing is a bad idea. The gap is that nobody believes it applies to _them_.

So the design goal was never "explain the rule". It was **make the player occupy the mistake,
then hand them the controls.**

Two decisions come straight out of that:

- **The pedestrian's POV replay is a locked camera.** You see what she saw — her phone screen
  filling the frame — and you physically cannot turn your head to look for the car. The camera
  refuses. That restriction _is_ the argument.
- **She walks at 1.2 m/s instead of a normal 1.4 m/s.** Phone users measurably walk slower and
  scan less. It is a small number nobody consciously notices, and it is called out in the
  debrief.

The scenario is set in Singapore and follows **left-hand traffic**: the driver sits on the
right, the car keeps to the left-hand lane, and she is struck by a car approaching from her
**right** — the direction a pedestrian here is supposed to check first. The level geometry
teaches the lesson before any text does.

---

## 3. Design Process

### 3.1 User stories

- As a **tertiary student**, I want to see the consequences of a distraction I recognise in
  myself, so that I take it seriously rather than dismissing it as something that happens to
  other people.
- As a **player**, I want to investigate the scene at my own pace before being asked to act,
  so that I am not rushed into guessing.
- As a **player**, I want to see the incident from more than one viewpoint, so that I
  understand what each person could and could not perceive.
- As a **player**, I want my choices to visibly change what happens, so that the connection
  between cause and outcome is undeniable.
- As a **player who got it wrong**, I want to be told exactly which factors I missed and why
  they mattered, so that I learn something rather than just failing.
- As an **educator or exhibitor**, I want a session to last only a few minutes, so that it
  works in a classroom or at a public event.

### 3.2 How we arrived at the design

**We started with a puzzle and ended with an argument.** The first concept was a
crash-investigation game where you collected clues and named the guilty party. We dropped the
"name the guilty party" part once we realised it taught the opposite of what the brief asked
for — it lets the player pick a villain and walk away feeling safe. Requiring **all four**
fixes to prevent the collision is what turns a quiz into a point.

**Investigation and intervention are deliberately separated.** The player explores the
aftermath with no timer at all, and only starts the countdown when they say they are ready.
Rushing the investigation would push people into guessing, and a wrong guess made under
pressure teaches nothing.

**Slow motion, not pause.** The intervention runs at a sixteenth speed rather than frozen,
because a ticking clock keeps the moment tense while still being generous — three seconds of
scenario time stretches to about fifty real ones. Crucially the player moves at full speed
while the world crawls, so the pressure never becomes clumsiness.

**Every fix shows its effect, even when you fail.** A player who only sorts out the driver
watches the crash happen _with the headlights on and the car visibly slower_. That near-miss
is more persuasive than a fail screen, because it shows how close it came and exactly what
was still missing.

**One incident, done properly.** We cut a second and third scenario early. A single collision
that can be examined from four angles and replayed with your changes is worth more than three
shallow ones.

### 3.3 Related documents

| Document                               | Location                          |
| -------------------------------------- | --------------------------------- |
| Full build plan and architecture       | `Docs/BUILD_PLAN.md`              |
| Background notes on the engine systems | `Docs/UNITY_ARCHITECTURE_PLAN.md` |
| Asset credits and licences             | `Docs/CREDITS.md`                 |
| Level design GDD (STLD)                | 🔲                                |
| DUX kiosk prototype (Figma)            | 🔲                                |
| Bubble diagram / flow / rhythm         | 🔲                                |

---

## 4. How It Plays

```
OBSERVE      The crash plays out from above, at full speed. Watch it as many
             times as you like.
    ↓
FREE ROAM    The clock freezes two seconds after impact. Walk the scene.
             Examine her phone, her headphones, the car. Talk to both people
             and ask to see the final seconds through their eyes.
             No timer. Continue when you are ready.
    ↓
INTERVENE    Rewound to three seconds before impact, running at 0.06× speed.
             You have about fifty real seconds. Walk up to either person,
             step into their view, and change what you can reach.
    ↓
RESOLVE      The same seconds replay at full speed with your changes applied
             from the start. Watch what you caused.
    ↓
DEBRIEF      Your score, and a factor-by-factor explanation of what mattered.
```

---

## 5. Controls

### 5.1 Player controls

| Input                             | Action                                               |
| --------------------------------- | ---------------------------------------------------- |
| **W A S D**                       | Walk                                                 |
| **Mouse**                         | Look                                                 |
| **Left Shift**                    | Sprint                                               |
| **Space**                         | Jump                                                 |
| **Left Mouse Button**             | Examine / interact with whatever the crosshair is on |
| **Q** _or_ **Right Mouse Button** | Step back out of a seat or a POV                     |
| **Enter**                         | Continue to the next phase                           |

The crosshair is the aim point in every view, including inside the car and inside another
person's eyes. There is no separate cursor mode to learn.

> **Note on Escape.** Escape also backs you out, but **inside the Unity Editor** it leaves the
> mouse cursor visible until you click the Game view again. That is the Editor forcibly
> releasing the cursor lock, not a bug in the game — it does not happen in a built player.
> `Q` is the intended key.

### 5.2 Developer / debug controls

🔲 _Remove this section from the submitted PDF if the debug scripts are stripped from the
final build. If they are left in, keep it — the markers will use it._

| Key                             | Action                                                                                           |
| ------------------------------- | ------------------------------------------------------------------------------------------------ |
| `Z` `X` `C` `V` `B` `N` `M` `K` | Jump to phase: Observe, Free Roam, her POV, his POV, passenger seat, Intervene, Resolve, Debrief |
| `P` / `R`                       | Play-pause / reset the simulation                                                                |
| `2` `3` `4`                     | Seek to impact / to Intervene start / to the aftermath                                           |
| `I`                             | Enter Intervene mode (rewind + slow motion + play)                                               |
| `[` `]`                         | Cycle simulation speed presets                                                                   |

---

## 6. Features

### 6.1 Implemented

- **Watch the same crash from four angles** — overhead, her eyes, his eyes, and on foot — with
  every replay identical down to the centimetre.
- **Instant rewind.** Jumping back into the incident is seamless, with nothing recorded and no
  animation reversed.
- **Slow motion that does not slow you down.** The world runs at 0.06× while you keep walking
  and looking at full speed.
- **Investigate before you act**, with no timer during the investigation phase.
- **Examine anything you can see**, with contextual highlighting and prompts.
- **Reach-restricted interventions.** Some things can only be changed from inside a person's
  view, others only from outside. Her headphones are addressed from the outside because nobody
  can see their own ears; her phone must be reached from behind her eyes.
- **Branching outcomes.** Every fix visibly changes the replay, whether or not you prevented
  the collision.
- **A countdown that is honest**, calibrated to the exact frame contact occurs.
- **Self-validating level setup.** Actors position themselves from a single impact marker, and
  the project reports in metres if a road is too short for the incident to fit.

### 6.2 Not yet implemented

🔲 _Update as the build progresses. Anything still listed here on submission day belongs under
"Limitations" instead._

- Final character models, animator controllers and Mixamo animation clips
- Dialogue system and the "see what they saw" button that launches each POV replay
- Scoring, grading and the educational debrief screen
- Bystander NPC (Marcus) and guide / stray-dog NPC (Isaiah)
- Ambient NavMesh crowd
- Optional fifth hazard: an open drink container in the car
- Modelled car interior and police training room
- Night lighting, baked lightmaps, post-processing and VFX
- Separate training-room scene with the briefing officer

---

## 7. AI Implementation and FSM Diagrams

Every character in the incident runs on a finite state machine — a fixed list of states, with
clearly defined conditions for moving between them. We chose plain `enum` + `switch` machines
over a more elaborate pattern for one reason: each team member owns one, and each has to be
able to read, diagram and defend their own.

Two properties make them robust:

- **Transitions fire on distance, not time.** A character asks "how far am I from the collision
  point?" rather than checking a stopwatch, so the logic survives the level designer moving the
  road around. There are no magic timestamps to retune.
- **Branches read the player's choices.** A single `if` at each decision point is what turns an
  intervention into a different ending.

### 7.1 `PedestrianVictim` — the student _(Adam)_

```
                    ┌──────────────┐
        START ─────►│   Walking    │  approaching, head up
                    └──────┬───────┘
                           │ 1s in, and her phone is still out
                           ▼
                    ┌──────────────┐
                    │  Distracted  │  head down on the phone
                    └──────┬───────┘
                           │
        both routes ───────┤ reaches the kerb (1.75 m from impact)
                           ▼
                    ┌──────────────┐
                    │    AtKerb    │  ◄── THE DECISION POINT
                    └──┬────────┬──┘
         all 4 fixed?  │        │  any hazard missing
                 YES   │        │   NO
                       ▼        ▼
              ┌──────────┐   ┌──────────┐
              │ Alerted  │   │ Crossing │  steps into the road
              │ (stops)  │   └────┬─────┘
              └────┬─────┘        │
                   │              ├── hit by the car ──► ┌────────┐
                   │              │                      │ Struck │
                   │              │ reaches the far kerb  └───┬────┘
                   │              ▼                           │
                   └────────► ┌──────┐                        │
                              │ Safe │                        │
                              └───┬──┘                        │
                                  ▼                           ▼
                              ┌───────────────────────────────┐
                              │          Aftermath            │
                              │  stands as a witness for the  │
                              │  free-roam investigation      │
                              └───────────────────────────────┘
```

**Implementation:** she walks a fixed route at a constant 1.2 m/s. `AtKerb` is a single-frame
decision state that reads what the player has fixed and sends her one of two ways — that
branch is the core of the whole game. She can only be struck while actually out in the road,
so she is never "hit" standing safely on the pavement. On every replay she re-applies the
player's changes from the very start, which is why the Resolve replay shows she never had her
phone out at all.

### 7.2 `IncidentVehicle` — the car _(Darryl)_

```
                    ┌──────────────┐
        START ─────►│   Cruising   │  50 km/h in a 40 zone, headlights off
                    └──────┬───────┘
                           │ 20 m from impact
                           ▼
                    ┌──────────────┐
                    │   Reacting   │  ◄── THE DECISION POINT
                    └──┬────────┬──┘
         all 4 fixed?  │        │  any hazard missing
                 YES   │        │   NO
                       ▼        ▼
              ┌──────────┐   ┌──────────┐
              │ Braking  │   │ Unaware  │  never saw her
              └────┬─────┘   └────┬─────┘
                   │              │ reaches her
                   ▼              ▼
              ┌──────────┐   ┌──────────┐
              │ Stopped  │   │  Impact  │  damaged bodywork swapped in
              └────┬─────┘   └────┬─────┘
                   └──────┬───────┘
                          ▼
                   ┌──────────────┐
                   │  Aftermath   │
                   └──────────────┘
```

**Implementation:** the car has **no obstacle sensor**. An automatic "brake if something is in
front of me" reflex — which is how our first prototype worked — makes the collision impossible
and the timing unpredictable. Braking here is a _decision_ the driver either makes or does not.

The interesting problem was that applying the brakes should make the crash _gentler_, not make
it _miss_. A slower car covers less ground and would arrive late. The car solves this by
working out its own starting position from its actual speed, so a braked car simply begins
closer to the junction and still arrives at the same moment. **Braking changes how hard the
crash is, never whether it happens.** Only fixing all four hazards avoids it.

Damage is a swap to a pre-damaged model rather than real deformation, so it resets cleanly for
every replay.

### 7.3 `ImpactDetector` — deciding when contact happens

Not a state machine, but it is the component that arbitrates the collision, so it belongs here.

Contact is detected by a distance check driven by the simulation clock rather than by a
physics trigger. The reason is that rewinding runs hundreds of simulation steps inside a single
frame, and Unity's physics runs on its own separate schedule — a trigger would simply never
fire during a rewind, and jumping to the aftermath would show the pedestrian standing calmly
inside the car.

### 7.4 `ScenarioDirector` — the phase state machine

```
   Observe ──► FreeRoam ──────────────► Intervene ──► Resolve ──► Debrief
   (auto)         │  ▲                     │  ▲                      │
                  ▼  │                     ▼  │                      │ Retry
            PovReplay┤               inside someone's view           │
            Passenger┘                                               ▼
              Seat                                            back to Observe
```

Every phase change runs through one method that sets **all six** of: the live camera, whether
the player can move, cursor state, simulation speed, whether the simulation is running, and
where the clock sits. Funnelling it through a single place is what prevents the classic bug of
an invisible cursor, a frozen player, and no idea which script disabled what.

Stepping into someone's view during the intervention is deliberately a _flag_ rather than a
phase, because the countdown must keep running the whole time you are in there.

### 7.5 Planned

🔲 _Implement and document before submission — the rubric requires one FSM per member._

**`BystanderNPC`** _(Marcus)_ — a NavMesh-driven witness.

```
Idle ──► NoticedIncident ──► MovingToScene ──► CallingHelp ──► Returning ──► Idle
```

**`GuideNPC`** _(Isaiah)_ — the dynamic follower, planned as a stray dog that reacts to the
player's position and draws attention to overlooked evidence.

```
Idle ──► FollowPlayer ──► LeadToEvidence ──► WaitForPlayer ──► Return
```

---

## 8. Puzzle Answers

**The collision is avoided only when all four contributing factors are addressed.** Fixing
one, two or three still results in a collision — but the replay visibly carries whatever the
player _did_ fix.

### 8.1 The four contributing factors

| #   | Factor                              | Where to find it                            | Where to change it                                                                     | Effect                                       |
| --- | ----------------------------------- | ------------------------------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------- |
| 1   | **Looking at her phone**            | In her hand — examine it during Free Roam   | **Inside her view only.** Approach her during Intervene, step into her eyes, look down | She pockets it and looks up                  |
| 2   | **Noise-cancelling headphones**     | On her head — aim at her head, not her body | **From outside only.** Aim at her head and click                                       | She takes them off and hears the car         |
| 3   | **Headlights off**                  | The dashboard switch                        | **Inside the driver's view only**                                                      | Headlights come on; the road ahead lights up |
| 4   | **Speeding — 50 km/h in a 40 zone** | The speedometer and brake pedal             | **Inside the driver's view only**                                                      | The car slows noticeably                     |

### 8.2 Red herrings

Clickable and described, but they contribute nothing: her **umbrella**, the car's **wiper
stalk**, and the car **radio**.

### 8.3 Why the reach restrictions exist

Factor 2 can only be reached from _outside_ because nobody can see their own ears. Her body
carries two separate interaction zones — one stopping below the neck for "talk to her", and a
smaller one on her head for the headphones — so aiming at her head naturally selects the
headphones and aiming anywhere else selects her.

### 8.4 The verdict

- **She** stepped into the road without checking, could not see because she was reading, and
  could not hear because of the headphones.
- **He** was over the limit at night with no headlights, so he could not see her and had no
  distance in which to stop.
- **Neither one alone caused it.** Remove any single factor and the collision still happens.

---

## 9. How It Works Under the Hood

The game shows the same eight seconds up to five times, so the incident has to be **exactly
repeatable** — identical positions, identical timings, every run.

Most game code is not. It drifts with frame rate, with randomness, and with physics. So the
crash is driven by a single clock that advances in fixed steps and moves every character
itself, rather than letting each one move on its own. That one decision is what makes
everything else possible:

- **Rewinding** resets the incident and fast-forwards it in a single frame. Nothing is
  recorded and no animation is played backwards.
- **The frozen aftermath** the player explores is simply the same scene with the clock parked
  two seconds after impact.
- **Slow motion** is a multiplier on that clock alone, which is why the player keeps moving at
  full speed while the world crawls.

The crash itself is **choreographed rather than simulated**: both characters are authored to
arrive at the same point at the same moment. Physics does not cause the collision — the timing
does. This is standard practice for scripted set pieces in games, and it is the only version
of this that was ever going to be finished on schedule.

🔲 _Detailed architecture, script-by-script, is in `Docs/BUILD_PLAN.md` if the markers want it._

---

## 10. Limitations and Known Issues

### 10.1 Known issues

| Issue                    | Detail                                                                                                                                                                                                                                           | Status                  |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------- |
| Escape key in the Editor | Pressing Escape to back out leaves the cursor visible until the Game view is clicked. The Unity Editor force-releases cursor lock on Escape and our code cannot win the argument. **Does not occur in a built player.** `Q` is the intended key. | Won't fix — Editor only |
| Debris is not repeatable | Loose parts thrown on impact use real physics, so they land differently each replay. Deliberate: decoration applied after the outcome is already decided.                                                                                        | By design               |
| 🔲                       | 🔲                                                                                                                                                                                                                                               | 🔲                      |

### 10.2 Deliberate simplifications

- The crash is choreographed rather than physically simulated.
- Damage is a model swap, not deformation.
- NavMesh navigation is used for ambient and guide characters only, never for the two crash
  participants, because their motion has to be perfectly repeatable.
- Braking changes the severity of the crash, not its timing — see §7.2.
- The intervention window is short by design: about fifty real seconds.

### 10.3 Scope not attempted

Full vehicle physics, true recorded rewind, ragdoll, multiple scenarios, multiple large maps.

---

## 11. Testing

🔲 _Expand with your own results, and add the DUX usability testing findings._

| Test               | Method                                               | Pass condition                                                                                           |
| ------------------ | ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Repeatability      | Reset and replay the incident ten times              | Impact lands at the identical time every run (≈ 6.78 s)                                                  |
| Rewind integrity   | Jump straight to the aftermath                       | The pedestrian is correctly in her struck state — proving the collision registered during a fast-forward |
| Level validation   | Set an impossible impact time                        | The project reports exactly how many metres short each road is                                           |
| Outcome branching  | Fix all four / only the car's / only hers / none     | Prevented; crash with lights on and slower; crash with no phone or headphones; crash at full severity    |
| Reach restrictions | Try to reach her phone from outside during Intervene | Not selectable until you step into her view                                                              |
| Performance        | 🔲 Built player on two machines                      | 🔲 Record frame rate, resolution and hardware                                                            |

---

## 12. Credits and References

The authoritative list is maintained in `Docs/CREDITS.md` — keep the two in sync.

### 12.1 Third-party assets

| Asset                                              | Source                           | Licence                                     | Used for                                                               |
| -------------------------------------------------- | -------------------------------- | ------------------------------------------- | ---------------------------------------------------------------------- |
| **Hodaart Low Poly Character Collection 3**        | Unity Asset Store                | Standard Unity Asset Store EULA (purchased) | All human characters. Character 01 = driver, Character 02 = pedestrian |
| **Pack_FREE_Cars**                                 | 🔲 _record the exact source URL_ | 🔲 _record the exact licence_               | Vehicles                                                               |
| **Unity Starter Assets — First Person Controller** | Unity Technologies               | Unity Companion License                     | Player movement and look                                               |
| **Mixamo animations**                              | https://www.mixamo.com (Adobe)   | Adobe account terms                         | 🔲 _list each clip used_                                               |

> 🔲 **ACTION REQUIRED:** `Pack_FREE_Cars` has no recorded source or licence. Both the I3E and
> 3RT rubrics require this, and "it was free" is not a licence.

### 12.2 Unity packages

Cinemachine 3.1.2 · AI Navigation 2.0.12 · Input System 1.19.0 · Timeline 1.8.12 ·
Universal RP 17.3.0 · ProBuilder 6.0.9 — all Unity Technologies, Unity Companion License.

### 12.3 Made by the team

🔲 _Complete this table — it is where 3RT marks are earned, so be specific._

| Asset                              | Author | Software           |
| ---------------------------------- | ------ | ------------------ |
| Modular road / pavement / kerb kit | Marcus | Maya               |
| Car interior                       | 🔲     | Maya               |
| Trim sheet                         | Marcus | 🔲                 |
| Training room modular interior     | 🔲     | Maya               |
| Gameplay systems and scripts       | Adam   | Visual Studio / C# |
| Level layout and scene assembly    | Darryl | Unity              |
| Kiosk experience and UX            | Isaiah | Figma              |

### 12.4 Acknowledgements

- Concept and delivery guidance from our I3E, STLD, 3RT and DUX tutors.
- The problem statement is from **Delta Challenge 2026: Guardians of Tomorrow, Track B**.
- 🔲 _Add any articles or statistics cited in the debrief text._

### 12.5 AI declaration

| Tool               | Used for                                                                                | By   |
| ------------------ | --------------------------------------------------------------------------------------- | ---- |
| Claude (Anthropic) | Unity architecture planning, C# gameplay scripting, code review, documentation drafting | Adam |
| 🔲                 | 🔲                                                                                      | 🔲   |

---

## 13. Build and Run

1. Open the project in **Unity 6000.3.13f1**.
2. Open 🔲 `Assets/Scenes/<final scene name>.unity`.
3. Press Play, or run the standalone build from `I3E_<NameOfApp>_Build.zip`.

**Repository hygiene:** `Library/`, `Temp/`, `Logs/`, `obj/` and build output are excluded via
`.gitignore`. All `.meta` files are committed — they carry the references between assets.
