using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DragFragment : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool drag = false;
    private bool dragLast = false;
    [SerializeField] public bool recovery = false;

    private Transform dragger;

    private void Start()
    {
        dragger = transform.Find("Dragger");
    }

    void Update()
    {
        if (drag)
        {
            if (dragLast == false)
            {
                Debug.Log("Please drag fragments to transform...");
                fragmentRef = FragmentData.fragments;

                dragger.gameObject.SetActive(true);
            }

            if (recovery)
            {
                dragger.GetComponent<Dragger>().ResetTransform();
                recovery = false;
            }

            dragLast = true;
        }
        else if (dragLast)
        {
            dragger.gameObject.SetActive(false);
            dragLast = false;
        }
    }
}
