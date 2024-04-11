using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;
using RosMessageTypes.Std;

public class ResultSubscriber : MonoBehaviour
{
    private ROSConnection m_Ros = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";
    private readonly string m_PlanSuccessTopic = "chris_plan_success";

    private Manipulator m_Manipulator = null;
    public bool m_isPlanExecuted = true;

    [HideInInspector] public string m_RobotState = "";

    private void Awake()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
    }

    private void Start()
    {
        m_Ros.Subscribe<ActionFeedbackUnity>(m_FeedbackTopic, CheckResult);
        m_Ros.Subscribe<BoolMsg>(m_PlanSuccessTopic, PlanResult);
    }

    private void OnDestroy()
    {
        m_Ros.Unsubscribe(m_FeedbackTopic);
        m_Ros.Unsubscribe(m_PlanSuccessTopic);
    }

    private void CheckResult(ActionFeedbackUnity message)
    {
        m_RobotState = message.feedback.state;
    }

    private void PlanResult(BoolMsg message)
    {
        if (!message.data && m_isPlanExecuted)
        {
            m_Manipulator.Colliding(true);
            m_isPlanExecuted = false;
        }

        if (message.data && !m_isPlanExecuted)
        {
            m_Manipulator.Colliding(false);
            m_isPlanExecuted = true;
        }
    }
}