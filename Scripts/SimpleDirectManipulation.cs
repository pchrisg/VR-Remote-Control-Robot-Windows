using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using UnityEngine.UIElements;

public class SimpleDirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;
    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private bool isInteracting = false;
    private Hand m_InteractingHand = null;

    private GameObject m_GhostObject = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();

        m_Interactable = GetComponent<Interactable>();
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT && !m_GripperControl.isGripping)
        {
            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                    TriggerReleased();

                else if (m_Trigger.GetState(m_InteractingHand.handType) && !m_PlanningRobot.isPlanning)
                    MoveEndEffector();
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
        }
    }

    private void MoveEndEffector()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
        m_ROSPublisher.PublishMoveArm();
    }

    private void TriggerReleased()
    {
        GameObject.Destroy(m_GhostObject);
        m_InteractingHand = null;
        isInteracting = false;

        if (m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishTrajectoryRequest();
        else
            m_ROSPublisher.PublishMoveArm();
    }
}
