using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem;

public class DirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    
    private bool isInteracting = false;
    private Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;
    private GameObject m_GhostObject = null;

    private readonly float m_Threshold = 10.0f;
    private readonly float m_ScaleFactor = 0.25f;

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
            if (m_Trigger.GetState(m_InteractingHand.handType))
                MoveManipulator();

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
                m_GhostObject = new GameObject("GhostObject");
                m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                m_GhostObject.transform.SetParent(m_InteractingHand.transform);
                isInteracting = true;

                if (m_InteractingHand != Player.instance.rightHand)
                    m_OtherHand = Player.instance.rightHand;
                else
                    m_OtherHand = Player.instance.leftHand;
            }
        }
    }

    private void MoveManipulator()
    {
        if(m_OtherHand != null && m_Trigger.GetState(m_OtherHand.handType))
        {
            Vector3 connectingVector = m_GhostObject.transform.position - gameObject.transform.position;
            Vector3 position = gameObject.transform.position + connectingVector * m_ScaleFactor;
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, gameObject.transform.rotation);
        }
        else
        {
            Quaternion rotation = Snapping();
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, rotation);

        }
    }

    private Quaternion Snapping()
    {
        float angle = Mathf.Acos(Vector3.Dot(-m_GhostObject.transform.right.normalized, Vector3.up.normalized)) * 180 / Mathf.PI;
        if (Mathf.Abs(90.0f - angle) < m_Threshold)
        {
            Vector3 projectedConnectingVector = Vector3.ProjectOnPlane(-m_GhostObject.transform.right.normalized, Vector3.up);
            Quaternion rotation = Quaternion.FromToRotation(-m_GhostObject.transform.right.normalized, projectedConnectingVector);
            return m_GhostObject.transform.rotation * rotation;
        }

        if (angle < m_Threshold || Mathf.Abs(180.0f - angle) < m_Threshold)
        {
            Vector3 projectedConnectingVector = Vector3.Project(-m_GhostObject.transform.right.normalized, Vector3.up);
            Quaternion rotation = Quaternion.FromToRotation(-m_GhostObject.transform.right.normalized, projectedConnectingVector);
            return m_GhostObject.transform.rotation * rotation;
        }

        return m_GhostObject.transform.rotation;
    }

    private void TriggerReleased()
    {
        if (m_ManipulationMode.mode == Mode.DIRECT && isInteracting)
        {
            GameObject.Destroy(m_GhostObject);
            m_InteractingHand = null;
            m_OtherHand = null;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}