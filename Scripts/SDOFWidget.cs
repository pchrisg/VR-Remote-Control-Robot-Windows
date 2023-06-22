using UnityEngine;

public class SDOFWidget : MonoBehaviour
{
    private Manipulator m_Manipulator = null;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);

        if (!value)
            m_Manipulator.ResetPosition();
    }
}
