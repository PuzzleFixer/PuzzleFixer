using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCollider : MonoBehaviour
{
    [SerializeField] private WIM wim;

    private void OnTriggerEnter(Collider other)
    {
        wim.SetColliderEnter(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        wim.SetColliderExit(other.transform);
    }
}
