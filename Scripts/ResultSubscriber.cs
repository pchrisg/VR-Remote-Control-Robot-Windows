using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;
using System;

public class ResultSubscriber : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip m_CollisionClip = null;
    [SerializeField] private AudioClip m_MotionClip = null;

    private ROSConnection m_Ros = null;

    private PlanningRobot m_PlanningRobot = null;
    private GameObject m_UR5 = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";

    private Manipulator m_Manipulator = null;
    
    private bool isPlanExecuted = true;
    private readonly string NotExecuted = "No motion plan found. No execution attempted.";

    [HideInInspector] public string m_RobotState = "";

    private void Awake()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();

        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot");

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
    }

    private void Start()
    {
        m_Ros.Subscribe<ActionFeedbackUnity>(m_FeedbackTopic, CheckResult);
    }

    private void OnDestroy()
    {
        m_Ros.Unsubscribe(m_FeedbackTopic);
    }

    private void CheckResult(ActionFeedbackUnity message)
    {
        m_RobotState = message.feedback.state;

        AudioSource ur5AudioSource = m_UR5.GetComponent<AudioSource>();

        if (message.feedback.state == "IDLE")
        {
            ur5AudioSource.Stop();
            if (message.status.text == NotExecuted && isPlanExecuted)
            {
                m_Manipulator.Colliding(true);

                ur5AudioSource.clip = m_CollisionClip;
                ur5AudioSource.Play();
                isPlanExecuted = false;
            }
        }
        else if (message.feedback.state == "MONITOR" || m_PlanningRobot.isPlanning)
        {
            if (!ur5AudioSource.isPlaying && !m_PlanningRobot.isPlanning)
            {
                ur5AudioSource.clip = m_MotionClip;
                ur5AudioSource.Play();
            }

            if (message.status.text != NotExecuted && !isPlanExecuted)
            {
                m_Manipulator.Colliding(false);

                isPlanExecuted = true;
            }
        }
        else if(m_PlanningRobot.isPlanning)
        {
            if (!ur5AudioSource.isPlaying)
            {
                ur5AudioSource.clip = m_MotionClip;
                ur5AudioSource.Play();
            }
        }
    }
}