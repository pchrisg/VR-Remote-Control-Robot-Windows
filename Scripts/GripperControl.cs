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
    private SDOFManipulation m_SDOFManipulation = null;

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Single m_Squeeze = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_ActivationHand = null;
    private Hand m_InteractingHand = null;
    
    private readonly float m_MaxGrip = 120.0f;
    private float m_TargetGrip = 0.0f;

    [HideInInspector] public bool isGripping = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_SDOFManipulation = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();
        m_ManipulatorAudioSource = gameObject.GetComponent<AudioSource>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Squeeze = SteamVR_Input.GetAction<SteamVR_Action_Single>("SqueezeTrigger");

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_RightHand.handType);
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_LeftHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_RightHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_LeftHand.handType);
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT || m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (m_ActivationHand != null && isGripping)
                CloseGripper();
        }
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (!isGripping && m_ActivationHand == null)
        {
            if (m_ManipulationMode.mode == ManipulationModes.Mode.SDOF)
            {
                if (m_SDOFManipulation.m_InteractingHand != null &&
                    m_SDOFManipulation.m_InteractingHand.IsStillHovering(m_SDOFManipulation.m_Interactable))
                    return;
            }

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

            isGripping = true;
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
        {
            m_ActivationHand = null;
            m_InteractingHand = null;
        }

        if (!m_Trigger.GetState(m_RightHand.handType) && !m_Trigger.GetState(m_LeftHand.handType))
            isGripping = false;
    }

    private void CloseGripper()
    {
        m_TargetGrip = m_Squeeze.GetAxis(m_InteractingHand.handType) * m_MaxGrip;
        if (m_TargetGrip < 1.0f)
            m_TargetGrip = 1.0f;

        Robotiq3FGripperRobotOutputMsg outputMessage = new Robotiq3FGripperRobotOutputMsg();
        outputMessage.rACT = 1;
        outputMessage.rPRA = (byte)(m_TargetGrip);

        m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
    }

    public void Attach()
    {
        m_ManipulatorAudioSource.clip = m_Attach;
        m_ManipulatorAudioSource.Play();
    }

    public void Detach()
    {
        m_ManipulatorAudioSource.clip = m_Detach;
        m_ManipulatorAudioSource.Play();
    }
}