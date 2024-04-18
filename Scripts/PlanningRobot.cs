using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System.Collections.Generic;
using ManipulationModes;

public class PlanningRobot : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private Manipulator m_Manipulator = null;
    private Transform m_ManipulatorPose = null;
    private ManipulationMode m_ManipulationMode = null;
    
    private Transform m_Robotiq = null;
    private Transform m_PlanRobotiq = null;

    private List<RobotTrajectoryMsg> m_Trajectories = new List<RobotTrajectoryMsg>();
    private List<Vector3> m_StartPoses = new List<Vector3>();
    private List<Vector3> m_ManipulatorPoses = new List<Vector3>();

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private bool m_DisplayPath = false;

    private Color m_ShowColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);
    private Color m_HideColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    [HideInInspector] public bool isPlanning = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ManipulatorPose = m_Manipulator.transform.Find("Pose");
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;

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
        m_PlanRobotiq = gameObject.transform.Find(connectingLink);
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
        if (!m_DisplayPath)
            GoToUR5();
    }

    private void OnDestroy()
    {
        m_PlanRobMat.color = m_HideColor;
    }

    public void Show(bool value)
    {
        isPlanning = value;

        if (!isPlanning)
        {
            if (m_Trajectories.Any())
                m_Trajectories.Clear();
            if (m_StartPoses.Any())
                m_StartPoses.Clear();

            m_DisplayPath = false;
            m_PlanRobMat.color = m_HideColor;
        }
    }

    public void RequestTrajectory()
    {
        m_ROSPublisher.PublishTrajectoryRequest(m_Robotiq.parent.position, m_Robotiq.parent.rotation, m_ManipulatorPose.position, m_ManipulatorPose.rotation);

        m_DisplayPath = true;
    }

    public void RequestTrajectory(Vector3 startPos, Vector3 destPos, bool displayPath)
    {
        m_DisplayPath = displayPath;

        if (!m_ManipulatorPoses.Any())
            m_ManipulatorPoses.Add(startPos);

        if (m_ManipulatorPoses.Last() != m_ManipulatorPose.position)
            m_ManipulatorPoses.Add(m_ManipulatorPose.position);

        m_StartPoses.Add(startPos);

        m_ROSPublisher.PublishTrajectoryRequest(startPos, m_ManipulatorPose.rotation, destPos, m_ManipulatorPose.rotation);
    }

    public void DeleteLastTrajectory()
    {
        StopAllCoroutines();
        m_DisplayPath = false;
        m_PlanRobMat.color = m_HideColor;
        m_Manipulator.IsColliding(false);

        if (m_ManipulatorPoses.Any())
            m_ManipulatorPoses.Remove(m_ManipulatorPoses.Last());

        while (m_StartPoses.Last() != m_ManipulatorPoses.Last())
        {
            if (m_Trajectories.Any())
                m_Trajectories.Remove(m_Trajectories.Last());
            if (m_StartPoses.Any())
                m_StartPoses.Remove(m_StartPoses.Last());
        }

        if (m_Trajectories.Any())
            m_Trajectories.Remove(m_Trajectories.Last());
        if (m_StartPoses.Any())
            m_StartPoses.Remove(m_StartPoses.Last());

        if (m_Trajectories.Any())
        {
            m_DisplayPath = true;
            m_PlanRobMat.color = m_ShowColor;

            StartCoroutine(DisplayPath());
        }
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        StopAllCoroutines();

        if(m_ManipulationMode.mode != Mode.RAILCREATOR)
            m_Trajectories.Clear();

        m_Trajectories.Add(trajectory);
        m_PlanRobMat.color = m_ShowColor;

        StartCoroutine(DisplayPath());
    }

    private IEnumerator DisplayPath()
    {
        yield return new WaitForSeconds(0.1f);
        while (m_DisplayPath)
        {
            yield return StartCoroutine(GoToManipulator());

            yield return new WaitForSeconds(0.5f);

            GoToUR5();
            yield return new WaitUntil(() => m_PlanRobotiq.position == m_Robotiq.position);
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
        if (!m_Trajectories.Any())
            yield break;

        foreach (var trajectory in m_Trajectories)
        {
            foreach (var point in trajectory.joint_trajectory.points)
            {
                if (m_DisplayPath == false)
                    break;

                var jointPositions = point.positions;
                var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                for (var joint = 0; joint < k_UR5NumJoints; joint++)
                {
                    var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                    joint1XDrive.target = result[joint];
                    m_PlanRobJoints[joint].xDrive = joint1XDrive;
                }
                yield return new WaitForSeconds(0.01f);
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    public void ExecuteTrajectory()
    {
        if (m_Trajectories.Any())
        {
            m_DisplayPath = false;
            m_PlanRobMat.color = m_HideColor;
            m_ROSPublisher.PublishExecutePlan(m_Trajectories.First());
            m_Trajectories.Remove(m_Trajectories.First());
        }
    }
}