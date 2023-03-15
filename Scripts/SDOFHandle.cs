using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]

public class SDOFHandle : MonoBehaviour
{
    private SDOFManipulation m_SDOFManipulation = null;

    private void Awake()
    {
        m_SDOFManipulation = gameObject.transform.parent.parent.GetComponent<SDOFManipulation>();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if(!m_SDOFManipulation.isInteracting)
        {
            m_SDOFManipulation.m_InteractingHand = hand;
            m_SDOFManipulation.m_Interactable = gameObject.GetComponent<Interactable>();
        }
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (!m_SDOFManipulation.isInteracting)
        {
            m_SDOFManipulation.m_InteractingHand = hand;
            m_SDOFManipulation.m_Interactable = gameObject.GetComponent<Interactable>();
        }
    }
}