using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicEffect : MonoBehaviour
{
    private DragFragment dragController = null;
    private Transform dragTransform = null;

    [Header("physics effect")]
    [SerializeField] [Range(0.0f, 1.0f)] float dragForce = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float linkDragForce = 0.4f;
    [SerializeField] [Range(0.0f, 1.0f)] float linkVerticalForce = 0.3f;
    [SerializeField] [Range(0.0f, 10.0f)] float rotateForce = 0.5f;
    [SerializeField] [Range(0.0f, 5.0f)] float rotateOtherDirReduceForce = 0.5f;

    [Header("setups")]
    [SerializeField] bool rotateAxisLocal = false;

    [Header("restrict link")]
    [SerializeField] public List<Vector3> anchorPoints = new List<Vector3>() { Vector3.zero };

    private List<Vector3> angleVectors = new List<Vector3>();

    void Update()
    {
        if (dragTransform != null)
        {
            TransformPosition();

            TransformRotation();
        }

        // release drag
        if (dragController != null && dragController.drag == false)
        {
            anchorPoints.RemoveAt(anchorPoints.Count - 1);
            dragController = null;
            dragTransform = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dragController == null)
        {
            dragController = other.transform.GetComponent<DragFragment>();
            if (other.transform.name == "Dragger" && dragController != null && dragController.drag)
            {
                dragTransform = other.transform;

                Vector3 linkDrag = Vector3.zero;    
                angleVectors = new List<Vector3>(); 
                foreach (var p in anchorPoints)
                {
                    linkDrag += p - transform.position;
                    angleVectors.Add(p - transform.position);
                }
                anchorPoints.Add(transform.position - linkDrag);

                other.transform.rotation = transform.rotation;  
            }
            else
                dragController = null;
        }
    }

    private void TransformPosition()
    {
        int dragging = 1;
        Vector3 controllerDrag = (dragTransform.position - transform.position) * dragging;
        Vector3 linkDrag = Vector3.zero;

        if (anchorPoints.Count > 0)
        {
            foreach (var p in anchorPoints)
            {
                Debug.DrawLine(transform.position, p, Color.red);
                linkDrag += p - transform.position;
            }

            Debug.DrawLine(transform.position, controllerDrag + transform.position, Color.green);
            Debug.DrawLine(transform.position, linkDrag + transform.position, Color.green);

            var dragLinkAngle = Vector3.Angle(controllerDrag, linkDrag) * Mathf.Deg2Rad;
            float linkDragAmplify = 0.1f;
            Vector3 dragOnLink = linkDrag.normalized * controllerDrag.magnitude * Mathf.Cos(dragLinkAngle);
            Vector3 dragOnVerticalLink = controllerDrag - dragOnLink;
            Vector3 linkDirDrag = linkDrag * Time.deltaTime * linkDragForce * linkDragAmplify + 
                dragOnLink * Time.deltaTime * dragForce;
            Vector3 linkVerticalDirDrag = dragOnVerticalLink * Time.deltaTime * linkVerticalForce;

            transform.position += linkDirDrag + linkVerticalDirDrag;
        }
        else
            transform.position += controllerDrag;
    }

    private void TransformRotation()
    {
        Vector3 controllerRotate = dragTransform.localEulerAngles - transform.localEulerAngles;
        if (angleVectors.Count > 0)
        {
            Quaternion testRotation = Quaternion.Slerp(transform.localRotation, dragTransform.localRotation, Time.deltaTime * rotateForce);
            float sumAngle = 0.0f;
            for (int i = 0; i < angleVectors.Count; i++)
            {
                Vector3 nextAngleVector = (testRotation * Quaternion.Inverse(transform.localRotation)) * angleVectors[i];
                sumAngle += Vector3.Angle(nextAngleVector, angleVectors[i]);
            }
            float rotateWeight = Mathf.Exp(-sumAngle * rotateOtherDirReduceForce);

            Quaternion nextRotation = Quaternion.Slerp(transform.localRotation, dragTransform.localRotation, Time.deltaTime * rotateForce * rotateWeight);
            for (int i = 0; i < angleVectors.Count; i++)
            {
                angleVectors[i] = rotateAxisLocal ? 
                    (nextRotation * Quaternion.Inverse(transform.localRotation)) * angleVectors[i] : // local--angleVectors rotate as the object rotate
                    angleVectors[i] = anchorPoints[i] - transform.position; // global--angleVectors do not rotate
                Debug.DrawLine(transform.position, transform.position + angleVectors[i], Color.magenta);
            }
            transform.localRotation = nextRotation;
        }
        else
            transform.localEulerAngles = transform.localEulerAngles + controllerRotate;
    }
}
