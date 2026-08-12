# Road Ready - Game Design Document

**Module:** Spatial Theory and Level Design (STLD)  
**Project:** Road Ready  
**Team:** Larp Larp Sahur Studios  
**Team members:** Adam, Darryl, Marcus and Isaiah  
**Engine:** Unity 6000.3.13f1, Universal Render Pipeline  
**Document status:** Early level-design draft  
**Final submission name:** `STLD_RoadReady_GDD.pdf`

---

## How to Use This Template

The main game idea is included, but the final level layout has not been confirmed. Use these labels while working:

- **[CONFIRMED]** - Agreed and tested by the team.
- **[TO CONFIRM]** - Still needs a decision.
- **[ADD EVIDENCE]** - Add an image, diagram, source or test result.

Remove the labels and instructions before exporting the final PDF.

The STLD brief requires the GDD to include:

- Level abstract
- Bubble diagram, gameplay flow and rhythm
- Level map
- Asset list

---

## 1. Game Overview

### 1.1 High Concept

**[CONFIRMED]** Road Ready is a short first-person road safety game in which the player investigates a nighttime collision near a campus. The player observes the incident, examines the aftermath, views it from the pedestrian's and driver's perspectives, and returns to the seconds before impact to change the outcome.

### 1.2 Learning Goal

The level should show that a road incident can have several overlapping causes. In this scenario, the pedestrian is distracted by a phone and headphones, while the driver is speeding with the headlights off.

> **Final learning outcome:** [Write what the player should understand or do differently after playing.]

### 1.3 Target Audience

| Item | Current direction |
| --- | --- |
| Audience | Tertiary students and young road users |
| Platform | Windows PC |
| Experience | Short classroom, showcase or exhibition session |
| Session length | [TO CONFIRM] |
| Camera style | First-person exploration with cinematic and POV sequences |

### 1.4 Core Gameplay Loop

```text
Observe the incident
    -> Investigate the scene
    -> View both perspectives
    -> Identify the hazards
    -> Intervene in slow motion
    -> Watch the changed outcome
    -> Receive a score and debrief
```

---

## 2. Level Abstract

> Keep the final abstract to one or two clear paragraphs.

### 2.1 Working Abstract

**[TO CONFIRM]** The level takes place at night near a fictional Singapore campus. Its main area is a road junction or pedestrian crossing where the incident occurs. It may also include a small training or briefing space that introduces the player to the reconstruction.

The environment is designed around investigation and changing viewpoints. The overhead view shows the whole incident, while street-level objects, road geometry and darkness limit what the pedestrian and driver can see. During free roam, the player follows a compact route between the impact point, pedestrian evidence, vehicle evidence and the intervention start. The same space is reused for the slow-motion intervention and final replay.

### 2.2 Level Purpose

| Question | Answer |
| --- | --- |
| What must the player learn? | [Fill in] |
| What must the player do? | [Fill in] |
| What should the player feel? | [Fill in] |
| What is the main landmark? | [TO CONFIRM] |
| What is the main conflict point? | The crossing and impact area |
| How does the environment support the lesson? | [Fill in] |

### 2.3 Proposed Spaces

1. Training or briefing area - optional introduction space.
2. Overhead incident observation.
3. Main road and pedestrian crossing.
4. Pedestrian evidence area.
5. Car exterior and interior evidence area.
6. Slow-motion intervention route.
7. Resolution and debrief.

**[TO CONFIRM]** Decide whether the training area and road incident use separate Unity scenes or one connected level.

### 2.4 Main Constraints

- The car and pedestrian routes must meet at the same impact point.
- The road needs enough approach distance for the final incident timing.
- The player must be able to reach all interventions during slow motion.
- Evidence should be findable without being visible from every position.
- Main paths must not block the scripted incident.
- Final modular environment pieces must be made by the team unless tutor approval is given.
- The level must include a meaningful Unity Terrain area.

---

## 3. Visual Research

### 3.1 Visual Direction

| Area | Current direction | Status |
| --- | --- | --- |
| Location | Singapore-inspired campus road or junction | [TO CONFIRM] |
| Time | Night | [TO CONFIRM] |
| Art style | Stylised or low-poly | [TO CONFIRM] |
| Mood | Quiet, tense and reflective | [TO CONFIRM] |
| Main colours | [Fill in] | [TO CONFIRM] |

### 3.2 Reference Study

| Reference | What we noticed | How it may influence the level | Source |
| --- | --- | --- | --- |
| Campus entrance or junction | [Fill in] | [Fill in] | [Add source] |
| Pedestrian crossing | [Fill in] | [Fill in] | [Add source] |
| Singapore road lighting at night | [Fill in] | [Fill in] | [Add source] |
| Training or simulation room | [Fill in] | [Fill in] | [Add source] |
| Stylised environment | [Fill in] | [Fill in] | [Add source] |

Explain how the team combined and changed these references to create an original fictional location:

> [Write the design response here.]

**Evidence to add:** annotated moodboard, location photographs, early sketches and captions explaining each image.

---

## 4. Level Design Goals and Spatial Theory

### 4.1 Design Pillars

| Pillar | Meaning | Level response |
| --- | --- | --- |
| Clear investigation | Players should know where they can explore without being given the answer | Use readable landmarks and a compact route |
| Multiple perspectives | The same place should reveal different information from each view | Design different overhead, pedestrian and driver sightlines |
| Shared responsibility | The level should not blame only one person | Place important evidence on both sides of the crossing |
| Fair tension | Intervention should feel urgent but remain possible | Keep routes direct and interactive targets readable |

### 4.2 Spatial Theory Plan

| Spatial idea | Possible use in Road Ready | Evidence to add |
| --- | --- | --- |
| Landmarks | Crossing, campus entrance, training centre or illuminated sign | Annotated screenshot |
| Paths | Pavements, road markings and lighting guide movement | Route diagram |
| Nodes | Impact point, pedestrian, car and intervention start | Bubble diagram |
| Edges | Kerbs, railings and planting separate pavement from road | Level screenshot |
| Thresholds | Moving into the reconstruction signals a change from learning to action | Entrance screenshot |
| Sightlines | Each camera reveals or hides different evidence | POV comparison |
| Occlusion | Vehicles, corners, planting or darkness partially block danger | Annotated POV image |
| Affordances | Crossings, paths, lights and highlights show possible actions | Gameplay screenshot |
| Compression and release | A smaller briefing space opens into the wider incident area | Layout comparison |
| Spatial storytelling | The placement of the phone, headphones, car and road markings explains the incident | Evidence map |

### 4.3 Main Spatial Argument

The overhead view explains the whole incident, but the pedestrian and driver views are limited. During free roam, the player moves through the level and connects the separate clues.

> [Explain what each viewpoint reveals or hides and why this improves the road safety lesson.]

---

## 5. Bubble Diagram, Flow and Rhythm

### 5.1 Bubble Diagram

![Road Ready bubble diagram showing the level spaces, gameplay flow and investigation branches](STLD_Assets/STLD_RoadReady_BubbleDiagram.png)

**Figure 5.1. Road Ready level bubble diagram.** Solid arrows show the required phase flow, while dashed arrows show areas that the player may explore in any order before returning to the main crossing. Bubble size represents each area's relative importance and amount of activity rather than its exact physical size.

The crossing is the largest bubble because it acts as the level's main landmark and investigation hub. The pedestrian, vehicle and environmental clue areas form short branches around it, allowing the player to compare both road users without travelling through separate levels. The same crossing is reused for observation, investigation, intervention and resolution, but the camera, available interactions and level pacing change between phases.

### 5.2 Gameplay Flow and Rhythm

| Phase | Player activity | Level-design support | Intensity |
| --- | --- | --- | --- |
| Briefing | Learn the objective and controls | Small space with one clear route forward | Low |
| Observe | Watch the incident | Clear view of both routes and impact point | High |
| Investigate | Explore and inspect clues | Compact loop with landmarks and evidence areas | Low to medium |
| POV replays | Understand limited visibility | Controlled sightlines and occlusion | Medium |
| Intervene | Reach and change hazards | Direct routes and clear interaction targets | Highest |
| Resolve | Watch the outcome | Unblocked view of the crossing | High |
| Debrief | Review the result | Calm visual presentation | Low |

```text
Briefing -> Observe -> Investigate -> POVs -> Intervene -> Resolve -> Debrief
   low       high       low/medium    medium      peak        high       low
```

**[ADD EVIDENCE]** Add the final duration of each phase and explain any pacing changes made after testing.

---

## 6. Level Map and Circulation

### 6.1 Map Placeholder

**[ADD EVIDENCE] Insert the final top-down level map here.**

The map should label:

- Player starting position and intended route.
- Pedestrian and vehicle routes.
- Impact point.
- Evidence and interaction points.
- Intervention starting position.
- Camera positions and directions.
- Main landmark and playable boundaries.
- Terrain area and important lights.

### 6.2 Suggested Legend

| Colour or symbol | Meaning |
| --- | --- |
| Blue line | Player route |
| Red line | Vehicle route |
| Yellow line | Pedestrian route |
| Red circle | Impact point |
| Green circle | Evidence or interaction point |
| Purple cone | Camera sightline |
| Grey area | Non-playable space |
| Green area | Unity Terrain |

### 6.3 Measurements

| Measurement | Final value | Reason |
| --- | --- | --- |
| Total playable area | [TO CONFIRM] | [Explain] |
| Road width | [TO CONFIRM] | Research and gameplay requirements |
| Pavement width | [TO CONFIRM] | Comfortable first-person movement |
| Crossing width | [TO CONFIRM] | [Explain] |
| Vehicle approach distance | [TO CONFIRM] | Must support incident speed and timing |
| Pedestrian approach distance | [TO CONFIRM] | Must support walking speed and timing |
| Investigation loop length | [TO CONFIRM] | Must fit the session length |

### 6.4 Intended Investigation Route

```text
Impact point
    -> Pedestrian evidence
    -> Car exterior
    -> Passenger seat and dashboard evidence
    -> Environmental evidence
    -> Intervention start
```

**[TO CONFIRM]** Decide whether the player may inspect clues in any order or follows a lightly guided route.

### 6.5 Wayfinding

- Keep the main landmark visible from important positions.
- Use road markings and pavements to explain movement.
- Use lighting to draw attention without highlighting every clue.
- Use buildings, planting and barriers to frame the playable area.
- Let the guide character assist only when important evidence is missed.
- Use UI directions only when the environment is not clear enough.

---

## 7. Modular Level Design and Terrain

### 7.1 Modular Approach

The final level will be assembled from reusable pieces made by the team. Greybox tools can be used for planning, but the final build should use the completed modular assets according to tutor guidance.

| Setting | Final choice | Reason |
| --- | --- | --- |
| Unity grid snap | [TO CONFIRM] | [Explain] |
| Main module width | [TO CONFIRM] | [Explain] |
| Wall height | [TO CONFIRM] | [Explain] |
| Road module length | [TO CONFIRM] | [Explain] |
| Pivot convention | [TO CONFIRM] | Supports snapping and replacement |

### 7.2 Proposed Modular Kit

| Category | Pieces |
| --- | --- |
| Road | Straight road, junction, crossing and lane markings |
| Pavement | Straight pavement, corners, kerbs and ramps |
| Exterior | Walls, corners, windows, entrance and roof pieces |
| Training interior | Walls, floors, ceilings, doors and windows |
| Boundaries | Railings, bollards, fences and barriers |
| Landscape | Planters, grass edges and terrain transitions |

### 7.3 Unity Terrain

**[TO CONFIRM]** Give Terrain a real purpose, such as a landscaped campus edge, grass verge, slope or planted buffer beside the road.

| Item | Decision |
| --- | --- |
| Terrain size | [TO CONFIRM] |
| Height variation | [TO CONFIRM] |
| Texture layers | [TO CONFIRM] |
| Trees and planting | [TO CONFIRM] |
| Connection to modular pavement | [TO CONFIRM] |
| Player access | [Playable / visual boundary / mixed] |

**Evidence to add:** greybox, grid settings, modular replacement, reused modules, terrain sculpting and final terrain connection.

---

## 8. Lighting and Atmosphere

The nighttime lighting should support the missing-headlights hazard while keeping the crossing, routes and evidence readable.

| Light or effect | Purpose | Final type |
| --- | --- | --- |
| Streetlights | Show the road shape and safe walking route | [Baked / mixed] |
| Campus lights | Establish the landmark and background | [Baked / mixed] |
| Vehicle headlights | Show the effect of intervention | Real-time |
| Evidence highlights | Give interaction feedback | Real-time or emissive |
| Ambient or moon light | Keep silhouettes readable | Directional |
| Post-processing | Support mood and visual consistency | URP Volume |

**Evidence to add:** greybox lighting, final lighting, post-processing, lightmaps and POV visibility comparison.

---

## 9. Asset List

### 9.1 Environment Assets

| Asset | Purpose | Modular? | Creator | Status |
| --- | --- | --- | --- | --- |
| Road sections | Build the junction and vehicle route | Yes | [Name] | [Status] |
| Pavement and kerbs | Define pedestrian space | Yes | [Name] | [Status] |
| Pedestrian crossing | Main conflict point and landmark | Yes | [Name] | [Status] |
| Campus exterior | Frame the road and establish the setting | Yes | [Name] | [Status] |
| Training room kit | Briefing and debrief space | Yes | [Name] | [Status] |
| Railings and barriers | Control circulation | Yes | [Name] | [Status] |
| Streetlights and signs | Wayfinding and road information | Reusable | [Name] | [Status] |
| Terrain and planting | Natural edge and boundary | No | [Name] | [Status] |

### 9.2 Gameplay Assets

| Asset | Purpose | Source or creator | Status |
| --- | --- | --- | --- |
| Pedestrian character | Main incident participant | [Fill in] | [Status] |
| Incident car | Main incident vehicle | [Fill in] | [Status] |
| Car interior | Driver POV and interactions | [Fill in] | [Status] |
| Phone and headphones | Pedestrian evidence | [Fill in] | [Status] |
| Dashboard controls | Driver evidence and interventions | [Fill in] | [Status] |
| Bystanders | Make the environment feel inhabited | [Fill in] | [Status] |
| Guide character | Help players find missed areas | [Fill in] | [Status] |

### 9.3 Effects and Audio

| Asset | Purpose | Source or creator | Status |
| --- | --- | --- | --- |
| Impact effect | Communicate the collision | [Fill in] | [Status] |
| Slow-motion effect | Show the intervention phase | [Fill in] | [Status] |
| Prevention effect | Communicate a safe outcome | [Fill in] | [Status] |
| Road ambience | Establish the nighttime location | [Fill in] | [Status] |
| Vehicle and collision audio | Incident feedback | [Fill in] | [Status] |

Record the creator, source link and licence of every external asset. Clearly separate team-made modular assets from approved external supporting assets.

---

## 10. Gameplay and NPC Placement

| Element | Proposed location | Level-design reason | Status |
| --- | --- | --- | --- |
| Pedestrian start | Pavement approach | Establish a clear walking route | [TO CONFIRM] |
| Vehicle start | Road approach | Support timing and anticipation | [TO CONFIRM] |
| Impact point | Pedestrian crossing | Readable from overhead and resolve cameras | [TO CONFIRM] |
| Pedestrian evidence | On the pedestrian | Connect clues to her behaviour | [CONFIRMED] |
| Driver evidence | Car interior | Requires a different location and viewpoint | [CONFIRMED] |
| Bystander route | Outside the incident paths | Add life without changing crash timing | [TO CONFIRM] |
| Guide route | Investigation loop | Assist players who miss evidence | [TO CONFIRM] |

### NavMesh Plan

- Bake walkable pavement and investigation spaces.
- Keep scripted incident routes separate from NavMesh movement.
- Stop ambient NPCs from entering the impact path.
- Add a screenshot of the final baked NavMesh.

---

## 11. Playtesting and Refinement

### 11.1 Main Questions

1. Can players understand where to go without help?
2. Can they find evidence on both sides of the incident?
3. Do the viewpoints communicate limited visibility?
4. Can they reach all interventions within the time limit?
5. Are important routes and objects readable at night?
6. Can players explain the road safety lesson afterwards?

### 11.2 Test Record

| Test | Success measure | Result | Change made |
| --- | --- | --- | --- |
| Navigation | Players reach every main area without getting lost | [Fill in] | [Fill in] |
| Evidence | Players find the required clues through exploration | [Fill in] | [Fill in] |
| Intervention route | All four changes are possible within a fair time | [Fill in] | [Fill in] |
| POV readability | Players explain what each road user could see | [Fill in] | [Fill in] |
| Lighting | Routes and evidence stay readable in the build | [Fill in] | [Fill in] |
| Performance | Build meets the team's frame-rate target | [Fill in] | [Fill in] |

### 11.3 Iteration Log

| Version | Problem found | Design change | Evidence |
| --- | --- | --- | --- |
| Greybox 1 | [Fill in] | [Fill in] | [Screenshot] |
| Greybox 2 | [Fill in] | [Fill in] | [Screenshot] |
| Modular pass | [Fill in] | [Fill in] | [Screenshot] |
| Lighting pass | [Fill in] | [Fill in] | [Screenshot] |
| Final build | [Fill in] | [Fill in] | [Screenshot] |

For every major change, show the original version, the problem, the updated version and why it works better.

---

## 12. Level Optimisation

| Method | Planned use | Evidence |
| --- | --- | --- |
| Static batching | Mark non-moving environment pieces as static | Inspector screenshot |
| Baked lighting | Bake suitable environment lighting | Lightmap screenshot |
| Occlusion culling | Avoid rendering hidden environment sections | Occlusion screenshot |
| LOD groups | Reduce detail on distant large assets | LOD screenshot |
| Trim sheet or atlas | Reuse materials across modular assets | Material evidence |
| Simple colliders | Avoid detailed mesh colliders where unnecessary | Collider screenshot |
| Limited NavMesh | Bake only the areas needed by NPCs | NavMesh screenshot |
| VFX limits | Control particle count and overdraw | VFX settings |
| Profiler testing | Check CPU, GPU and memory use | Profiler capture |

| Test device | Resolution and quality | Average FPS | Lowest FPS |
| --- | --- | --- | --- |
| [Device 1] | [Fill in] | [Fill in] | [Fill in] |
| [Device 2] | [Fill in] | [Fill in] | [Fill in] |

---

## 13. Walkthrough Video Plan

**Final file name:** `STLD_RoadReady_Walkthrough.mp4`

| Shot | Show | Explain |
| --- | --- | --- |
| 1 | Full level from above | Main zones and layout |
| 2 | Starting area and player route | Flow and wayfinding |
| 3 | Modular pieces | How the kit builds the environment |
| 4 | Terrain | Its purpose and connection to the level |
| 5 | Crossing and landmark | Main incident focus |
| 6 | Pedestrian and driver POVs | Sightlines and occlusion |
| 7 | Investigation and intervention routes | Pacing and gameplay support |
| 8 | Lighting and atmosphere | Nighttime visibility and mood |
| 9 | Optimisation evidence | Lightmaps, LODs, occlusion or Profiler |
| 10 | Final outcome | How the level supports the learning goal |

Keep the walkthrough focused on level-design decisions instead of retelling the full story.

---

## 14. Team Contribution

| Team member | Main project role | STLD contribution | Evidence |
| --- | --- | --- | --- |
| Adam | Gameplay and integration | [Fill in] | Commits, screenshots or Jira tasks |
| Darryl | Scene assembly and level design | [Fill in] | Maps, diagrams, commits or Jira tasks |
| Marcus | Modular assets, lighting and VFX | [Fill in] | Models, materials or screenshots |
| Isaiah | UX, UI and guide system | [Fill in] | Prototype, testing or Jira tasks |

---

## 15. Final Checklist

### Required GDD Content

- [ ] Final level abstract.
- [ ] Bubble diagram.
- [ ] Gameplay flow and rhythm.
- [ ] Labelled level map and legend.
- [ ] Complete asset list.

### Supporting Evidence

- [ ] Visual research with sources and analysis.
- [ ] Spatial theory connected to actual level decisions.
- [ ] Greybox and modular replacement screenshots.
- [ ] Team-made modular kit and grid settings.
- [ ] Meaningful Unity Terrain use.
- [ ] Circulation, sightlines and occlusion evidence.
- [ ] Lighting and atmosphere development.
- [ ] Playtest findings and visible refinements.
- [ ] Optimisation and performance evidence.
- [ ] Individual contributions.

### Submission

- [ ] Remove all template instructions and status labels.
- [ ] Check image quality, captions, sources and licences.
- [ ] Export `STLD_RoadReady_GDD.pdf`.
- [ ] Export `STLD_RoadReady_Walkthrough.mp4`.
- [ ] Test both files before uploading.

---

## Appendix A. Decisions Still Needed

| Decision | Options | Owner | Deadline | Final choice |
| --- | --- | --- | --- | --- |
| Final road layout | Junction / crossing / campus entrance | [Name] | [Date] | [Fill in] |
| Training area | Separate scene / connected level / opening screen | [Name] | [Date] | [Fill in] |
| Time and weather | Night / dusk / other | [Name] | [Date] | [Fill in] |
| Main landmark | [Options] | [Name] | [Date] | [Fill in] |
| Terrain purpose | [Options] | [Name] | [Date] | [Fill in] |
| Investigation order | Open order / lightly guided route | [Name] | [Date] | [Fill in] |
| Playable boundary | [Options] | [Name] | [Date] | [Fill in] |

## Appendix B. Sources

| Source | Creator or organisation | Link | How it informed the level |
| --- | --- | --- | --- |
| [Title] | [Name] | [URL] | [Explanation] |
