/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * AmbientVehicle.cs
 * Background traffic that loops a waypoint route and brakes for obstacles.
 */

using UnityEngine;

public class AmbientVehicle : MonoBehaviour
{
    public enum CarState { Driving, Braking }
    public CarState currentState = CarState.Driving;

    [Header("Movement Settings")]
    public float maxSpeed = 10f;
    public float minTurnSpeed = 3f; 
    public float turnSpeed = 5f;
    
    // Made public again so you can rapidly assign them in the Inspector
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("Sensor Settings")]
    public float sensorLength = 2f;
    public LayerMask obstacleMask; 

    void Start()
    {
        // Instantly calculates the nearest waypoint the moment you hit Play
        FindClosestWaypoint();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        CheckForObstacles();

        if (currentState == CarState.Driving)
        {
            DriveAlongPath();
        }
    }

    void FindClosestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float closestDistance = Mathf.Infinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[i].position);
            
            if (distanceToWaypoint < closestDistance)
            {
                closestDistance = distanceToWaypoint;
                currentWaypointIndex = i;
            }
        }
    }

    void DriveAlongPath()
    {
        Transform target = waypoints[currentWaypointIndex];

        Vector3 flatTargetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = flatTargetPosition - transform.position;
        
        float currentSpeed = maxSpeed; 

        if (direction != Vector3.zero)
        {
            float angleToTarget = Vector3.Angle(transform.forward, direction);
            float speedMultiplier = 1f - Mathf.Clamp01(angleToTarget / 60f);
            currentSpeed = Mathf.Lerp(minTurnSpeed, maxSpeed, speedMultiplier);

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, flatTargetPosition, currentSpeed * Time.deltaTime);

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