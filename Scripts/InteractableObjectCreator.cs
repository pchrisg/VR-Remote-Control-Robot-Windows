using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using Unity.VisualScripting;

public class InteractableObjectCreator : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;

    public Hand hand = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_Grip.GetState(hand.handType) || !other.isTrigger || !((other is BoxCollider) || (other is CapsuleCollider)))
            return;

        else
        {
            if (m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
            {
                if (other.GetComponent<CollisionHandling>() == null)
                    AddInteractableObject(other);
                else
                    RemoveInteractableObject(other);
            }

            else if (other.GetComponent<CollisionHandling>() != null && other.GetComponent<CollisionHandling>().m_isAttachable)
                SetFocusObject(other);
        }
    }

    private void AddInteractableObject(Collider other)
    {
        bool isAttachable = false;
        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
            isAttachable = true;

        other.AddComponent<CollisionHandling>();
        other.GetComponent<CollisionHandling>().SetupCollisionHandling(isAttachable);

        other.AddComponent<InteractableObject>();
        other.GetComponent<InteractableObject>().AddInteractableObject(isAttachable, other);
    }

    private void RemoveInteractableObject(Collider other)
    {
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR && other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && !other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        Destroy(other.GetComponent<CollisionHandling>());

        if (other.GetComponent<InteractableObject>() != null)
            Destroy(other.GetComponent<InteractableObject>());
    }

    private void SetFocusObject(Collider other)
    {
        if (m_InteractableObjects.m_FocusObject == null)
        {
            m_InteractableObjects.SetFocusObject(other.gameObject);
            other.GetComponent<CollisionHandling>().SetAsFocusObject(true);
        }

        else if (m_InteractableObjects.m_FocusObject == other.gameObject)
        {
            m_InteractableObjects.SetFocusObject(null);
            other.GetComponent<CollisionHandling>().SetAsFocusObject(false);
        }
    }
}