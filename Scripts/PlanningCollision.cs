/**
 * Added to each planningrobot joint
 */

using UnityEngine;

public class PlanningCollision : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_PlanRobMat = null;
    [SerializeField] private Material m_CollidingMat = null;

    private Renderer[] m_Renderers = null;

    private void Awake()
    {
        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent != null && other.transform.parent.tag != "Moveable")
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_CollidingMat;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.parent != null && other.transform.parent.tag != "Moveable")
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_PlanRobMat;
        }
    }
}