using RosMessageTypes.ChrisUr5Moveit;
using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System;

public class PlanningRobot : MonoBehaviour
{
    [SerializeField] private GameObject m_UR5 = null;
    [SerializeField] private Manipulator m_Manipulator = null;
    [SerializeField] private Material m_Mat = null;
    [SerializeField] private ManipulatorPublisher m_RosPublisher = null;
    [HideInInspector] public RobotTrajectoryMsg m_Trajectory = null;

    private const int k_NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private bool followUR5 = false;
    private bool m_DisplayPath = false;

    [HideInInspector] public bool isPlanning = false;

    public void Awake()
    {
        m_PlanRobJoints = new ArticulationBody[k_NumJoints];
        m_UR5Joints = new ArticulationBody[k_NumJoints];
        var linkName = string.Empty;
        for (var joint = 0; joint < k_NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_LinkNames[joint];
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

    public void Show(bool value)
    {
        float r, g, b, a;
        r = b = g = 200.0f/255.0f;

        if (value)
            a = 100.0f / 255.0f;
        else
        {
            a = 0.0f;
            m_Manipulator.ResetPosition();
            m_Trajectory = null;
            m_DisplayPath = false;
        }

        m_Mat.color = new Color(r, g, b, a);

        followUR5 = !followUR5;
        isPlanning = !isPlanning;
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

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        m_Trajectory = trajectory;
        StopAllCoroutines();
        m_DisplayPath = false;
        StartCoroutine(DisplayPath());
    }

    public void StopDisplaying()
    {
        m_DisplayPath = false;
    }

    IEnumerator DisplayPath()
    {
        if (m_Trajectory != null)
        {
            m_DisplayPath = true;
            while(m_DisplayPath)
            {
                GoToUR5();
                yield return new WaitForSeconds(0.1f);

                // For every robot pose in trajectory plan
                foreach (var t in m_Trajectory.joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    // Set the joint values for every joint
                    for (var joint = 0; joint < k_NumJoints; joint++)
                    {
                        var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_PlanRobJoints[joint].xDrive = joint1XDrive;
                    }
                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(0.1f);
                }
                // Wait for the robot to achieve the final pose from joint assignment
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectory != null)
        {
            m_RosPublisher.PublishExecutePlan(m_Trajectory);
            m_DisplayPath = false;
            m_Trajectory = null;
        }
    }
}