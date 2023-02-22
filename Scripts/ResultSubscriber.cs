using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;

public class ResultSubscriber : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private GameObject m_UR5 = null;

    [Header("Materials")]
    [SerializeField] private Material m_LightGrey = null;
    [SerializeField] private Material m_InCollisionMaterial = null;

    private ROSConnection m_Ros = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";
    
    private bool isPlanExecuted = true;
    private readonly string NotExecuted = "No motion plan found. No execution attempted.";
    private Renderer[] m_Renderers = null;

    private void Awake()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Renderers = m_UR5.GetComponentsInChildren<Renderer>();
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
        if(message.feedback.state == "IDLE")
        {
            if (message.status.text == NotExecuted && isPlanExecuted)
            {
                foreach (Renderer renderer in m_Renderers)
                    renderer.material = m_InCollisionMaterial;

                isPlanExecuted = false;
            }
        }
        else if (message.feedback.state == "MONITOR")
        {
            if (message.status.text != NotExecuted && !isPlanExecuted)
            {
                foreach (Renderer renderer in m_Renderers)
                    renderer.material = m_LightGrey;

                isPlanExecuted = true;
            }
        }
    }
}
