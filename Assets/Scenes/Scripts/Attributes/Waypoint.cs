using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    // This entire script exists just to contain public variables on each waypoint.
    [Tooltip("Time the enemy waits after arriving at this waypoint.")]
    public float individualWaypointPause = 1.0f;
}
