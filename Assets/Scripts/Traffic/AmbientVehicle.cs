/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * AmbientVehicle.cs
 * Background traffic that loops a waypoint route and brakes for obstacles.
 */

using UnityEngine;

/// <summary>
/// Controls the movement and basic obstacle detection for an ambient vehicle
/// The vehicle drives along a defined array of waypoints and slows down during turns
/// </summary>
public class AmbientVehicle : MonoBehaviour
{
    /// <summary>
    /// Represents the current action state of the car
    /// </summary>
    public enum CarState { Driving, Braking }
    
    /// <summary>
    /// The current state of the vehicle. Defaults to Driving
    /// </summary>
    public CarState currentState = CarState.Driving;

    [Header("Movement Settings")]
    /// <summary>
    /// The maximum straight-line speed of the vehicle
    /// </summary>
    public float maxSpeed = 10f;
    
    /// <summary>
    /// The minimum speed the vehicle slows down to when taking a sharp turn
    /// </summary>
    public float minTurnSpeed = 3f; 
    
    /// <summary>
    /// How quickly the vehicle rotates to face its target waypoint
    /// </summary>
    public float turnSpeed = 5f;
    
    /// <summary>
    /// The array of waypoints the vehicle will follow
    /// Must be assigned in the Inspector.
    /// </summary>
    public Transform[] waypoints;
    
    /// <summary>
    /// The index of the waypoint the vehicle is currently driving towards
    /// </summary>
    private int currentWaypointIndex = 0;

    [Header("Sensor Settings")]
    /// <summary>
    /// The distance the front raycast shoots out to detect obstacles
    /// </summary>
    public float sensorLength = 2f;
    
    /// <summary>
    /// The layers that the sensor considers as obstacles (e.g., other cars, traffic lights)
    /// </summary>
    public LayerMask obstacleMask; 

    /// <summary>
    /// Initializes the vehicle by finding the closest waypoint so it doesn't 
    /// automatically drive to the first array index if placed mid-route
    /// </summary>
    void Start()
    {
        FindClosestWaypoint();
    }

    /// <summary>
    /// Called once per frame. Handles obstacle detection and movement state
    /// </summary>
    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        CheckForObstacles();

        if (currentState == CarState.Driving)
        {
            DriveAlongPath();
        }
    }

    /// <summary>
    /// Calculates the distance to all waypoints in the array and sets the 
    /// current target to the one physically closest to the vehicle's starting position
    /// </summary>
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

    /// <summary>
    /// Moves the vehicle towards the current waypoint, handles rotation, 
    /// dynamically adjusts speed based on turn angle, and cycles to the next waypoint upon arrival
    /// </summary>
    void DriveAlongPath()
    {
        Transform target = waypoints[currentWaypointIndex];

        // Flattens the target position on the Y axis to prevent the car from tilting up/down
        Vector3 flatTargetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = flatTargetPosition - transform.position;
        
        float currentSpeed = maxSpeed; 

        if (direction != Vector3.zero)
        {
            // Calculate turn angle to dynamically reduce speed on corners
            float angleToTarget = Vector3.Angle(transform.forward, direction);
            float speedMultiplier = 1f - Mathf.Clamp01(angleToTarget / 60f);
            currentSpeed = Mathf.Lerp(minTurnSpeed, maxSpeed, speedMultiplier);

            // Smoothly rotate towards the target
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }

        // Move the vehicle forward
        transform.position = Vector3.MoveTowards(transform.position, flatTargetPosition, currentSpeed * Time.deltaTime);

        // Check if the vehicle has reached the waypoint (within a 0.5 unit threshold)
        if (Vector3.Distance(transform.position, flatTargetPosition) < 0.5f)
        {
            currentWaypointIndex++;
            
            // Loop back to the start of the array if the end is reached
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0; 
            }
        }
    }

    /// <summary>
    /// Shoots a raycast straight forward from the vehicle's front bumper
    /// Changes the state to Braking if an obstacle is detected, otherwise reverts to Driving.
    /// </summary>
    void CheckForObstacles()
    {
        RaycastHit hit;
        
        // Elevate the raycast slightly so it doesn't clip the ground
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f); 

        // Draw a yellow debug line in the Scene view to visualize the sensor
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