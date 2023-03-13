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

    [Header("Robots")]
    [SerializeField] private GameObject m_UR5 = null;
    [SerializeField] private GameObject m_Robotiq = null;
    private GameObject m_EndEffector = null;

    private ROSConnection m_Ros = null;
    private readonly string m_JointStatesTopic = "/joint_states";

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_UR5Joints = null;
    
    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_RobotiqJoints = null;
    private ArticulationBody[] m_EndEffectorJoints = null;

    [HideInInspector] public static readonly string[] m_UR5LinkNames = { 
        "base_link/base_link_inertia/shoulder_pan_joint", 
        "/shoulder_lift_joint",
        "/elbow_joint",
        "/wrist_1_joint",
        "/wrist_2_joint",
        "/wrist_3_joint" };

    [HideInInspector]
    public static readonly string[] m_RobotiqLinkNames = {
        "palm",
        "/finger_1_link_0",
        "/finger_1_link_1",
        "/finger_1_link_2",
        "/finger_1_link_3",
        "/finger_2_link_0",
        "/finger_2_link_1",
        "/finger_2_link_2",
        "/finger_2_link_3",
        "/finger_middle_link_0/finger_middle_link_1",
        "/finger_middle_link_2",
        "/finger_middle_link_3" };

    private void Awake()
    {
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector");
        if (m_EndEffector != null)

        m_UR5Angles = new float[6];
        m_UR5Joints = new ArticulationBody[6];

        string linkname = string.Empty;
        for (var i = 0; i < k_UR5NumJoints; i++)
        {
            linkname += m_UR5LinkNames[i];
            m_UR5Joints[i] = m_UR5.transform.Find(linkname).GetComponent<ArticulationBody>();
        }
        linkname = string.Empty;

        m_RobotiqAngles = new float[11];
        m_RobotiqJoints = new ArticulationBody[11];
        m_EndEffectorJoints = new ArticulationBody[11];

        for (var i = 1; i < k_RobotiqNumJoints; i += 4)
        {
            linkname = m_RobotiqLinkNames[0];
            for (var j = i; j < i + 4 && j < k_RobotiqNumJoints + 1; j++)
            {
                linkname += m_RobotiqLinkNames[j];
                m_RobotiqJoints[j - 1] = m_Robotiq.transform.Find(linkname).GetComponent<ArticulationBody>();
                m_EndEffectorJoints[j - 1] = m_EndEffector.transform.Find(linkname).GetComponent<ArticulationBody>();
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
                m_EndEffectorJoints[i].xDrive = m_RobotiqJoints[i].xDrive;
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
}