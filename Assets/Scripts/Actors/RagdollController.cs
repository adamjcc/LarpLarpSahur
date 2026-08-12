/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * RagdollController.cs
 * Switches a character between animation and physics on impact.
 */

using UnityEngine;

/// <summary>
/// Lets a character go limp and be thrown by the collision, then puts every bone back for
/// the next replay.
///
/// HOW A RAGDOLL WORKS
/// Unity's Ragdoll Wizard adds a Rigidbody, a collider and a joint to each major bone.
/// While the Animator is running, those Rigidbodies must be kinematic or they fight the
/// animation. Turning the Animator off and the Rigidbodies on hands the body to physics.
///
/// THE PART EVERYONE FORGETS
/// A ragdoll leaves the bones scattered wherever they landed, and Animator.Rebind() cannot
/// fix that because the Animator was switched off while it happened. So this records every
/// bone's starting position at Awake and puts them all back before handing control to the
/// Animator again. Without it, the second playthrough starts with a character folded into
/// an impossible shape.
///
/// The physics itself is not repeatable, and that is fine — it only ever runs AFTER the
/// collision has already been decided, exactly like the debris on the car.
/// </summary>
public class RagdollController : MonoBehaviour
{
    [Tooltip("The character's Animator. Switched off while the ragdoll is active.")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("The collider used for normal walking around, e.g. the capsule on the root. " +
             "Switched off so it cannot fight the ragdoll's own colliders.")]
    [SerializeField] private Collider mainCollider;

    [Tooltip("Root of the skeleton, usually mixamorig:Hips. Everything under it is collected " +
             "automatically.")]
    [SerializeField] private Transform skeletonRoot;

    [Tooltip("How hard the character is thrown on impact.")]
    [SerializeField] private float launchForce = 6f;

    /// <summary>True while physics has control of the body.</summary>
    public bool IsRagdollActive { get; private set; }

    private Rigidbody[] bones;
    private Collider[] boneColliders;

    // Every bone's resting pose, so we can rebuild the character exactly
    private Transform[] boneTransforms;
    private Vector3[] bonePositions;
    private Quaternion[] boneRotations;

    private void Awake()
    {
        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (skeletonRoot == null && characterAnimator != null) skeletonRoot = characterAnimator.transform;

        CollectRagdollParts();
        CacheRestingPose();
        SetRagdollActive(false);
    }

    /// <summary>
    /// Finds only the bits the Ragdoll Wizard created, and nothing else.
    ///
    /// THIS IS THE IMPORTANT BIT. A naive GetComponentsInChildren&lt;Collider&gt;() also picks up
    /// every interaction trigger on the character — her body volume, her headphones, her
    /// phone — and switching those off makes her completely unclickable. So we start from
    /// the Rigidbodies the wizard added and take only the collider sitting on each of those
    /// same objects, skipping triggers entirely.
    /// </summary>
    private void CollectRagdollParts()
    {
        Transform searchRoot = skeletonRoot != null ? skeletonRoot : transform;

        bones = searchRoot.GetComponentsInChildren<Rigidbody>();

        var found = new System.Collections.Generic.List<Collider>();

        foreach (Rigidbody bone in bones)
        {
            if (bone == null) continue;

            // Only colliders on the same GameObject as a ragdoll bone
            foreach (Collider c in bone.GetComponents<Collider>())
            {
                // Triggers are interaction volumes, never ragdoll parts
                if (c != null && !c.isTrigger && c != mainCollider) found.Add(c);
            }
        }

        boneColliders = found.ToArray();
    }

    /// <summary>
    /// Remembers where every bone sits before anything has moved it.
    /// </summary>
    private void CacheRestingPose()
    {
        if (skeletonRoot == null) return;

        boneTransforms = skeletonRoot.GetComponentsInChildren<Transform>();
        bonePositions = new Vector3[boneTransforms.Length];
        boneRotations = new Quaternion[boneTransforms.Length];

        for (int i = 0; i < boneTransforms.Length; i++)
        {
            bonePositions[i] = boneTransforms[i].localPosition;
            boneRotations[i] = boneTransforms[i].localRotation;
        }
    }

    /// <summary>
    /// Hands the body to physics, or takes it back.
    /// </summary>
    /// <param name="active">True to go limp, false to return to animation.</param>
    public void SetRagdollActive(bool active)
    {
        IsRagdollActive = active;

        // Animation and physics cannot both drive the bones
        if (characterAnimator != null) characterAnimator.enabled = !active;
        if (mainCollider != null) mainCollider.enabled = !active;

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;

            bones[i].isKinematic = !active;

            if (!active)
            {
                // Stop dead, or leftover motion would carry into the next replay
                bones[i].linearVelocity = Vector3.zero;
                bones[i].angularVelocity = Vector3.zero;
            }
        }

        // Only the wizard's own colliders, collected in CollectRagdollParts. Interaction
        // triggers are never in this list, so she stays clickable throughout.
        foreach (Collider c in boneColliders)
        {
            if (c != null) c.enabled = active;
        }

        // Put every bone back before the Animator takes over again
        if (!active) RestoreRestingPose();
    }

    /// <summary>
    /// Goes limp and throws the body in the given direction.
    /// Called by PedestrianVictim the moment the car reaches her.
    /// </summary>
    /// <param name="direction">Which way the car was travelling.</param>
    public void Launch(Vector3 direction)
    {
        SetRagdollActive(true);

        // Angle it slightly upward, which reads far better than a flat shove
        Vector3 force = (direction.normalized + Vector3.up * 0.4f).normalized * launchForce;

        foreach (Rigidbody bone in bones)
        {
            if (bone != null) bone.AddForce(force, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Rebuilds the skeleton exactly as it was before the ragdoll scattered it.
    /// </summary>
    private void RestoreRestingPose()
    {
        if (boneTransforms == null) return;

        for (int i = 0; i < boneTransforms.Length; i++)
        {
            if (boneTransforms[i] == null) continue;

            boneTransforms[i].localPosition = bonePositions[i];
            boneTransforms[i].localRotation = boneRotations[i];
        }
    }
}
