using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolBehavior : MonoBehaviour
{
    public List<GameObject> patrolWaypoints = new List<GameObject>();
    [Tooltip("Time the enemy waits after arriving back at its starting position.")]
    public float selfWaypointPause = 0.0f;
    public float globalPause = 2.0f;
    //private List<Vector3> waypoints = new List<Vector3>();

    private NavMeshAgent agent;

    private bool walkingThroughDoor = false;

    // Start is called before the first frame update

    void Start()
    {
        // Add Point "A" (current position)
        //waypoints.Add(transform.position);

        GameObject myPoint = Instantiate(Resources.Load("Prefabs/Waypoint") as GameObject, this.transform.position, this.transform.rotation);
        myPoint.transform.SetParent(GameObject.Find("Waypoints").transform);
        myPoint.GetComponent<Waypoint>().individualWaypointPause = selfWaypointPause;

        patrolWaypoints.Insert(0, myPoint);

        // Components
        agent = GetComponent<NavMeshAgent>();

        // Save waypoints
        /*foreach (GameObject obj in patrolWaypoints)
        {
            waypoints.Add(obj.transform.position);
        }*/

        StartCoroutine(Patrol());

        StartCoroutine(CheckDoors());
    }

    private int pathingTo = 0;

    //private float nextActionTime = 0.0f;
    //private float period = 0.1f;

    private void FixedUpdate()
    {
        if (this.agent == null)
            return;

        if (!walkingThroughDoor)
        {
            if (this.agent.velocity.magnitude > 0.1f)
            {

                this.GetComponent<Animator>().SetBool("IsMoving", true);
            }
            else
            {
                this.GetComponent<Animator>().SetBool("IsMoving", false);
            }
        }
    }


    IEnumerator Patrol()
    {
        var alertBehavior = GetComponent<EnemyAlertBehavior>();

        while (true)
        {
            if (alertBehavior == null || agent == null) // I am dead
            {
                break;
            }

            if (!agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            if (alertBehavior.Health <= 0)
            {
                agent.isStopped = true;
                break;
            }
            float alertness = alertBehavior.alertness;

            if (alertness >= 100)
            {
                yield return null;
                continue;
            }

           
            float dist = agent.remainingDistance;

            if (dist != Mathf.Infinity && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance == 0)
            {

                { // look towards direction of waypoint
                    Quaternion lookRotation = Quaternion.LookRotation(patrolWaypoints[pathingTo].transform.forward, Vector3.up);
                    this.transform.rotation = lookRotation;
                }


                { // wait for period seconds
                    float period = patrolWaypoints[pathingTo].GetComponent<Waypoint>().individualWaypointPause;
                    yield return new WaitForSeconds(period);

                    alertness = alertBehavior.alertness;
                }
                
                
                if (agent == null) // Check, if still alive
                    break;

                { // Choose next waypoint
                    if (pathingTo < patrolWaypoints.Count - 1)
                    {
                        pathingTo += 1;
                    }
                    else
                    {
                        pathingTo = 0;
                    }

                    if (alertness < 100)
                    {
                        if (pathingTo < patrolWaypoints.Count)
                        {
                            if (patrolWaypoints[pathingTo] != null)
                            {
                                agent.SetDestination(patrolWaypoints[pathingTo].transform.position);
                            }
                        }
                    }
                }
                
            }
            

            yield return null;
        }

        
    }

    private IEnumerator CheckDoors()
    {
        float health = GetComponent<EnemyAlertBehavior>().Health;

        while (health > 0)
        {

            // TODO walk through door
            if (agent.isOnOffMeshLink)
            {

                yield return WalkThroughDoor();

                agent.CompleteOffMeshLink();
            }

            health = GetComponent<EnemyAlertBehavior>().Health;

            yield return null;

        }
    }

    private IEnumerator WalkThroughDoor()
    {
        walkingThroughDoor = true;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Transform doorTransform = data.offMeshLink.endTransform.parent;

        Door doorBehavior = doorTransform.GetComponent<Door>();
        doorBehavior.Interact();

        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        while (agent.transform.position != endPos)
        {
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            yield return null;
        }

        walkingThroughDoor = false;
    }

    // Update is called once per frame
    void Update()
    {
        var alertBehavior = GetComponent<EnemyAlertBehavior>();

        if (alertBehavior == null)
        {
            Destroy(this);
            return;
        }

        if (alertBehavior.Health <= 0)
        {
            StopCoroutine(Patrol());
            StopCoroutine(WalkThroughDoor());

            var viewCam = transform.Find("ViewCam").Find("VisionField").gameObject;

            viewCam.GetComponent<MeshRenderer>().enabled = false;
            Destroy(viewCam.GetComponent<Rigidbody>());

            Destroy(this.agent);
            Destroy(this);
            return;
        }

        if (alertBehavior.alertness >= 100)
        {
            StopCoroutine(Patrol());
        }

        
    }

    
}
