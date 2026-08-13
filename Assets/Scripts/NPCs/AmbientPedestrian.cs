/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * AmbientPedestrian.cs
 * Background pedestrian that wanders using Unity NavMesh.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A background pedestrian who wanders the level so the streets do not feel empty.
///
/// They pick a random walkable point, walk to it, then pick another. They can also be told
/// to stop, so they do not stroll through the collision while it is playing.
///
/// The walking animation is driven from how fast the NavMeshAgent is actually moving, so
/// the feet match the movement without anything having to tell the character when to walk.
///
/// STATE DIAGRAM
///
///     Roaming  --(arrived)-->  pick a new point  -->  Roaming
///        |
///        v
///     Stopped
/// </summary>
public class AmbientPedestrian : MonoBehaviour
{
    /// <summary>What the pedestrian is currently doing.</summary>
    public enum NPCState
    {
        /// <summary>Walking to a random point, then choosing another.</summary>
        Roaming,

        /// <summary>Standing still.</summary>
        Stopped
    }

    /// <summary>Current behaviour. Visible in the Inspector for debugging.</summary>
    public NPCState currentState;

    /// <summary>How far from its current spot it will pick a new destination.</summary>
    public float roamRadius = 15f;

    [Header("Animation")]
    /// <summary>
    /// The character's Animator. Found in the children if left empty, which is where it
    /// lives when the model is a child of the NavMeshAgent object.
    /// </summary>
    [SerializeField] private Animator animator;

    /// <summary>
    /// How fast it has to be moving before the walk animation starts.
    /// Stops the feet twitching when the agent is only drifting into position.
    /// </summary>
    [SerializeField] private float walkThreshold = 0.15f;

    private NavMeshAgent agent;

    // Which parameters this Animator Controller actually has. Setting one that does not
    // exist logs a warning every single frame, and with several pedestrians in the scene
    // that buries everything else in the Console.
    private HashSet<string> animatorParameters;

    /// <summary>Finds the agent and animator, then starts wandering.</summary>
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();

        ChangeState(NPCState.Roaming);
    }

    /// <summary>Records which parameters the controller has, once.</summary>
    private void CacheAnimatorParameters()
    {
        animatorParameters = new HashSet<string>();

        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            animatorParameters.Add(p.name);
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case NPCState.Roaming:
                // Arrived? Pick somewhere new to go.
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    GoToRandomPoint();
                }
                break;

            case NPCState.Stopped:
                break;
        }

        UpdateAnimation();
    }

    /// <summary>
    /// Matches the animation to how fast the agent is really travelling.
    ///
    /// Sets whichever parameters the controller happens to have — a "Walking" bool for the
    /// stock Hodaart controller, or a "Speed" float for a blend tree — so this works with
    /// either without being changed.
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Flat speed, ignoring any up or down movement on slopes
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (animatorParameters.Contains("Walking"))
        {
            animator.SetBool("Walking", speed > walkThreshold);
        }

        if (animatorParameters.Contains("Speed"))
        {
            animator.SetFloat("Speed", speed);
        }
    }

    /// <summary>
    /// Switches state and does the one-off work that switching needs.
    /// </summary>
    /// <param name="newState">The state to move into.</param>
    public void ChangeState(NPCState newState)
    {
        currentState = newState;

        if (currentState == NPCState.Stopped)
        {
            agent.isStopped = true;
        }
        else if (currentState == NPCState.Roaming)
        {
            agent.isStopped = false;

            // Start walking if they do not already have somewhere to be
            if (!agent.hasPath) GoToRandomPoint();
        }
    }

    /// <summary>
    /// Picks a random walkable point nearby and heads for it.
    /// </summary>
    private void GoToRandomPoint()
    {
        // A random point in a sphere around where they are standing
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;

        // That point is probably not on the NavMesh, so ask for the nearest one that is
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>Stops this pedestrian where they stand.</summary>
    public void FreezeNPC()
    {
        ChangeState(NPCState.Stopped);
    }
}
