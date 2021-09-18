using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class NPCEvent : UnityEvent<GameObject> { }

public class DepthCamera : MonoBehaviour
{
    public GameObject viewCone;

    Mesh mesh;

    public UnityEvent OnPlayerSpotted;
    public UnityEvent OnPlayerLeft;

    public NPCEvent OnNPCSpotted;
    public UnityEvent OnNPCLeft;

    public UnityEvent OnAlertNPCSpotted;

    private float viewConeLength;

    private bool playerClose = false;
    private bool playerInSight = false;

    private bool npcInSight = false;

    private Transform playerTransform;

    private List<Transform> enemiesInRange;

    [SerializeField]
    private bool drawCone = false;

    // Start is called before the first frame update
    void Start()
    {
        enemiesInRange = new List<Transform>();

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        Physics.queriesHitTriggers = false;

        mesh = new Mesh();
        mesh.name = "ViewCone";

        viewCone.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    
    // Update is called once per frame
    void Update()
    {
        CheckForPlayer();

        CheckForNpcs();

        if (drawCone)
        {
            DrawViewCone();
        }
    }

    

    private bool IsPlayerClose()
    {
        float distance = (playerTransform.position - this.transform.position).magnitude;

        return (distance < this.GetComponent<SphereCollider>().radius);
    }

    private void CheckForPlayer()
    {
        bool spotted = false;

        if (playerClose) // player within vicinity
        {
            if (!playerTransform.GetComponent<PlayerMovement>().isSafe)
            {
                float vcAngle = this.GetComponent<Camera>().fieldOfView;
                Vector3 npcForward = this.transform.forward;
                Vector3 playerToNpc = playerTransform.position - this.transform.position;
                playerToNpc.y = 0;
                float npcToPlayerAngle = Vector3.Angle(npcForward, playerToNpc);

                if (npcToPlayerAngle <= vcAngle) // player within view cone
                {
                    Ray ray = new Ray(this.transform.position, playerToNpc);

                    RaycastHit hitInfo;

                    int layer = ~(1 << LayerMask.NameToLayer("Enemy") | 1 << LayerMask.NameToLayer("ViewLayer"));

                    if (Physics.Raycast(ray, out hitInfo, viewConeLength * 1.1f, layer))
                    {
                        if (hitInfo.collider.tag == "Player")
                        {
                            spotted = true;

                            if (!playerInSight)
                            {
                                playerInSight = true;
                                OnPlayerSpotted.Invoke();
                            }
                        }

                    }
                }
            }
        }

        if (!spotted && playerInSight)
        {
            playerInSight = false;
            OnPlayerLeft.Invoke();
        }
    }

    private void CheckForNpcs()
    {
        foreach (Transform enemy in enemiesInRange)
        {
            

            if (enemy != null)
            {
                

                var behavior = enemy.GetComponent<EnemyAlertBehavior>();

                if (behavior.Health == 0) // Enemy is dead! Alert!
                {
                    float vcAngle = this.GetComponent<Camera>().fieldOfView;
                    Vector3 npcForward = this.transform.forward;
                    Vector3 npcToNpc = (enemy.transform.TransformPoint(enemy.GetComponent<BoxCollider>().center)) - this.transform.position;
                    //npcToNpc.y = 0;
                    float npcToNpcAngle = Vector3.Angle(npcForward, npcToNpc);

                    
                    if (npcToNpcAngle <= vcAngle) // Npc within view cone
                    {

                        Ray ray = new Ray(this.transform.position, npcToNpc);


                        RaycastHit hitInfo;

                        int layer = ~(1 << LayerMask.NameToLayer("ViewLayer") | 1 << LayerMask.NameToLayer("Player"));

                        if (Physics.Raycast(ray, out hitInfo, viewConeLength * 1.1f, layer))
                        {
                            if (hitInfo.collider.tag == "Enemy")
                            {
                                if (!npcInSight)
                                {
                                    npcInSight = true;
                                    OnNPCSpotted.Invoke(hitInfo.collider.gameObject); // npc is dead, alert
                                }
                            }

                        }
                    }
                }
                else // Enemy is not dead. Check if they are alert..?
                {
                    if (behavior.alertness >= 100) // Enemy is alert! Alert!
                    {
                        float vcAngle = this.GetComponent<Camera>().fieldOfView;
                        Vector3 npcForward = this.transform.forward;
                        Vector3 npcToNpc = (enemy.transform.TransformPoint(enemy.GetComponent<BoxCollider>().center)) - this.transform.position;
                        //npcToNpc.y = 0;
                        float npcToNpcAngle = Vector3.Angle(npcForward, npcToNpc);

                        if (npcToNpcAngle <= vcAngle) // Npc within view cone
                        {
                            Ray ray = new Ray(this.transform.position, npcToNpc);

                            RaycastHit hitInfo;

                            int layer = ~(1 << LayerMask.NameToLayer("ViewLayer"));

                            if (Physics.Raycast(ray, out hitInfo, viewConeLength * 1.1f, layer))
                            {
                                if (hitInfo.collider.tag == "Enemy")
                                {
                                    if (!npcInSight)
                                    {
                                        npcInSight = true;
                                        OnAlertNPCSpotted.Invoke(); // npc is dead, alert
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawViewCone()
    {
        int width = GetComponent<Camera>().pixelWidth;
        int height = GetComponent<Camera>().pixelHeight;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<Vector3> verticesTop = new List<Vector3>();
        List<int> trianglesTop = new List<int>();

        int nVerticesMain = width;

        viewConeLength = GetComponent<SphereCollider>().radius;

        viewCone.GetComponent<Renderer>().material.SetFloat("_Distance", viewConeLength);

        int idx = nVerticesMain;

        bool topStarted = false;

        for (int x = 0; x < width; x++)
        {
            float length = viewConeLength;

            Ray ray = GetComponent<Camera>().ScreenPointToRay(new Vector3(x, height / 2));


            RaycastHit hitInfo;

            int layer = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Enemy") | 1 << LayerMask.NameToLayer("ViewLayer"));

            bool hitSuccess = Physics.Raycast(ray, out hitInfo, length, layer);
            if (hitSuccess)
            {
                length = hitInfo.distance;
            }

            Vector3 start = ray.origin;
            Vector3 end = ray.origin + ray.direction * length;

            Vector3 startLocal = start - transform.position;
            startLocal.y = -transform.position.y + 0.01f;


            Vector3 endLocal = end - transform.position;
            endLocal.y = -transform.position.y + 0.01f;

            startLocal = transform.InverseTransformVector(startLocal);
            endLocal = transform.InverseTransformVector(endLocal);

            //startLocal.x = 0;

            vertices.Add(startLocal);
            vertices.Add(endLocal);



            if (x < (width - 1))
            {
                triangles.Add(x * 2 + 0);
                triangles.Add(x * 2 + 1);
                triangles.Add((x + 1) * 2 + 0);

                triangles.Add((x + 1) * 2 + 0);
                triangles.Add(x * 2 + 1);
                triangles.Add((x + 1) * 2 + 1);
            }

            if (hitSuccess && hitInfo.collider.gameObject.layer == 9)
            {
                // get top of collider
                Vector3 startTop = hitInfo.point;
                startTop.y = hitInfo.collider.bounds.max.y;

                //float topLength = hitInfo.collider.bounds.size.z;
                float topLength = 1f;

                Vector3 endTop = startTop + ray.direction * topLength;
                endTop.y = startTop.y;

                startTop -= transform.position;
                startTop.y += 0.05f;

                endTop -= transform.position;
                endTop.y += 0.05f;

                startTop = transform.InverseTransformVector(startTop);
                endTop = transform.InverseTransformVector(endTop);


                verticesTop.Add(startTop);
                verticesTop.Add(endTop);

                if (topStarted)
                {
                    // check following x value as well
                    trianglesTop.Add((idx - 1) * 2 + 0);
                    trianglesTop.Add((idx - 1) * 2 + 1);
                    trianglesTop.Add(idx * 2 + 0);

                    trianglesTop.Add(idx * 2 + 0);

                    trianglesTop.Add((idx - 1) * 2 + 1);
                    trianglesTop.Add(idx * 2 + 1);
                }

                topStarted = true;


                idx++;


            }
            else
            {
                topStarted = false;
            }

        }

        mesh.Clear();

        vertices.AddRange(verticesTop);
        triangles.AddRange(trianglesTop);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            playerTransform = other.transform;
            playerClose = true;
        } 
        else if (other.tag == "Enemy" && other.gameObject.GetInstanceID() != this.transform.parent.gameObject.GetInstanceID())
        {
            // Add enemies in vicinity
            enemiesInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            playerClose = false;
        }
        else if (other.tag == "Enemy")
        {
            // Remove Npcs that are out of sight
            if (enemiesInRange.Contains(other.transform))
                enemiesInRange.Remove(other.transform);
        }
    }
}
