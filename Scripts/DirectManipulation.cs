using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

[RequireComponent(typeof(Interactable))]

public class DirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;
    private CollisionObjects m_CollisionObjects = null;
    private GripperControl m_GripperControl = null;

    private Interactable m_Interactable = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    
    private bool isInteracting = false;
    public Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;

    private Vector3 m_InitPos = Vector3.zero;
    private Quaternion m_FocusRot = Quaternion.identity;
    private GameObject m_GhostObject = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();

        m_Interactable = GetComponent<Interactable>();
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void Update()
    {
        if(m_FocusRot != Quaternion.identity && m_FocusRot != gameObject.transform.rotation)
        {
            m_ROSPublisher.PublishMoveArm();
            m_FocusRot = Quaternion.identity;
        }

        if ((m_ManipulationMode.mode == Mode.DIRECT || m_ManipulationMode.mode == Mode.RAILCREATOR) && !m_GripperControl.isGripping)
        {
            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType) || m_Trigger.GetStateUp(m_OtherHand.handType))
                    TriggerReleased();

                else
                {
                    if (m_Trigger.GetStateDown(m_OtherHand.handType))
                        m_InitPos = gameObject.transform.position;

                    if (m_Trigger.GetState(m_InteractingHand.handType))
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

    private void OnHandHoverEnd(Hand hand)
    {
        if (!isInteracting)
            m_InteractingHand = null;
    }

    private void TriggerGrabbed()
    {
        if (m_InteractingHand.IsStillHovering(m_Interactable))
        {
            m_GhostObject = new GameObject("GhostObject");
            m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            m_GhostObject.transform.SetParent(m_InteractingHand.transform);
            isInteracting = true;
            m_InitPos = gameObject.transform.position;

            if (m_InteractingHand != Player.instance.rightHand)
                m_OtherHand = Player.instance.rightHand;
            else
                m_OtherHand = Player.instance.leftHand;
        }
    }

    private void MoveManipulator()
    {
        Vector3 position = m_GhostObject.transform.position;
        Quaternion rotation = gameObject.transform.rotation;

        if (m_Trigger.GetState(m_OtherHand.handType))
        {
            Vector3 connectingVector = m_GhostObject.transform.position - m_InitPos;
            position = m_InitPos + connectingVector * ManipulationMode.SCALINGFACTOR;
        }            

        if (m_CollisionObjects.m_FocusObject != null && 
            !m_CollisionObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        {
            position = PositionSnapping(position);
            rotation = LookAtFocusObject(position);
        }
            
        else if (m_ManipulationMode.mode == Mode.DIRECT && !m_Trigger.GetState(m_OtherHand.handType))
            rotation = RotationSnapping();

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);

        if (m_ManipulationMode.mode == Mode.DIRECT && !m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishMoveArm();
    }

    private Vector3 PositionSnapping(Vector3 position)
    {
        Transform focusObject = m_CollisionObjects.m_FocusObject.transform;
        Vector3 connectingVector = position - focusObject.position;

        float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, focusObject.up)) * Mathf.Rad2Deg;
        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            return focusObject.position + Vector3.ProjectOnPlane(connectingVector, focusObject.up);
        }

        if (angle < ManipulationMode.ANGLETHRESHOLD)
        {
            return focusObject.position + Vector3.Project(connectingVector, focusObject.up);
        }

        return position;
    }

    private Quaternion RotationSnapping()
    {
        float angle = Mathf.Acos(Vector3.Dot(m_GhostObject.transform.right.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;

        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            // snap to xz plane
            Vector3 forward = Vector3.ProjectOnPlane(m_GhostObject.transform.forward, Vector3.up.normalized);
            float ang = Mathf.Acos(Vector3.Dot(m_GhostObject.transform.up.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
            Vector3 up = ang <= 90 ? Vector3.up : -Vector3.up;

            return Quaternion.LookRotation(forward, up);
        }
        if (angle < ManipulationMode.ANGLETHRESHOLD)
        {
            //snap to y axis
            Vector3 right = Vector3.Project(m_GhostObject.transform.right, Vector3.up);
            Vector3 up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, Vector3.up);
            Vector3 forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        return m_GhostObject.transform.rotation;
    }

    public void FocusObjectSelected()
    {
        m_FocusRot = gameObject.transform.rotation;
        Quaternion rotation = LookAtFocusObject(gameObject.transform.position);
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(gameObject.transform.position, rotation);
    }

    private Quaternion LookAtFocusObject(Vector3 position)
    {
        /*Transform focusObject = m_CollisionObjects.m_FocusObject.transform;

        Vector3 right = position - focusObject.position;
        Vector3 forward = Vector3.Cross(right.normalized, m_GhostObject.transform.up.normalized);
        Vector3 up = Vector3.Cross(forward.normalized, right.normalized);

        return Quaternion.LookRotation(forward, up);*/

        Transform focusObject = m_CollisionObjects.m_FocusObject.transform;

        Vector3 right = position - focusObject.position;

        float angle = Mathf.Acos(Vector3.Dot(gameObject.transform.up.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        Vector3 up = angle <= 90 ? Vector3.up : -Vector3.up;
        Vector3 forward = Vector3.Cross(right.normalized, up.normalized);

        up = Vector3.Cross(forward.normalized, right.normalized);

        return Quaternion.LookRotation(forward, up);
    }

    private void TriggerReleased()
    {
        GameObject.Destroy(m_GhostObject);
        m_OtherHand = null;
        m_InitPos = Vector3.zero;
        isInteracting = false;

        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_ROSPublisher.PublishTrajectoryRequest();
            else
                m_ROSPublisher.PublishMoveArm();
        }
    }
}