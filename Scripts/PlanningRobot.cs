using RosMessageTypes.ChrisUr5Moveit;
using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System;

public class PlanningRobot : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_UR5 = null;
    [SerializeField] private Manipulator m_Manipulator = null;
    [SerializeField] private Material m_Mat = null;
    [SerializeField] private ROSPublisher m_ROSPublisher = null;
    
    private RobotTrajectoryMsg m_Trajectory = null;

    private const int k_NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private bool followUR5 = false;
    private bool displayPath = false;
    private bool isAtUR5 = false;

    private const float color = 200.0f / 255.0f;
    private readonly float r = color;
    private readonly float g = color;
    private readonly float b = color;
    private float a = 0.0f;

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

    private void OnDestroy()
    {
        if(isPlanning)
        {
            a = 0.0f;
            m_Mat.color = new Color(r, g, b, a);
        }
    }

    public void Show()
    {
        if (isPlanning)
            a = 100.0f / 255.0f;
        else
        {
            a = 0.0f;
            m_Manipulator.ResetPosition();
            m_Trajectory = null;
            displayPath = false;
        }

        m_Mat.color = new Color(r, g, b, a);

        followUR5 = !followUR5;
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
        displayPath = false;
        StartCoroutine(DisplayPath());
    }

    IEnumerator DisplayPath()
    {
        if (m_Trajectory != null)
        {
            displayPath = true;
            while(displayPath)
            {
                isAtUR5 = false;
                GoToManipulator();
                yield return new WaitForSeconds(0.5f);

                GoToUR5();
                isAtUR5 = true;
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectory != null)
        {
            if (isAtUR5)
                GoToManipulator();

            m_ROSPublisher.PublishExecutePlan(m_Trajectory);
            displayPath = false;
            m_Trajectory = null;
        }
    }
}