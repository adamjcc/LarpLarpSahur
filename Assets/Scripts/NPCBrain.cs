using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    // Our updated states
    public enum NPCState { Roaming, Stopped }
    public NPCState currentState;

    // distance the NPC is allowed to wander from their current spot
    public float roamRadius = 15f; 
    
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = NPCState.Roaming;
        
        // first random spot when the game starts
        GoToRandomPoint(); 
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.Roaming:
                agent.isStopped = false;
                
                // check if the NPC has arrived at their destination (within 0.5 units)
                // helps tell if they aren't currently calculating a path
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    GoToRandomPoint(); // Pick a new random spot!
                }
                break;

            case NPCState.Stopped:
                agent.isStopped = true; 
                break;
        }
    }

    // idk what this function that does the math to find a valid spot on the blue NavMesh
    void GoToRandomPoint()
    {
        // pick a completely random point inside a virtual sphere
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        
        // add our NPC's current position to it so the sphere centers around them
        randomDirection += transform.position; 
        
        NavMeshHit hit;
        // ask Unity to find the closest valid blue NavMesh spot near that random point
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1))
        {
            // 4. Tell the agent to go there
            agent.SetDestination(hit.position);
        }
    }
    
    public void FreezeNPC()
    {
        currentState = NPCState.Stopped;
    }
}