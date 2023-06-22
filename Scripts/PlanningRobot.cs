using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;

public class PlanningRobot : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private Manipulator m_Manipulator = null;

    private RobotTrajectoryMsg m_Trajectory = null;

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private bool followUR5 = false;
    private bool displayPath = false;

    [HideInInspector] public bool isPlanning = false;

    public void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();

        m_PlanRobJoints = new ArticulationBody[k_UR5NumJoints];
        m_UR5Joints = new ArticulationBody[k_UR5NumJoints];

        m_PlanGripJoints = new ArticulationBody[k_RobotiqNumJoints];
        m_RobotiqJoints = new ArticulationBody[k_RobotiqNumJoints];

        GameObject ur5 = GameObject.FindGameObjectWithTag("robot");
        var linkName = string.Empty;
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_UR5LinkNames[joint];
            m_PlanRobJoints[joint] = gameObject.transform.Find(linkName).GetComponent<ArticulationBody>();
            m_UR5Joints[joint] = ur5.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        GameObject robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        string connectingLink = linkName + "/flange/Robotiq/palm/";
        for (var i = 0; i < k_RobotiqNumJoints; i += 4)
        {
            linkName = string.Empty;
            for (var j = i; j < i + 4 && j < k_RobotiqNumJoints; j++)
            {
                linkName += JointStateSubscriber.m_RobotiqLinkNames[j];
                m_RobotiqJoints[j] = robotiq.transform.Find(linkName).GetComponent<ArticulationBody>();
                m_PlanGripJoints[j] = gameObject.transform.Find(connectingLink+linkName).GetComponent<ArticulationBody>();
            }
        }

        followUR5 = true;
    }

    private void Update()
    {
        if (followUR5)
            GoToUR5();
    }

    private void OnDestroy()
    {
        if(isPlanning)
        {
            m_PlanRobMat.color = new Color(m_PlanRobMat.color.r, m_PlanRobMat.color.g, m_PlanRobMat.color.b, 0.0f);
        }
    }

    public void Show()
    {
        isPlanning = !isPlanning;
        followUR5 = !followUR5;

        float a = 100.0f / 255.0f;
        if (!isPlanning)
        {
            a = 0.0f;
            m_Manipulator.ResetPosition();
            m_Trajectory = null;
            displayPath = false;
        }

        m_PlanRobMat.color = new Color(m_PlanRobMat.color.r, m_PlanRobMat.color.g, m_PlanRobMat.color.b, a);
    }

    public void GoToUR5()
    {
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            var joint1XDrive = m_PlanRobJoints[joint].xDrive;
            joint1XDrive.target = m_UR5Joints[joint].xDrive.target;
            m_PlanRobJoints[joint].xDrive = joint1XDrive;
        }

        for (var joint = 0; joint < k_RobotiqNumJoints; joint++)
        {
            var joint1XDrive = m_PlanGripJoints[joint].xDrive;
            joint1XDrive.target = m_RobotiqJoints[joint].xDrive.target;
            m_PlanGripJoints[joint].xDrive = joint1XDrive;
        }
    }

    public void GoToManipulator()
    {
        // For every robot pose in trajectory plan
        foreach (var t in m_Trajectory.joint_trajectory.points)
        {
            var jointPositions = t.positions;
            var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

            for (var joint = 0; joint < k_UR5NumJoints; joint++)
            {
                var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                joint1XDrive.target = result[joint];
                m_PlanRobJoints[joint].xDrive = joint1XDrive;
            }
        }

        if(!displayPath)
            m_Trajectory = null;
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        m_Trajectory = trajectory;
        displayPath = true;
        StopAllCoroutines();
        StartCoroutine(DisplayPath());
    }

    IEnumerator DisplayPath()
    {
        while(displayPath)
        {
            GoToManipulator();
            yield return new WaitForSeconds(0.5f);

            GoToUR5();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectory != null)
        {
            displayPath = false;
            m_ROSPublisher.PublishExecutePlan(m_Trajectory);

            GoToManipulator();
        }
    }
}