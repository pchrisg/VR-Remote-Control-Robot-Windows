using System.Collections;
using UnityEngine;

public class RobotAssistant : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;

    public struct RobotLimits
    {
        public float[] limits;
    }

    [Header("Current Target")]
    [SerializeField] private float[] m_Targets = new float[6];

    [Header("Robot Limits")]
    [SerializeField] private float[] m_ShoulderPan = new float[2];
    [SerializeField] private float[] m_ShoulderLift = new float[2];
    [SerializeField] private float[] m_ElbowJoint = new float[2];
    [SerializeField] private float[] m_Wrist1 = new float[2];
    [SerializeField] private float[] m_Wrist2 = new float[2];
    [SerializeField] private float[] m_Wrist3 = new float[2];
    private readonly float m_Threshhold = 20.0f;

    private RobotLimits[] m_RobotLimits = new RobotLimits[6];

    private const int k_UR5NumJoints = 6;
    private ArticulationBody[] m_PlanRobJoints = null;
    private ArticulationBody[] m_UR5Joints = null;

    private const int k_RobotiqNumJoints = 11;
    private ArticulationBody[] m_PlanGripJoints = null;
    private ArticulationBody[] m_RobotiqJoints = null;

    private readonly float[] m_HintTargets = new float[6];
    public bool m_DisplayHint = false;
    private Coroutine m_ActiveCoroutine = null;

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

        m_RobotLimits[0].limits = m_ShoulderPan;
        m_RobotLimits[1].limits = m_ShoulderLift;
        m_RobotLimits[2].limits = m_ElbowJoint;
        m_RobotLimits[3].limits = m_Wrist1;
        m_RobotLimits[4].limits = m_Wrist2;
        m_RobotLimits[5].limits = m_Wrist3;
    }

    private void Update()
    {
        GoToUR5();
    }

    private void OnDestroy()
    {
        m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);
    }

    private void GoToUR5()
    {
        int jointHint = CheckLimits();

        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            if (jointHint == -1 || jointHint != joint)
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

    private int CheckLimits()
    {
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
            m_Targets[joint] = m_UR5Joints[joint].xDrive.target;

        int jointHint = -1;
        for (var joint = 0; joint < k_UR5NumJoints; joint++)
        {
            if (m_Targets[joint] - m_Threshhold < m_RobotLimits[joint].limits[0])
            {
                m_HintTargets[joint] = m_Targets[joint] + 30.0f;
                jointHint = joint;
                break;
            }

            else if (m_Targets[joint] + m_Threshhold > m_RobotLimits[joint].limits[1])
            {
                m_HintTargets[joint] = m_Targets[joint] - 30.0f;
                jointHint = joint;
                break;
            }
        }

        if (jointHint != -1)
            DisplayHint(true, jointHint);
        else
            DisplayHint(false);

        return jointHint;
    }

    private void DisplayHint(bool value, int joint = -1)
    {
        if (value)
        {
            m_DisplayHint = true;
            m_ActiveCoroutine ??= StartCoroutine(DisplayHintCoroutine(joint));
        }
        else
        {
            m_DisplayHint = false;
            m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);

            if (m_ActiveCoroutine != null)
                StopCoroutine(m_ActiveCoroutine);

            m_ActiveCoroutine = null;
        }
    }

    private IEnumerator DisplayHintCoroutine(int joint)
    {
        while (m_DisplayHint)
        {
            m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.4f);
            yield return StartCoroutine(GoToTarget(joint, m_HintTargets[joint]));

            m_PlanRobMat.color = new(0.8f, 0.8f, 0.8f, 0.0f);
            var joint1XDrive = m_PlanRobJoints[joint].xDrive;
            joint1XDrive.target = m_Targets[joint];
            m_PlanRobJoints[joint].xDrive = joint1XDrive;

            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator GoToTarget(int joint, float target)
    {
        var joint1XDrive = m_PlanRobJoints[joint].xDrive;
        float modifier = 0.5f;

        if (joint1XDrive.target > target)
            modifier *= -1;

        while (m_DisplayHint && Mathf.Abs(joint1XDrive.target - target) > 1.0f)
        {
            joint1XDrive.target += modifier;
            m_PlanRobJoints[joint].xDrive = joint1XDrive;

            yield return null;
        }
    }
}