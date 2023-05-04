using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;
using System;

public class ResultSubscriber : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_GreyMat = null;
    [SerializeField] private Material m_LightGreyMat = null;
    [SerializeField] private Material m_CollisionMat = null;

    [Header("Sounds")]
    [SerializeField] private AudioClip m_Collision = null;
    [SerializeField] private AudioClip m_Motion = null;


    private PlanningRobot m_PlanningRobot = null;
    private GameObject m_UR5 = null;
    private GameObject m_Robotiq = null;
    private ROSConnection m_Ros = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";
    
    private bool isPlanExecuted = true;
    private readonly string NotExecuted = "No motion plan found. No execution attempted.";
    private Renderer[] m_UR5Renderers = null;
    private Renderer[] m_GripperRenderers = null;

    private void Awake()
    {
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_UR5Renderers = m_UR5.GetComponentsInChildren<Renderer>();
        m_GripperRenderers = m_Robotiq.GetComponentsInChildren<Renderer>();
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
        AudioSource ur5AudioSource = m_UR5.GetComponent<AudioSource>();

        if (message.feedback.state == "IDLE")
        {
            ur5AudioSource.Stop();
            if (message.status.text == NotExecuted && isPlanExecuted)
            {
                foreach (Renderer renderer in m_UR5Renderers)
                    renderer.material = m_CollisionMat;

                ur5AudioSource.clip = m_Collision;
                ur5AudioSource.Play();
                isPlanExecuted = false;
            }
        }
        else if (message.feedback.state == "MONITOR" || m_PlanningRobot.isPlanning)
        {
            if (!ur5AudioSource.isPlaying && !m_PlanningRobot.isPlanning)
            {
                ur5AudioSource.clip = m_Motion;
                ur5AudioSource.Play();
            }

            if (message.status.text != NotExecuted && !isPlanExecuted)
            {
                foreach (Renderer renderer in m_UR5Renderers)
                    renderer.material = m_LightGreyMat;

                foreach (Renderer renderer in m_GripperRenderers)
                    renderer.material = m_GreyMat;

                isPlanExecuted = true;
            }
        }
        else if(m_PlanningRobot.isPlanning)
        {
            if (!ur5AudioSource.isPlaying)
            {
                ur5AudioSource.clip = m_Motion;
                ur5AudioSource.Play();
            }
        }
    }
}