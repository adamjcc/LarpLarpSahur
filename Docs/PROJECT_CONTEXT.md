# Project context — carry-over notes

Written 11 Aug 2026. **Read this first if you are picking the project up cold**, or if the
assistant's chat history has been lost. `BUILD_PLAN.md` holds the roadmap; this file holds the
decisions, conventions and gotchas that are easy to forget and expensive to rediscover.

---

## Who and what

| | |
|---|---|
| Game | **Road Ready — See the Risk. Change the Outcome.** |
| Team | Larp Larp Sahur Studios |
| Members | Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai |
| Module | Y2S1 IP — Integrated Project (I3E · STLD · 3RT · DUX) |
| Engine | Unity 6000.3.13f1, URP 17.3, Cinemachine 3.1.2 |
| Main scene | `Assets/Scenes/MainScene.unity` |
| **Scripts** | **`Assets/Scripts/`** — moved out of `_Project/` on 11 Aug. GUIDs verified intact. |
| Authoritative ReadMe | **`README.md` in the repo root**, NOT `Docs/I3E_ReadMe.md` |

> ⚠️ `Docs/I3E_ReadMe.md` is a stale near-duplicate of the root README. The root one is ahead
> (it has the Figma link and the real Sketchfab credits). **Delete the Docs copy** or replace
> it with a pointer, before the two drift further apart.

---

## Decisions that are settled — do not relitigate

- **One incident, done properly.** Scenarios 2 and 3 were cut early and stay cut.
- **Car and pedestrian**, at night, Singapore left-hand traffic: driver on the right, car in
  the left lane, and she is struck by a car approaching **from her right** — the direction a
  pedestrian here is supposed to check first.
- **The collision is avoided only when all four hazards are fixed.** Fixing fewer still shows
  every change the player made in the replay, but the crash happens.
- **Four hazards:** her phone (from inside her POV) · her headphones (from *outside*, aiming
  at her head) · headlights off (driver's POV) · speeding (driver's POV, brake pedal).
- **Red herrings matter.** An investigation where everything clickable is a correct answer is
  a checklist, not an investigation. Aim for six or more non-contributing objects.
- **Her POV replay is a locked camera.** You see her phone screen and physically cannot look
  up at the car. That restriction *is* the argument the game is making.
- **She walks at 1.2 m/s** instead of a normal 1.4. Phone users measurably walk slower. It is
  a real research finding and it belongs in the debrief.

---

## The three architectural rules everything rests on

1. **One clock.** `ScenarioRunner` advances `ScenarioTime` in fixed 1/60 s steps and ticks
   every crash actor itself. **Actors never move in `Update()`.** This is what makes the
   incident repeatable, which is what makes replay and rewind possible at all.
2. **Rewind is not recorded.** `SeekTo(t)` resets to zero and re-simulates forward inside one
   frame. Nothing is stored, no animation is reversed. The frozen aftermath the player walks
   through is the same scene with the clock parked at `impactTime + 2`.
3. **`Time.timeScale` is never touched.** It stays at 1.0 for the whole game. Slow motion is a
   multiplier on the scenario clock alone, which is why the player keeps moving at full speed
   while the world crawls.

Corollaries: no `Random`, no `NavMeshAgent`, no `Rigidbody` and no reactive sensors anywhere
near the two crash actors. NavMesh is for the ambient crowd, the bystander and the dog only.

---

## Traps that already cost us time

Each of these was a real bug. The rule underneath each one repeats.

| What happened | The rule |
|---|---|
| Camera pitched down couldn't reach the brake pedal | Unity's **positive X-rotation is nose-DOWN**. A field called `minPitch` was the *up* limit. |
| Camera turned while dragging the mouse to a button | **`PovLook` is on the camera, not the player**, so disabling player control never reached it. It now bails when the cursor is unlocked. |
| Q closed a dialogue *and* ejected you from the car | `GetKeyDown` stays true for a whole frame and **script order is undefined**. `UIManager.ModalBlockingInput` stays true for the rest of the closing frame. |
| Camera went down when the mouse went up | The `Look` binding carries **`InvertVector2` (invertY defaults true)** and `ScaleVector2(0.05)`. Add `look.y` to pitch, don't subtract. Sensitivity ≈ **4**, not 0.2. |
| Alt-tab silently freed the cursor | **`StarterAssetsInputs` re-applies its own `cursorLocked` on focus.** `PlayerRig.SetCursorLocked` keeps both in sync. |
| Props buried inside the interaction capsule | A raycast returns the **nearest** hit. Body capsule stops below the chin; props sit outside its radius. |
| Pedestrian collapsed on every camera change | **A seek re-runs the whole incident in one frame**, so anything triggered at impact fires again. Witness mode is set *before* the seek. |
| Debrief showed empty boxes | **TMP can only draw glyphs in its font atlas.** No tick, cross, middle dot or em dash in LiberationSans. Runtime strings are plain ASCII. |
| UI all bunched in the middle | **Empty GameObjects in a Canvas default to a 100×100 centre-anchored rect.** Containers must be full-stretch before children can anchor to screen edges. |
| Component couldn't be attached | **Unity requires the filename to match the class name** for MonoBehaviours. (`AbientPedastrian.cs` held `class AmbientPedestrian`.) |

---

## Conventions

**Every script** starts with a header naming the studio, all four members and the module.
Classes, serialized fields and methods carry XML `<summary>` comments. Line comments explain
*why*, not *what* — `score++` does not need a comment saying it increments the score.

**Never** put a raw `<` inside a `///` comment; it makes the XML invalid. Use `&lt;`.

**Access levels:** `protected` only where a subclass genuinely uses the member. `[SerializeField]
private` still shows in the Inspector, so private is the default.

---

## Things that still need doing

- **The car model has no recorded source or author.** Both I3E and 3RT require it, and the
  other two Sketchfab models are CC Attribution — which legally requires naming the creator.
- **`PlayerRaycaster.cs`, `InteractableEvidence.cs`, `NPCBrain.cs`, `VehicleController.cs`**
  are dead prototypes still sitting in `Assets/Scripts/`. Nothing references them, but
  `PlayerRaycaster` is a **second independent raycaster** — attach it by accident and you get
  double interactions. Delete or move them.
- **`Docs/I3E_ReadMe.md`** duplicate, see the warning at the top.
- **`PathFollowerTest`, `HudPromptPlaceholder`, `PhaseHudPlaceholder`** were scaffolding and
  can go once the Canvas is confirmed.
- **Bystander and Guide FSMs** are a rubric requirement, not polish. Isaiah's `CompanionBrain`
  has started; its `FindUnexaminedClue()` should ask `EvidenceLedger.Has(id)` instead of
  cycling an array.

---

## Numbers, for reference

Impact at **t = 7.0 s** · car **50 km/h in a 40 zone** · she walks **1.2 m/s** · intervene
rewinds **3 s** and runs at **0.06×**, giving about **50 real seconds** · POV replay covers
**impact − 5 s to impact + 1 s** · free roam freezes at **impact + 2 s**.

Full as-built values, including every camera position and both phone poses, are in the
appendix of `BUILD_PLAN.md`.
