using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [SerializeField] private Transform m_EndEffector;

    private void Awake()
    {
        gameObject.transform.SetParent(m_EndEffector);
    }

    public void ResetPosition()
    {
        gameObject.transform.position = m_EndEffector.transform.position;
        gameObject.transform.rotation = m_EndEffector.transform.rotation;
        gameObject.transform.SetParent(m_EndEffector);
    }
}