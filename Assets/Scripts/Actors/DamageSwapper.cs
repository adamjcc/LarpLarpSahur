/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * DamageSwapper.cs
 * Swaps the car between its intact and damaged versions.
 */

using UnityEngine;

/// <summary>
/// Shows one set of objects before the crash and a different set afterwards.
///
/// Our 3RT tutor suggested this instead of really deforming the mesh, and it turned out to
/// be the right call for another reason too: the incident replays over and over, and a mesh
/// swap resets in one line while real deformation would not reset at all.
///
/// Anything you do NOT drag into either array is simply left alone, so shared parts like the
/// wheels and the interior can stay put while only the bodywork changes.
/// </summary>
public class DamageSwapper : MonoBehaviour
{
    [Header("Before the crash")]
    /// <summary>Objects that are visible while the car is still undamaged.</summary>
    [Tooltip("Drag in the clean bodywork, e.g. _CARPROPER. Hidden the moment the car is hit.")]
    [SerializeField] private GameObject[] intactObjects;

    [Header("After the crash")]
    /// <summary>Objects that only appear once the car has been hit.</summary>
    [Tooltip("Drag in the wrecked bodywork, e.g. _CARDAMAGED. Hidden until the car is hit.")]
    [SerializeField] private GameObject[] damagedObjects;

    [Header("Optional loose parts")]
    /// <summary>
    /// Small parts thrown loose on impact. These need a Rigidbody, and they are the game's
    /// clearest use of real Unity physics.
    /// </summary>
    [Tooltip("Optional. A hubcap, a wing mirror. Each needs a Rigidbody.")]
    [SerializeField] private Rigidbody[] debris;

    /// <summary>How hard the loose parts are thrown.</summary>
    [SerializeField] private float debrisForce = 4f;

    // Where each piece of debris started, RELATIVE to the car. Storing world positions
    // instead would drop the hubcap back at the impact point on every replay rather than
    // back on the wheel, because the car itself has moved by then.
    private Vector3[] debrisStartLocalPositions;
    private Quaternion[] debrisStartLocalRotations;
    private Transform[] debrisStartParents;

    /// <summary>True once the car has been switched to its damaged state.</summary>
    public bool IsDamaged { get; private set; }

    /// <summary>
    /// Remembers where the debris started, then puts the car into its undamaged state.
    /// </summary>
    private void Awake()
    {
        debrisStartLocalPositions = new Vector3[debris.Length];
        debrisStartLocalRotations = new Quaternion[debris.Length];
        debrisStartParents = new Transform[debris.Length];

        for (int i = 0; i < debris.Length; i++)
        {
            if (debris[i] == null) continue;

            debrisStartLocalPositions[i] = debris[i].transform.localPosition;
            debrisStartLocalRotations[i] = debris[i].transform.localRotation;
            debrisStartParents[i] = debris[i].transform.parent;
        }

        SetDamaged(false);
    }

    /// <summary>
    /// Switches between the clean car and the wrecked one.
    ///
    /// Called with true by IncidentVehicle the moment the collision is detected, and with
    /// false every time the scenario resets — which is why a prevented crash never shows
    /// the damaged version.
    /// </summary>
    /// <param name="damaged">True to show the wreck, false to show the clean car.</param>
    public void SetDamaged(bool damaged)
    {
        IsDamaged = damaged;

        SetActiveAll(intactObjects, !damaged);
        SetActiveAll(damagedObjects, damaged);

        for (int i = 0; i < debris.Length; i++)
        {
            Rigidbody rb = debris[i];
            if (rb == null) continue;

            if (damaged) ThrowDebris(rb);
            else RestoreDebris(rb, i);
        }
    }

    /// <summary>Shows or hides every object in an array, skipping any empty slots.</summary>
    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(active);
        }
    }

    /// <summary>Detaches one loose part and lets physics throw it.</summary>
    private void ThrowDebris(Rigidbody rb)
    {
        // Unparent first, or the part would keep sliding along with the car
        rb.transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.gameObject.SetActive(true);

        // Random is safe HERE because it is pure decoration applied after the outcome has
        // already been decided. It must never appear anywhere in the crash actors.
        Vector3 direction = (Random.onUnitSphere + Vector3.up).normalized;
        rb.AddForce(direction * debrisForce, ForceMode.Impulse);
    }

    /// <summary>Freezes one loose part and puts it back where it started.</summary>
    private void RestoreDebris(Rigidbody rb, int index)
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.transform.SetParent(debrisStartParents[index], false);
        rb.transform.localPosition = debrisStartLocalPositions[index];
        rb.transform.localRotation = debrisStartLocalRotations[index];
    }
}
