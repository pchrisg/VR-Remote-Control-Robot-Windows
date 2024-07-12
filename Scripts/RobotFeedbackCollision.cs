/**
 * Added to each joint of the Robot Feedback
 */

using UnityEngine;
using FeedBackModes;

public class RobotFeedbackCollision : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_FeebackMat = null;
    [SerializeField] private Material m_CollidingMat = null;
    private ExperimentManager m_ExperimentManager = null;

    private Renderer[] m_Renderers = null;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_ExperimentManager.m_FeedbackMode != Mode.NONE)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_CollidingMat;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_ExperimentManager.m_FeedbackMode != Mode.NONE)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_FeebackMat;
        }
    }

    public void ResetColor()
    {
        foreach (Renderer renderer in m_Renderers)
            renderer.material = m_FeebackMat;
    }
}