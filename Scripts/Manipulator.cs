using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private Transform m_EndEffector;

    private void Awake()
    {
        gameObject.transform.SetParent(m_EndEffector);
    }

    private void Start()
    {
        Invoke("Unhinge", 1.0f);
    }

    private void Unhinge()
    {
        gameObject.transform.SetParent(null);
    }

    public void ResetPosition()
    {
        gameObject.transform.position = m_EndEffector.transform.position;
        gameObject.transform.rotation = m_EndEffector.transform.rotation;
    }
}