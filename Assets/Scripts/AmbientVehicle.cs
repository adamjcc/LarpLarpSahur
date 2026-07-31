using UnityEngine;

public class AmbientVehicle : MonoBehaviour
{
    public enum CarState { Driving, Braking }
    public CarState currentState = CarState.Driving;

    [Header("Movement Settings")]
    public Transform[] waypoints;
    public float speed = 10f;
    public float turnSpeed = 5f;
    private int currentWaypointIndex = 0;

    [Header("Sensor Settings")]
    public float sensorLength = 6f;
    
    
    public LayerMask obstacleMask; 

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

       
        Vector3 flatTargetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTargetPosition, speed * Time.deltaTime);

        Vector3 direction = flatTargetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }

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

        // raycast now filters by the obstacleMask 
        if (Physics.Raycast(rayStart, transform.forward, out hit, sensorLength, obstacleMask))
        {
            currentState = CarState.Braking;
        }
        else
        {
            currentState = CarState.Driving;
        }
    }
}