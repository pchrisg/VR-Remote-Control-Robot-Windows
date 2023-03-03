using UnityEngine;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using Unity.VisualScripting;

public class RobotiqJointStateSubscriber : MonoBehaviour
{
    [Header("Robot")]
    [SerializeField] private GameObject m_Robotiq = null;

    [Header("Joint Angles")]
    [SerializeField] private float[] m_Angles = null;
    
    private ROSConnection m_Ros = null;
    private readonly string m_JointStatesTopic = "/robotiq_joint_states";

    private const int k_NumJoints = 11;
    private ArticulationBody[] m_Joints = null;
    
    [HideInInspector]
    public static readonly string[] m_LinkNames = {
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
        m_Angles = new float[11];
        m_Joints = new ArticulationBody[11];

        string linkname = m_LinkNames[0];
        for (var i = 1; i < 5; i++)
        {
            linkname += m_LinkNames[i];
            m_Joints[i-1] = m_Robotiq.transform.Find(linkname).GetComponent<ArticulationBody>();
        }
        linkname = m_LinkNames[0];
        for (var i = 5; i < 9; i++)
        {
            linkname += m_LinkNames[i];
            m_Joints[i-1] = m_Robotiq.transform.Find(linkname).GetComponent<ArticulationBody>();
        }
        linkname = m_LinkNames[0];
        for (var i = 9; i < 12; i++)
        {
            linkname += m_LinkNames[i];
            m_Joints[i - 1] = m_Robotiq.transform.Find(linkname).GetComponent<ArticulationBody>();
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
        for (var i = 0; i < k_NumJoints; i++)
        {
            var joint1XDrive = m_Joints[i].xDrive;
            m_Angles[i] = (float)(message.position[i]) * Mathf.Rad2Deg;
            joint1XDrive.target = m_Angles[i];
            m_Joints[i].xDrive = joint1XDrive;
        }

        yield return new WaitForSeconds(0.1f);
    }
}