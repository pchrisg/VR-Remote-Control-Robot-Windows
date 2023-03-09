using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDOFWidget : MonoBehaviour
{
    private GameObject m_Manipulator = null;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("EndEffector");
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);

        if (value)
            SetManipulatorAsChild();

        if (!value)
        {
            SetManipulatorAsParent();
            m_Manipulator.GetComponent<Manipulator>().ResetPosition();
        }
    }

    public void SetManipulatorAsChild()
    {
        gameObject.transform.SetParent(null);
        m_Manipulator.transform.SetParent(gameObject.transform);
    }

    public void SetManipulatorAsParent()
    {
        m_Manipulator.transform.SetParent(null);
        gameObject.transform.SetParent(m_Manipulator.transform);
    }
}
