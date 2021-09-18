using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class EnemyAlertBehavior : MonoBehaviour//, IPointerClickHandler
{
    private NavMeshAgent agent; 
    //private Rigidbody rigidBody;

    public Transform geometryComponent;

    public enum AlertedReactionType
    {
        None,
        Flee,
        Brawl,
        Shoot
    }

    private GameObject Player;
    public GameObject viewCone;

    private Vector3 lastPlayerPosition;

    public AudioClip [] shootClips;
    public AudioClip[] punchClips;

    private PlayerMovement playerMov;

    private bool playerInSight = false;
    private bool isAttacking = false;

    private bool isSearching = false;

    [Header("Alert Settings")]
    public AlertedReactionType alertReaction = AlertedReactionType.None;
    [Range(0, 100)] public float alertness = 0f;

    public float walkingAlertIncrease = 0.2f;
    public float runningAlertIncrease = 1.0f;
    public float alertDecrease = 0.1f;
    public float undisguisedMultiplier = 2.0f;

    [Header("Combat Settings")]
    [Range(0, 100)] public float Health = 100.0f;


    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");

        playerMov = Player.GetComponent<PlayerMovement>();

        agent = GetComponent<NavMeshAgent>();
        //rigidBody = GetComponent<Rigidbody>();
        StartCoroutine(ChangeViewconeColor());

        

    }

    public void PlayerInSight()
    {
        if (agent == null)
            return;

        playerInSight = true;

        if (alertness > 50f || !playerMov.isDisguised || playerMov.inSuspiciousArea || playerMov.isCarrying || playerMov.illegalItem != PlayerMovement.IllegalItemType.None)
        {
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
        }
    }

    public void PlayerOutOfSight()
    {
        if (agent == null)
            return;

        playerInSight = false;

        if (alertness >= 100 && alertReaction != AlertedReactionType.Flee)
        {
            lastPlayerPosition = Player.transform.position;
            isSearching = true;

            StartCoroutine(Search());
        }
        

        agent.isStopped = false;
    }

    public bool CanSeePlayer()
    {
        return playerInSight;
    }

    private IEnumerator ChangeViewconeColor()
    {
        Color ok = Color.green;
        Color alert = Color.red;

        while (true)
        {
            if (agent == null)
                break;

            if (playerInSight && alertness <= 100)
            {
                Color vcColor = Color.Lerp(ok, alert, (alertness / 100f));
                viewCone.GetComponent<Renderer>().material.SetColor("_Color", vcColor);
            }
            else if (!playerInSight && alertness >= 0)
            {
                Color vcColor = Color.Lerp(ok, alert, (alertness / 100f));
                viewCone.GetComponent<Renderer>().material.SetColor("_Color", vcColor);
            }

            yield return null;
        }
    }

    public void NPCSpotted(GameObject npc)
    {
        if (agent == null)
            return;

        agent.isStopped = true;

        alertness = 100;

        isSearching = true;
        lastPlayerPosition = npc.transform.position;

        StartCoroutine(Search());

        
       
    }

    public void AlertNPCSpotted()
    {
        if (agent == null)
            return;

        agent.isStopped = true;

        alertness = 100;

        isSearching = true;
        lastPlayerPosition = Player.transform.position;

        StartCoroutine(Search());
    }


    // Update is called once per frame

    private bool alreadyDead = false;
    // Omae wa mou shindeiru.
    // NANI?!?


    void Update()
    {
        if (TimeLimit.GameOver)
        {

            StopAllCoroutines();

            return;
        }

        if (Health > 0)
        {
            if (playerInSight && Player.GetComponent<PlayerMovement>().Health > 0)
            {
                if (alertness < 100 && (!playerMov.isDisguised || playerMov.isDetected || playerMov.inSuspiciousArea || playerMov.isCarrying || playerMov.illegalItem != PlayerMovement.IllegalItemType.None))
                {
                    float multi = 1;
                    /*
                    if (playerMov.inSuspiciousArea && playerMov.isDisguised)
                    {
                        multi = multi / 2;
                    }
                    */
                    if (playerMov.illegalItem != PlayerMovement.IllegalItemType.None)
                    {
                        multi = multi * 2;
                    }

                    if (Player.GetComponent<NavMeshAgent>().speed > 3.5)
                    {
                        alertness += runningAlertIncrease * multi; // Running, rises faster.
                    }
                    else
                    {
                        alertness += walkingAlertIncrease * multi; // Walking, rises slowly.
                    }
                    // TODO depends on distance to player
                }

                if (alertness > 50f || !playerMov.isDisguised || playerMov.inSuspiciousArea || playerMov.isCarrying || playerMov.illegalItem != PlayerMovement.IllegalItemType.None)
                {
                    this.transform.LookAt(Player.transform);
                }
            }
            else // return to normal
            {
                if (Player.GetComponent<PlayerMovement>().Health <= 0 && alertness > 0)
                {
                    alertness = 0;
                    if (alertReaction == AlertedReactionType.Shoot)
                    {
                        GetComponent<Animator>().SetBool("IsShooting", false);
                    }
                    else if (alertReaction == AlertedReactionType.Brawl)
                    {
                        GetComponent<Animator>().SetBool("IsPunching", false);
                    }

                    this.agent.isStopped = false;
                }

                if (alertness > 0)
                {
                    if (!playerInSight)
                    {
                        if (alertness < 100)
                        {
                            alertness -= alertDecrease; ; // TODO depends on distance to player
                        }
                        
                    }
                }
            }

            if (alertness < 0)
            {
                alertness = 0;
            }


            if (alertness >= 100)
            {
                playerMov.isDetected = true;

                alertness = 100;

                React();
            }
            else
            {
                // normal behavior
                
            }

            if (playerInSight && playerMov.killedEnemy)
            {
                playerMov.isDetected = true;
                alertness += 100;
            }
        }
        else
        {
            if (!alreadyDead)
            {
                alreadyDead = true;
                playerMov.Disguise(transform.gameObject);

                GetComponent<Animator>().SetTrigger("Die");

                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                Destroy(viewCone.transform.parent.GetComponent<DepthCamera>());

                BoxCollider collider = this.gameObject.GetComponent<BoxCollider>();
                collider.center = new Vector3(-0.12f,0.5f,1.3f);

                var colliderSize = collider.size;
                colliderSize.z = 1.5f;
                collider.size = new Vector3(0.7f, 1.0f, 1.5f); ;

            }

            alertness = 0;
        }
    }

    

    private IEnumerator Search()
    {
        bool goingToLastPlayerPos = true;

        agent.SetDestination(lastPlayerPosition);

        while (isSearching)
        {
            if (goingToLastPlayerPos && agent.remainingDistance < 0.1f && !agent.pathPending)
            { // Reached destination, start looking around
                goingToLastPlayerPos = false;

            }
            else if (!goingToLastPlayerPos)
            {

                for (int i = 0; i < 3; i++)
                {
                    if (playerInSight)
                    {
                        isSearching = false;
                        break;
                    }

                    int angle = Random.Range(0, 360);

                    this.transform.Rotate(Vector3.up, angle);

                    yield return new WaitForSeconds(1f);
                }

                if (!playerInSight)
                {
                    isSearching = false;
                    alertness = 99;
                }
                

            }

            yield return null;
        }

        
    }

    private void React()
    {
        if (alertReaction == AlertedReactionType.Flee)
        {
            // Flee
            if (agent.isStopped)
            {
                agent.isStopped = false;

                RunAway();
            }
        }
        else if (alertReaction == AlertedReactionType.Brawl)
        {
            if (agent.isStopped)
                agent.isStopped = false;

            agent.SetDestination(Player.transform.position);

            PunchBehavior();

        }
        else if (alertReaction == AlertedReactionType.Shoot)
        {
            if (agent.isStopped)
                agent.isStopped = false;

            ShootBehavior();
        }
    }

    private void ShootBehavior()
    {
        if (playerInSight)
        {
            // Shoot

            if (!isAttacking)
            {
                isAttacking = true;
                agent.SetDestination(this.transform.position);
            }
            
            this.transform.LookAt(Player.transform);

            this.GetComponent<Animator>().SetBool("IsShooting", true);
        }
        else // if player not in sight, run until close enough
        {
            
            isAttacking = false;
            this.GetComponent<Animator>().SetBool("IsShooting", false);

            

            //agent.SetDestination(Player.transform.position);
        }
    }

    private void PunchBehavior()
    {
        bool playerClose = agent.remainingDistance < 1.3f && !agent.pathPending;

        Debug.Log(agent.remainingDistance);

        if (playerClose)
        {
            // Shoot

            if (!isAttacking)
            {
                isAttacking = true;
                
            }

            agent.isStopped = true;


            this.transform.LookAt(Player.transform);

            this.GetComponent<Animator>().SetBool("IsPunching", true);
        }
        else // if player not close, run until close enough
        {
            if (isAttacking)
            {
                isAttacking = false;
                
            }

            agent.isStopped = false;


            this.GetComponent<Animator>().SetBool("IsPunching", false);

            //
        }
    }

    private void RunAway()
    {
        GameObject area = GameObject.FindGameObjectWithTag("AreaFlee");

        if (area != null)
        {
            var collider = area.GetComponent<BoxCollider>();

            float xMin = area.transform.position.x - collider.bounds.extents.x;
            float xMax = area.transform.position.x + collider.bounds.extents.x;

            float zMin = area.transform.position.z - collider.bounds.extents.z;
            float zMax = area.transform.position.z + collider.bounds.extents.z;

            float x = Random.Range(xMin, xMax);
            float y = area.transform.position.y;
            float z = Random.Range(zMin, zMax);


            agent.SetDestination(new Vector3(x,y,z));
        }
    }

    public void Shoot()
    {
        int idx = Random.Range(0, shootClips.Length);

        this.GetComponent<AudioSource>().clip = shootClips[idx];
        this.GetComponent<AudioSource>().Play();

        Player.GetComponent<PlayerMovement>().Hit(34f);
    }

    public void Punch()
    {
        Player.GetComponent<PlayerMovement>().Hit(20f);

        if (punchClips == null || punchClips.Length == 0)
            return;

        int idx = Random.Range(0, punchClips.Length);

        this.GetComponent<AudioSource>().clip = punchClips[idx];
        this.GetComponent<AudioSource>().Play();

        
    }

    private void OnMouseOver()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().MouseOverEnemy(this.gameObject, !this.alreadyDead);
    }

    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }


}
