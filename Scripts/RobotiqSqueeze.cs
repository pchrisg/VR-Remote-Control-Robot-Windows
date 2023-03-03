using RosMessageTypes.Robotiq3fGripperArticulated;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class RobotiqSqueeze : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Single m_Squeeze = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private const float m_TimeInterval = 0.5f;
    private float period = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        m_Squeeze = SteamVR_Input.GetAction<SteamVR_Action_Single>("Squeeze");

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void Update()
    {
        if(m_Trigger.GetState(m_LeftHand.handType))
        {
            SqueezeHand();
            
        }
    }

    private void SqueezeHand()
    {
        if (period > m_TimeInterval)
        {
            Robotiq3FGripperRobotOutputMsg outputMessage = new Robotiq3FGripperRobotOutputMsg();
            outputMessage.rACT = 1;
            outputMessage.rPRA = (byte)(m_Squeeze.GetAxis(m_RightHand.handType) * 255);

            m_ROSPublisher.PublishRobotiqSqueeze(outputMessage);
            period = 0;
        }
        period += UnityEngine.Time.deltaTime;
    }
}
