using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


public class DetectFragmentLinks : MonoBehaviour
{
    public Collider colliderCache;

    private GroupManager groupManager;

    private void OnEnable()
    {
        colliderCache = null;
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        transform.position = new Vector3(9, 0, 6);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enter trigger of: " + other.name);
        if (groupManager.IsFragmentChild(other.transform))
            colliderCache = other;
    }
}
