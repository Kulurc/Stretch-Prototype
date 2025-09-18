using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrphanChild : MonoBehaviour
{
    //Sets the parent object ot null at Awake
    private void Awake(){ gameObject.transform.parent = null;}
}
