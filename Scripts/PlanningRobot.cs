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

    private bool displayPath = false;

    private Color m_ShowColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);
    private Color m_HideColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    [HideInInspector] public bool isPlanning = false;

    private void Awake()
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
    }

    private void Update()
    {
        if (!displayPath)
            GoToUR5();
    }

    private void OnDestroy()
    {
        m_PlanRobMat.color = m_HideColor;
    }

    public void Show()
    {
        isPlanning = !isPlanning;

        if (!isPlanning)
        {
            m_Manipulator.ResetPosition();
            m_Trajectory = null;
            displayPath = false;
            m_PlanRobMat.color = m_HideColor;
        }
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        StopAllCoroutines();

        m_Trajectory = trajectory;
        displayPath = true;
        m_PlanRobMat.color = m_ShowColor;

        StartCoroutine(DisplayPath());
    }

    private IEnumerator DisplayPath()
    {
        while (displayPath)
        {
            yield return StartCoroutine(GoToManipulator());

            yield return new WaitForSeconds(0.5f);

            GoToUR5();

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void GoToUR5()
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

    private IEnumerator GoToManipulator()
    {
        if (m_Trajectory == null)
            yield break;

        // For every robot pose in trajectory plan
        foreach (var t in m_Trajectory.joint_trajectory.points)
        {
            if (displayPath == false)
                break;

            var jointPositions = t.positions;
            var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

            for (var joint = 0; joint < k_UR5NumJoints; joint++)
            {
                var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                joint1XDrive.target = result[joint];
                m_PlanRobJoints[joint].xDrive = joint1XDrive;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectory != null)
        {
            displayPath = false;
            m_PlanRobMat.color = m_HideColor;
            m_ROSPublisher.PublishExecutePlan(m_Trajectory);
        }
    }
}