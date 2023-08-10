using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class SDOFManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private PlanningRobot m_PlanningRobot = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;

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
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.SDOF && !m_GripperControl.isGripping)
        {
            m_ManipulationMode.isInteracting = isInteracting;

            if (!isInteracting && m_InteractingHand != null && m_Trigger.GetStateDown(m_InteractingHand.handType))
                TriggerGrabbed();

            if (isInteracting)
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                    TriggerReleased();

                else
                {
                    if (m_Trigger.GetStateDown(m_OtherHand.handType))
                    {
                        m_InitPos = m_Interactable.transform.position;
                        m_InitDir = m_InitPos - m_Manipulator.transform.position;
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
            m_InitDir = m_InitPos - m_Manipulator.transform.position;

            if (m_InteractingHand == Player.instance.rightHand)
                m_OtherHand = Player.instance.leftHand;
            else
                m_OtherHand = Player.instance.rightHand;
        }
    }

    private void TriggerHeld()
    {
        if(!isTranslating && !isRotating)
        {
            Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitHandPos;
            if (connectingVector.magnitude >= ManipulationMode.DISTANCETHRESHOLD)
            {
                float axisUpDotCV = Vector3.Dot(m_InitDir.normalized, connectingVector.normalized);
                if (Mathf.Abs(axisUpDotCV) < 0.5)   // if angle > 45deg
                {
                    isRotating = true;

                    Transform handleAxis = m_Interactable.transform.parent;
                    float axisRightDotCV = Vector3.Dot(connectingVector.normalized, handleAxis.right.normalized);
                    float axisForwardDotCV = Vector3.Dot(connectingVector.normalized, handleAxis.forward.normalized);

                    if (Mathf.Abs(axisRightDotCV) < Mathf.Abs(axisForwardDotCV))    // if angle to right closer to 90deg
                        m_PlaneNormal = handleAxis.right;
                    else
                        m_PlaneNormal = handleAxis.forward;
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

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitPos;
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, handleAxis.up);

        if (m_Trigger.GetState(m_OtherHand.handType))
            projectedConnectingVector *= ManipulationMode.SCALINGFACTOR;

        Vector3 movement = m_InitPos + projectedConnectingVector - m_Interactable.transform.position;

        Vector3 position = m_Manipulator.transform.position + movement;

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, m_Manipulator.transform.rotation);
    }

    void Rotate()
    {
        /*Vector3 fromVector = m_Interactable.transform.parent.up;
        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Manipulator.transform.position;

        if (Vector3.Dot(fromVector.normalized, connectingVector.normalized) < 0)    // if angle greater than 90deg
            fromVector *= -1;

        Vector3 toVector = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);

        //toVector = Snapping(toVector);
        float angle = Vector3.SignedAngle(fromVector, toVector, m_PlaneNormal);

        if (m_Trigger.GetState(m_OtherHand.handType))
        {
            angle = Vector3.SignedAngle(m_InitDir, toVector, m_PlaneNormal) * ManipulationMode.SCALINGFACTOR;
            m_InitDir = toVector;
        }

        Quaternion rotation = Quaternion.AngleAxis(angle, m_PlaneNormal) * m_Manipulator.transform.rotation;
        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);*/

        Vector3 currentVector = m_Interactable.transform.position - m_Manipulator.transform.position;
        float currentAngle = Vector3.SignedAngle(m_InitDir, currentVector, m_PlaneNormal);

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Manipulator.transform.position;
        Vector3 targetVector = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        float targetAngle = Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);

        if (m_Trigger.GetState(m_OtherHand.handType))
            targetAngle *= ManipulationMode.SCALINGFACTOR;
        else
            targetAngle = Snapping(targetVector);

        float movement = targetAngle - currentAngle;

        Quaternion rotation = Quaternion.AngleAxis(movement, m_PlaneNormal) * m_Manipulator.transform.rotation;
        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);
    }

    private float Snapping(Vector3 targetVector)
    {
        float angleToX = Mathf.Acos(Vector3.Dot(targetVector.normalized, Vector3.right.normalized)) * Mathf.Rad2Deg;
        float angleToY = Mathf.Acos(Vector3.Dot(targetVector.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        float angleToZ = Mathf.Acos(Vector3.Dot(targetVector.normalized, Vector3.forward.normalized)) * Mathf.Rad2Deg;

        if (Mathf.Abs(90.0f - angleToX) < ManipulationMode.ANGLETHRESHOLD)
            targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.right);

        if (Mathf.Abs(90.0f - angleToY) < ManipulationMode.ANGLETHRESHOLD)
            targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.up);

        if (Mathf.Abs(90.0f - angleToZ) < ManipulationMode.ANGLETHRESHOLD)
            targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.forward);

        return Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);
    }

    private void TriggerReleased()
    {
        m_Interactable = null;
        m_InteractingHand = null;
        isInteracting = false;
        isTranslating = false;
        isRotating = false;

        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.RequestTrajectory();
        else
            m_ROSPublisher.PublishMoveArm();
    }

    public void Flash(bool value)
    {
        foreach(SDOFHandle handle in gameObject.GetComponentsInChildren<SDOFHandle>())
        {
            handle.Flash(value);
        }
    }
}