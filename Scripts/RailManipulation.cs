using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using static Rails;

public class RailManipulation : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private ManipulationMode m_ManipulationMode = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;
    [SerializeField] private Rails m_Rails = null;

    private ROSPublisher m_ROSPublisher = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private bool isInteracting = false;
    private Hand m_InteractingHand = null;

    private const float m_TimeInterval = 0.5f;
    private float period = 0.0f;

    private int m_ActiveRail = 0;

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
        if (m_ManipulationMode.mode == Mode.RAIL && !isInteracting && m_Rails.rails.Length <= 0)
            m_ManipulationMode.mode = Mode.DIRECT;

        if (isInteracting)
        {
            if (m_Trigger.GetStateUp(m_InteractingHand.handType))
            {
                TriggerReleased();
            }
            else
            {
                FollowRail();
                if (!m_PlanningRobot.isPlanning && period > m_TimeInterval)
                {
                    m_ROSPublisher.PublishConstrainedMovement();
                    period = 0;
                }
                period += UnityEngine.Time.deltaTime;
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
        if (m_ManipulationMode.mode == Mode.RAIL && !isInteracting && m_InteractingHand != null)
        {
            if (m_InteractingHand.IsStillHovering(m_Interactable))
            {
                isInteracting = true;
            }
        }
    }

    private void FollowRail()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        Vector3 connectingVector = m_InteractingHand.transform.position - m_Rails.rails[m_ActiveRail].start;

        float CosAngle = Vector3.Dot(Vector3.Normalize(connectingVector), Vector3.Normalize(rail));

        if (CosAngle < 0)
        {
            gameObject.transform.position = m_Rails.rails[m_ActiveRail].start;

            if (m_ActiveRail > 0)
                m_ActiveRail--;
        }
        else
        {
            Vector3 projectedPos = CosAngle * connectingVector.magnitude * Vector3.Normalize(rail);

            if(projectedPos.magnitude < rail.magnitude)
                gameObject.transform.position = m_Rails.rails[m_ActiveRail].start + projectedPos;
            else
            {
                gameObject.transform.position = m_Rails.rails[m_ActiveRail].end;

                if (m_Rails.rails.Length > m_ActiveRail + 1)
                    m_ActiveRail++;
            }
        }
    }

    private void TriggerReleased()
    {
        if (m_ManipulationMode.mode == Mode.RAIL && isInteracting)
        {
            gameObject.transform.SetParent(null);
            m_InteractingHand = null;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
        }
    }
}