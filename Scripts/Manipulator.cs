using UnityEngine;
using RosMessageTypes.Robotiq3fGripperArticulated;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_EndEffectorMat;
    [SerializeField] private Material m_CollidingMat;
    [SerializeField] private Material m_YAxisMat;

    private Transform m_Robotiq;

    Renderer[] m_Renderers = null;

    private bool isColliding = false;

    private void Awake()
    {
        m_Renderers = gameObject.transform.Find("palm").GetComponentsInChildren<Renderer>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;
    }

    private void Start()
    {
        Invoke("ResetPosition", 1.2f);
    }

    public void ResetPosition()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
    }

    public void ResetColour()
    {
        if (!isColliding)
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

    public void Collide()
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