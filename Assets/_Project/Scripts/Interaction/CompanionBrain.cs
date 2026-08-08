using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CompanionBrain : MonoBehaviour
{
    public enum CompanionState { FollowingPlayer, LeadingToClue }
    public CompanionState currentState = CompanionState.FollowingPlayer;

    [Header("Targets")]
    public Transform player;
    [Tooltip("Drag your clue/hazard GameObjects here for testing")]
    public Transform[] allClues; 

    [Header("Settings")]
    public float followDistance = 3f;
    
    private NavMeshAgent agent;
    private Transform currentClueTarget;
    
    // Fake ledger for sandbox testing
    private int currentClueIndex = 0; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = followDistance;
    }

    void Update()
    {
        switch (currentState)
        {
            case CompanionState.FollowingPlayer:
                FollowPlayer();
                break;
            case CompanionState.LeadingToClue:
                LeadToClue();
                break;
        }

        // TESTING: Press 'H' (for Hint) to tell the dog to find an unexamined clue
        if (Input.GetKeyDown(KeyCode.H))
        {
            FindUnexaminedClue();
        }
    }

    void FollowPlayer()
    {
        if (player != null)
        {
            // stay a bit behind the player
            agent.stoppingDistance = followDistance; 
            agent.SetDestination(player.position);
        }
    }

    void LeadToClue()
    {
        if (currentClueTarget != null)
        {
            // get right up to the clue
            agent.stoppingDistance = 1.5f; 
            agent.SetDestination(currentClueTarget.position);

            // if the helper has reached the clue, rotate to look at the player
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (player != null)
                {
                    Vector3 lookPos = player.position - transform.position;
                    lookPos.y = 0; // Keep the dog level
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 5f);
                }
            }
        }
    }

    public void FindUnexaminedClue()
    {
        // TODO: In the final build, this will ask Adam's EvidenceLedger.
        // For now, we just cycle through the array to test the movement.
        if (allClues.Length == 0) return;

        if (currentClueIndex < allClues.Length)
        {
            currentClueTarget = allClues[currentClueIndex];
            currentState = CompanionState.LeadingToClue;
            currentClueIndex++;
        }
        else
        {
            Debug.Log("All clues found! Going back to following the player.");
            currentState = CompanionState.FollowingPlayer;
        }
    }

    // You can call this from your Interaction script when a player clicks the clue
    public void OnClueExaminedByPlayer()
    {
        currentState = CompanionState.FollowingPlayer;
    }
}