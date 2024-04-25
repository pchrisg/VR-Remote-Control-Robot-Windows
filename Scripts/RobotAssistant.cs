using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAssistant : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    public struct RobotLimits
    {
        public float min;
        public float max;
    }

    [Header("Thresholds")]
    [SerializeField] private float[] m_Thresholds = new float[6];

    [Header("Current Target")]
    [SerializeField] private float[] m_Targets = new float[6];

    [Header("Robot Limits")]
    [SerializeField] private float[] m_ShoulderPan = new float[2];
    [SerializeField] private float[] m_ShoulderLift = new float[2];
    [SerializeField] private float[] m_ElbowJoint = new float[2];
    [SerializeField] private float[] m_Wrist1 = new float[2];
    [SerializeField] private float[] m_Wrist2 = new float[2];
    [SerializeField] private float[] m_Wrist3 = new float[2];

    private readonly RobotLimits[] m_RobotLimits = new RobotLimits[6];

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private readonly float[] m_HintTargets = new float[6];
    public bool m_DisplayHint = false;
    private Coroutine m_ActiveCoroutine = null;

    private bool m_isDisplayingMovement = false;

    private void Awake()
    {
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

        m_RobotLimits[0].min = m_ShoulderPan[0];
        m_RobotLimits[0].max = m_ShoulderPan[1];
        m_RobotLimits[1].min = m_ShoulderLift[0];
        m_RobotLimits[1].max = m_ShoulderLift[1];
        m_RobotLimits[2].min = m_ElbowJoint[0];
        m_RobotLimits[2].max = m_ElbowJoint[1];
        m_RobotLimits[3].min = m_Wrist1[0];
        m_RobotLimits[3].max = m_Wrist1[1];
        m_RobotLimits[4].min = m_Wrist2[0];
        m_RobotLimits[4].max = m_Wrist2[1];
        m_RobotLimits[5].min = m_Wrist3[0];
        m_RobotLimits[5].max = m_Wrist3[1];
    }

    private void Update()
    {
        if(!m_isDisplayingMovement)
            GoToUR5();
    }

    private void OnDestroy()
    {
        m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);
    }

    private void GoToUR5()
    {
        List<int> jointHints = CheckLimits();

        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            if (!jointHints.Contains(joint))
            {
                var joint1XDrive = m_PlanRobJoints[joint].xDrive;
                joint1XDrive.target = m_Targets[joint];
                m_PlanRobJoints[joint].xDrive = joint1XDrive;
            }
        }

        for (var joint = 0; joint < k_RobotiqNumJoints; joint++)
        {
            var joint1XDrive = m_PlanGripJoints[joint].xDrive;
            joint1XDrive.target = m_RobotiqJoints[joint].xDrive.target;
            m_PlanGripJoints[joint].xDrive = joint1XDrive;
        }
    }

    private List<int> CheckLimits()
    {
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
            m_Targets[joint] = m_UR5Joints[joint].xDrive.target;

        List<int> jointHints = new();
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            if (m_Targets[joint] - m_Thresholds[joint] < m_RobotLimits[joint].min)
            {
                m_HintTargets[joint] = m_Targets[joint] + (m_Thresholds[joint] * 6);
                jointHints.Add(joint);
            }

            else if (m_Targets[joint] + m_Thresholds[joint] > m_RobotLimits[joint].max)
            {
                m_HintTargets[joint] = m_Targets[joint] - (m_Thresholds[joint] * 6);
                jointHints.Add(joint);
            }

            else
                m_HintTargets[joint] = m_Targets[joint];
        }

        // if manipulator behind base
        if (m_Targets[1] >= -90)
        {
            float total = (m_Targets[1] + 180) * -1;
            float sum = m_Targets[1] + m_Targets[2];
            if (sum > total)
            {
                float difference = sum - total;

                m_HintTargets[1] = m_Targets[1] - difference / 4;
                total = (m_HintTargets[1] + 180) * -1;
                sum = m_HintTargets[1] + m_Targets[2];
                difference = sum - total;

                m_HintTargets[2] = m_Targets[2] - difference;

                if (!jointHints.Contains(1))
                    jointHints.Add(1);
                if (!jointHints.Contains(2))
                    jointHints.Add(2);
            }
        }

        // if shoulder lift or elbow hint
        if (jointHints.Contains(1) || jointHints.Contains(2))
        {
            float sum = m_HintTargets[1] + m_HintTargets[2];

            m_HintTargets[3] = (270 - Mathf.Abs(sum)) * -1;

            if (!jointHints.Contains(3))
                jointHints.Add(3);
        }

        DisplayHint(jointHints);

        return jointHints;
    }

    private void DisplayHint(List<int> jointHints)
    {
        if (jointHints.Count > 0)
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

            m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);
        }
    }

    private IEnumerator DisplayHintCoroutine(List<int> jointHints)
    {
        m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.4f);
        yield return StartCoroutine(GoToTargetCoroutine(jointHints, m_HintTargets));

        yield return new WaitForSeconds(1.0f);

        m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);

        for (var i = 0; i < jointHints.Count; i++)
        {
            int joint = jointHints[i];
            var joint1XDrive = m_PlanRobJoints[joint].xDrive;
            joint1XDrive.target = m_Targets[joint];
            m_PlanRobJoints[joint].xDrive = joint1XDrive;
        }
        
        yield return new WaitForSeconds(0.5f);
        m_ActiveCoroutine = null;
    }

    private IEnumerator GoToTargetCoroutine(List<int> jointHints, float[] targets)
    {
        bool reachedTargets = false;
        while (m_DisplayHint && !reachedTargets)
        {
            reachedTargets = true;
            for (var i = 0; i < jointHints.Count; i++)
            {
                int joint = jointHints[i];
                var joint1XDrive = m_PlanRobJoints[joint].xDrive;

                if (Mathf.Abs(joint1XDrive.target - targets[joint]) > 1.0f)
                {
                    reachedTargets = false;

                    float modifier = 1.0f;
                    if (joint1XDrive.target > targets[joint])
                        modifier *= -1;

                    joint1XDrive.target += modifier;
                    m_PlanRobJoints[joint].xDrive = joint1XDrive;
                }
            }

            yield return null;
        }
    }

    public IEnumerator DisplayJointMovement(bool value, int joint, int modifier = 1)
    {
        m_isDisplayingMovement = value;

        if (joint != -1)
        {
            if (m_isDisplayingMovement)
            {
                m_UR5Joints[joint].transform.GetComponent<EmergencyStop>().ChangeAppearance(3);
                m_HintTargets[joint] = m_Targets[joint] + ((m_Thresholds[joint] * 6) * modifier);
                DisplayHint(new List<int> { joint });
            }
            else
            {
                m_UR5Joints[joint].transform.GetComponent<EmergencyStop>().ChangeAppearance(1);
                m_HintTargets[joint] = m_Targets[joint];
                DisplayHint(new List<int> { });
            }
        }

        yield return new WaitUntil(() => m_ActiveCoroutine == null);
    }
}