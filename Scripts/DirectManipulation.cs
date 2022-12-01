using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;
[RequireComponent(typeof(Interactable))]

public class DirectManipulation : MonoBehaviour
{
    [EnumFlags]
    [Tooltip("The flags used to attach this object to the hand.")]
    public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand;

    [Tooltip("When detaching the object, should it return to its original parent?")]
    public bool restoreOriginalParent = false;

    [SerializeField] private Planner m_Planner;
    [SerializeField] private ManipulatorPublisher m_ManipulationPublisher;

    protected bool attached = false;
    protected float attachTime;
    protected Vector3 attachPosition;
    protected Quaternion attachRotation;

    public UnityEvent onPickUp;
    public UnityEvent onDetachFromHand;
    public HandEvent onHeldUpdate;

    [HideInInspector] public Interactable interactable;

    //-------------------------------------------------
    protected virtual void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    //-------------------------------------------------
    protected virtual void OnHandHoverBegin(Hand hand)
    {
        bool showHint = false;

        if (!attached)
        {
            GrabTypes bestGrabType = hand.GetBestGrabbingType();

            if (bestGrabType != GrabTypes.None)
            {
                hand.AttachObject(gameObject, bestGrabType, attachmentFlags);
                showHint = false;
            }
        }

        if (showHint)
        {
            hand.ShowGrabHint();
        }
    }

    //-------------------------------------------------
    protected virtual void OnHandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
    }

    //-------------------------------------------------
    protected virtual void HandHoverUpdate(Hand hand)
    {
        GrabTypes startingGrabType = hand.GetGrabStarting();

        if (startingGrabType != GrabTypes.None)
        {
            if (!restoreOriginalParent)
                gameObject.transform.parent = null;

            hand.AttachObject(gameObject, startingGrabType, attachmentFlags);
            hand.HideGrabHint();
        }
    }

    //-------------------------------------------------
    protected virtual void OnAttachedToHand(Hand hand)
    {
        attached = true;

        onPickUp.Invoke();

        hand.HoverLock(null);

        attachTime = Time.time;
        attachPosition = transform.position;
        attachRotation = transform.rotation;
    }

    //-------------------------------------------------
    protected virtual void OnDetachedFromHand(Hand hand)
    {
        attached = false;

        onDetachFromHand.Invoke();

        hand.HoverUnlock(null);
        if (m_Planner.isPlanning)
            m_ManipulationPublisher.PublishTrajectoryRequest();
    }

    //-------------------------------------------------
    protected virtual void HandAttachedUpdate(Hand hand)
    {
        if (hand.IsGrabEnding(this.gameObject))
        {
            hand.DetachObject(gameObject);
        }

        if (onHeldUpdate != null)
            onHeldUpdate.Invoke(hand);

        if (!m_Planner.isPlanning)
            m_ManipulationPublisher.PublishMoveArm();
    }
}