using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class DirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    
    private bool isInteracting = false;
    private Hand m_InteractingHand = null;

    private Vector3 m_PrevHandPos = Vector3.zero;
    private Quaternion m_InitRot;
    private Quaternion m_InitHandRot = Quaternion.identity;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
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
            //if (m_Trigger.GetState(m_InteractingHand.handType))
            //    MoveManipulator();

            if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                TriggerReleased();

            else if (!m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishMoveArm();
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
        if (m_ManipulationMode.mode == Mode.DIRECT && m_InteractingHand != null && !isInteracting)
        {
            if (m_InteractingHand.IsStillHovering(m_Interactable))
            {
                gameObject.transform.SetParent(m_InteractingHand.transform);
                isInteracting = true;

                //m_PrevHandPos = m_InteractingHand.objectAttachmentPoint.position;
                //m_InitRot = gameObject.transform.rotation;
                //m_InitHandRot = m_InteractingHand.objectAttachmentPoint.rotation;
            }
        }
    }

    private void MoveManipulator()
    {
        Vector3 deltaTranslation = m_InteractingHand.objectAttachmentPoint.position - m_PrevHandPos;
        Quaternion deltaRotation = m_InteractingHand.objectAttachmentPoint.rotation * Quaternion.Inverse(m_InitHandRot);

        gameObject.transform.position += deltaTranslation;
        gameObject.transform.rotation = deltaRotation * m_InitHandRot;

        m_PrevHandPos = m_InteractingHand.objectAttachmentPoint.position;
    }

    private void TriggerReleased()
    {
        if (m_ManipulationMode.mode == Mode.DIRECT && isInteracting)
        {
            gameObject.transform.SetParent(null);
            m_InteractingHand = null;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}