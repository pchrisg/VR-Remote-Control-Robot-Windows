using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using FeedBackModes;

namespace FeedBackModes
{
    public enum Mode
    {
        NONE,
        ERROR,
        ALWAYS
    };
}

public class RobotFeedback : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_FeedbackMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private Manipulator m_Manipulator = null;
    private Transform m_ManipulatorPose = null;
    private Mode m_Mode = Mode.NONE;
    private ExperimentManager m_ExperimentManager = null;

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private Color m_ShowColor = new(0.8f, 0.8f, 0.8f, 0.4f);
    private Color m_HideColor = new(0.0f, 0.0f, 0.0f, 0.0f);

    [HideInInspector] public bool isPlanning = false;

    private Vector3 m_PrevPosition = Vector3.zero;
    private Quaternion m_PrevRotation = Quaternion.identity;

    private bool m_busFree = true;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ManipulatorPose = m_Manipulator.transform.Find("Pose");
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Mode = m_ExperimentManager.m_FeedbackMode;

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
        string connectingLink = linkName + "/flange/tool0/palm/";
        for (var i = 0; i < k_RobotiqNumJoints; i += 4)
        {
            linkName = string.Empty;
            for (var j = i; j < i + 4 && j < k_RobotiqNumJoints; j++)
            {
                linkName += JointStateSubscriber.m_RobotiqLinkNames[j];
                m_RobotiqJoints[j] = robotiq.transform.Find(linkName).GetComponent<ArticulationBody>();
                m_PlanGripJoints[j] = gameObject.transform.Find(connectingLink + linkName).GetComponent<ArticulationBody>();
            }
        }
    }

    private void OnDestroy()
    {
        m_FeedbackMat.color = m_HideColor;
    }

    private void Update()
    {
        if (m_Mode != m_ExperimentManager.m_FeedbackMode)
        {
            if (m_ExperimentManager.m_FeedbackMode == Mode.ALWAYS)
            {
                m_FeedbackMat.color = m_ShowColor;
            }
            else
                m_FeedbackMat.color = m_HideColor;

            foreach (var joint in gameObject.GetComponentsInChildren<RobotFeedbackCollision>())
                joint.ResetColor();

            m_Mode = m_ExperimentManager.m_FeedbackMode;
        }
    }

    public void ResetPositionAndRotation()
    {
        m_PrevPosition = m_ManipulatorPose.position;
        m_PrevRotation = m_ManipulatorPose.rotation;

        GoToUR5();
    }

    public void RequestTrajectory()
    {
        if (m_busFree)
        {
            m_busFree = false;
            m_ROSPublisher.PublishTrajectoryRequest(m_PrevPosition, m_PrevRotation, m_ManipulatorPose.position, m_ManipulatorPose.rotation);

            m_PrevPosition = m_ManipulatorPose.position;
            m_PrevRotation = m_ManipulatorPose.rotation;
        }
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        GoToManipulator(trajectory);
        m_busFree = true;
    }

    private void GoToManipulator(RobotTrajectoryMsg trajectory)
    {
        if (trajectory.joint_trajectory.points.Length > 0)
        {
            var point = trajectory.joint_trajectory.points[^1];

            var jointPositions = point.positions;
            var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

            for (var joint = 0; joint < k_UR5NumJoints; joint++)
            {
                var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                joint1XDrive.target = result[joint];
                m_PlanRobJoints[joint].xDrive = joint1XDrive;
            }
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
}