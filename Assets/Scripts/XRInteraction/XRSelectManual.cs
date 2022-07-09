using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;
using System;
using System.Linq;

public class XRSelectManual : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable = null;
    private GameObject grabPoint;

    Transform FragmentTf;

    private void Start()
    {
        FragmentTf = GameObject.Find("Fragments").transform;
    }


    void Update()
    {
        if (xrGrabInteractable == null)
        {
            xrGrabInteractable = GetComponent<XRGrabInteractable>();

            xrGrabInteractable.selectEntered.AddListener(HoldingEnter);
            xrGrabInteractable.selectExited.AddListener(ExitHolding);
        }
    }

    

    private void HoldingEnter(SelectEnterEventArgs interactor)
    {
        Debug.Log("grab compare: enter");

        var bcAll = FragmentTf.GetComponentsInChildren<Transform>()
            .Where(t => t.name == FragmentTypes.Default)
            .Select(t => t.GetComponent<BoxCollider>().enabled = false);


        grabPoint = new GameObject("GrabPoint");
        grabPoint.transform.parent = interactor.interactableObject.transform;
        grabPoint.transform.position = interactor.interactorObject.transform.position;
        grabPoint.transform.rotation = interactor.interactorObject.transform.rotation;
        xrGrabInteractable.attachTransform = grabPoint.transform;
    }

    private void ExitHolding(SelectExitEventArgs interactor)
    {
        Debug.Log("grab compare: exit");

        var bcAll = FragmentTf.GetComponentsInChildren<Transform>()
            .Where(t => t.name == FragmentTypes.Default)
            .Select(t => t.GetComponent<BoxCollider>().enabled = true);

        Destroy(grabPoint);
    }
}
