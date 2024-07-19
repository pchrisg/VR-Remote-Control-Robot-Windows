using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using FeedBackModes;
using System.Collections.Generic;
using System.Collections;

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
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;
    private Transform m_ManipulatorPose = null;
    //private Mode m_Mode = Mode.NONE;
    private ExperimentManager m_ExperimentManager = null;

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_FeedbackJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_FeedGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private GameObject m_FeedPalm = null;

    private Color m_ShowColor = new(1.0f, 1.0f, 1.0f, 0.2f);
    private Color m_HideColor = new(0.0f, 0.0f, 0.0f, 0.0f);

    private Vector3 m_PrevPosition = Vector3.zero;
    private Quaternion m_PrevRotation = Quaternion.identity;

    private bool m_busFree = true;

    public struct RobotLimits
    {
        public float min;
        public float max;
    }

    [Header("Thresholds")]
    [SerializeField] private float[] m_Thresholds = new float[6];

    [Header("Current Target")]
    [SerializeField] private float[] m_Targets = new float[6];
    private readonly float[] m_HintTargets = new float[6];

    [Header("Robot Limits")]
    [SerializeField] private float[] m_ShoulderPan = new float[2];
    [SerializeField] private float[] m_ShoulderLift = new float[2];
    [SerializeField] private float[] m_ElbowJoint = new float[2];
    [SerializeField] private float[] m_Wrist1 = new float[2];
    [SerializeField] private float[] m_Wrist2 = new float[2];
    [SerializeField] private float[] m_Wrist3 = new float[2];

    private readonly RobotLimits[] m_RobotLimits = new RobotLimits[6];

    public bool m_DisplayHint = false;

    private Coroutine m_ActiveCoroutine = null;

    private bool m_isInteracting = false;
    private readonly List<GameObject> m_CollidingParts = new();
    private bool m_isOutOfBounds = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ManipulatorPose = m_Manipulator.transform.Find("Pose");
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_FeedbackJoints = new ArticulationBody[k_UR5NumJoints];
        m_UR5Joints = new ArticulationBody[k_UR5NumJoints];

        m_FeedGripJoints = new ArticulationBody[k_RobotiqNumJoints];
        m_RobotiqJoints = new ArticulationBody[k_RobotiqNumJoints];

        GameObject ur5 = GameObject.FindGameObjectWithTag("robot");
        var linkName = string.Empty;
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_UR5LinkNames[joint];
            m_FeedbackJoints[joint] = gameObject.transform.Find(linkName).GetComponent<ArticulationBody>();
            m_UR5Joints[joint] = ur5.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        GameObject robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        string connectingLink = linkName + "/flange/tool0/palm/";
        m_FeedPalm = gameObject.transform.Find(connectingLink).gameObject;
        for (var i = 0; i < k_RobotiqNumJoints; i += 4)
        {
            linkName = string.Empty;
            for (var j = i; j < i + 4 && j < k_RobotiqNumJoints; j++)
            {
                linkName += JointStateSubscriber.m_RobotiqLinkNames[j];
                m_RobotiqJoints[j] = robotiq.transform.Find(linkName).GetComponent<ArticulationBody>();
                m_FeedGripJoints[j] = m_FeedPalm.transform.Find(linkName).GetComponent<ArticulationBody>();
            }
        }

        m_RobotLimits[0].min = m_ShoulderPan[0];
        m_RobotLimits[0].max = m_ShoulderPan[1];
        m_Thresholds[0] = (m_RobotLimits[0].max - m_RobotLimits[0].min)/14;

        m_RobotLimits[1].min = m_ShoulderLift[0];
        m_RobotLimits[1].max = m_ShoulderLift[1];
        m_Thresholds[1] = (m_RobotLimits[1].max - m_RobotLimits[1].min) / 14;

        m_RobotLimits[2].min = m_ElbowJoint[0];
        m_RobotLimits[2].max = m_ElbowJoint[1];
        m_Thresholds[2] = (m_RobotLimits[2].max - m_RobotLimits[2].min) / 14;

        m_RobotLimits[3].min = m_Wrist1[0];
        m_RobotLimits[3].max = m_Wrist1[1];
        m_Thresholds[3] = (m_RobotLimits[3].max - m_RobotLimits[3].min) / 14;

        m_RobotLimits[4].min = m_Wrist2[0];
        m_RobotLimits[4].max = m_Wrist2[1];
        m_Thresholds[4] = (m_RobotLimits[4].max - m_RobotLimits[4].min) / 14;

        m_RobotLimits[5].min = m_Wrist3[0];
        m_RobotLimits[5].max = m_Wrist3[1];
        m_Thresholds[5] = (m_RobotLimits[5].max - m_RobotLimits[5].min) / 14;
    }

    //private void Start()
    //{
    //    ChangeMode();
    //}

    private void OnDestroy()
    {
        m_FeedbackMat.color = m_HideColor;
    }

    private void Update()
    {
        if (m_isInteracting != m_ManipulationMode.IsInteracting())
        {
            m_isInteracting = m_ManipulationMode.IsInteracting();
            ShowFeedback(m_isInteracting);
        }

        if (!m_isInteracting)
            ResetPositionAndRotation();

        //if (m_Mode != m_ExperimentManager.m_FeedbackMode)
        //    ChangeMode();
    }

    //private void ChangeMode()
    //{
    //    m_Mode = m_ExperimentManager.m_FeedbackMode;

    //    if (m_Mode == Mode.ALWAYS)
    //        m_FeedbackMat.color = m_ShowColor;
    //    else
    //        m_FeedbackMat.color = m_HideColor;

        
    //}

    private void ShowFeedback(bool value)
    {
        if(value && m_ExperimentManager.m_FeedbackMode == Mode.ALWAYS)
            m_FeedbackMat.color = m_ShowColor;
        else
            m_FeedbackMat.color = m_HideColor;

        foreach (var joint in m_FeedbackJoints)
            joint.GetComponent<RobotFeedbackCollision>().ResetColor();
    }

    public void ResetPositionAndRotation()
    {
        m_PrevPosition = m_ManipulatorPose.position;
        m_PrevRotation = m_ManipulatorPose.rotation;

        GoToUR5();
    }

    private void GoToUR5()
    {
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            var joint1XDrive = m_FeedbackJoints[joint].xDrive;
            joint1XDrive.target = m_UR5Joints[joint].xDrive.target;
            m_FeedbackJoints[joint].xDrive = joint1XDrive;
        }

        for (var joint = 0; joint < k_RobotiqNumJoints; joint++)
        {
            var joint1XDrive = m_FeedGripJoints[joint].xDrive;
            joint1XDrive.target = m_RobotiqJoints[joint].xDrive.target;
            m_FeedGripJoints[joint].xDrive = joint1XDrive;
        }
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
        if (trajectory.joint_trajectory.points.Length > 0)
        {
            m_isOutOfBounds = false;
            m_Targets = GetTargets(trajectory);
            GoToManipulator();
        }
        else
        {
            m_isOutOfBounds = true;
            if (m_ExperimentManager.m_FeedbackMode != Mode.NONE)
                NoPlan();
        }

        m_busFree = true;
    }

    private float[] GetTargets(RobotTrajectoryMsg trajectory)
    {
        var point = trajectory.joint_trajectory.points[^1];

        var jointPositions = point.positions;
        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

        return result;
    }

    private void GoToManipulator()
    {
        List<int> jointHints = CheckLimits();

        RobotFeedbackCollision collision;
        bool limitHit = false;
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            collision = m_FeedbackJoints[joint].GetComponent<RobotFeedbackCollision>();
            if (jointHints.Contains(joint))
            {
                limitHit = true;
                collision.ChangeAppearance(2);
            }
            else
            {
                if(!collision.IsColliding())
                {
                    if (limitHit)
                        collision.ChangeAppearance(3);
                    else
                        collision.ChangeAppearance(1);
                }
                

                var joint1XDrive = m_FeedbackJoints[joint].xDrive;
                joint1XDrive.target = m_Targets[joint];
                m_FeedbackJoints[joint].xDrive = joint1XDrive;
            }
        }

        collision = m_FeedPalm.GetComponent<RobotFeedbackCollision>();
        if(!collision.IsColliding())
        {
            if (limitHit)
                collision.ChangeAppearance(3);
            else
                collision.ChangeAppearance(1);
        }

        for (var joint = 0; joint < k_RobotiqNumJoints; joint++)
        {
            collision = m_FeedGripJoints[joint].GetComponent<RobotFeedbackCollision>();
            if (!collision.IsColliding())
            {
                if (limitHit)
                    collision.ChangeAppearance(3);
                else
                    collision.ChangeAppearance(1);
            }
        }
    }

    private List<int> CheckLimits()
    {
        List<int> jointHints = new();
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            if (m_Targets[joint] - m_Thresholds[joint] < m_RobotLimits[joint].min)
            {
                m_HintTargets[joint] = m_Targets[joint] + (m_Thresholds[joint] * 3);
                jointHints.Add(joint);
            }

            else if (m_Targets[joint] + m_Thresholds[joint] > m_RobotLimits[joint].max)
            {
                m_HintTargets[joint] = m_Targets[joint] - (m_Thresholds[joint] * 3);
                jointHints.Add(joint);
            }

            else
                m_HintTargets[joint] = m_Targets[joint];
        }

        // if manipulator behind base
        //if (m_Targets[1] >= -90)
        //{
        //    float total = (m_Targets[1] + 180) * -1;
        //    float sum = m_Targets[1] + m_Targets[2];
        //    if (sum > total)
        //    {
        //        float difference = sum - total;

        //        m_HintTargets[1] = m_Targets[1] - difference / 4;
        //        total = (m_HintTargets[1] + 180) * -1;
        //        sum = m_HintTargets[1] + m_Targets[2];
        //        difference = sum - total;

        //        m_HintTargets[2] = m_Targets[2] - difference;

        //        if (!jointHints.Contains(1))
        //            jointHints.Add(1);
        //        if (!jointHints.Contains(2))
        //            jointHints.Add(2);
        //    }
        //}

        // if shoulder lift or elbow hint
        //if (jointHints.Contains(1) || jointHints.Contains(2))
        //{
        //    float sum = m_HintTargets[1] + m_HintTargets[2];

        //    m_HintTargets[3] = (270 - Mathf.Abs(sum)) * -1;

        //    if (!jointHints.Contains(3))
        //        jointHints.Add(3);
        //}

        if (jointHints.Count > 0 && m_ExperimentManager.m_FeedbackMode != Mode.NONE)
        {
            m_DisplayHint = true;
            m_ActiveCoroutine ??= StartCoroutine(DisplayHintCoroutine(jointHints));
        }
        else
        {
            m_DisplayHint = false;

            if (m_ActiveCoroutine != null)
                StopCoroutine(m_ActiveCoroutine);

            m_ActiveCoroutine = null;
        }

        return jointHints;
    }

    private IEnumerator DisplayHintCoroutine(List<int> jointHints)
    {
        //m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.4f);
        yield return StartCoroutine(HintCoroutine(jointHints, m_HintTargets));

        yield return new WaitForSeconds(1.0f);

        //m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);

        for (var i = 0; i < jointHints.Count; i++)
        {
            int joint = jointHints[i];
            var joint1XDrive = m_FeedbackJoints[joint].xDrive;
            joint1XDrive.target = m_Targets[joint];
            m_FeedbackJoints[joint].xDrive = joint1XDrive;
        }

        yield return new WaitForSeconds(0.5f);
        m_ActiveCoroutine = null;
    }

    private IEnumerator HintCoroutine(List<int> jointHints, float[] targets)
    {
        bool reachedTargets = false;
        while (m_DisplayHint && !reachedTargets)
        {
            reachedTargets = true;
            for (var i = 0; i < jointHints.Count; i++)
            {
                int joint = jointHints[i];
                var joint1XDrive = m_FeedbackJoints[joint].xDrive;

                if (Mathf.Abs(joint1XDrive.target - targets[joint]) > 1.0f)
                {
                    reachedTargets = false;

                    float modifier = 1.0f;
                    if (joint1XDrive.target > targets[joint])
                        modifier *= -1;

                    joint1XDrive.target += modifier;
                    m_FeedbackJoints[joint].xDrive = joint1XDrive;
                }
            }

            yield return null;
        }
    }

    private void NoPlan()
    {
        RobotFeedbackCollision collision = m_FeedPalm.GetComponent<RobotFeedbackCollision>();
        collision.ChangeAppearance(2);

        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            collision = m_FeedbackJoints[joint].GetComponent<RobotFeedbackCollision>();
            collision.ChangeAppearance(2);
        }

        for (var joint = 0; joint < k_RobotiqNumJoints; joint++)
        {
            collision = m_FeedGripJoints[joint].GetComponent<RobotFeedbackCollision>();
            collision.ChangeAppearance(2);
        }
    }

    public void AddCollidingPart(GameObject part)
    {
        m_CollidingParts.Add(part);
    }

    public void RemoveCollidingPart(GameObject part)
    {
        m_CollidingParts.Remove(part);
    }

    public bool IsColliding()
    {
        return m_CollidingParts.Count > 0;
    }

    public bool IsOutOfBounds()
    {
        return m_isOutOfBounds;
    }
}