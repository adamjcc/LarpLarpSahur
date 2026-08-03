/// The contract for anything the player can look at and click.
///
/// WHAT AN INTERFACE IS, and why this one matters more than any other file in the project:
/// An interface is a promise. It lists members a class must have, but contains no code
/// itself. PlayerInteractor talks ONLY to this promise — it has no idea whether it is
/// pointing at a phone, a headlight switch, a person or a car door.
///
/// That means you can invent ten new kinds of clickable thing and never touch the raycaster
/// again. The alternative — the version your old PlayerRaycaster used — was
/// GetComponent<InteractableEvidence>(), which would have grown into five different
/// GetComponent checks stacked on top of each other.
public interface IInteractable
{
    /// Text shown at the bottom of the screen, e.g. "Examine her headphones".
    string Prompt { get; }

    /// False greys it out entirely — no highlight, no prompt, no click.
    /// This is how a dashboard button stays unreachable until you are inside the car.
    bool IsAvailable { get; }

    /// How close the player must be. Small props want ~3.5 m; a person wants 12 m.
    float MaxDistance { get; }

    /// The player just looked at me.
    void OnFocusEnter();

    /// The player just looked away.
    void OnFocusExit();

    /// The player clicked me.
    void OnInteract();
}
