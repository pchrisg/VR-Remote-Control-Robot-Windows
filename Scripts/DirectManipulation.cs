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

    [SerializeField] private PlanningRobot m_PlanningRobot = null;
    [SerializeField] private ROSPublisher m_ROSPublisher = null;

    protected bool attached = false;
    protected float attachTime = 0.0f;
    protected Vector3 attachPosition = Vector3.zero;
    protected Quaternion attachRotation = Quaternion.identity;

    public UnityEvent onPickUp;
    public UnityEvent onDetachFromHand;
    public HandEvent onHeldUpdate;
    private const float m_TimeInterval = 0.5f;
    private float period = 0.0f;

    [HideInInspector] public Interactable interactable;

    protected virtual void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

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

    protected virtual void OnHandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
    }

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

    protected virtual void OnAttachedToHand(Hand hand)
    {
        attached = true;

        onPickUp.Invoke();

        hand.HoverLock(null);

        attachTime = Time.time;
        attachPosition = transform.position;
        attachRotation = transform.rotation;
    }

    protected virtual void OnDetachedFromHand(Hand hand)
    {
        attached = false;

        onDetachFromHand.Invoke();

        hand.HoverUnlock(null);
        if (m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishTrajectoryRequest();
    }

    protected virtual void HandAttachedUpdate(Hand hand)
    {
        if (hand.IsGrabEnding(gameObject))
        {
            hand.DetachObject(gameObject);
        }

        if (onHeldUpdate != null)
            onHeldUpdate.Invoke(hand);

        if (!m_PlanningRobot.isPlanning && period > m_TimeInterval)
        {
            m_ROSPublisher.PublishMoveArm();
            period = 0;
        }
        period += UnityEngine.Time.deltaTime;        
    }
}