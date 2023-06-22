using UnityEngine;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using Unity.VisualScripting;

public class JointStateSubscriber : MonoBehaviour
{
    [Header("Joint Angles")]
    [SerializeField] private float[] m_UR5Angles = null;
    [SerializeField] private float[] m_RobotiqAngles = null;

    private GameObject m_UR5 = null;
    private GameObject m_Robotiq = null;
    private GameObject m_Manipulator = null;

    private ROSConnection m_Ros = null;
    private readonly string m_JointStatesTopic = "/joint_states";

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_UR5Joints = null;
    
    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_RobotiqJoints = null;
    private ArticulationBody[] m_ManipulatorJoints = null;

    [HideInInspector] 
    public static readonly string[] m_UR5LinkNames = { 
        "base_link/base_link_inertia/shoulder_link", 
        "/upper_arm_link",
        "/forearm_link",
        "/wrist_1_link",
        "/wrist_2_link",
        "/wrist_3_link" };

    [HideInInspector]
    public static readonly string[] m_RobotiqLinkNames = {
        "finger_1_link_0",
        "/finger_1_link_1",
        "/finger_1_link_2",
        "/finger_1_link_3",
        "finger_2_link_0",
        "/finger_2_link_1",
        "/finger_2_link_2",
        "/finger_2_link_3",
        "finger_middle_link_0/finger_middle_link_1",
        "/finger_middle_link_2",
        "/finger_middle_link_3" };

    private void Awake()
    {
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator");

        m_UR5Angles = new float[6];
        m_UR5Joints = new ArticulationBody[6];

        string linkName = string.Empty;
        for (var i = 0; i < k_UR5NumJoints; i++)
        {
            linkName += m_UR5LinkNames[i];
            m_UR5Joints[i] = m_UR5.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        m_RobotiqAngles = new float[11];
        m_RobotiqJoints = new ArticulationBody[11];
        m_ManipulatorJoints = new ArticulationBody[11];

        
        for (var i = 0; i < k_RobotiqNumJoints; i += 4)
        {
            linkName = string.Empty;
            for (var j = i; j < i + 4 && j < k_RobotiqNumJoints; j++)
            {
                linkName += m_RobotiqLinkNames[j];
                m_RobotiqJoints[j] = m_Robotiq.transform.Find(linkName).GetComponent<ArticulationBody>();
                m_ManipulatorJoints[j] = m_Manipulator.transform.Find("palm/"+linkName).GetComponent<ArticulationBody>();
            }
        }

        m_Ros = ROSConnection.GetOrCreateInstance();
    }

    private void Start()
    {
        m_Ros.Subscribe<SensorUnity>(m_JointStatesTopic, GetJointPositions);
    }
    
    public void OnDestroy()
    {
        m_Ros.Unsubscribe(m_JointStatesTopic);
    }

    private void GetJointPositions(SensorUnity sensorMsg)
    {
        StartCoroutine(SetJointValues(sensorMsg));
    }
    
    IEnumerator SetJointValues(SensorUnity message)
    {
        if (message.name[0] == "elbow_joint")
        {
            for (int i = 0; i < 3; i++)
            {
                var jointXDrive = m_UR5Joints[2 - i].xDrive;
                m_UR5Angles[2 - i] = (float)(message.position[i]) * Mathf.Rad2Deg;
                jointXDrive.target = m_UR5Angles[2 - i];
                m_UR5Joints[2 - i].xDrive = jointXDrive;
            }
            for (var i = 3; i < k_UR5NumJoints; i++)
            {
                var joint1XDrive = m_UR5Joints[i].xDrive;
                m_UR5Angles[i] = (float)(message.position[i]) * Mathf.Rad2Deg;
                joint1XDrive.target = m_UR5Angles[i];
                m_UR5Joints[i].xDrive = joint1XDrive;
            }
        }
        else if (message.name[0] == "palm_finger_1_joint")
        {
            for (var i = 0; i < k_RobotiqNumJoints; i++)
            {
                var jointXDrive = m_RobotiqJoints[i].xDrive;
                m_RobotiqAngles[i] = (float)(message.position[i]) * Mathf.Rad2Deg;
                jointXDrive.target = m_RobotiqAngles[i];
                m_RobotiqJoints[i].xDrive = jointXDrive;
                m_ManipulatorJoints[i].xDrive = m_RobotiqJoints[i].xDrive;
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
}