using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

[RequireComponent(typeof(Interactable))]

public class SimpleDirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private ExperimentManager m_ExperimentManager = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private Interactable m_Interactable = null;
    private bool m_isInteracting = false;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;

    private GameObject m_GhostObject = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_RightHand = Player.instance.rightHand;
        m_LeftHand = Player.instance.leftHand;

        m_Interactable = GetComponent<Interactable>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_RightHand.handType);
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_LeftHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_RightHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_LeftHand.handType);
    }

    private void Update()
    {
        if (m_isInteracting && m_ExperimentManager.m_AllowUserControl)
            MoveManipulator();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!m_isInteracting)
            m_InteractingHand = hand;
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT && m_ExperimentManager.m_AllowUserControl)
        {
            if (!m_isInteracting && m_InteractingHand != null && fromSource == m_InteractingHand.handType && m_InteractingHand.IsStillHovering(m_Interactable))
            {
                m_isInteracting = true;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                m_GhostObject = new GameObject("GhostObject");
                m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                m_GhostObject.transform.SetParent(m_InteractingHand.transform);

                m_LeftHand.GetComponent<Hand>().Hide();
                m_RightHand.GetComponent<Hand>().Hide();
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
        {
            if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                Destroy(m_GhostObject);

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();

                m_ROSPublisher.PublishMoveArm();
            }
        }
    }

    private void MoveManipulator()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
        m_ROSPublisher.PublishMoveArm();
    }

    public Hand InteractingHand()
    {
        return m_InteractingHand;
    }
}