using System.Collections;
using UnityEngine;

public class Teapot : MonoBehaviour
{
    [SerializeField] private Material m_TeapotMat = null;

    private Coroutine m_Flashing = null;

    private Color m_DefaultColor = new();
    private Color m_FlashColor = new(1.0f, 1.0f, 0.0f, 0.4f);

    private void Awake()
    {
        m_DefaultColor = m_TeapotMat.color;
    }

    private void OnDestroy()
    {
        m_TeapotMat.color = m_DefaultColor;
    }

    public void Flash(bool value)
    {
        if (value)
            m_Flashing ??= StartCoroutine(Flashing());

        else
        {
            if (m_Flashing != null)
                StopCoroutine(m_Flashing);

            m_Flashing = null;
            m_TeapotMat.color = m_DefaultColor;
        }
    }

    private IEnumerator Flashing()
    {
        while (true)
        {
            m_TeapotMat.color = m_FlashColor;
            yield return new WaitForSeconds(1.0f);
            m_TeapotMat.color = m_DefaultColor;

            yield return new WaitForSeconds(1.0f);
        }
    }
}
