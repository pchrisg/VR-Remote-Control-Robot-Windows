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
    public float m_Speed = 10.0f;

    private ROSPublisher m_ROSPublisher = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Forward = null;
    private SteamVR_Action_Boolean m_Reverse = null;

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
        m_Forward = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Forward");
        m_Reverse = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Reverse");

        m_Trigger.onStateDown += TriggerGrabbed;
        m_Forward.onStateDown += ForwardPressed;
        m_Reverse.onStateDown += ReversePressed;
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= TriggerGrabbed;
        m_Forward.onStateDown -= ForwardPressed;
        m_Reverse.onStateDown -= ReversePressed;
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            m_ActiveRail = 0;
            isInteracting = false;
        }

        if (isInteracting)
        {
            if(m_InteractingHand != null)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                    Released();

                else
                {
                    FollowRail();
                    if (!m_PlanningRobot.isPlanning)
                        m_ROSPublisher.PublishMoveArm();
                }
            }
            else
            {
                if (m_Forward.GetStateUp(Player.instance.leftHand.handType) || m_Reverse.GetStateUp(Player.instance.leftHand.handType))
                    Released();

                else
                {
                    if (m_Forward.GetState(Player.instance.leftHand.handType))
                        Forward();

                    if (m_Reverse.GetState(Player.instance.leftHand.handType))
                        Reverse();

                    if (!m_PlanningRobot.isPlanning)
                        m_ROSPublisher.PublishMoveArm();
                }
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

    private void ForwardPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        isInteracting = true;
    }

    private void ReversePressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        isInteracting = true;
    }

    private void Forward()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        gameObject.transform.position += Vector3.Normalize(rail) * m_Speed;

        Vector3 connectingVector = gameObject.transform.position - m_Rails.rails[m_ActiveRail].start;
        if(connectingVector.magnitude > rail.magnitude)
        {
            gameObject.transform.position = m_Rails.rails[m_ActiveRail].end;
            if (m_Rails.rails.Length > m_ActiveRail + 1 && period > m_TimeInterval)
            {
                m_ActiveRail++;
                period = 0;
            }
            period += UnityEngine.Time.deltaTime;
        }
    }

    private void Reverse()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        gameObject.transform.position -= Vector3.Normalize(rail) * m_Speed;

        Vector3 connectingVector = gameObject.transform.position - m_Rails.rails[m_ActiveRail].start;
        if (Vector3.Dot(Vector3.Normalize(connectingVector), Vector3.Normalize(rail)) < 0)
        {
            gameObject.transform.position = m_Rails.rails[m_ActiveRail].start;
            if (m_ActiveRail > 0 && period > m_TimeInterval)
            {
                m_ActiveRail--;
                period = 0;
            }
            period += UnityEngine.Time.deltaTime;
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

            if (m_ActiveRail > 0 && period > m_TimeInterval)
            {
                m_ActiveRail--;
                period = 0;
            }
            period += UnityEngine.Time.deltaTime;
        }
        else
        {
            Vector3 projectedPos = CosAngle * connectingVector.magnitude * Vector3.Normalize(rail);

            if(projectedPos.magnitude < rail.magnitude)
                gameObject.transform.position = m_Rails.rails[m_ActiveRail].start + projectedPos;
            else
            {
                gameObject.transform.position = m_Rails.rails[m_ActiveRail].end;

                if (m_Rails.rails.Length > m_ActiveRail + 1 && period > m_TimeInterval)
                {
                    m_ActiveRail++;
                    period = 0;
                }
                period += UnityEngine.Time.deltaTime;
            }
        }
    }

    private void Released()
    {
        if (m_ManipulationMode.mode == Mode.RAIL && isInteracting)
        {
            gameObject.transform.SetParent(null);
            m_InteractingHand = null;
            isInteracting = false;
            period = 0.0f;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}