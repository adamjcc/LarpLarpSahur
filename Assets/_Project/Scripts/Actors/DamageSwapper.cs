using UnityEngine;

/// Swaps an intact model for a damaged one. This is your 3RT tutor's advice, and it is the
/// right call — real mesh deformation would eat days and cannot be reset cleanly, whereas
/// this resets in one line for every replay.
///
/// Setup: make two child GameObjects under the car, "Body_Intact" and "Body_Damaged",
/// and drag them into the two slots. For the grey-box test, two differently coloured cubes
/// is genuinely enough.
public class DamageSwapper : MonoBehaviour
{
    [SerializeField] private GameObject intactVersion;
    [SerializeField] private GameObject damagedVersion;

    [Tooltip("Optional: loose bits (hubcap, wing mirror, her phone) that get thrown on " +
             "impact. These need a Rigidbody. This is the genuine Unity Physics in the game.")]
    [SerializeField] private Rigidbody[] debris;

    [SerializeField] private float debrisForce = 4f;

    // Where the debris started RELATIVE TO THE CAR, so a replay can put it back correctly
    // even though the car itself has moved. Storing world positions would drop the hubcap
    // back at the impact point every replay instead of back on the wheel.
    private Vector3[] debrisStartLocalPositions;
    private Quaternion[] debrisStartLocalRotations;
    private Transform[] debrisStartParents;

    public bool IsDamaged { get; private set; }

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

    public void SetDamaged(bool damaged)
    {
        IsDamaged = damaged;

        if (intactVersion != null) intactVersion.SetActive(!damaged);
        if (damagedVersion != null) damagedVersion.SetActive(damaged);

        for (int i = 0; i < debris.Length; i++)
        {
            Rigidbody rb = debris[i];
            if (rb == null) continue;

            if (damaged)
            {
                // Unparent first, or the debris would keep following the car as it slides
                rb.transform.SetParent(null, true);

                // Let physics take over and throw it.
                // Random is fine HERE — debris is pure decoration after the outcome has
                // already been decided. It must never appear anywhere in the crash actors.
                rb.isKinematic = false;
                rb.gameObject.SetActive(true);

                Vector3 direction = (Random.onUnitSphere + Vector3.up).normalized;
                rb.AddForce(direction * debrisForce, ForceMode.Impulse);
            }
            else
            {
                // Freeze it and put it back exactly where it started, ready for the replay
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.transform.SetParent(debrisStartParents[i], false);
                rb.transform.localPosition = debrisStartLocalPositions[i];
                rb.transform.localRotation = debrisStartLocalRotations[i];
            }
        }
    }
}
