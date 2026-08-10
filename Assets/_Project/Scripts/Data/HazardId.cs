/// Every clickable contributing factor and red herring in the scenario.
///
/// One enum covers both meanings: it names the thing the player LOOKS at, and it names the
/// change that happens when they ACT on it. "PedestrianPhone" is both the phone in her hand
/// and the fact that she has put it away.
///
/// An enum is just a fixed list of named options. Using one instead of loose strings means
/// Unity gives you a dropdown in the Inspector and typos become compile errors instead of
/// silent bugs.
public enum HazardId
{
    // ---- Pedestrian: real contributing factors ----
    PedestrianPhone,        // looking down at her phone while walking
    PedestrianHeadphones,   // noise-cancelling headphones, can't hear the car

    // ---- Driver: real contributing factors ----
    CarHeadlights,          // headlights off at night
    CarSpeed,               // over the posted limit

    // ---- Driver: optional 5th, build last ----
    CarDrinkBottle,         // open drink container in the car

    // ------------------------------------------------------------------
    //  NON-CONTRIBUTING OBJECTS
    //
    //  Everything below can be highlighted, examined and clicked exactly like a real
    //  hazard, but none of it counts toward the 4/4 needed to prevent the collision.
    //  Tick "Is Red Herring" on the component and the game tells the player plainly
    //  that changing it would not have helped.
    //
    //  They matter more than they look: an investigation where every clickable thing
    //  is a correct answer is not an investigation, it is a checklist.
    //  Add as many as you like — this list is free to grow.
    // ------------------------------------------------------------------

    // On the pedestrian
    PedestrianUmbrella,
    PedestrianBag,
    PedestrianShoes,
    PedestrianHat,
    PedestrianSodaCan,

    // In or on the car
    CarWipers,
    CarRadio,
    CarMirror,
    CarSeatbelt,
    CarGloveBox,
    CarHandbrake,
    CarGearStick,

    // Around the scene
    RoadSign,
    StreetLight,
    RoadMarking,
    KerbRamp,
    DrainCover
}
