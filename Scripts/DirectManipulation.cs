using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class DirectManipulation : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private ManipulationMode m_ManipulationMode = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;

    private ROSPublisher m_ROSPublisher = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    
    private bool isInteracting = false;
    private Hand m_InteractingHand = null;
    private const float m_TimeInterval = 0.5f;
    private float period = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();

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
            {
                TriggerReleased();
            }
            else if (!m_PlanningRobot.isPlanning && period > m_TimeInterval)
            {
                m_ROSPublisher.PublishMoveArm();
                period = 0;
            }
            period += UnityEngine.Time.deltaTime;
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
        if (m_ManipulationMode.mode == Mode.DIRECT && !isInteracting && m_InteractingHand != null)
        {
            if (m_InteractingHand.IsStillHovering(m_Interactable))
            {
                gameObject.transform.SetParent(m_InteractingHand.transform);
                isInteracting = true;
            }
        }
    }

    private void TriggerReleased()
    {

        if (isInteracting)
        {
            gameObject.transform.SetParent(null);
            m_InteractingHand = null;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
        }
    }
}