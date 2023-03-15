using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

[RequireComponent(typeof(Interactable))]

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

    private Vector3 m_InitPos = Vector3.zero;
    private GameObject m_GhostObject = null;

    private readonly float m_Threshold = 50.0f;
    private readonly float m_ScalingFactor = 0.25f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();

        m_Interactable = GetComponent<Interactable>();
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType) || m_Trigger.GetStateUp(m_OtherHand.handType))
                    TriggerReleased();

                else
                {
                    if (m_Trigger.GetStateDown(m_OtherHand.handType))
                        m_InitPos = gameObject.transform.position;

                    if (m_Trigger.GetState(m_InteractingHand.handType))
                        MoveManipulator();
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

    private void TriggerGrabbed()
    {
        if (m_InteractingHand.IsStillHovering(m_Interactable))
        {
            m_GhostObject = new GameObject("GhostObject");
            m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            m_GhostObject.transform.SetParent(m_InteractingHand.transform);
            isInteracting = true;
            m_InitPos = gameObject.transform.position;

            if (m_InteractingHand != Player.instance.rightHand)
                m_OtherHand = Player.instance.rightHand;
            else
                m_OtherHand = Player.instance.leftHand;
        }
    }

    private void MoveManipulator()
    {
        if(m_Trigger.GetState(m_OtherHand.handType))
        {
            Vector3 connectingVector = m_GhostObject.transform.position - m_InitPos;
            Vector3 position = m_InitPos + connectingVector * m_ScalingFactor;
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, gameObject.transform.rotation);
        }
        else
        {
            //Quaternion rotation = Snapping();
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
        }

        if (!m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishMoveArm();
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
        GameObject.Destroy(m_GhostObject);
        m_InteractingHand = null;
        m_OtherHand = null;
        m_InitPos = Vector3.zero;
        isInteracting = false;

        if (m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishTrajectoryRequest();
        else
            m_ROSPublisher.PublishMoveArm();
    }
}