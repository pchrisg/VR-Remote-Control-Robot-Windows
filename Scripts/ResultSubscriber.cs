using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionFeedbackUnity = RosMessageTypes.Moveit.MoveGroupActionFeedbackMsg;

public class ResultSubscriber : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_GreyMat = null;
    [SerializeField] private Material m_LightGreyMat = null;
    [SerializeField] private Material m_CollisionMat = null;

    private PlanningRobot m_PlanningRobot = null;
    private GameObject m_UR5 = null;
    private GameObject m_Gripper = null;
    private ROSConnection m_Ros = null;
    private readonly string m_FeedbackTopic = "/move_group/feedback";
    
    private bool isPlanExecuted = true;
    private readonly string NotExecuted = "No motion plan found. No execution attempted.";
    private Renderer[] m_UR5Renderers = null;
    private Renderer[] m_GripperRenderers = null;

    private void Awake()
    {
        string linkname = string.Empty;
        for(var i = 0; i < JointStateSubscriber.m_UR5LinkNames.Length; i++)
        {
            linkname += JointStateSubscriber.m_UR5LinkNames[i];
        }
        linkname += "/flange/Gripper";

        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Gripper = m_UR5.transform.Find(linkname).gameObject;
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_UR5Renderers = m_UR5.GetComponentsInChildren<Renderer>();
        m_GripperRenderers = m_Gripper.GetComponentsInChildren<Renderer>();
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
                foreach (Renderer renderer in m_UR5Renderers)
                    renderer.material = m_CollisionMat;

                m_UR5.GetComponent<AudioSource>().Play();
                isPlanExecuted = false;
            }
        }
        else if (message.feedback.state == "MONITOR" || m_PlanningRobot.isPlanning)
        {
            if (message.status.text != NotExecuted && !isPlanExecuted)
            {
                foreach (Renderer renderer in m_UR5Renderers)
                    renderer.material = m_LightGreyMat;

                foreach (Renderer renderer in m_GripperRenderers)
                    renderer.material = m_GreyMat;

                isPlanExecuted = true;
            }
        }
    }
}