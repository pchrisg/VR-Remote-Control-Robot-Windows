using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]

public class SDOFHandle : MonoBehaviour
{
    private SDOFManipulation m_SDOFManipulation = null;

    private Coroutine m_ActiveCoroutine = null;

    private Material m_HandleMat = null;
    private Color m_OriginalColor = new();
    private Color m_FlashingColor = new(1.0f, 1.0f, 0.0f, 0.4f);

    private void Awake()
    {
        m_SDOFManipulation = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();

        m_HandleMat = gameObject.GetComponent<Renderer>().material;
        m_OriginalColor = m_HandleMat.color;
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!m_SDOFManipulation.IsInteracting())
            m_SDOFManipulation.SetInteractingHand(hand, gameObject.transform);
    }

    public void Flash(bool value)
    {
        if (value)
            m_ActiveCoroutine ??= StartCoroutine(FlashCoroutine());

        else
        {
            if (m_ActiveCoroutine != null)
                StopCoroutine(m_ActiveCoroutine);
            m_HandleMat.color = m_OriginalColor;
        }
    }

    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            m_HandleMat.color = m_FlashingColor;
            yield return new WaitForSeconds(1.0f);

            m_HandleMat.color = m_OriginalColor;
            yield return new WaitForSeconds(1.0f);
        }
    }
}