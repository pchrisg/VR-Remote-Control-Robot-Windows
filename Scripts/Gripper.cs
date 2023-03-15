using RosMessageTypes.Robotiq3fGripperArticulated;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class Gripper : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Single m_Squeeze = null;

    private bool isInteracting = false;
    private Hand m_InitHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private readonly float m_MaxGrip = 220;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Squeeze = SteamVR_Input.GetAction<SteamVR_Action_Single>("SqueezeTrigger");

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.GRIPPER)
        {
            if (!isInteracting)
            {
                if (m_Trigger.GetState(m_RightHand.handType))
                {
                    m_InitHand = m_RightHand;
                    m_InteractingHand = m_LeftHand;
                    isInteracting = true;
                }
                else if (m_Trigger.GetState(m_LeftHand.handType))
                {
                    m_InitHand = m_LeftHand;
                    m_InteractingHand = m_RightHand;
                    isInteracting = true;
                }
            }
            else
            {
                if (m_Trigger.GetStateUp(m_InitHand.handType))
                    TriggerReleased();

                if (m_Trigger.GetState(m_InitHand.handType))
                    CloseGripper();
            }
        }
    }

    private void CloseGripper()
    {
            Robotiq3FGripperRobotOutputMsg outputMessage = new Robotiq3FGripperRobotOutputMsg();
            outputMessage.rACT = 1;
            outputMessage.rPRA = (byte)(m_Squeeze.GetAxis(m_InteractingHand.handType) * m_MaxGrip);

            m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
    }

    private void TriggerReleased()
    {
        m_InitHand = null;
        m_InteractingHand = null;
        isInteracting = false;
    }
}