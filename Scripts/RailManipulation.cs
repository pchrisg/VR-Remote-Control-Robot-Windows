using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using System.Linq;

[RequireComponent(typeof(Interactable))]

public class RailManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;
    private CollisionObjects m_CollisionObjects = null;
    private Rails m_Rails = null;
    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;

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
    private float pause = 0.0f;

    private readonly float m_Speed = 0.2f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Rails = GameObject.FindGameObjectWithTag("Rails").GetComponent<Rails>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();

        m_Interactable = GetComponent<Interactable>();
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Forward = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchRight");
        m_Reverse = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchLeft");

        m_Forward.onChange += RightTouched;
        m_Reverse.onChange += LeftTouched;
    }

    private void OnDestroy()
    {
        m_Forward.onChange -= RightTouched;
        m_Reverse.onChange -= LeftTouched;
    }

    private void Update()
    {
        if (m_ManipulationMode.mode != Mode.RAIL && m_ActiveRail != 0)
            m_ActiveRail = 0;

        if(m_ManipulationMode.mode == Mode.RAIL && !m_GripperControl.isGripping)
        {
            m_ManipulationMode.isInteracting = isInteracting;

            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType) ||
                        m_Forward.GetStateUp(m_InteractingHand.handType) ||
                        m_Reverse.GetStateUp(m_InteractingHand.handType) ||
                        m_Trigger.GetStateUp(m_OtherHand.handType))
                    Released();

                else
                {
                    if (m_Trigger.GetStateDown(m_OtherHand.handType))
                        m_InitPos = gameObject.transform.position;

                    else
                        MoveManipulator();
                }
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

    private void RightTouched(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (m_ManipulationMode.mode == Mode.RAIL)
        {
            m_InteractingHand = Player.instance.leftHand;
            m_OtherHand = Player.instance.rightHand;
            isInteracting = true;
        }
    }

    private void LeftTouched(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (m_ManipulationMode.mode == Mode.RAIL)
        {
            m_InteractingHand = Player.instance.leftHand;
            m_OtherHand = Player.instance.rightHand;
            isInteracting = true;
        }
    }

    private void MoveManipulator()
    {
        Vector3 position = gameObject.transform.position;
        Quaternion rotation = gameObject.transform.rotation;

        if (m_Trigger.GetState(m_InteractingHand.handType))
            position = FollowRail();

        else
        {
            if (m_Forward.GetState(m_InteractingHand.handType))
                position = Forward();

            if (m_Reverse.GetState(m_InteractingHand.handType))
                position = Reverse();
        }

        if (m_CollisionObjects.m_FocusObject != null && !m_CollisionObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
            rotation = m_CollisionObjects.LookAtFocusObject(position, gameObject.transform, m_Rails.m_Rails[m_ActiveRail].rail.transform.up);

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);

        if (!m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishMoveArm();
    }

    private Vector3 FollowRail()
    {
        Vector3 position = m_InitPos;

        Vector3 rail = m_Rails.m_Rails[m_ActiveRail].end - m_Rails.m_Rails[m_ActiveRail].start;
        Vector3 connectingVectorToStart = m_InteractingHand.objectAttachmentPoint.position - m_Rails.m_Rails[m_ActiveRail].start;
        Vector3 connectingVectorToEnd = m_InteractingHand.objectAttachmentPoint.position - m_Rails.m_Rails[m_ActiveRail].end;

        if (connectingVectorToStart.magnitude > connectingVectorToEnd.magnitude)
        {
            Vector3 projectedConnectingVector = Vector3.Project(connectingVectorToStart, rail);
            Vector3 movement = m_Rails.m_Rails[m_ActiveRail].start + projectedConnectingVector - m_InitPos;

            if (m_Trigger.GetState(m_OtherHand.handType))
                movement *= ManipulationMode.SCALINGFACTOR;

            position = GetNextRail(rail, m_InitPos + movement);
        }
        else
        {
            Vector3 projectedConnectingVector = Vector3.Project(connectingVectorToEnd, rail);
            Vector3 movement = m_Rails.m_Rails[m_ActiveRail].end + projectedConnectingVector - m_InitPos;

            if (m_Trigger.GetState(m_OtherHand.handType))
                movement *= ManipulationMode.SCALINGFACTOR;

            position = GetPreviousRail(rail, m_InitPos + movement);
        }

        return position;
    }

    private Vector3 Forward()
    {
        Vector3 rail = m_Rails.m_Rails[m_ActiveRail].end - m_Rails.m_Rails[m_ActiveRail].start;

        Vector3 movement = rail.normalized * UnityEngine.Time.deltaTime * m_Speed;
        if (m_Trigger.GetState(m_OtherHand.handType))
            movement *= ManipulationMode.SCALINGFACTOR;

        Vector3 position = GetNextRail(rail, gameObject.transform.position + movement);

        return position;
    }

    private Vector3 Reverse()
    {
        Vector3 rail = m_Rails.m_Rails[m_ActiveRail].start - m_Rails.m_Rails[m_ActiveRail].end;

        Vector3 movement = rail.normalized * UnityEngine.Time.deltaTime * m_Speed;
        if (m_Trigger.GetState(m_OtherHand.handType))
            movement *= ManipulationMode.SCALINGFACTOR;

        Vector3 position = GetPreviousRail(rail, gameObject.transform.position + movement);

        return position;
    }

    private Vector3 GetNextRail(Vector3 rail, Vector3 position)
    {
        Vector3 connectingVector = position - m_Rails.m_Rails[m_ActiveRail].start;
        if (connectingVector.magnitude > rail.magnitude)
        {
            position = m_Rails.m_Rails[m_ActiveRail].end;

            if (m_ActiveRail < m_Rails.m_Rails.Count - 1 && pause > m_TimeInterval)
            {
                m_ActiveRail++;
                pause = 0;
            }
            else if (m_ActiveRail == m_Rails.m_Rails.Count - 1)
            {
                if (m_Rails.m_Rails.First().start == m_Rails.m_Rails.Last().end && pause > m_TimeInterval)
                {
                    m_ActiveRail = 0;
                    pause = 0;
                }
            }
            pause += UnityEngine.Time.deltaTime;
        }

        return position;
    }

    private Vector3 GetPreviousRail(Vector3 rail, Vector3 position)
    {
        Vector3 connectingVector = position - m_Rails.m_Rails[m_ActiveRail].end;
        if (connectingVector.magnitude > rail.magnitude)
        {
            position = m_Rails.m_Rails[m_ActiveRail].start;

            if (m_ActiveRail > 0 && pause > m_TimeInterval)
            {
                m_ActiveRail--;
                pause = 0;
            }
            else if (m_ActiveRail == 0)
            {
                if (m_Rails.m_Rails.First().start == m_Rails.m_Rails.Last().end && pause > m_TimeInterval)
                {
                    m_ActiveRail = m_Rails.m_Rails.Count - 1;
                    pause = 0;
                }
            }
            pause += UnityEngine.Time.deltaTime;
        }

        return position;
    }
    
    private void Released()
    {
        if (m_ManipulationMode.mode == Mode.RAIL && isInteracting)
        {
            m_InteractingHand = null;
            m_OtherHand = null;
            m_InitPos = Vector3.zero;
            pause = 0.0f;
            isInteracting = false;

            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.RequestTrajectory();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}