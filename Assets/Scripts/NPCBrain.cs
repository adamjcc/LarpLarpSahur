using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    
    public enum NPCState { Walking, Stopped }
    public NPCState currentState;

    
    public Transform myDestination; 
    
    private NavMeshAgent agent;

    void Start()
    {
        // grab the NavMeshAgent 
        agent = GetComponent<NavMeshAgent>();
        
        // start the game in the walking state
        currentState = NPCState.Walking; 
    }

    void Update()
    {
        //checks what state we are in every frame
        switch (currentState)
        {
            case NPCState.Walking:
                agent.isStopped = false;
                // Tell the agent to walk to the target
                if (myDestination != null)
                {
                    agent.SetDestination(myDestination.position);
                }
                break;

            case NPCState.Stopped:
                // Instantly freeze the agent in place
                agent.isStopped = true; 
                break;
        }
    }

    //call this later from your Raycast script to freeze the NPCs
    public void FreezeNPC()
    {
        currentState = NPCState.Stopped;
    }
}