using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrokeSelectFragments : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool groupFragment = false;
    private bool groupFragmentLast = false;

    private GroupManager groupManager;

    private GameObject strokeBall; 
    [HideInInspector] public List<GameObject> selectedFrag = new List<GameObject>(); 

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
    }

    private void Update()
    {
        if (groupFragment)
        {
            if (groupFragmentLast == false)
            {
                Debug.Log("Please move the ball in scene view...");
                fragmentRef = FragmentData.fragments;

                strokeBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                strokeBall.AddComponent<Rigidbody>();
                strokeBall.GetComponent<Rigidbody>().isKinematic = true;
                strokeBall.GetComponent<Rigidbody>().useGravity = false;
                strokeBall.GetComponent<SphereCollider>().isTrigger = true;
                strokeBall.AddComponent<StrokeSelector>();              // add obj when collide
                strokeBall.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                strokeBall.transform.position = new Vector3(1.5f, 0.5f, 0.5f);
                strokeBall.transform.parent = transform;
                strokeBall.name = "Stroke";

                selectedFrag.Clear();
            }

            groupFragmentLast = true;
        }
        else if (groupFragmentLast)
        {
            if (selectedFrag.Count > 0)
            {
                Debug.Log("event mouse up: grouping...");
                selectedFrag = selectedFrag.Distinct().ToList();

                AreaSelectFragments.GroupFragments(groupManager, selectedFrag);
            }

            Destroy(strokeBall);
            groupFragmentLast = false;
        }
    }
}
