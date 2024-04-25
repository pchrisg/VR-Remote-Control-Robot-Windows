using RosMessageTypes.Robotiq3fGripperArticulated;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class GripperControl : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip m_Attach = null;
    [SerializeField] private AudioClip m_Detach = null;

    private ROSPublisher m_ROSPublisher = null;
    private AudioSource m_ManipulatorAudioSource = null;
    private ManipulationMode m_ManipulationMode = null;
    private SimpleDirectManipulation m_SimpleDirectManipulation = null;
    private ConstrainedDirectManipulation m_ConstrainedDirectManipulation = null;
    private SDOFManipulation m_SDOFManipulation = null;
    private ExperimentManager m_ExperimentManager = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_GrippingHand = null;

    private readonly float m_MaxGrasp = 120.0f;
    private float m_TargetGrasp = 0.0f;

    private bool m_isInteracting = false;
    private bool m_isGripping = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();

        GameObject manipulator = GameObject.FindGameObjectWithTag("Manipulator");
        m_SimpleDirectManipulation = manipulator.GetComponent<SimpleDirectManipulation>();
        m_ConstrainedDirectManipulation = manipulator.GetComponent<ConstrainedDirectManipulation>();
        m_SDOFManipulation = manipulator.transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();
        m_ManipulatorAudioSource = gameObject.GetComponent<AudioSource>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_RightHand.handType);
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_LeftHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_RightHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_LeftHand.handType);
    }

    private void Update()
    {
        if (m_isInteracting && m_ExperimentManager.m_AllowUserControl)
            MoveGripper();
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ExperimentManager.m_AllowUserControl)
        {
            Hand interactingHand = null;
            if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
                interactingHand = m_SimpleDirectManipulation.InteractingHand();

            if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
                interactingHand = m_ConstrainedDirectManipulation.InteractingHand();

            if (m_ManipulationMode.mode == Mode.SDOF)
                interactingHand = m_SDOFManipulation.InteractingHand();

            if (interactingHand != null)
            {
                if (m_InteractingHand == null || m_InteractingHand != interactingHand)
                {
                    m_InteractingHand = interactingHand;

                    if (m_InteractingHand == m_LeftHand)
                        m_GrippingHand = m_RightHand;
                    else
                        m_GrippingHand = m_LeftHand;
                }

                if (fromSource == m_GrippingHand.handType)
                {
                    m_isInteracting = true;
                    m_isGripping = !m_isGripping;
                }
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_GrippingHand != null && fromSource == m_GrippingHand.handType)
            m_isInteracting = false;
    }

    private void MoveGripper()
    {
        // Closing
        if (m_isGripping)
            m_TargetGrasp = m_MaxGrasp;
        // Openning
        else
            m_TargetGrasp = 0.0f;

        Robotiq3FGripperRobotOutputMsg outputMessage = new()
        {
            rACT = 1,
            rPRA = (byte)(m_TargetGrasp)
        };

        m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
    }

    public void PlayAttachSound()
    {
        m_ManipulatorAudioSource.clip = m_Attach;
        m_ManipulatorAudioSource.Play();
    }

    public void PlayDetachSound()
    {
        m_ManipulatorAudioSource.clip = m_Detach;
        m_ManipulatorAudioSource.Play();
    }

    public Hand GrippingHand()
    {
        return m_GrippingHand;
    }
}