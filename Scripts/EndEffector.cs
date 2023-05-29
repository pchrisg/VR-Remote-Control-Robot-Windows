using UnityEngine;

public class EndEffector : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_EndEffectorMat;

    [Header("Scene Object")]
    [SerializeField] private Transform m_Robotiq3FGripper;

    private void Start()
    {
        m_EndEffectorMat.color = new Color(51.0f / 255.0f, 51.0f / 255.0f, 51.0f / 255.0f, 100.0f / 255.0f);

        Invoke("ResetPosition", 1.2f);
    }

    public void ResetPosition()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq3FGripper.transform.position, m_Robotiq3FGripper.transform.rotation);
    }
}