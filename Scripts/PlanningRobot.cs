using RosMessageTypes.ChrisUr5Moveit;
using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System;
using System.Runtime.CompilerServices;

public class PlanningRobot : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private SDOFWidget m_SDOFWidget = null;

    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private GameObject m_UR5 = null;
    private EndEffector m_EndEffector = null;

    private RobotTrajectoryMsg m_Trajectory = null;

    private const int k_NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private bool followUR5 = false;
    private bool displayPath = false;

    [HideInInspector] public bool isPlanning = false;

    public void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<EndEffector>();

        m_PlanRobJoints = new ArticulationBody[k_NumJoints];
        m_UR5Joints = new ArticulationBody[k_NumJoints];
        var linkName = string.Empty;
        for (var joint = 0; joint < k_NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_UR5LinkNames[joint];
            m_PlanRobJoints[joint] = gameObject.transform.Find(linkName).GetComponent<ArticulationBody>();
            m_UR5Joints[joint] = m_UR5.transform.Find(linkName).GetComponent<ArticulationBody>();
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

            if(m_SDOFWidget.isActiveAndEnabled)
            {
                m_SDOFWidget.SetEndEffectorAsParent();
                m_EndEffector.ResetPosition();
                m_SDOFWidget.SetEndEffectorAsChild();
            }
            else
                m_EndEffector.ResetPosition();

            m_Trajectory = null;
            displayPath = false;
        }

        m_PlanRobMat.color = new Color(m_PlanRobMat.color.r, m_PlanRobMat.color.g, m_PlanRobMat.color.b, a);
    }

    public void GoToUR5()
    {
        for (var joint = 0; joint < k_NumJoints; joint++)
        {
            var joint1XDrive = m_PlanRobJoints[joint].xDrive;
            joint1XDrive.target = m_UR5Joints[joint].xDrive.target;
            m_PlanRobJoints[joint].xDrive = joint1XDrive;
        }
    }

    public void GoToManipulator()
    {
        // For every robot pose in trajectory plan
        foreach (var t in m_Trajectory.joint_trajectory.points)
        {
            var jointPositions = t.positions;
            var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

            for (var joint = 0; joint < k_NumJoints; joint++)
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
            GoToManipulator();

            m_ROSPublisher.PublishExecutePlan(m_Trajectory);
            displayPath = false;
            m_Trajectory = null;
        }
    }
}