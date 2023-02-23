using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]

public class SDOFManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private PlanningRobot m_PlanningRobot = null;
    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    
    private bool isInteracting = false;
    private bool isTranslating = false;
    private bool isRotating = false;

    private Vector3 m_PlaneNormal = Vector3.zero;

    private Hand m_InteractingHand = null;
    private Vector3 m_PrevHandPos = Vector3.zero;
    private readonly float m_Threshold = 0.05f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();

        m_Interactable = GetComponent<Interactable>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        m_Trigger.onStateDown += TriggerGrabbed;
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= TriggerGrabbed;
    }

    private void Update()
    {
        if (isInteracting)
        {
            if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                TriggerReleased();
            
            else
            {
                TriggerHeld();
            }
        }
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources hand)
    {
        if (!isInteracting && m_InteractingHand != null)
        {
            if (m_InteractingHand.IsStillHovering(m_Interactable))
            {
                m_PrevHandPos = m_InteractingHand.objectAttachmentPoint.position;
                isInteracting = true;
            }
        }
    }

    private void TriggerHeld()
    {
        if(!isTranslating && !isRotating)
        {
            Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_PrevHandPos;
            if (connectingVector.magnitude >= m_Threshold)
            {
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

        if (!m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishMoveArm();
    }

    void Translate()
    {
        Transform handleAxis = gameObject.transform.parent.transform;
        Transform widget = handleAxis.parent.transform;

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - gameObject.transform.position;
        Vector3 projHandVec = Vector3.Project(connectingVector, handleAxis.up);

        widget.transform.position += projHandVec;
    }

    void Rotate()
    {
        Transform handleAxis = gameObject.transform.parent.transform;
        Transform widget = handleAxis.parent.transform;

        Vector3 connectingVector;
        if (handleAxis.GetChild(0) == gameObject.transform)
            connectingVector = m_InteractingHand.objectAttachmentPoint.position - widget.transform.position;
        else
            connectingVector = widget.transform.position - m_InteractingHand.objectAttachmentPoint.position;

        Vector3 direction = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        widget.transform.rotation = Quaternion.FromToRotation(handleAxis.up, direction) * widget.transform.rotation;
    }

    private void TriggerReleased()
    {
        m_InteractingHand = null;
        isInteracting = false;
        isTranslating = false;
        isRotating = false;

        if (m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishTrajectoryRequest();
        else
            m_ROSPublisher.PublishMoveArm();
    }
}