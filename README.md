# Road Ready - See the Risk. Change the Outcome.

**Ngee Ann Polytechnic | Diploma in Immersive Media | Year 2.1 Integrated Project 2026**

| Project detail | Information                                           |
| -------------- | ----------------------------------------------------- |
| Team           | Larp Larp Sahur Studios                               |
| Members        | Adam, Darryl, Marcus and Isaiah                       |
| Engine         | Unity 6000.3.13f1 with Universal Render Pipeline 17.3 |
| Repository     | https://github.com/adamjcc/LarpLarpSahur              |
| Main scene     | `Assets/Scenes/MainScene.unity`                       |

| Modules | Links                                                                                                                             |
| ------- | --------------------------------------------------------------------------------------------------------------------------------- |
| DUX     | [Figma Wireframe](https://www.figma.com/design/oL2NMkQUYGt3H3hDnVynWE/DUX-IP2026?node-id=0-1&t=uROoYH6eHqK9znUz-1) · Proposal Doc |
| STLD    | Gameplay Walkthrough Video · Game Design Doc                                                                                      |

---

## 1. Overview

### About Road Ready

_Road Ready_ is a short first-person road safety game about how several small choices can lead to one serious incident.

On a quiet road near a campus, a student approaches a pedestrian crossing while looking at her phone and wearing noise-cancelling headphones. At the same time, a speeding car approaches with its headlights off. The player takes the role of a crash investigator and studies what happened from different points of view.

The player first watches the collision from above. They then explore the scene, speak to the people involved and examine possible hazards. After seeing the final moments through the pedestrian's and driver's eyes, the player returns to the seconds before the collision and tries to prevent it.

The main message is simple: **a road accident may not have only one cause**. In this incident, both the pedestrian and the driver made unsafe choices. The player has to understand both sides before they can create a safe outcome.

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

### Main Features

- **One complete road safety incident** presented from an overhead view, the pedestrian's view, the driver's view and the player's first-person view.
- A **free-roam investigation** with dialogue, evidence descriptions and object highlighting.
- **Raycast interaction** for examining people, vehicles and smaller objects.
- A **slow-motion intervention** where the world moves slowly but the player remains easy to control.
- Four contributing **hazards** and several **red herrings**.
- Different **replay outcomes** that show which changes the player made, even when the collision is not fully prevented.
- A **scoring** and **debrief screen** that explains the player's result.
- **Four team-owned AI state machines**, including two incident characters and two NavMesh characters.
- Ambient pedestrians, a guide character, final character animation, a modelled car interior, a training room, lighting, post-processing and visual effects.
- A companion **DUX kiosk experience** for shorter public road safety activities.

---

## 3. Gameplay

### How the Game Plays

1. **Observe** - Watch the full incident from an overhead camera.
2. **Investigate** - Walk around the frozen scene, speak to both people and inspect objects that may have contributed to the collision.
3. **View both perspectives** - Replay the final moments through the pedestrian's and driver's eyes.
4. **Intervene** - Return to three seconds before impact. The incident runs at 0.06x speed while the player moves normally and changes the hazards they found.
5. **Resolve** - Watch the incident again with the player's changes applied.
6. **Debrief** - Receive a score and a clear explanation of the factors that were found, missed or changed.

The investigation has no timer, so players can look around at their own pace. The countdown only begins during the intervention.

### Controls

| Input                       | Gamepad      | Action                                              |
| --------------------------- | ------------ | --------------------------------------------------- |
| `W`, `A`, `S`, `D`          | Left stick   | Move                                                |
| `Mouse`                     | Right stick  | Look around and aim the crosshair                   |
| `Left Shift`                | Left trigger | Sprint                                              |
| `Space`                     | A            | Jump                                                |
| `Left Mouse Button` or `E`  | A            | Examine, speak or interact                          |
| `Q` or `Right Mouse Button` | B            | Leave a point-of-view camera or passenger seat      |
| `F`                         | Y            | Hide the steering wheel while in the driver's seat  |
| `Enter`                     | Start        | Continue to the next part of the experience         |
| `Escape`                    | -            | Open the pause menu or back out of the current view |

All controls are defined in a Unity Input Action Asset, so keyboard, mouse and gamepad are supported from the same set of actions.

The same crosshair is used while walking, sitting inside the car and viewing the scene through another person's eyes. Interactive objects highlight when the player is close enough to use them.

The driver sits close to the wheel, so the steering wheel and indicator stalk block the view of the pedals. `F` hides them while the player is in the driver's seat and restores them automatically on the way out.

Before the intervention begins, a confirmation box explains what is about to happen and warns that the investigation cannot be returned to. This stops players from starting the countdown by accident.

---

## 4. AI Implementation and Finite State Machines

A finite state machine, or FSM, gives a character a small list of behaviours called states. The character changes state when something happens, such as reaching the kerb, noticing the collision or finding that the player missed a hazard.

Each teammate created and documented one FSM. The two people involved in the collision follow fixed paths so that every replay stays consistent. The bystander and guide use Unity's NavMesh system because their movement can be more flexible.

### 4.1 Student Pedestrian FSM - Adam

```text
Walking -> Distracted -> At Kerb
                            |
                            | all four hazards changed
                            v
                       Alerted? -> Safe
                            |
                            | one or more hazards missed
                            v
                       Crossing -> Struck -> Aftermath
```

The student walks towards the crossing at 1.2 metres per second. If her phone has not been put away, she becomes distracted while approaching the road. When she reaches the kerb, the FSM checks the player's changes. A complete intervention makes her notice the danger and stop. If a hazard is still active, she continues into the road and the incident plays out.

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

The car follows a set route towards the crossing. Its lights and speed change according to the player's intervention. The FSM decides whether the driver notices the student in time. If every contributing hazard has been addressed, the car brakes and stops safely. Otherwise, it reaches the collision point. A damaged model is shown after impact instead of deforming the car with real-time physics.

### 4.3 Bystander FSM - Marcus

```text
Idle -> Roaming -> Notices Incident -> Moves to Scene
                                      |
                                      v
                              Calls for Help -> Returns
```

The bystander moves around the environment using a NavMeshAgent. When the incident occurs, the character stops roaming, moves towards a safe viewing position and calls for help. After the scene resets, the bystander returns to normal behaviour. This gives the environment a visible reaction to the incident without affecting the fixed crash timing.

### 4.4 Guide FSM - Isaiah

```text
Idle -> Follows Player -> Leads to Evidence -> Waits
             ^                                  |
             |__________________________________|
```

The guide follows the player during the investigation. If important evidence has not been examined, it moves towards that area and waits nearby without directly giving away the answer. Once the player catches up or finds the evidence, the guide returns to following. Unity Navigation is used to find a safe route around the level.

### Game Phase FSM

The game itself also uses a simple state machine:

```text
Observe -> Investigate -> Intervene -> Resolve -> Debrief
              |
              +-> Pedestrian POV / Driver POV -> Investigate
```

This system controls the active camera, player movement, interaction access, simulation speed and user interface. Keeping these changes in one place prevents different phases from leaving the player stuck in the wrong camera or control mode.

---

## 5. Solving the Game

The collision is prevented only when all four contributing factors are changed. Fixing fewer than four still affects the replay, but it does not create a completely safe outcome.

| Contributing factor                                 | How the player finds it               | How the player changes it                    | Result                                                   |
| --------------------------------------------------- | ------------------------------------- | -------------------------------------------- | -------------------------------------------------------- |
| The student is looking at her phone                 | Examine the phone in her hand         | Enter her point of view and put it away      | She looks up while approaching the road                  |
| The student is wearing noise-cancelling headphones  | Examine the headphones from outside   | Aim at her head and remove them              | She can hear the approaching car                         |
| The car's headlights are off                        | Examine the dashboard controls        | Enter the driver's view and switch them on   | The road and pedestrian become more visible              |
| The driver is speeding at 50 km/h in a 40 km/h area | Check the speedometer and brake pedal | Enter the driver's view and reduce the speed | The car approaches more slowly and has more time to stop |

The final verdict is shared responsibility. The student crossed without paying attention and could not hear the car. The driver was speeding at night without headlights. The safe ending only occurs when both road users change their behaviour.

---

## 6. How the Main Systems Work

### Repeatable incident and rewind

A single scenario clock controls the student, car and collision timing. It advances in fixed steps, which keeps every replay consistent. The game does not record or reverse the whole scene. To move backwards, it resets the incident and quickly runs it forward to the required moment before showing the next view.

### Interaction and physics

The interaction system sends a ray from the active camera through the crosshair. If the ray reaches a valid object, the object highlights and shows the correct prompt. Unity Physics is also used for player collision, interaction zones and visual impact effects.

### Evidence and interventions

The game separately remembers what the player examined and what they changed. This lets the debrief explain the difference between a hazard the player did not notice and one they noticed but chose not to fix.

### Navigation

Ambient pedestrians, the bystander and the guide move on a baked NavMesh. The two main incident characters use fixed routes instead because their positions must remain the same in every replay.

### Vehicle damage

The car holds two versions of its bodywork. The clean version is shown until the collision is detected, at which point it is hidden and the damaged version takes its place. Shared parts such as the wheels and interior belong to neither list, so they stay put. Loose parts with a Rigidbody are thrown by real physics on impact and returned to their exact starting position on every reset.

This follows our 3RT tutor's advice to swap models rather than deform them, and it suits the game for a second reason: the incident replays many times, and a model swap resets in a single line.

### Code standards

Every script carries a header naming the team, the members and the module. Classes, properties and methods use XML documentation comments, with line comments where the reason for a piece of code is not obvious from reading it.

Each team member designed and implemented one finite state machine, documented in section 4 with its diagram. Shared behaviour lives in base classes so the same logic is not written twice: `ScenarioActor` handles resetting and animation safety, and `PathScenarioActor` handles path following and automatic placement.

---

## 7. Testing and Limitations

The final build was checked using the following tests:

| Test               | Expected result                                                                         | Outcome |
| ------------------ | --------------------------------------------------------------------------------------- | ------- |
| Replay consistency | The collision happens at the same point and time on repeated runs                       | Passed  |
| Reset and rewind   | Characters, animations, hazards and effects return to the correct state                 | Passed  |
| Interaction access | Objects can only be changed from the intended position or point of view                 | Passed  |
| Outcome branching  | Four fixes prevent the collision; partial fixes remain visible in the replay            | Passed  |
| NavMesh behaviour  | Ambient characters and the guide reach valid destinations without blocking the incident | Passed  |
| Damage swap        | Damaged bodywork appears only after a collision, and never when it is prevented         | Passed  |
| Retry              | Score, examined evidence and hidden objects all return to their starting state          | Passed  |
| Input devices      | Every action works from keyboard and mouse, and from a gamepad                          | Passed  |
| Build test         | The main menu, game loop, retry option and exit flow work in the standalone build       | Passed  |

### Known issues

No game-breaking issues were found in the final standalone build.

When testing inside the Unity Editor, pressing Escape may release the mouse cursor until the Game view is clicked again. This is Editor behaviour and does not occur in the standalone build. Q or Right Mouse Button can also be used to leave a point-of-view camera.

### Deliberate limitations

- Road Ready contains one detailed incident rather than several shorter scenarios.
- The collision is carefully timed instead of using realistic vehicle physics.
- Rewind is created by resetting and replaying the scenario, not by reversing every object in real time.
- Vehicle damage uses a model swap instead of live deformation.
- Ambient NPCs support the scene but do not change the main incident outcome.

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
| Car model with separated interior parts                             | 🔲 _add source link and author_                                                                                                                  | The car involved in the collision                   |

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
2. Open `Assets/Scenes/MainScene.unity`.
3. Press Play.

The full Unity project is submitted as `I3E_RoadReady_Project.zip`.
