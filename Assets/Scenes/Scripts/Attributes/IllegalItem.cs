using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IllegalItem : MonoBehaviour
{
    public enum IllegalItemType
    {
        None,
        PricelessJewel,
        BagOfCash,
    }

    public IllegalItemType itemType = IllegalItemType.None;


    void Start()
    {
        PlayerMovement.objectiveCount += 1;
    }

    void Update()
    {
        
    }
}
