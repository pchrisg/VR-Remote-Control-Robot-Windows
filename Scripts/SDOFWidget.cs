using System.Collections;
using System.Collections.Generic;
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

        if (value)
            SetEndEffectorAsChild();

        if (!value)
        {
            SetEndEffectorAsParent();
            m_EndEffector.GetComponent<EndEffector>().ResetPosition();
        }
    }

    public void SetEndEffectorAsChild()
    {
        gameObject.transform.SetParent(null);
        m_EndEffector.transform.SetParent(gameObject.transform);
    }

    public void SetEndEffectorAsParent()
    {
        m_EndEffector.transform.SetParent(null);
        gameObject.transform.SetParent(m_EndEffector.transform);
    }
}
