using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class SimpleDirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private bool isInteracting = false;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_ActivationHand = null;
    private Hand m_InteractingHand = null;

    private Vector3 m_PreviousPosition = new();
    private Quaternion m_PreviousRotation = new();

    private GameObject m_GhostObject = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();

        m_RightHand = Player.instance.rightHand;
        m_LeftHand = Player.instance.leftHand;

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_RightHand.handType);
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_LeftHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_RightHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_LeftHand.handType);
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
        {
            m_ManipulationMode.isInteracting = isInteracting;

            if (m_ActivationHand != null && isInteracting)
                MoveManipulator();
        }
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (!isInteracting && m_ActivationHand == null)
        {
            m_GhostObject = new GameObject("GhostObject");
            m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);

            if (fromSource == m_LeftHand.handType)
            {
                m_ActivationHand = m_LeftHand;
                m_InteractingHand = m_RightHand;
            }
            else
            {
                m_ActivationHand = m_RightHand;
                m_InteractingHand = m_LeftHand;
            }

            m_PreviousPosition = m_InteractingHand.transform.position;
            m_PreviousRotation = m_InteractingHand.transform.rotation;

            isInteracting = true;
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
        {
            m_ActivationHand = null;
            Destroy(m_GhostObject);
            m_ROSPublisher.PublishMoveArm();
        }

        if (!m_Trigger.GetState(m_RightHand.handType) && !m_Trigger.GetState(m_LeftHand.handType))
            isInteracting = false;
    }

    private void MoveManipulator()
    {
        Vector3 fromToPosition = m_InteractingHand.transform.position - m_PreviousPosition;
        Quaternion fromToRotation = m_InteractingHand.transform.rotation * Quaternion.Inverse(m_PreviousRotation);

        m_GhostObject.transform.position += fromToPosition;
        m_GhostObject.transform.rotation = fromToRotation * m_GhostObject.transform.rotation;

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
        m_ROSPublisher.PublishMoveArm();

        m_PreviousPosition = m_InteractingHand.transform.position;
        m_PreviousRotation = m_InteractingHand.transform.rotation;
    }
}