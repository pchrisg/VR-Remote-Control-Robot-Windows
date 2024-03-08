using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;

public class ResultSubscriber : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip m_MotionClip = null;

    private ROSConnection m_Ros = null;

    private GameObject m_UR5 = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";

    private Manipulator m_Manipulator = null;
    
    private bool isPlanExecuted = true;
    private readonly string NotExecuted = "No motion plan found. No execution attempted.";

    private bool m_isMoving = false;
    private float m_ElapsedTime = 0.0f;

    [HideInInspector] public string m_RobotState = "";

    private void Awake()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();
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

    private void Update()
    {
        AudioSource ur5AudioSource = m_UR5.GetComponent<AudioSource>();

        if (m_isMoving)
        {
            if (!ur5AudioSource.isPlaying)
            {
                if (ur5AudioSource.clip != m_MotionClip)
                    ur5AudioSource.clip = m_MotionClip;

                ur5AudioSource.Play();
            }
        }
        else
        {
            m_ElapsedTime += Time.deltaTime;

            if (ur5AudioSource.isPlaying && m_ElapsedTime > 0.2f)
                ur5AudioSource.Stop();
        }
    }

    private void CheckResult(ActionFeedbackUnity message)
    {
        m_RobotState = message.feedback.state;

        if (message.feedback.state == "IDLE")
        {
            m_isMoving = false;
            m_ElapsedTime = 0.0f;

            if (message.status.text == NotExecuted && isPlanExecuted)
            {
                m_Manipulator.Colliding(true);
                isPlanExecuted = false;
            }
        }
        else if (message.feedback.state == "MONITOR")
        {
            m_isMoving = true;

            if (message.status.text != NotExecuted && !isPlanExecuted)
            {
                m_Manipulator.Colliding(false);
                isPlanExecuted = true;
            }
        }
    }
}