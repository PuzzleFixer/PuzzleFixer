using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokeSelector : MonoBehaviour
{
    StrokeSelectFragments strokeFrag;

    private void Start()
    {
        strokeFrag = GameObject.Find("StrokeSelectFragments").GetComponent<StrokeSelectFragments>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (strokeFrag.groupFragment && other.transform.name == FragmentTypes.Default)
            strokeFrag.selectedFrag.Add(other.transform.parent.parent.gameObject);
    }
}
