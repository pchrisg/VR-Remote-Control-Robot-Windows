using RosMessageTypes.ChrisUr5Moveit;
using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System;
using System.Runtime.CompilerServices;

public class PlanningRobot : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private EndEffector m_EndEffector = null;

    private RobotTrajectoryMsg m_Trajectory = null;

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_GripperNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_GripperJoints = null;

    private bool followUR5 = false;
    private bool displayPath = false;

    [HideInInspector] public bool isPlanning = false;

    public void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<EndEffector>();

        m_PlanRobJoints = new ArticulationBody[k_UR5NumJoints];
        m_UR5Joints = new ArticulationBody[k_UR5NumJoints];

        m_PlanGripJoints = new ArticulationBody[k_GripperNumJoints];
        m_GripperJoints = new ArticulationBody[k_GripperNumJoints];

        GameObject ur5 = GameObject.FindGameObjectWithTag("robot");
        var linkName = string.Empty;
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_UR5LinkNames[joint];
            m_PlanRobJoints[joint] = gameObject.transform.Find(linkName).GetComponent<ArticulationBody>();
            m_UR5Joints[joint] = ur5.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        GameObject gripper = GameObject.FindGameObjectWithTag("Gripper");
        string connectingLink = linkName + "/flange/Gripper/";
        linkName = string.Empty;
        for (var i = 1; i < k_GripperNumJoints; i += 4)
        {
            linkName = JointStateSubscriber.m_GripperLinkNames[0];
            for (var j = i; j < i + 4 && j < k_GripperNumJoints + 1; j++)
            {
                linkName += JointStateSubscriber.m_GripperLinkNames[j];
                m_GripperJoints[j - 1] = gripper.transform.Find(linkName).GetComponent<ArticulationBody>();
                m_PlanGripJoints[j - 1] = gameObject.transform.Find(connectingLink+linkName).GetComponent<ArticulationBody>();
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
            m_EndEffector.ResetPosition();
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

        for (var joint = 0; joint < k_GripperNumJoints; joint++)
        {
            var joint1XDrive = m_PlanGripJoints[joint].xDrive;
            joint1XDrive.target = m_GripperJoints[joint].xDrive.target;
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
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        m_Trajectory = trajectory;
        StopAllCoroutines();
        StartCoroutine(DisplayPath());
    }

    IEnumerator DisplayPath()
    {
        if (m_Trajectory != null)
        {
            displayPath = true;
            while(displayPath)
            {
                GoToManipulator();
                yield return new WaitForSeconds(0.5f);

                GoToUR5();
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectory != null)
        {
            displayPath = false;
            m_ROSPublisher.PublishExecutePlan(m_Trajectory);

            GoToManipulator();
            m_Trajectory = null;
        }
    }
}