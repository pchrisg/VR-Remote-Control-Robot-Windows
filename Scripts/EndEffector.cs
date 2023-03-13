using UnityEngine;

public class EndEffector : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private Transform m_Robotiq3FGripper;

    private void Start()
    {
        Invoke("ResetPosition", 1.0f);
    }

    public void ResetPosition()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq3FGripper.transform.position, m_Robotiq3FGripper.transform.rotation);
    }
}