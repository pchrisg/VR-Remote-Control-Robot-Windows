using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class SDOFManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private PlanningRobot m_PlanningRobot = null;
    private ManipulationMode m_ManipulationMode = null;
    private Transform m_EndEffector = null;
    private Gripper m_Gripper = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private bool isTranslating = false;
    private bool isRotating = false;

    private Vector3 m_PlaneNormal = Vector3.zero;
    private Vector3 m_InitHandPos = Vector3.zero;
    private Vector3 m_InitPos = Vector3.zero;
    private Vector3 m_InitDir = Vector3.zero;

    private Hand m_OtherHand = null;
    [HideInInspector] public bool isInteracting = false;
    [HideInInspector] public Hand m_InteractingHand = null;
    [HideInInspector] public Interactable m_Interactable = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector").transform;
        m_Gripper = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<Gripper>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.SDOF && !m_Gripper.isGripping)
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
                    {
                        m_InitPos = m_Interactable.transform.position;
                        m_InitDir = m_Interactable.transform.parent.up;
                    }

                    if (m_Trigger.GetState(m_InteractingHand.handType))
                        TriggerHeld();
                }
            }
        }
    }

    private void TriggerGrabbed()
    {
        if (m_InteractingHand.IsStillHovering(m_Interactable))
        {
            m_InitHandPos = m_InteractingHand.objectAttachmentPoint.position;
            isInteracting = true;
            m_InitPos = m_Interactable.transform.position;
            m_InitDir = m_Interactable.transform.parent.up;

            if (m_InteractingHand != Player.instance.rightHand)
                m_OtherHand = Player.instance.rightHand;
            else
                m_OtherHand = Player.instance.leftHand;
        }
    }

    private void TriggerHeld()
    {
        if(!isTranslating && !isRotating)
        {
            Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitHandPos;
            if (connectingVector.magnitude >= ManipulationMode.DISTANCETHRESHOLD)
            {
                Transform handleAxis = m_Interactable.transform.parent;

                float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, handleAxis.up.normalized)) * 180 / Mathf.PI;
                if ((Mathf.Abs(angle - 90.0f) < angle) && (Mathf.Abs(angle - 90.0f) < Mathf.Abs(180.0f - angle)))
                {
                    angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, handleAxis.right.normalized)) * 180 / Mathf.PI;
                    if ((Mathf.Abs(angle - 90.0f) < angle) && (Mathf.Abs(angle - 90.0f) < Mathf.Abs(180.0f - angle)))
                        m_PlaneNormal = handleAxis.right;
                    else
                        m_PlaneNormal = handleAxis.forward;

                    isRotating = true;
                }
                else
                    isTranslating = true;
            }
        }

        if (isTranslating)
            Translate();

        if (isRotating)
            Rotate();

        if (!m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishMoveArm();
    }

    void Translate()
    {
        Transform handleAxis = m_Interactable.transform.parent;
        Transform widget = handleAxis.parent;

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Interactable.transform.position;
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, handleAxis.up);

        if (m_Trigger.GetState(m_OtherHand.handType))
        {
            Vector3 scaledVector = m_InteractingHand.objectAttachmentPoint.position - m_InitPos;
            Vector3 scaledPos = m_InitPos + Vector3.Project(scaledVector, handleAxis.up) * ManipulationMode.SCALINGFACTOR;

            projectedConnectingVector = scaledPos - m_Interactable.transform.position;
        }

        Vector3 position = m_EndEffector.position + projectedConnectingVector;

        m_EndEffector.GetComponent<ArticulationBody>().TeleportRoot(position, m_EndEffector.rotation);
    }

    void Rotate()
    {
        Transform handleAxis = m_Interactable.transform.parent;
        Transform widget = handleAxis.parent;

        Vector3 connectingVector;
        if (handleAxis.GetChild(0) == m_Interactable.transform)
            connectingVector = m_InteractingHand.objectAttachmentPoint.position - widget.transform.position;
        else
            connectingVector = widget.transform.position - m_InteractingHand.objectAttachmentPoint.position;

        Vector3 direction = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);

        direction = Snapping(direction);
        float angle = Vector3.SignedAngle(handleAxis.up, direction, m_PlaneNormal);

        if (m_Trigger.GetState(m_OtherHand.handType))
        {
            angle = Vector3.SignedAngle(m_InitDir, direction, m_PlaneNormal) * ManipulationMode.SCALINGFACTOR;
            m_InitDir = direction;
        }

        Quaternion rotation = Quaternion.AngleAxis(angle, m_PlaneNormal) * m_EndEffector.rotation;
        m_EndEffector.GetComponent<ArticulationBody>().TeleportRoot(m_EndEffector.position, rotation);
    }

    private Vector3 Snapping(Vector3 direction)
    {
        float angleToX = Mathf.Acos(Vector3.Dot(direction.normalized, Vector3.right.normalized)) * Mathf.Rad2Deg;
        float angleToY = Mathf.Acos(Vector3.Dot(direction.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        float angleToZ = Mathf.Acos(Vector3.Dot(direction.normalized, Vector3.forward.normalized)) * Mathf.Rad2Deg;

        if (Mathf.Abs(90.0f - angleToX) < ManipulationMode.ANGLETHRESHOLD)
            direction = Vector3.ProjectOnPlane(direction, Vector3.right);

        if (Mathf.Abs(90.0f - angleToY) < ManipulationMode.ANGLETHRESHOLD)
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);

        if (Mathf.Abs(90.0f - angleToZ) < ManipulationMode.ANGLETHRESHOLD)
            direction = Vector3.ProjectOnPlane(direction, Vector3.forward);

        return direction;
    }

    private void TriggerReleased()
    {
        m_Interactable = null;
        m_InteractingHand = null;
        isInteracting = false;
        isTranslating = false;
        isRotating = false;

        if (m_PlanningRobot.isPlanning)
            m_ROSPublisher.PublishTrajectoryRequest();
        else
            m_ROSPublisher.PublishMoveArm();
    }
}