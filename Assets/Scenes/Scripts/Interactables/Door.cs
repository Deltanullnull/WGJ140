using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    // Start is called before the first frame update
    public Transform doorTransform;
    public Transform doorObject;
    bool isClosing;

    public Transform leftSide, rightSide;

    public AudioClip doorOpen, doorClose;

    void Start()
    {
        StartCoroutine(OpenClose());
    }

    private IEnumerator OpenClose()
    {
        float speed = 100f;
        this.isClosing = true;
        float angle = doorTransform.localRotation.eulerAngles.y;

        while (true)
        {
            
            if (!this.isClosing && angle < 90f)
            {
                

                angle = Mathf.Min(angle + Time.deltaTime * speed, 90f);
                doorTransform.localRotation = Quaternion.AngleAxis(angle, Vector3.up);

                if (angle >= 90f)
                {
                    yield return new WaitForSeconds(5f);

                    this.isClosing = true;

                    this.GetComponent<AudioSource>().clip = doorClose;
                    this.GetComponent<AudioSource>().Play();
                }
            }
            else if (this.isClosing && angle > 0f)
            {
                angle = Mathf.Max(angle - Time.deltaTime * speed, 0f);
                doorTransform.localRotation = Quaternion.AngleAxis(angle, Vector3.up);   
            }
            else
            {
                doorObject.GetComponent<BoxCollider>().enabled = true;
            }

            yield return null;
        }
    }

    public void Interact()
    {
        this.isClosing = false;

        {
            this.GetComponent<AudioSource>().clip = doorOpen;
        }

        this.GetComponent<AudioSource>().Play();

        doorObject.GetComponent<BoxCollider>().enabled = false;

    }

    public Vector3 GetBestPoint()
    {
        var pos1 = leftSide.position;
        var pos2 = rightSide.position;

        float lengthPos1 = float.MaxValue;
        float lengthPos2 = float.MaxValue;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            UnityEngine.AI.NavMeshPath path1 = new UnityEngine.AI.NavMeshPath();
            if (player.GetComponent<UnityEngine.AI.NavMeshAgent>().CalculatePath(pos1, path1))
            {
                

                float length = 0f;
                for(int i = 0; i < path1.corners.Length - 1; i++)
                {
                    var pointA = path1.corners[i];
                    var pointB = path1.corners[i + 1];

                    length += Vector3.Distance(pointA, pointB);
                }

                lengthPos1 = length;

            }


            UnityEngine.AI.NavMeshPath path2 = new UnityEngine.AI.NavMeshPath();
            if (player.GetComponent<UnityEngine.AI.NavMeshAgent>().CalculatePath(pos2, path2))
            {
                float length = 0f;
                for (int i = 0; i < path2.corners.Length - 1; i++)
                {
                    var pointA = path2.corners[i];
                    var pointB = path2.corners[i + 1];

                    length += Vector3.Distance(pointA, pointB);
                }

                lengthPos2 = length;
            }

        }

        if (lengthPos2 < lengthPos1)
            return pos2;

        return pos1;
    }
}
