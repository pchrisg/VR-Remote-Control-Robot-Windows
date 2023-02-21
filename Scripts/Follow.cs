using UnityEngine;

public class Follow : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private Transform m_ToFollow;

    private void Awake()
    {
        gameObject.transform.SetParent(m_ToFollow);
    }

    public void ResetPosition()
    {
        gameObject.transform.position = m_ToFollow.transform.position;
        gameObject.transform.rotation = m_ToFollow.transform.rotation;
        gameObject.transform.SetParent(m_ToFollow);
    }
}