using RosMessageTypes.Robotiq3fGripperArticulated;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

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

    private bool[] isHandHovering = { false, false };
    private Hand m_InitHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private readonly float m_MaxGrip = 220.0f;
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
    }

    private void Update()
    {
        if (!m_ManipulationMode.isInteracting &&
            (m_ManipulationMode.mode == ManipulationModes.Mode.SIMPLEDIRECT || 
            m_ManipulationMode.mode == ManipulationModes.Mode.DIRECT ||
            m_ManipulationMode.mode == ManipulationModes.Mode.SDOF ||
            m_ManipulationMode.mode == ManipulationModes.Mode.RAIL))
        {
            if (!isGripping)
            {
                if(m_InitHand == null)
                    TriggerGrabbedCheck();
            }
            else
            {
                if (!m_Trigger.GetState(m_InitHand.handType) && m_Squeeze.GetAxis(m_InteractingHand.handType) == 0)
                    TriggerReleased();

                else if (m_Trigger.GetState(m_InitHand.handType))
                    CloseGripper();
            }
        }
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (hand == m_RightHand)
            isHandHovering[0] = true;
        else
            isHandHovering[1] = true;
    }

    private void OnHandHoverEnd(Hand hand)
    {
        if (hand == m_RightHand)
            isHandHovering[0] = false;
        else
            isHandHovering[1] = false;
    }

    private void TriggerGrabbedCheck()
    {
        if (isHandHovering[0] || isHandHovering[1])
            return;

        if (m_ManipulationMode.mode == ManipulationModes.Mode.SDOF)
        {
            if (m_SDOFManipulation.m_InteractingHand != null &&
                m_SDOFManipulation.m_InteractingHand.IsStillHovering(m_SDOFManipulation.m_Interactable))
                return;
        }

        if (m_Trigger.GetState(m_RightHand.handType))
        {
            m_InitHand = m_RightHand;
            m_InteractingHand = m_LeftHand;
        }
        else if (m_Trigger.GetState(m_LeftHand.handType))
        {
            m_InitHand = m_LeftHand;
            m_InteractingHand = m_RightHand;
        }

        if (m_InitHand != null)
            isGripping = true;
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

    private void TriggerReleased()
    {
        m_InitHand = null;
        m_InteractingHand = null;
        isGripping = false;
    }
}