using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

[RequireComponent(typeof(Interactable))]

public class SDOFManipulation : MonoBehaviour
{
    private SteamVR_Action_Boolean m_Trigger;
    private Interactable m_Interactable;
    private ManipulatorPublisher m_Publisher;

    private bool isInteracting;
    private bool isTranslating;
    private bool isRotating;

    private Vector3 m_PlaneNormal;

    private Hand m_InteractingHand;
    private Vector3 m_PrevHandPos;

    //-------------------------------------------------
    protected virtual void Awake()
    {
        m_Interactable = GetComponent<Interactable>();
    }

    private void Start()
    {
        m_Publisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ManipulatorPublisher>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, SteamVR_Input_Sources.Any);

        m_InteractingHand = null;
        isInteracting = false;
        isTranslating = false;
        isRotating = false;
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    //-------------------------------------------------
    private void HandHoverUpdate(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    private void Update()
    {
        if (isInteracting)
        {
            if(m_Trigger.GetState(m_InteractingHand.handType))
            {
                TriggerHeld();
            }
            else
            {
                TriggerReleased();
            }
        }
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources hand)
    {
        if (!isInteracting && m_InteractingHand != null)
        {
            if (m_InteractingHand.IsStillHovering(m_Interactable))
            {
                if(!isInteracting)
                {
                    m_PrevHandPos = m_InteractingHand.objectAttachmentPoint.position;
                    isInteracting = true;
                }
            }
        }
    }

    private void TriggerHeld()
    {
        if(!isTranslating && !isRotating)
        {
            if (Vector3.Distance(m_PrevHandPos, m_InteractingHand.objectAttachmentPoint.position) >= 0.1f)
            {
                Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_PrevHandPos;
                Transform handleAxis = gameObject.transform.parent.transform;

                float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, handleAxis.up.normalized)) * 180 / Mathf.PI;

                if ((Mathf.Abs(angle - 90.0f) < angle) && (Mathf.Abs(angle - 90.0f) < Mathf.Abs(180.0f - angle)))
                {
                    angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, handleAxis.right.normalized)) * 180 / Mathf.PI;
                    if ((Mathf.Abs(angle - 90.0f) < angle) && (Mathf.Abs(angle - 90.0f) < Mathf.Abs(180.0f - angle)))
                        m_PlaneNormal = handleAxis.right;
                    else
                        m_PlaneNormal = handleAxis.forward;

                    isRotating = true;
                }
                else
                    isTranslating = true;
            }
        }

        if (isTranslating)
            Translate();

        if (isRotating)
            Rotate();
    }

    void Rotate()
    {
        Debug.Log("rotate");
        Transform handleAxis = gameObject.transform.parent.transform;
        Transform widget = handleAxis.parent.transform;
        Vector3 connectingVector;

        if (handleAxis.GetChild(0) == gameObject.transform)
            connectingVector = m_InteractingHand.objectAttachmentPoint.transform.position - widget.transform.position;
        else
            connectingVector = widget.transform.position - m_InteractingHand.objectAttachmentPoint.transform.position;

        Vector3 direction = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        widget.transform.rotation = Quaternion.FromToRotation(handleAxis.up, direction) * widget.transform.rotation;
    }

    void Translate()
    {
        Debug.Log("translate");
        Transform handleAxis = gameObject.transform.parent.transform;
        Transform widget = handleAxis.parent.transform;

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.transform.position - gameObject.transform.position;
        Vector3 projHandVec = (Vector3.Dot(connectingVector, handleAxis.up) * handleAxis.up);

        widget.transform.position += projHandVec;
    }

    private void TriggerReleased()
    {
        if(isRotating)
        {
            m_Publisher.PublishMoveArm();
        }
        if(isTranslating)
        {
            m_Publisher.PublishConstrainedMovement();
        }

        m_InteractingHand = null;
        isInteracting = false;
        isTranslating = false;
        isRotating = false;
    }
}