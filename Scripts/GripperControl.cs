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
    
    private readonly float m_MaxGrasp = 120.0f;
    private float m_TargetGrasp = 0.0f;
    private float m_PreviousGrasp = 0.0f;

    private bool m_isInteracting = false;
    private bool m_isGripping = false;
    private bool m_isReleasing = true;
    private bool m_isLocked = false;

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
        if (m_isInteracting && m_ActivationHand != null && (m_isGripping || m_isReleasing))
            MoveGripper();
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT || m_ManipulationMode.mode == Mode.DIRECT || (m_ManipulationMode.mode == Mode.SDOF && m_SDOFManipulation.GetHoveringHand() == null && !m_SDOFManipulation.IsInteracting()))
        {
            if (!m_isInteracting && m_ActivationHand == null)
            {
                m_isInteracting = true;

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
            }
            
            if (m_ActivationHand != null)
            {
                if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
                {
                    if (m_isGripping)
                    {
                        m_PreviousGrasp = 0.0f;
                        m_isGripping = false;
                        m_isReleasing = true;
                    }
                    else
                    {
                        m_isLocked = true;
                        m_isGripping = true;
                        m_isReleasing = false;
                    }
                }
            }


            //if (!m_isGripping && m_ActivationHand == null)
            //{
            //    m_isGripping = true;

            //    if (fromSource == m_LeftHand.handType)
            //    {
            //        m_ActivationHand = m_LeftHand;
            //        m_InteractingHand = m_RightHand;
            //    }
            //    else
            //    {
            //        m_ActivationHand = m_RightHand;
            //        m_InteractingHand = m_LeftHand;
            //    }
            //}
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
            m_ActivationHand = null;

        if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            MoveGripper();

        if (!m_Trigger.GetState(m_RightHand.handType) && !m_Trigger.GetState(m_LeftHand.handType))
            m_isInteracting = false;
    }

    private void MoveGripper()
    {
        m_TargetGrasp = m_Squeeze.GetAxis(m_InteractingHand.handType) * m_MaxGrasp;

        if (m_TargetGrasp == 0.0f)
        {
            if (m_isGripping)
                m_TargetGrasp = m_MaxGrasp;
        }

        if (m_isGripping)
        {
            if (m_TargetGrasp < m_PreviousGrasp)
                m_TargetGrasp = m_PreviousGrasp;
            else
                m_PreviousGrasp = m_TargetGrasp;
        }

        if (m_isReleasing)
        {
            if (m_isLocked)
            {
                if (m_TargetGrasp == m_MaxGrasp)
                    m_isLocked = false;
                else
                    m_TargetGrasp = m_MaxGrasp;
            }
        }

        Robotiq3FGripperRobotOutputMsg outputMessage = new()
        {
            rACT = 1,
            rPRA = (byte)(m_TargetGrasp)
        };

        m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);

        //m_TargetGrasp = m_Squeeze.GetAxis(m_InteractingHand.handType) * m_MaxGrasp;

        //Robotiq3FGripperRobotOutputMsg outputMessage = new()
        //{
        //    rACT = 1,
        //    rPRA = (byte)(m_TargetGrasp)
        //};

        //m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
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
}