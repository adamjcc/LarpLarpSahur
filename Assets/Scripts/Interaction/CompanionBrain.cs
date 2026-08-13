/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * CompanionBrain.cs
 * The robot that follows the player around and analyses them.
 */

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The analysis robot that trails the player through the investigation.
///
/// It keeps its distance on purpose. In the story it is recording the recruit, not helping
/// them, so it hangs back and watches rather than getting underfoot.
///
/// It moves with a NavMeshAgent, so it walks around walls and furniture instead of pushing
/// through them.
///
/// STATE DIAGRAM
///
///     Idle  --(player walks away)-->  Following
///       ^                                 |
///       |______(caught up again)__________|
///
///     Standby  &lt;-- any cutscene, replay or point-of-view camera
///
/// Standby matters most for how the game looks: during a replay or from inside someone's
/// eyes, a robot floating in shot would ruin it. So it stops and hides itself until the
/// player is back on their own feet.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CompanionBrain : MonoBehaviour
{
    /// <summary>What the robot is currently doing.</summary>
    public enum CompanionState
    {
        /// <summary>Close enough. Hovers in place and watches the player.</summary>
        Idle,

        /// <summary>Player has walked off. Catching up.</summary>
        Following,

        /// <summary>Hidden and still, because a cutscene or point-of-view camera is running.</summary>
        Standby
    }

    [Header("Who it follows")]
    /// <summary>The player. Found by tag if left empty.</summary>
    [Tooltip("Leave empty and it finds the object tagged Player.")]
    [SerializeField] private Transform player;

    [Header("How closely")]
    /// <summary>How far back it settles once it has caught up.</summary>
    [Tooltip("Metres. It stops here rather than crowding the player.")]
    [SerializeField] private float followDistance = 4f;

    /// <summary>
    /// How much further than followDistance the player has to get before it bothers moving.
    /// Without this gap it would twitch between Idle and Following on the spot.
    /// </summary>
    [Tooltip("Extra slack before it starts moving again. Stops it jittering.")]
    [SerializeField] private float catchUpDistance = 6f;

    [Header("Hover")]
    /// <summary>
    /// How high it floats above the ground.
    ///
    /// This drives the agent's Base Offset rather than the transform, because a NavMeshAgent
    /// owns its own position — moving the transform directly fights it and the robot ends up
    /// stuck or sinking.
    /// </summary>
    [SerializeField] private float hoverHeight = 1.4f;

    /// <summary>How far it drifts up and down while hovering.</summary>
    [SerializeField] private float bobAmount = 0.12f;

    /// <summary>How quickly it bobs.</summary>
    [SerializeField] private float bobSpeed = 2f;

    [Header("Facing")]
    /// <summary>How quickly it turns to face the player.</summary>
    [SerializeField] private float turnSpeed = 5f;

    [Header("Read-only")]
    [SerializeField] private CompanionState state = CompanionState.Idle;

    private NavMeshAgent agent;
    private ScenarioDirector director;
    private Renderer[] renderers;

    /// <summary>
    /// Sets the agent up, finds the player, and makes sure the robot is actually standing
    /// on the NavMesh before anything tries to move it.
    /// </summary>
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // We handle turning ourselves. Left on, the agent also rotates the robot to face
        // wherever it is moving, and the two fight — which looks like the model tipping
        // or spinning oddly.
        agent.updateRotation = false;

        agent.stoppingDistance = followDistance;

        director = FindFirstObjectByType<ScenarioDirector>();
        renderers = GetComponentsInChildren<Renderer>();

        if (player == null)
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("[CompanionBrain] No player found. Drag the player in, or tag " +
                             "it 'Player'.", this);
        }

        PlaceOnNavMesh();
    }

    /// <summary>
    /// Drops the robot onto the nearest point of the NavMesh.
    ///
    /// THIS IS THE USUAL REASON A FOLLOWER DOES NOT MOVE. A NavMeshAgent that is not sitting
    /// on the NavMesh ignores SetDestination completely and does nothing at all. If the
    /// robot was placed floating in the air, or slightly off the walkable surface, it never
    /// gets attached — so we search nearby and warp it on.
    /// </summary>
    private void PlaceOnNavMesh()
    {
        if (agent.isOnNavMesh) return;

        // Look for walkable ground within 10 metres of wherever it was put
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return;
        }

        Debug.LogError("[CompanionBrain] This robot is not on a NavMesh and there is none " +
                       "within 10 metres, so it cannot move.\n" +
                       "   CHECK: has the NavMeshSurface been baked since the level moved?\n" +
                       "   CHECK: does the agent's Agent Type match the one the surface was " +
                       "baked with?\n" +
                       "   CHECK: is the robot placed over walkable ground rather than off " +
                       "the edge of the level?", this);
    }

    private void Update()
    {
        if (player == null || agent == null) return;

        // Work out whether the robot is allowed to be seen at all right now
        if (!ShouldBeVisible())
        {
            ChangeState(CompanionState.Standby);
            return;
        }

        // Coming back from Standby: start by catching up
        if (state == CompanionState.Standby) ChangeState(CompanionState.Following);

        // Nothing below works until the agent is attached to the NavMesh
        if (!agent.isOnNavMesh) return;

        // Distance measured flat, ignoring height, so hovering does not count as being
        // further away
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        switch (state)
        {
            case CompanionState.Idle:
                // Only start moving once the player is properly away, not the moment
                // they take one step
                if (distance > catchUpDistance) ChangeState(CompanionState.Following);
                break;

            case CompanionState.Following:
                agent.SetDestination(player.position);

                if (distance <= followDistance) ChangeState(CompanionState.Idle);
                break;
        }

        UpdateHover();
        FacePlayer(toPlayer);
    }

    /// <summary>
    /// Floats the robot at the right height by nudging the agent's Base Offset, which is
    /// the only safe way to change a NavMeshAgent's height.
    /// </summary>
    private void UpdateHover()
    {
        agent.baseOffset = hoverHeight + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
    }

    /// <summary>Turns smoothly to face the player, staying upright.</summary>
    private void FacePlayer(Vector3 toPlayer)
    {
        if (toPlayer.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(toPlayer),
                                              turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// True only in the parts of the game where the player is walking around as themselves.
    ///
    /// Hidden during the menu, every replay, and any time the camera is inside someone
    /// else's head — a robot drifting through those shots would look like a bug.
    /// </summary>
    private bool ShouldBeVisible()
    {
        if (director == null) return true;

        // Never while looking through another character's eyes or sitting in the car
        if (director.IsInNpcView) return false;

        return director.Phase == GamePhase.Briefing ||
               director.Phase == GamePhase.FreeRoam ||
               director.Phase == GamePhase.Intervene;
    }

    /// <summary>
    /// Switches state and applies whatever that state needs — hiding the meshes and
    /// stopping the agent for Standby, letting it move again for the other two.
    /// </summary>
    private void ChangeState(CompanionState next)
    {
        if (state == next) return;
        state = next;

        bool hidden = next == CompanionState.Standby;

        // isStopped can only be set while the agent is actually on the NavMesh
        if (agent.isOnNavMesh) agent.isStopped = hidden;

        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = !hidden;
        }
    }
}
