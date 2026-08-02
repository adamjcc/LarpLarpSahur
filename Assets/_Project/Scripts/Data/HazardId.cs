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

    // ---- Red herrings: clickable, described, change nothing ----
    PedestrianUmbrella,
    CarWipers,
    CarRadio
}
