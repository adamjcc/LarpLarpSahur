/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * IInteractable.cs
 * The contract every clickable object in the game implements.
 */

/// <summary>
/// The contract for anything the player can look at and click.
///
/// WHAT AN INTERFACE IS, and why this one matters more than any other file in the project:
/// An interface is a promise. It lists members a class must have, but contains no code
/// itself. PlayerInteractor talks ONLY to this promise — it has no idea whether it is
/// pointing at a phone, a headlight switch, a person or a car door.
///
/// That means you can invent ten new kinds of clickable thing and never touch the raycaster
/// again. The alternative — the version your old PlayerRaycaster used — was
/// GetComponent&lt;InteractableEvidence&gt;(), which would have grown into five different
/// GetComponent checks stacked on top of each other.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Text shown at the bottom of the screen, e.g. "Examine her headphones".
    /// </summary>
    string Prompt { get; }

    /// <summary>
    /// False greys it out entirely — no highlight, no prompt, no click.
    /// This is how a dashboard button stays unreachable until you are inside the car.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// How close the player must be. Small props want ~3.5 m; a person wants 12 m.
    /// </summary>
    float MaxDistance { get; }

    /// <summary>
    /// The player just looked at me.
    /// </summary>
    void OnFocusEnter();

    /// <summary>
    /// The player just looked away.
    /// </summary>
    void OnFocusExit();

    /// <summary>
    /// The player clicked me.
    /// </summary>
    void OnInteract();
}
