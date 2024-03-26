using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class InteractableObjectCreator : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;

    private Hand m_Hand = null;
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
                if (other.GetComponent<CollisionHandling>() == null)
                    AddInteractableObject(other);
                else
                    RemoveInteractableObject(other);
            }

            else if (m_Grip.GetState(m_Hand.handType) && other.GetComponent<CollisionHandling>() != null && other.GetComponent<CollisionHandling>().m_isAttachable)
                SetFocusObject(other);
        }
    }

    public void Setup(string indexFinger)
    {
        if(indexFinger == "right")
        {
            m_Hand = Player.instance.rightHand;
            m_OtherHand = Player.instance.leftHand;
        }
        else if (indexFinger == "left")
        {
            m_Hand = Player.instance.leftHand;
            m_OtherHand = Player.instance.rightHand;
        }

        m_Grip.AddOnStateDownListener(GripGrabbed, m_Hand.handType);
        m_Grip.AddOnStateUpListener(GripReleased, m_Hand.handType);
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_isInteracting = true;
            m_ManipulationMode.IsInteracting(m_isInteracting);
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_isInteracting = false;

            if (!m_Grip.GetState(m_OtherHand.handType))
                m_ManipulationMode.IsInteracting(m_isInteracting);
        }
    }

    private void AddInteractableObject(Collider other)
    {
        //bool isAttachable = false;
        //if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        //    isAttachable = true;

        m_InteractableObjects.AddInteractableObject(other);

        //other.AddComponent<CollisionHandling>();
        //other.GetComponent<CollisionHandling>().SetupCollisionHandling(isAttachable);

        //other.AddComponent<InteractableObject>();
        //other.GetComponent<InteractableObject>().AddInteractableObject(isAttachable, other);
    }

    private void RemoveInteractableObject(Collider other)
    {
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR && other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && !other.GetComponent<CollisionHandling>().m_isAttachable)
            return;

        m_InteractableObjects.RemoveInteractableObject(other);

        //Destroy(other.GetComponent<CollisionHandling>());

        //if (other.GetComponent<InteractableObject>() != null)
        //{
        //    other.GetComponent<InteractableObject>().RemoveInteractableObject();
        //    Destroy(other.GetComponent<InteractableObject>());
        //}
    }

    private void SetFocusObject(Collider other)
    {
        m_InteractableObjects.SetFocusObject(other);

        //if (m_InteractableObjects.m_FocusObject == null)
        //{
        //    m_InteractableObjects.SetFocusObject(other.gameObject);
        //    other.GetComponent<CollisionHandling>().SetAsFocusObject(true);
        //}

        //else if (m_InteractableObjects.m_FocusObject == other.gameObject)
        //{
        //    m_InteractableObjects.SetFocusObject(null);
        //    other.GetComponent<CollisionHandling>().SetAsFocusObject(false);
        //}
    }
}