using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDOFWidget : MonoBehaviour
{
    [SerializeField] GameObject m_Manipulator;

    public void Show(bool value)
    {
        if(!value)
            SetManipulatorAsParent();
 
        gameObject.SetActive(value);

        if(value)
            SetManipulatorAsChild();
    }

    public void SetManipulatorAsChild()
    {
        gameObject.transform.SetParent(null);
        m_Manipulator.transform.SetParent(gameObject.transform);
    }

    private void SetManipulatorAsParent()
    {
        m_Manipulator.transform.SetParent(null);
        gameObject.transform.SetParent(m_Manipulator.transform);
    }
}
