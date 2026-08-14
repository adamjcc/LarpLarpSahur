# Road Ready - See the Risk. Change the Outcome.

**Ngee Ann Polytechnic | Diploma in Immersive Media | Year 2.1 Integrated Project 2026**

| Project detail | Information                                                            |
| -------------- | ---------------------------------------------------------------------- |
| Team           | Larp Larp Sahur Studios                                                |
| Members        | Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai   |
| Engine         | Unity 6000.3.13f1 with Universal Render Pipeline 17.3                  |
| Repository     | https://github.com/adamjcc/LarpLarpSahur                               |
| Scenes         | `Assets/Scenes/GameScene.unity` and `Assets/Scenes/Start Office.unity` |

| Modules | Links                                                                                                                                                                   |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DUX     | [Figma Wireframe](https://www.figma.com/design/oL2NMkQUYGt3H3hDnVynWE/DUX-IP2026?node-id=0-1&t=uROoYH6eHqK9znUz-1) · [Proposal Doc](https://canva.link/6xcuach9q7m69jm) |
| STLD    | Gameplay Walkthrough Video · Game Design Doc                                                                                                                            |

---

## 1. Overview

### About Road Ready

_Road Ready_ is a short first-person road safety game about how several small choices can lead to one serious incident.

The player is a police recruit. After a briefing at the academy they are sent into a reconstruction of a collision from the night before. On a quiet road near a campus, a student approached a pedestrian crossing while looking at her phone and wearing noise-cancelling headphones. At the same moment, a speeding car approached with its headlights off.

The player watches the collision from above, explores the frozen scene, speaks to both people involved and examines the objects around them. After seeing the final seconds through the pedestrian's and the driver's own eyes, they are sent back to just before impact and given one chance to change what happens.

The main message is simple: **a road accident may not have only one cause**. In this incident both the pedestrian and the driver made unsafe choices, and the collision is only avoided when every factor is addressed. The game will not let the player blame one person, because the simulation itself refuses to agree with them.

Road Ready was created in response to the Delta Challenge 2026 road safety topic, which asks how road safety education can be made more engaging for young people. The Unity game is supported by a DUX kiosk experience containing shorter road safety questions and community results.

---

## 2. Design Process

### User Stories

- As a **tertiary student**, I want to see the consequences of a distraction I recognise in myself, so that I take it seriously rather than dismissing it as something that happens to other people.
- As a **player**, I want to investigate the scene at my own pace before being asked to act, so that I am not rushed into guessing.
- As a **player**, I want to see the incident from more than one viewpoint, so that I understand what each person could and could not perceive.
- As a **player**, I want my choices to visibly change what happens, so that the connection between cause and outcome is undeniable.
- As a **player who got it wrong**, I want to be told exactly which factors I missed and why they mattered, so that I learn something rather than just failing.
- As an **educator or exhibitor**, I want a session to last only a few minutes, so that it works in a classroom or at a public event.

### Design decisions

**One incident, examined properly.** Earlier plans included three separate scenarios. They were cut so that a single collision could be shown from four viewpoints, replayed with the player's changes, and explained properly at the end.

**All four factors, or none.** An earlier version graded the player on how many hazards they found. That let them pick a villain and walk away feeling safe, which is the opposite of what the brief asks for. Requiring every factor turns a quiz into an argument.

**Investigation without a timer.** The player explores for as long as they like and starts the countdown themselves. A wrong answer given under pressure teaches nothing.

**Restrictions that make the point.** The pedestrian's replay uses a locked camera. The player sees her phone screen filling the view and physically cannot look up to find the car. That restriction is the argument the game is making.

**Objects that do not matter.** Alongside the four real factors there are several red herrings. An investigation where everything clickable is a correct answer is a checklist, not an investigation.

### Main Features

- **One complete road safety incident** shown from an overhead view, the pedestrian's view, the driver's view and the player's own first-person view.
- **A briefing scene** at the police academy, loaded alongside the main level, where a training officer explains the task.
- A **free-roam investigation** with dialogue, evidence descriptions and object highlighting.
- **Raycast interaction** for examining people, vehicles and small objects, with access rules deciding where each one can be reached from.
- A **slow-motion intervention** where the world runs at a sixteenth speed while the player continues to move normally.
- Four contributing **hazards** and several **red herrings**.
- **Replay outcomes that carry the player's changes**, whether or not the collision was prevented.
- A **scoring and debrief screen** that separates what the player noticed from what they acted on.
- **Four team-owned AI state machines**, plus ambient pedestrians, ambient traffic and a signalled junction.
- A **main menu** with an orbiting camera, and a companion robot that follows the player through the investigation.
- A companion **DUX kiosk experience** for shorter public road safety activities.

---

## 3. Gameplay

### How the Game Plays

1. **Main Menu** - The camera circles the level behind the title while the player chooses to start, read the instructions or quit.
2. **Briefing** - The player spawns in the police academy, walks to their training officer and hears why they are there.
3. **Observe** - The incident plays from an overhead camera, and can be replayed as often as the player wants.
4. **Investigate** - The clock freezes two seconds after impact. The player walks the scene, examines objects and speaks to both people involved.
5. **View both perspectives** - From within either conversation, the player can replay the final seconds through that person's eyes.
6. **Intervene** - After a confirmation, the incident rewinds to a few seconds before impact and runs at 0.06x speed. The player approaches either person, steps into their view and changes what they found.
7. **Resolve** - The same seconds replay at full speed with the player's changes applied from the start.
8. **Debrief** - A score, a grade and a factor-by-factor explanation, with the option to retry or return to the menu.

The investigation has no timer. The countdown only begins during the intervention.

### Controls

| Input                       | Gamepad      | Action                                             |
| --------------------------- | ------------ | -------------------------------------------------- |
| `W`, `A`, `S`, `D`          | Left stick   | Move                                               |
| `Mouse`                     | Right stick  | Look around and aim the crosshair                  |
| `Left Shift`                | Left trigger | Sprint                                             |
| `Space`                     | A            | Jump                                               |
| `Left Mouse Button` or `E`  | A            | Examine, speak or interact                         |
| `Q` or `Right Mouse Button` | B            | Leave a point-of-view camera or the passenger seat |
| `F`                         | Y            | Hide the steering wheel while in the driver's seat |
| `Enter`                     | Start        | Continue to the next part of the experience        |

All controls are defined in a Unity Input Action Asset, so keyboard, mouse and gamepad are supported from the same set of actions.

The same crosshair is used while walking, sitting inside the car and viewing the scene through another person's eyes. It changes appearance the moment it lands on something interactive, and objects brighten when the player is close enough to use them.

The driver sits close to the wheel, so the steering wheel and indicator stalk block the view of the pedals. `F` hides them while the player is in the driver's seat and restores them automatically on the way out.

Before the intervention begins, a confirmation box explains what is about to happen and warns that the investigation cannot be returned to.

---

## 4. AI Implementation and Finite State Machines

A finite state machine, or FSM, gives a character a small list of behaviours called states, and clear conditions for moving between them.

Each teammate designed and implemented one. The two characters involved in the collision follow fixed routes so that every replay is identical. The ambient characters and the companion robot move freely, because their movement can vary without affecting the incident.

Two properties are shared by the incident state machines. Transitions fire on **distance rather than time**, so the logic survives the level designer moving the road. And the branch at each decision point **reads the player's interventions**, which is what turns a choice into a different ending.

### 4.1 Student Pedestrian FSM - Adam

```text
Walking -> Distracted -> At Kerb
                            |
                            | all four hazards changed
                            v
                       Alerted -> Safe
                            |
                            | one or more hazards missed
                            v
                       Crossing -> Struck -> Aftermath
```

The student walks towards the crossing at 1.2 metres per second. If her phone has not been put away she becomes distracted while approaching the road. When she reaches the kerb the FSM checks the player's changes: a complete intervention makes her notice the danger and stop, while any missing hazard sends her into the road.

She can only be struck while actually in the road, so she is never hit while standing safely on the pavement. During the investigation the same character is presented standing and able to talk, handled by a witness mode that suppresses the collision animation while the scene is being re-simulated.

### 4.2 Incident Driver and Car FSM - Darryl

```text
Cruising -> Reacting
               |
               | all four hazards changed
               v
          Braking -> Stopped -> Aftermath
               |
               | one or more hazards missed
               v
          Unaware -> Impact -> Aftermath
```

The car follows a set route towards the crossing at 50 km/h in a 40 km/h zone. Its headlights and speed change according to the player's intervention, and the FSM decides whether the driver reacts in time.

The car deliberately has no obstacle sensor. An automatic "brake if something is in front of me" reflex would make the collision impossible, so braking here is a decision the driver either makes or does not.

Applying the brakes makes the crash gentler without making it miss. The car works out its own starting position from its actual speed, so a slower car simply begins closer to the junction and still arrives at the same moment.

### 4.3 Ambient Pedestrian FSM - Marcus

```text
Roaming -> (reaches destination) -> picks a new point -> Roaming
   |
   v
Stopped
```

Background pedestrians wander the environment on a baked NavMesh, choosing a new destination each time they arrive at one. They can be stopped on command so they do not walk through the incident while it is playing. This gives the scene life without touching the fixed crash timing.

### 4.4 Companion Robot FSM - Isaiah

```text
   Idle  --(player walks away)-->  Following
     ^                                  |
     |______(caught up again)___________|

   Standby  <-- any replay, point-of-view camera or the menu
```

A small robot follows the player through the briefing, the investigation and the intervention. In the story it is recording the recruit rather than helping them, so it keeps its distance and hangs back rather than getting underfoot.

It floats and moves itself rather than using a NavMeshAgent, since it does not need a walking route around obstacles. The gap between the distance it settles at and the distance that makes it move again stops it twitching on the spot every time the player shifts. The third state, Standby, hides the robot and stops it during any replay or point-of-view camera, where a robot drifting through the shot would look like a fault.

### 4.5 Traffic Light System

```text
N/S Green -> N/S Yellow -> All Red -> E/W Green -> E/W Yellow -> All Red -> repeat
```

The junction runs a six-phase signal cycle. Each light switches its own bulbs and toggles an invisible stop line that holds ambient traffic, so background cars obey the signals without any awareness of the main incident.

### Game Phase FSM

The game itself is a state machine, and it is the spine of the project:

```text
Main Menu -> Briefing -> Observe -> Investigate -> Intervene -> Resolve -> Debrief
                                        |                                    |
                                        +-> Pedestrian POV / Driver POV      +-> Retry
                                        +-> Passenger seat                   +-> Main Menu
```

Every phase change runs through a single method that sets **all six** of: the live camera, whether the player can move, the cursor state, the simulation speed, whether the simulation is running, and where the clock sits. Keeping those together in one place is what prevents a phase from leaving the player stuck with an invisible cursor or the wrong camera.

---

## 5. Solving the Game

The collision is prevented only when all four contributing factors are changed. Fixing fewer still changes the replay visibly, but it does not produce a safe outcome.

| Contributing factor                                 | How the player finds it               | How the player changes it                    | Result                                                   |
| --------------------------------------------------- | ------------------------------------- | -------------------------------------------- | -------------------------------------------------------- |
| The student is looking at her phone                 | Examine the phone in her hand         | Enter her point of view and put it away      | She looks up while approaching the road                  |
| The student is wearing noise-cancelling headphones  | Examine the headphones on her head    | Aim at her head from outside and remove them | She can hear the approaching car                         |
| The car's headlights are off                        | Examine the indicator stalk           | Enter the driver's view and switch them on   | The road and the pedestrian become visible               |
| The driver is speeding at 50 km/h in a 40 km/h area | Check the speedometer and brake pedal | Enter the driver's view and use the brake    | The car approaches more slowly and has more time to stop |

Two of these can only be reached from inside a person's point of view, and one only from outside. The headphones are the outside case, because nobody can see their own ears — the player has to aim at her head while standing in front of her.

Several other objects can be examined and changed but contribute nothing, including her umbrella, the car's wiper stalk, the radio and the gear stick. Changing one is not punished, but the game says plainly that it made no difference.

The final verdict is shared responsibility. The student crossed without paying attention and could not hear the car. The driver was speeding at night without headlights. The safe ending only occurs when both road users change their behaviour.

---

## 6. How the Main Systems Work

### Repeatable incident and rewind

A single scenario clock drives the student, the car and the collision. It advances in fixed steps rather than by frame time, which keeps every replay identical.

The game does not record or reverse anything. To move backwards it resets the incident and re-runs it forward to the required moment inside a single frame, faster than the screen refreshes. The frozen aftermath the player walks through is the same scene with the clock parked two seconds after impact.

Slow motion is a multiplier on this clock alone, never on Unity's global time. That is why the world can crawl at a sixteenth speed while the player continues to walk and look at full speed.

### Automatic placement

The two characters are not positioned by hand. A single marker sits where the collision happens, and each of them works out where it must start in order to arrive there at exactly the right moment. Changing a speed or moving the marker re-solves everything, and the project reports in metres if a road is too short for the incident to fit.

### Interaction and physics

One raycast is sent from the active camera through the crosshair. Everything clickable implements the same interface, so people, vehicles, dashboard controls and small props are all handled by the same code.

Each object declares where it can be changed from — inside a point of view, outside one, or either — which is what makes the headphones and the phone behave differently.

Unity Physics is used for player collision, the interaction volumes, the loose parts thrown from the car on impact and the pedestrian's ragdoll. The collision itself is detected by a distance check driven by the simulation clock rather than a physics trigger, because rewinding runs hundreds of simulation steps inside one frame and a trigger would never fire during it.

### Evidence and interventions

The game separately remembers what the player **examined** and what they **changed**. This is what lets the debrief distinguish a factor the player never noticed from one they noticed and chose not to act on, and say something different about each.

### Two scenes

The police academy is a separate scene loaded **alongside** the main level rather than replacing it. Nothing is destroyed, so the player, the interface and every system keep working, and no state has to be carried across. The office is unloaded again as soon as the briefing ends.

### Navigation

Ambient pedestrians move on a baked NavMesh. The two incident characters use fixed routes instead, because their positions must be identical in every replay, and the companion robot floats and steers itself.

### Vehicle damage

The car holds two versions of its bodywork. The clean version is shown until the collision is detected, at which point it is hidden and the damaged version takes its place, along with impact particles. Shared parts such as the wheels and interior belong to neither list, so they stay put.

This follows our 3RT tutor's advice to swap models rather than deform them, and it suits the game for a second reason: the incident replays many times, and a model swap resets in a single line.

### Interface

The interface is a single Screen Space canvas driven by one manager, so no gameplay script touches a text field directly. Panels fade and scale as they appear, the countdown gives a small kick on every whole second, and a hazard tracker fills in as each factor is dealt with.

### Code standards

Every script carries a header naming the team, the members and the module. Classes, properties and methods use XML documentation comments, with line comments where the reason for a piece of code is not obvious from reading it.

Shared behaviour lives in base classes so the same logic is not written twice. One base class handles resetting and animation safety for anything taking part in the incident, and another handles path following and automatic placement.

---

## 7. Testing and Limitations

The final build was checked using the following tests:

| Test               | Expected result                                                                          | Outcome |
| ------------------ | ---------------------------------------------------------------------------------------- | ------- |
| Replay consistency | The collision happens at the same point and time on repeated runs                        | Passed  |
| Reset and rewind   | Characters, animations, hazards and effects return to the correct state                  | Passed  |
| Interaction access | Objects can only be changed from the intended position or point of view                  | Passed  |
| Outcome branching  | Four fixes prevent the collision; partial fixes remain visible in the replay             | Passed  |
| Damage swap        | Damaged bodywork and particles appear only after a collision, never when it is prevented | Passed  |
| Retry              | Score, examined evidence and hidden objects all return to their starting state           | Passed  |
| Scene loading      | The briefing scene loads, the player spawns correctly and it unloads afterwards          | Passed  |
| NavMesh behaviour  | Ambient characters reach valid destinations without blocking the incident                | Passed  |
| Input devices      | Every action works from keyboard and mouse, and from a gamepad                           | Passed  |
| Build test         | The menu, game loop, retry option and exit flow work in the standalone build             | Passed  |

### Known issues

No game-breaking issues were found in the final standalone build.

When testing inside the Unity Editor, pressing Escape may release the mouse cursor until the Game view is clicked again. This is Editor behaviour and does not occur in the standalone build. `Q` or Right Mouse Button can also be used to leave a point-of-view camera.

Loose parts thrown from the car and the pedestrian's ragdoll use real physics, so they land slightly differently on each replay. This is deliberate — both only ever run after the outcome has already been decided.

### Deliberate limitations

- Road Ready contains one detailed incident rather than several shorter scenarios.
- The collision is carefully timed rather than produced by realistic vehicle physics.
- Rewind is created by resetting and replaying the incident, not by reversing every object in real time.
- Vehicle damage uses a model swap instead of live deformation.
- Ambient characters and traffic support the scene but do not change the outcome.
- Highlighting brightens an object's own materials rather than drawing a true outline, which would have required a custom render pass.

These choices kept the experience clear, repeatable and suitable for a short classroom or exhibition session.

---

## 8. Credits and References

### External assets and software

| Asset or tool                                                       | Source                                                                                                                                           | Use in Road Ready                                   |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------- |
| Hodaart Low Poly Character Collection 3                             | Unity Asset Store, Standard Unity Asset Store EULA                                                                                               | Human characters                                    |
| Unity Starter Assets - First Person Controller                      | Unity Technologies, Unity Companion License                                                                                                      | Player movement and camera controls                 |
| Mixamo                                                              | Adobe, https://www.mixamo.com                                                                                                                    | Character animation clips                           |
| FREE Cartoon Car Pack - Simple Vehicles                             | Unity Asset Store, Standard Unity Asset Store EULA                                                                                               | Background and placeholder vehicle assets           |
| Cinemachine, AI Navigation, Input System, Timeline and Universal RP | Unity Technologies, Unity Companion License                                                                                                      | Cameras, navigation, input, sequences and rendering |
| Headphones Model                                                    | [User "~Drift~" on Sketchfab](https://skfb.ly/oRFAr), licensed under [Creative Commons Attribution](http://creativecommons.org/licenses/by/4.0/) | The student's noise-cancelling headphones           |
| Low Poly Mobile Phone                                               | [User "kimmy.k" on Sketchfab](https://skfb.ly/owJHu), licensed under [Creative Commons Attribution](http://creativecommons.org/licenses/by/4.0/) | The student's phone                                 |
| Car model with separated interior parts                             | [Sketchfab by user scailman](https://sketchfab.com/3d-models/low-poly-small-car-ebe7c5e98a7448b5abb2eaf0cb22b766)                                | The car involved in the collision                   |
| Environment and prop packs                                          | Unity Mega Furniture Pack on Unity Asset Store                                                                                                   | Level environment and interface                     |

The full asset list, exact source links and licence records are kept in `Docs/CREDITS.md`.

### Acknowledgements

- Project guidance from our I3E, STLD, 3RT and DUX tutors.
- Road safety problem statement from Delta Challenge 2026: Guardians of Tomorrow, Track B.

---

## 9. Build and Run

1. Extract `I3E_RoadReady_Build.zip`.
2. Open the extracted folder.
3. Run `Road Ready.exe`.

To open the project in Unity:

1. Open the project using Unity 6000.3.13f1.
2. Open `Assets/Scenes/GameScene.unity`.
3. Press Play.

Both `GameScene` and `Start Office` must be listed in **File > Build Settings**, because the briefing scene is loaded at runtime.

The full Unity project is submitted as `I3E_RoadReady_Project.zip`.
