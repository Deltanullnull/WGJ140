using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehavior : MonoBehaviour
{
    private float xPos;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            xPos = Input.mousePosition.x;
        }

        if (Input.GetMouseButton(2))
        {
            float dx = Input.mousePosition.x - xPos;

            this.transform.Rotate(Vector3.up, dx);

            xPos = Input.mousePosition.x;
        }
    }
}
