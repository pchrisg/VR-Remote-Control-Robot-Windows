using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using UnityEngine.UIElements;

[RequireComponent(typeof(Interactable))]

public class RailManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;
    private CollisionObjects m_CollisionObjects = null;
    private Rails m_Rails = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Forward = null;
    private SteamVR_Action_Boolean m_Reverse = null;

    private int m_ActiveRail = 0;
    private bool isInteracting = false;

    private Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;

    private Vector3 m_InitPos = Vector3.zero;

    private const float m_TimeInterval = 0.5f;
    private float period = 0.0f;

    public readonly float m_Speed = 0.005f;
    private readonly float m_ScalingFactor = 0.25f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Rails = GameObject.FindGameObjectWithTag("Rails").GetComponent<Rails>();

        m_Interactable = GetComponent<Interactable>();
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Forward = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressEast");
        m_Reverse = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressWest");

        m_Forward.onStateDown += ForwardPressed;
        m_Reverse.onStateDown += ReversePressed;
    }

    private void OnDestroy()
    {
        m_Forward.onStateDown -= ForwardPressed;
        m_Reverse.onStateDown -= ReversePressed;
    }

    private void Update()
    {
        if(m_ManipulationMode.mode == Mode.RAIL)
        {
            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                Vector3 position = gameObject.transform.position;
                Quaternion rotation = gameObject.transform.rotation;

                if (m_CollisionObjects.m_FocusObject != null && !m_CollisionObjects.m_FocusObject.GetComponent<AttachableObject>().m_isAttached)
                    rotation = LookAtFocusObject();

                if (m_InteractingHand != null)
                {
                    if (m_OtherHand == null || m_Trigger.GetStateUp(m_InteractingHand.handType) || m_Trigger.GetStateUp(m_OtherHand.handType))
                        Released();

                    else
                    {
                        if (m_Trigger.GetStateDown(m_OtherHand.handType))
                            m_InitPos = gameObject.transform.position;

                        if (m_Trigger.GetState(m_InteractingHand.handType))
                            position = FollowRail();
                    }
                }

                else
                {
                    SteamVR_Input_Sources leftHand = Player.instance.leftHand.handType;
                    if (m_Forward.GetState(leftHand))
                        position = Forward();

                    else if (m_Reverse.GetState(leftHand))
                        position = Reverse();

                    else
                        Released();
                }

                gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);

                if (!m_PlanningRobot.isPlanning)
                    m_ROSPublisher.PublishMoveArm();
            }
        }
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = hand;
    }

    private void TriggerGrabbed()
    {
        if (m_InteractingHand.IsStillHovering(m_Interactable) && m_Trigger.GetState(m_InteractingHand.handType))
        {
            m_InitPos = gameObject.transform.position;
            isInteracting = true;

            if (m_InteractingHand != Player.instance.rightHand)
                m_OtherHand = Player.instance.rightHand;
            else
                m_OtherHand = Player.instance.leftHand;
        }
    }

    private void ForwardPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.RAIL)
            isInteracting = true;
    }

    private void ReversePressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.RAIL)
            isInteracting = true;
    }

    private Vector3 FollowRail()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        Vector3 connectingVectorToStart = m_InteractingHand.objectAttachmentPoint.position - m_Rails.rails[m_ActiveRail].start;
        Vector3 connectingVectorToEnd = m_InteractingHand.objectAttachmentPoint.position - m_Rails.rails[m_ActiveRail].end;
        bool startVectorBigger = connectingVectorToStart.magnitude > connectingVectorToEnd.magnitude ? true : false;

        Vector3 connectingVector = startVectorBigger ? connectingVectorToStart : connectingVectorToEnd;
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, rail);

        if (m_Trigger.GetState(m_OtherHand.handType))
        {
            Vector3 scaledVector = m_InteractingHand.objectAttachmentPoint.position - m_InitPos;
            Vector3 scaledPos = m_InitPos + Vector3.Project(scaledVector, rail) * m_ScalingFactor;


            projectedConnectingVector = startVectorBigger ?
                scaledPos - m_Rails.rails[m_ActiveRail].start :
                scaledPos - m_Rails.rails[m_ActiveRail].end;
        }

        Vector3 position = Vector3.zero;
        if (projectedConnectingVector.magnitude < rail.magnitude)
            position = startVectorBigger ?
                m_Rails.rails[m_ActiveRail].start + projectedConnectingVector :
                m_Rails.rails[m_ActiveRail].end + projectedConnectingVector;
        else
        {
            if(startVectorBigger)
            {
                m_InitPos = m_Rails.rails[m_ActiveRail].end;
                position = m_Rails.rails[m_ActiveRail].end;

                if(period > m_TimeInterval)
                {
                    if (m_ActiveRail < m_Rails.rails.Length - 1)
                        m_ActiveRail++;

                    else if (m_Rails.rails[0].start == m_Rails.rails[^1].end)
                        m_ActiveRail = 0;

                    period = 0;
                }
                period += UnityEngine.Time.deltaTime;
            }
            else
            {
                m_InitPos = m_Rails.rails[m_ActiveRail].start;
                position = m_Rails.rails[m_ActiveRail].start;

                if (period > m_TimeInterval)
                {
                    if (m_ActiveRail > 0)
                        m_ActiveRail--;

                    else if (m_Rails.rails[0].start == m_Rails.rails[^1].end)
                        m_ActiveRail = m_Rails.rails.Length - 1;

                    period = 0;
                }
                period += UnityEngine.Time.deltaTime;
            }
        }

        return position;
    }

    private Vector3 Forward()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        float scaling = m_Trigger.GetState(Player.instance.rightHand.handType) ? m_ScalingFactor : 1.0f;

        Vector3 position = gameObject.transform.position += rail.normalized * m_Speed * scaling;

        Vector3 connectingVector = position - m_Rails.rails[m_ActiveRail].start;
        if(connectingVector.magnitude > rail.magnitude)
        {
            position = m_Rails.rails[m_ActiveRail].end;

            if (m_ActiveRail < m_Rails.rails.Length - 1 && period > m_TimeInterval)
            {
                m_ActiveRail++;
                period = 0;
            }
            else if (m_ActiveRail == m_Rails.rails.Length - 1)
            {
                if (m_Rails.rails[0].start == m_Rails.rails[^1].end && period > m_TimeInterval)
                {
                    m_ActiveRail = 0;
                    period = 0;
                }
            }
            period += UnityEngine.Time.deltaTime;
        }

        return position;
    }

    private Vector3 Reverse()
    {
        Vector3 rail = m_Rails.rails[m_ActiveRail].end - m_Rails.rails[m_ActiveRail].start;
        float scaling = m_Trigger.GetState(Player.instance.rightHand.handType) ? m_ScalingFactor : 1.0f;

        Vector3 position = gameObject.transform.position -= rail.normalized * m_Speed * scaling;

        Vector3 connectingVector = position - m_Rails.rails[m_ActiveRail].start;
        if (Vector3.Dot(connectingVector.normalized, rail.normalized) < 0)
        {
            position = m_Rails.rails[m_ActiveRail].start;

            if (m_ActiveRail > 0 && period > m_TimeInterval)
            {
                m_ActiveRail--;
                period = 0;
            }
            else if (m_ActiveRail == 0)
            {
                if (m_Rails.rails[0].start == m_Rails.rails[^1].end && period > m_TimeInterval)
                {
                    m_ActiveRail = m_Rails.rails.Length - 1;
                    period = 0;
                }
            }
            period += UnityEngine.Time.deltaTime;
        }

        return position;
    }

    private Quaternion LookAtFocusObject()
    {
        Vector3 right = gameObject.transform.position - m_CollisionObjects.m_FocusObject.transform.position;

        float angle = Mathf.Acos(Vector3.Dot(gameObject.transform.up.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        Vector3 up = angle <= 90 ? Vector3.up : -Vector3.up;
        Vector3 forward = Vector3.Cross(right.normalized, up.normalized);

        up = Vector3.Cross(forward.normalized, right.normalized);

        return Quaternion.LookRotation(forward, up);
    }

    private void Released()
    {
        if (m_ManipulationMode.mode == Mode.RAIL && isInteracting)
        {
            m_InteractingHand = null;
            m_OtherHand = null;
            m_InitPos = Vector3.zero;
            period = 0.0f;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}