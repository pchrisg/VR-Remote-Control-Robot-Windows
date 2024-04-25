/**
 * Added to each planningrobot joint
 */

using UnityEngine;

public class PlanningCollision : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;
    [SerializeField] private Material m_CollidingMat = null;

    private PlanningRobot m_PlanningRobot = null;

    private Renderer[] m_Renderers = null;

    private void Awake()
    {
        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("RobotAssistant").GetComponent<PlanningRobot>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_PlanningRobot.isPlanning)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_CollidingMat;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_PlanningRobot.isPlanning)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_PlanRobMat;
        }
    }
}