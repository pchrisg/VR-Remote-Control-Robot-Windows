using UnityEngine;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;

public class JointStateSubscriber : MonoBehaviour
{
    const int k_NumJoints = 6;
    public static readonly string[] m_LinkNames = { 
        "base_link/base_link_inertia/shoulder_pan_joint", 
        "/shoulder_lift_joint",
        "/elbow_joint",
        "/wrist_1_joint",
        "/wrist_2_joint",
        "/wrist_3_joint" };

    private string m_JointStates = "/joint_states";
    private ROSConnection m_Ros;

    [SerializeField] private GameObject m_UR5;
    [SerializeField] private float[] m_Angles;

    private ArticulationBody[] m_Joints;
    
    // Start is called before the first frame update
    void Start()
    {
        m_Angles = new float[6];
        m_Joints = new ArticulationBody[6];

        string linkname = string.Empty;
        for (var i = 0; i < k_NumJoints; i++)
        {
            linkname += m_LinkNames[i];
            m_Joints[i] = m_UR5.transform.Find(linkname).GetComponent<ArticulationBody>();
        }

        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.Subscribe<SensorUnity>(m_JointStates, GetJointPositions);
    }
    
    public void UnSub()
    {
        m_Ros.Unsubscribe(m_JointStates);
    }

    private void GetJointPositions(SensorUnity sensorMsg)
    {
        StartCoroutine(SetJointValues(sensorMsg));
    }
    
    IEnumerator SetJointValues(SensorUnity message)
    {
        for (int i = 0; i < 3; i++)
        {
            var joint1XDrive = m_Joints[2-i].xDrive;
            m_Angles[2-i] = (float)(message.position[i]) * Mathf.Rad2Deg;
            joint1XDrive.target = m_Angles[2-i];
            m_Joints[2-i].xDrive = joint1XDrive;
        }
        for (var i = 3; i < k_NumJoints; i++)
        {
            var joint1XDrive = m_Joints[i].xDrive;
            m_Angles[i] = (float)(message.position[i]) * Mathf.Rad2Deg;
            joint1XDrive.target = m_Angles[i];
            m_Joints[i].xDrive = joint1XDrive;
        }

        yield return new WaitForSeconds(0.1f);
    }
}