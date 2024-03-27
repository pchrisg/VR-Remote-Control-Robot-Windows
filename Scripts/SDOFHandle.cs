using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]

public class SDOFHandle : MonoBehaviour
{
    private SDOFManipulation m_SDOFManipulation = null;

    private Coroutine m_Flashing = null;
    private Material m_HandleMat = null;
    private Color m_DefaultColor = new();
    private Color m_FlashingColor = new(1.0f, 1.0f, 0.0f, 0.4f);

    private void Awake()
    {
        m_SDOFManipulation = gameObject.transform.parent.parent.GetComponent<SDOFManipulation>();

        m_HandleMat = gameObject.GetComponent<Renderer>().material;
        m_DefaultColor = m_HandleMat.color;
    }

    private void OnHandHoverBegin(Hand hand)
    {
        m_SDOFManipulation.SetHoveringHand(hand, gameObject.transform);
    }

    private void HandHoverUpdate(Hand hand)
    {
        m_SDOFManipulation.SetHoveringHand(hand, gameObject.transform);
    }

    private void OnHandHoverEnd(Hand hand)
    {
        m_SDOFManipulation.SetHoveringHand(null, null);
    }

    public void Flash(bool value)
    {
        if (value)
            m_Flashing ??= StartCoroutine(Flashing());

        else
        {
            if (m_Flashing != null)
                StopCoroutine(m_Flashing);
            m_HandleMat.color = m_DefaultColor;
        }
    }

    private IEnumerator Flashing()
    {
        while (true)
        {
            m_HandleMat.color = m_FlashingColor;
            yield return new WaitForSeconds(1.0f);
            m_HandleMat.color = m_DefaultColor;

            yield return new WaitForSeconds(1.0f);
        }
    }
}