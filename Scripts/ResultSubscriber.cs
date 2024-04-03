using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;
using Unity.VisualScripting;

public class ResultSubscriber : MonoBehaviour
{
    private ROSConnection m_Ros = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";

    private Manipulator m_Manipulator = null;
    public bool isPlanExecuted = true;

    private readonly string NotExecuted = "No motion plan found. No execution attempted.";

    [HideInInspector] public string m_RobotState = "";

    private void Awake()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();

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

        if (message.feedback.state == "IDLE")
        {
            if (message.status.text == NotExecuted && isPlanExecuted)
            {
                m_Manipulator.Colliding(true);
                isPlanExecuted = false;
            }
            else if (!isPlanExecuted)
            {
                m_Manipulator.Colliding(false);
                isPlanExecuted = true;
            }
        }
    }
}