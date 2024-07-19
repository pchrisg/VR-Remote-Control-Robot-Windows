using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(Interactable))]

public class SimpleDirectManipulation : MonoBehaviour
{
    [SerializeField] private GameObject m_TetherPrefab = null;
    public float m_TetherDistance = 0.25f;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private GameObject m_EndEffector = null;
    private ExperimentManager m_ExperimentManager = null;
    private RobotFeedback m_RobotFeedback = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private Interactable m_Interactable = null;
    private bool m_isInteracting = false;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;

    private GameObject m_GhostObject = null;
    private GameObject m_Tether = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_EndEffector = GameObject.FindGameObjectWithTag("Robotiq");
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_RobotFeedback = GameObject.FindGameObjectWithTag("RobotFeedback").GetComponent<RobotFeedback>();

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
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT && !m_isInteracting)
        {
            m_InteractingHand = hand;

            if (hand == m_RightHand)
                m_OtherHand = m_LeftHand;
            else
                m_OtherHand = m_RightHand;

            if (m_ManipulationMode.m_ShowHints)
            {
                ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Trigger, "Move Manipulator", false);
                ControllerButtonHints.ShowTextHint(m_OtherHand, m_Trigger, "Operate Gripper", false);
            }
        }
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

                m_Tether = Instantiate(m_TetherPrefab);
                m_Tether.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                m_Tether.transform.localScale = new Vector3(0.0025f, 0.0f, 0.0025f);

                m_LeftHand.GetComponent<Hand>().Hide();
                m_RightHand.GetComponent<Hand>().Hide();

                if (m_ManipulationMode.m_ShowHints)
                    ControllerButtonHints.HideTextHint(m_InteractingHand, m_Trigger);
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
                Destroy(m_Tether);

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();

                m_ROSPublisher.PublishMoveArm();
                
                if (m_ManipulationMode.m_ShowHints)
                    ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Trigger, "Move Manipulator", false);
            }
        }
    }

    private void MoveManipulator()
    {
        Vector3 connectingVector = m_GhostObject.transform.position - m_EndEffector.transform.position;
        if(connectingVector.magnitude < m_TetherDistance)
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
        else
        {
            Vector3 position = m_EndEffector.transform.position + connectingVector.normalized * m_TetherDistance;
            gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, m_GhostObject.transform.rotation);
        }

        connectingVector = gameObject.transform.position - m_EndEffector.transform.position;
        m_Tether.transform.SetPositionAndRotation(m_EndEffector.transform.position + connectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, connectingVector));
        m_Tether.transform.localScale = new(0.0025f, connectingVector.magnitude * 0.5f, 0.0025f);

        m_RobotFeedback.RequestTrajectory();
        m_ROSPublisher.PublishMoveArm();
    }

    public Hand InteractingHand()
    {
        return m_InteractingHand;
    }
}