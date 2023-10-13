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
    private CollisionObjects m_CollisionObjects = null;

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
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();

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

    private void Translate()
    {
        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitPos;
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, m_InitDir);

        if (m_Trigger.GetState(m_OtherHand.handType))
            projectedConnectingVector *= ManipulationMode.SCALINGFACTOR;

        Vector3 movement = m_InitPos + projectedConnectingVector - m_InitDir - m_Manipulator.transform.position;
        Vector3 position = m_Manipulator.transform.position + movement;

        if (m_CollisionObjects.m_FocusObject != null && !m_CollisionObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
            position = PositionSnapping(position);

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, m_Manipulator.transform.rotation);
    }

    private Vector3 PositionSnapping(Vector3 position)
    {
        Transform focusObjectPose = m_CollisionObjects.m_FocusObject.transform;

        Plane focObjXYPlane = new Plane(focusObjectPose.forward, focusObjectPose.position);
        Plane focObjXZPlane = new Plane(focusObjectPose.up, focusObjectPose.position);
        Plane focObjYZPlane = new Plane(focusObjectPose.right, focusObjectPose.position);
        Ray direction = new Ray(m_InitPos, m_InitDir);
        float intersection = 0.0f;

        Vector3 connectingVector = position - focusObjectPose.position;

        float angleToFocObjX = Vector3.Angle(connectingVector, focusObjectPose.right);
        float angleToFocObjY = Vector3.Angle(connectingVector, focusObjectPose.up);
        float angleToFocObjZ = Vector3.Angle(connectingVector, focusObjectPose.forward);

        if (Mathf.Abs(90.0f - angleToFocObjX) < ManipulationMode.ANGLETHRESHOLD && Mathf.Abs(90.0f - angleToFocObjX) > 0.1f)
            focObjYZPlane.Raycast(direction, out intersection);

        if (Mathf.Abs(90.0f - angleToFocObjY) < ManipulationMode.ANGLETHRESHOLD && Mathf.Abs(90.0f - angleToFocObjY) > 0.1f)
            focObjXZPlane.Raycast(direction, out intersection);

        if (Mathf.Abs(90.0f - angleToFocObjZ) < ManipulationMode.ANGLETHRESHOLD && Mathf.Abs(90.0f - angleToFocObjZ) > 0.1f)
            focObjXYPlane.Raycast(direction, out intersection);

        if (intersection != 0.0f)
            position = direction.GetPoint(intersection);
        
        return position;
    }

    private void Rotate()
    {
        Vector3 currentVector = m_Interactable.transform.position - m_Manipulator.transform.position;
        float currentAngle = Vector3.SignedAngle(m_InitDir, currentVector, m_PlaneNormal);

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Manipulator.transform.position;
        Vector3 targetVector = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        float targetAngle = Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);

        if (m_Trigger.GetState(m_OtherHand.handType))
            targetAngle *= ManipulationMode.SCALINGFACTOR;
        else
            targetAngle = RotationSnapping(targetVector);

        float movement = targetAngle - currentAngle;

        Quaternion rotation = Quaternion.AngleAxis(movement, m_PlaneNormal) * m_Manipulator.transform.rotation;
        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);
    }

    private float RotationSnapping(Vector3 targetVector)
    {
        if (m_CollisionObjects.m_FocusObject != null && !m_CollisionObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        {
            Transform focusObjectPose = m_CollisionObjects.m_FocusObject.transform;

            float angleToFocObjX = Vector3.Angle(targetVector, focusObjectPose.right);
            float angleToFocObjY = Vector3.Angle(targetVector, focusObjectPose.up);
            float angleToFocObjZ = Vector3.Angle(targetVector, focusObjectPose.forward);

            if (Mathf.Abs(90.0f - angleToFocObjX) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.right.normalized);

            if (Mathf.Abs(90.0f - angleToFocObjY) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.up.normalized);

            if (Mathf.Abs(90.0f - angleToFocObjZ) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.forward.normalized);

            Vector3 connectingVector = m_Manipulator.transform.position - focusObjectPose.position;
            
            float angle = Vector3.Angle(m_Manipulator.transform.right, connectingVector);
            if (angle < 90.0f)
            {
                Vector3 currentVector = m_Interactable.transform.position - m_Manipulator.transform.position;
                float dotProd = Vector3.Dot(m_Manipulator.transform.right.normalized, currentVector.normalized);
                if (Mathf.Abs(dotProd) < 0.5f)
                {
                    float angleToConVec = Vector3.Angle(targetVector, connectingVector);

                    if (Mathf.Abs(90.0f - angleToConVec) < ManipulationMode.ANGLETHRESHOLD)
                        targetVector = Vector3.ProjectOnPlane(targetVector, connectingVector);
                }
                else
                {
                    Vector3 norm1 = Vector3.ProjectOnPlane(Vector3.forward, connectingVector);
                    Vector3 norm2 = Vector3.Cross(connectingVector.normalized, norm1.normalized);

                    float angleToNorm1 = Vector3.Angle(targetVector, norm1);
                    float angleToNorm2 = Vector3.Angle(targetVector, norm2);

                    if (Mathf.Abs(90.0f - angleToNorm1) < ManipulationMode.ANGLETHRESHOLD)
                        targetVector = Vector3.ProjectOnPlane(targetVector, norm1);

                    if (Mathf.Abs(90.0f - angleToNorm2) < ManipulationMode.ANGLETHRESHOLD)
                        targetVector = Vector3.ProjectOnPlane(targetVector, norm2);
                }
            }
        }
        else
        {
            float angleToX = Vector3.Angle(targetVector, Vector3.right);
            float angleToY = Vector3.Angle(targetVector, Vector3.up);
            float angleToZ = Vector3.Angle(targetVector, Vector3.forward);

            if (Mathf.Abs(90.0f - angleToX) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.right);

            if (Mathf.Abs(90.0f - angleToY) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.up);

            if (Mathf.Abs(90.0f - angleToZ) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.forward);
        }

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