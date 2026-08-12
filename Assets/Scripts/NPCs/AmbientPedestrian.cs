/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * AmbientPedestrian.cs
 * Background pedestrian that wanders using Unity NavMesh.
 */

using UnityEngine;
using UnityEngine.AI;

public class AmbientPedestrian : MonoBehaviour
{
    public enum NPCState { Roaming, Stopped }
    public NPCState currentState;

    public float roamRadius = 15f; 
    
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // initialize using our new transition method
        ChangeState(NPCState.Roaming); 
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.Roaming:
                // We no longer set isStopped = false here every frame
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    GoToRandomPoint(); 
                }
                break;

            case NPCState.Stopped:
                // We no longer set isStopped = true here every frame
                break;
        }
    }

    // handle the one-time logic when switching states
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
            //start movement if they don't already have a path
            if (!agent.hasPath) GoToRandomPoint();
        }
    }

    void GoToRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position; 
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    public void FreezeNPC()
    {
        
        ChangeState(NPCState.Stopped);
    }
}