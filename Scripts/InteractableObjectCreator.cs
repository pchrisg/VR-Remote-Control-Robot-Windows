using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class InteractableObjectCreator : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;

    private Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;

    private bool m_isInteracting = false;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && ((other is BoxCollider) || (other is CapsuleCollider)))
        {
            if (m_isInteracting)
            {
                if (m_ManipulationMode.mode != Mode.CONSTRAINEDDIRECT)
                {
                    if (other.GetComponent<CollisionHandling>() == null)
                        AddInteractableObject(other);
                    else
                        RemoveInteractableObject(other);
                }
                else if(!m_ManipulationMode.IsInteracting() && other.GetComponent<CollisionHandling>() != null && other.GetComponent<CollisionHandling>().m_isAttachable)
                    m_InteractableObjects.SetFocusObject(other);
            }
        }
    }

    public void Setup(string indexFinger)
    {
        if(indexFinger == "right")
        {
            m_InteractingHand = Player.instance.rightHand;
            m_OtherHand = Player.instance.leftHand;
        }
        else if (indexFinger == "left")
        {
            m_InteractingHand = Player.instance.leftHand;
            m_OtherHand = Player.instance.rightHand;
        }

        m_Grip.AddOnStateDownListener(GripGrabbed, m_InteractingHand.handType);
        m_Grip.AddOnStateUpListener(GripReleased, m_InteractingHand.handType);
    }

    public void ShowHints()
    {
        if (!m_Grip.GetState(m_InteractingHand.handType))
        {
            if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
                ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Collision Object", false);
            else
                ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Attachable Object", false);
        }
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT || m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_isInteracting = true;

            if(m_ManipulationMode.mode != Mode.CONSTRAINEDDIRECT)
            {
                m_ManipulationMode.IsInteracting(m_isInteracting);

                if (m_ManipulationMode.m_ShowHints)
                    ControllerButtonHints.HideTextHint(m_InteractingHand, m_Grip);
            }
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT || m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_isInteracting = false;

            if (m_ManipulationMode.mode != Mode.CONSTRAINEDDIRECT)
            {
                if (!m_Grip.GetState(m_OtherHand.handType))
                    m_ManipulationMode.IsInteracting(m_isInteracting);

                if (m_ManipulationMode.m_ShowHints)
                {
                    if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
                        ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Collision Object", false);
                    else
                        ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Attachable Object", false);
                }
            }
        }
    }

    private void AddInteractableObject(Collider other)
    {
        m_InteractableObjects.AddInteractableObject(other);
    }

    private void RemoveInteractableObject(Collider other)
    {
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR && other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && !other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        m_InteractableObjects.RemoveInteractableObject(other);
    }
}