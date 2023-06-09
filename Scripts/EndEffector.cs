using UnityEngine;

public class EndEffector : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private Transform m_Robotiq3FGripper;

    [Header("Material")]
    [SerializeField] private Material m_EndEffectorMat;
    [SerializeField] private Material m_CollidingMat;
    [SerializeField] private Material m_YAxisMat;

    Renderer[] m_Renderers = null;

    private bool isColliding = false;

    private void Awake()
    {
        m_Renderers = gameObject.transform.Find("palm").GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        Invoke("ResetPosition", 1.2f);
    }

    public void ResetPosition()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq3FGripper.transform.position, m_Robotiq3FGripper.transform.rotation);
    }

    public void ResetColour()
    {
        if(!isColliding)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_EndEffectorMat;
        }
    }

    public void AlignedWithYAxis()
    {
        if (!isColliding)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_YAxisMat;
        }
    }

    public void Colliding()
    {
        isColliding = true;
        foreach (Renderer renderer in m_Renderers)
            renderer.material = m_CollidingMat;
    }

    public void NotColliding()
    {
        isColliding = false;
        ResetColour();
    }
}