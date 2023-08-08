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

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Single m_Squeeze = null;

    private bool isInteracting = false;
    private bool[] isHandHovering = { false, false };
    private Hand m_InitHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private readonly float m_MaxGrip = 220.0f;
    //private float m_ObjGrip = 220.0f;
    private float m_CurrentGrip = 0.0f;
    private float m_TargetGrip = 0.0f;

    private readonly float m_GripSpeed = 1.0f;

    [HideInInspector] public bool isGripping = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulatorAudioSource = gameObject.GetComponent<AudioSource>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Squeeze = SteamVR_Input.GetAction<SteamVR_Action_Single>("SqueezeTrigger");

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void Update()
    {
        if (isGripping)
        {
            if (!isInteracting)
            {
                if(m_InitHand == null)
                    TriggerGrabbedCheck();

                else if (!m_Trigger.GetState(m_InitHand.handType) && !m_Trigger.GetState(m_InteractingHand.handType))
                    TriggerReleased();
            }
            else
            {
                if (m_Trigger.GetStateUp(m_InitHand.handType))
                    isInteracting = false;

                else if (m_Trigger.GetState(m_InitHand.handType))
                    CloseGripper();
            }
        }
    }

    public void Show(bool value)
    {
        isGripping = value;

        TriggerReleased();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (hand == m_RightHand)
            isHandHovering[0] = true;
        else
            isHandHovering[1] = true;
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (hand == m_RightHand)
            isHandHovering[0] = true;
        else
            isHandHovering[1] = true;
    }

    private void TriggerGrabbedCheck()
    {
        if (m_Trigger.GetState(m_RightHand.handType) && !isHandHovering[0])
        {
            m_InitHand = m_RightHand;
            m_InteractingHand = m_LeftHand;
            isInteracting = true;
        }
        else if (m_Trigger.GetState(m_LeftHand.handType) && !isHandHovering[1])
        {
            m_InitHand = m_LeftHand;
            m_InteractingHand = m_RightHand;
            isInteracting = true;
        }
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

    private void CloseGripper()
    {
        m_TargetGrip = m_Squeeze.GetAxis(m_InteractingHand.handType) * m_MaxGrip;
        if (m_TargetGrip < 1.0f)
            m_TargetGrip = 1.0f;

        if (m_TargetGrip == m_CurrentGrip)
            return;

        if(m_TargetGrip > m_CurrentGrip)
        {
            m_CurrentGrip += m_GripSpeed;
            if (m_TargetGrip < m_CurrentGrip)
                m_CurrentGrip = m_TargetGrip;
        }
        if (m_TargetGrip < m_CurrentGrip)
        {
            m_CurrentGrip -= m_GripSpeed;
            if (m_TargetGrip > m_CurrentGrip)
                m_CurrentGrip = m_TargetGrip;
        }

        Robotiq3FGripperRobotOutputMsg outputMessage = new Robotiq3FGripperRobotOutputMsg();
        outputMessage.rACT = 1;
        outputMessage.rPRA = (byte)(m_CurrentGrip);

        m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
    }

    private void TriggerReleased()
    {
        m_InitHand = null;
        m_InteractingHand = null;
        isInteracting = false;
        isHandHovering[0] = false;
        isHandHovering[1] = false;
    }
}