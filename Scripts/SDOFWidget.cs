using UnityEngine;

public class SDOFWidget : MonoBehaviour
{
    private GameObject m_EndEffector = null;

    private void Awake()
    {
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector");
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);

        if (!value)
            m_EndEffector.GetComponent<EndEffector>().ResetPosition();
    }
}
