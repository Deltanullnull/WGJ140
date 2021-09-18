using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector]
    public static int objectiveCount = 0;

    private NavMeshAgent agent;

    public Transform geometryComponent;

    public Sprite iconKnife;
    public Sprite iconAction;

    private IInteractable interactableObjectSelected, interactableObject;


    public enum IllegalItemType
    {
        None,
        PricelessJewel,
        BagOfCash,
    }

    [Header("Sound FX")]
    public AudioClip alertNoise;
    [Space(10)]
    public AudioClip disguiseNoise;
    [Space(10)]
    public AudioClip stab1;
    public AudioClip stab2;
    public AudioClip stab3;
    public AudioClip stab4;
    private List<AudioClip> stabSounds = new List<AudioClip>();

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 5.5f;

    [Header("Suspicious Variables")]
    public bool isSafe = false;
    public bool inSuspiciousArea = false;
    public bool isCarrying = false;
    public bool isDetected = false;
    [Space(10)]
    public IllegalItemType illegalItem = IllegalItemType.None;
    private GameObject itemGoalArea;

    [Header("Disguise Settings")]
    public bool hasDisguise = false;
    public bool isDisguised = false;

    [Header("Combat Settings")]
    public bool killedEnemy = false;
    [Range(0, 100)] public float Health = 100.0f;


    private bool killedEnemy_ = false; // helps keep killedEnemy alive for two frames

    private Transform npcTarget;

    private Transform cameraTarget;

    private bool walkingThroughDoor = false;

    // Start is called before the first frame update
    void Start()
    {
        cameraTarget = GameObject.Find("CameraTarget").transform;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        stabSounds.Add(stab1);
        stabSounds.Add(stab2);
        stabSounds.Add(stab3);
        stabSounds.Add(stab4);

        StartCoroutine(CheckDoors());
    }

    // Update is called once per frame
    private bool amAlreadySeen = false;
    void Update()
    {
        if (TimeLimit.GameOver)
        {
            //StopAllCoroutines();

            return;
        }

        if (this.Health <= 0 )
        {
            return;
        }

        cameraTarget.position = this.transform.position;

        //Camera.main.transform.localPosition = new Vector3(transform.position.x, transform.position.z / 2, Camera.main.transform.localPosition.z);

        //print(Camera.main.transform.position);

        if (npcTarget != null && !isCarrying)
        {
            //if (!isCarrying)
            agent.SetDestination(npcTarget.position - npcTarget.forward);



            if (agent.remainingDistance < 0.8f && !agent.pathPending)
            {
                var alertBehavior =  npcTarget.GetComponent<EnemyAlertBehavior>();
                if (alertBehavior != null)
                {
                    if (alertBehavior.Health == 0)
                    {
                        // TODO carry
                        isCarrying = true;

                        GetComponent<Animator>().SetBool("IsCarrying", true);
                        npcTarget.GetComponent<Animator>().SetBool("IsCarried", true);

                        npcTarget.GetComponent<BoxCollider>().enabled = false;

                        npcTarget.SetParent(this.transform);

                        npcTarget.localPosition = Vector3.zero;
                    }
                    else
                    {
                        alertBehavior.Health = 0;
                        
                        int r = Random.Range(1, 5);
                        GetComponent<AudioSource>().PlayOneShot(stabSounds[r - 1]);

                        npcTarget = null;
                    }

                    


                }

                
            }
        }
        else if (interactableObject != null)
        {
            
            if (agent.remainingDistance < 1.5f && agent.remainingDistance > 0f && !agent.pathPending)
            {
                interactableObject.Interact();
                agent.SetDestination(this.transform.position);

                interactableObject = null;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (interactableObjectSelected != null)
            {
                interactableObject = interactableObjectSelected;

                Vector3 position = interactableObject.GetBestPoint();

                agent.SetDestination(position);
                // TODO walk to interactable and if close enough, interact

                //interactableObject.Interact();
            }
            else
            {
                interactableObjectSelected = null;

                Vector2 screenPos = Input.mousePosition;

                ClickOnScreen(screenPos);
            }
            
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (isCarrying)
            {
                isCarrying = false;

                GetComponent<Animator>().SetBool("IsCarrying", false);

                npcTarget.SetParent(null);

                npcTarget.GetComponent<BoxCollider>().enabled = true; ;
                npcTarget.GetComponent<Animator>().SetBool("IsCarried", false);

                npcTarget = null;
            }

            Vector2 screenPos = Input.mousePosition;

            SelectNpc(screenPos);

        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (agent.speed == walkSpeed)
            {
                agent.speed = runSpeed;
            }
            else
            {
                agent.speed = walkSpeed;
            }
        }

        

        if (hasDisguise)
        {
            if (AmSeen(true))
            {
                isDisguised = false;
                if (!amAlreadySeen)
                {
                    amAlreadySeen = true;
                    GetComponent<AudioSource>().PlayOneShot(alertNoise);
                }
            }
            else
            {
                isDisguised = true;
                amAlreadySeen = false;
            }
        }
    }

    private IEnumerator KilledEnemy()
    {
        killedEnemy = true;

        yield return new WaitForSeconds(0.1f);

        killedEnemy = false;
    }

    public void Hit(float hitpoints)
    {
        this.Health -= hitpoints;
    }

    private void FixedUpdate()
    {
        

        if (this.Health <= 0)
        {
            if (agent != null)
            {
                this.GetComponent<Animator>().SetTrigger("Die");
                Destroy(agent);
            }

            return;
        }

        if (!walkingThroughDoor)
        {
            if (this.agent.velocity.magnitude > 0.2f)
            {
                this.GetComponent<Animator>().SetBool("IsMoving", true);
            }
            else
            {
                this.GetComponent<Animator>().SetBool("IsMoving", false);
            }
        }
        
    }

    private IEnumerator CheckDoors()
    {
        while (true)
        {
            if (agent == null)
                break;

            // TODO walk through door
            if (agent.isOnOffMeshLink)
            {

                yield return WalkThroughDoor();

                agent.CompleteOffMeshLink();
            }
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
            if (agent == null)
                break;

            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            yield return null;
        }

        walkingThroughDoor = false;
    }

    public void SetNPCTarget(Transform enemy)
    {
        npcTarget = enemy;
        agent.SetDestination(npcTarget.position - npcTarget.forward);

    }

    private IEnumerator DisguiseCoroutine(GameObject enemy)
    {
        this.GetComponent<Animator>().SetTrigger("Stab");

        GameObject particles = Instantiate(Resources.Load("Prefabs/Particles") as GameObject, this.transform.position, Quaternion.identity);
        Destroy(particles, 5f);

        GetComponent<AudioSource>().PlayOneShot(disguiseNoise);

        yield return new WaitForSeconds(0.5f);
        

        GameObject.Destroy(this.geometryComponent.gameObject);
        GameObject newGeometry = Instantiate(enemy.GetComponent<EnemyAlertBehavior>().geometryComponent.gameObject, this.transform);

        this.geometryComponent = newGeometry.transform;

        this.GetComponent<Animator>().avatar = enemy.GetComponent<Animator>().avatar;
        this.GetComponent<Animator>().runtimeAnimatorController = enemy.GetComponent<Animator>().runtimeAnimatorController;
        

        yield return null;
            
        this.GetComponent<Animator>().Rebind();


        isDetected = false;

        hasDisguise = true;
        isDisguised = true;

        StartCoroutine(KilledEnemy());
    }

    public void Disguise(GameObject enemy)
    {
        StartCoroutine(DisguiseCoroutine(enemy));
    }

    private bool AmSeen(bool byAlerted = false)
    {
        bool seen = false;

        // @josh: Better use  GameObject.FindObjectsOfType<EnemyAlertBehavior>() 



        // get root objects in scene
        List<GameObject> rootObjects = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);

        // iterate root objects and do something
        foreach (EnemyAlertBehavior alertBehavior in GameObject.FindObjectsOfType<EnemyAlertBehavior>())
        {
            if (alertBehavior == null)
                continue;

            if (alertBehavior.CanSeePlayer())
            {
                if (byAlerted)
                {
                    if (alertBehavior.alertness >= 100f)
                    {
                        seen = true;
                    }
                }
                else
                {
                    seen = true;
                }
            }
        }

        // return
        return seen;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AreaSafe")
        {
            isSafe = true;
            Debug.Log("Entered safe area");
        }
        else if (other.tag == "AreaSuspicious")
        {
            inSuspiciousArea = true;
        }

        if (other.tag == "IllegalItem")
        {
            if (illegalItem == IllegalItemType.None)
            {
                illegalItem = (IllegalItemType)other.gameObject.GetComponent<IllegalItem>().itemType;
                Destroy(other.gameObject);
                print("Picked up illegal item! [" + illegalItem + "]");
            }
        }

        if (other.tag == "ItemDropoff")
        {
            if (illegalItem != IllegalItemType.None)
            {
                Debug.Log("Dropped off illegal item! [" + illegalItem + "]");
                objectiveCount -= 1;

                illegalItem = IllegalItemType.None;

                if (objectiveCount <= 0)
                {
                    print(" ~~ ROBBERY SUCCESSFUL! ~~");
                    // Time.timeScale = 0;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "AreaSafe")
        {
            isSafe = false;
            Debug.Log("Left safe area");
        }
        else if (other.tag == "AreaSuspicious")
        {
            inSuspiciousArea = false;
        }
    }


    private void ClickOnScreen(Vector2 screenPos)
    {

        Ray ray = Camera.main.ScreenPointToRay(screenPos);



        RaycastHit hitInfo;
        int layer =1 << LayerMask.NameToLayer("Ground");

        if (Physics.Raycast(ray, out hitInfo, 50f, layer))
        {
            Vector3 worldPos = hitInfo.point;

            if (!isCarrying)
                npcTarget = null;

            agent.SetDestination(worldPos);
        }
    }

    private void SelectNpc(Vector2 screenPos)
    {

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        RaycastHit hitInfo;
        int layer = 1 << LayerMask.NameToLayer("Enemy");

        if (Physics.Raycast(ray, out hitInfo, 50f, layer))
        {
            npcTarget = hitInfo.transform;
        }
    }

    public void MouseOverEnemy(GameObject enemy, bool isAlive)
    {
        if (isAlive)
            Cursor.SetCursor(iconKnife.texture, Vector2.zero, CursorMode.ForceSoftware);
        else
        {
            Cursor.SetCursor(iconAction.texture, Vector2.zero, CursorMode.ForceSoftware);
        }
    }

    public void MouseOverObject(IInteractable interactable)
    {
        interactableObjectSelected = interactable;
        
        Cursor.SetCursor(iconAction.texture, Vector2.zero, CursorMode.ForceSoftware);
    }

    public void MouseExitObject(IInteractable interactable)
    {
        interactableObjectSelected = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }
}
