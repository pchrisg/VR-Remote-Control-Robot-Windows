using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDOFWidget : MonoBehaviour
{
    [SerializeField] GameObject m_Manipulator = null;

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
