using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public enum CarState { Driving, Braking }
    public CarState currentState = CarState.Driving;

    [Header("Movement Settings")]
    public Transform[] waypoints;
    public float speed = 10f;
    public float turnSpeed = 5f;
    private int currentWaypointIndex = 0;

    [Header("Sensor Settings")]
    public float sensorLength = 2f;

    void Update()
    {
        CheckForObstacles();

        if (currentState == CarState.Driving)
        {
            DriveAlongPath();
        }
    }

    void DriveAlongPath()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];

        // use the target's X and Z, but keep the car's current Y height
        Vector3 flatTargetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);

        // move forward towards the flattened target (prevents driving into the ground a prev tests)
        transform.position = Vector3.MoveTowards(transform.position, flatTargetPosition, speed * Time.deltaTime);

        // calculate direction based on the flat target (prevents slanting)
        Vector3 direction = flatTargetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }

        // check if reached the waypoint
        if (Vector3.Distance(transform.position, flatTargetPosition) < 0.5f)
        {
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0; 
            }
        }
    }

    void CheckForObstacles()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f); 

        Debug.DrawRay(rayStart, transform.forward * sensorLength, Color.yellow);

        if (Physics.Raycast(rayStart, transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("NPC") || hit.collider.CompareTag("Vehicle"))
            {
                currentState = CarState.Braking;
            }
            else
            {
                currentState = CarState.Driving;
            }
        }
        else
        {
            currentState = CarState.Driving;
        }
    }
}