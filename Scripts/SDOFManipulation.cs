using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class SDOFManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;
    private ExperimentManager m_ExperimentManager = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_HoveringHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_OtherHand = null;

    private Transform m_Handle = null;

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Grip = null;

    private bool isTranslating = false;
    private bool isRotating = false;

    private Vector3 m_PlaneNormal = new();
    private Vector3 m_InitHandPos = new();
    private Vector3 m_InitPos = new();
    private Vector3 m_InitDir = new();
    private Vector3 m_PreviousDir = new();

    private bool m_isInteracting = false;
    private bool m_isScaling = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_RightHand = Player.instance.rightHand;
        m_LeftHand = Player.instance.leftHand;

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_RightHand.handType);
        m_Trigger.AddOnStateDownListener(TriggerGrabbed, m_LeftHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_RightHand.handType);
        m_Trigger.AddOnStateUpListener(TriggerReleased, m_LeftHand.handType);

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Grip.AddOnStateDownListener(GripGrabbed, m_RightHand.handType);
        m_Grip.AddOnStateDownListener(GripGrabbed, m_LeftHand.handType);
        m_Grip.AddOnStateUpListener(GripReleased, m_RightHand.handType);
        m_Grip.AddOnStateUpListener(GripReleased, m_LeftHand.handType);
    }

    private void Update()
    {
        if (m_isInteracting && m_InteractingHand != null && m_ExperimentManager.IsRunning())
            MoveManipulator();
    }

    public void SetHoveringHand(Hand hand, Transform handle)
    {
        m_HoveringHand = hand;
        m_Handle = handle;
    }

    public Hand GetHoveringHand()
    {
        return m_HoveringHand;
    }

    public bool IsInteracting()
    {
        return m_isInteracting;
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF && m_ExperimentManager.IsRunning())
        {
            if (!m_isInteracting && m_InteractingHand == null && m_HoveringHand != null && fromSource == m_HoveringHand.handType)
            {
                m_isInteracting = true;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                if (fromSource == m_LeftHand.handType)
                {
                    m_InteractingHand = m_LeftHand;
                    m_OtherHand = m_RightHand;
                }
                else
                {
                    m_InteractingHand = m_RightHand;
                    m_OtherHand = m_LeftHand;
                }

                m_InitHandPos = m_InteractingHand.objectAttachmentPoint.position;
                m_InitPos = m_Handle.position;
                m_InitDir = m_InitPos - m_Manipulator.transform.position;
                m_PreviousDir = m_InitDir;
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
                m_InteractingHand = null;

            if (!m_Trigger.GetState(m_RightHand.handType) && !m_Trigger.GetState(m_LeftHand.handType))
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                isTranslating = false;
                isRotating = false;

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();
            }
        }
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            if (m_InteractingHand == null)
            {
                m_isScaling = true;
                m_ExperimentManager.RecordModifier("SCALING", true);
            }

            else if (m_OtherHand != null && fromSource == m_OtherHand.handType)
            {
                m_isScaling = true;
                m_InitPos = m_Handle.position;
                m_InitDir = m_InitPos - m_Manipulator.transform.position;
                m_ExperimentManager.RecordModifier("SCALING", true);
            }
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            if ((!m_Grip.GetState(m_RightHand.handType) && !m_Grip.GetState(m_LeftHand.handType)) || (m_InteractingHand != null && fromSource != m_InteractingHand.handType))
            {
                m_isScaling = false;
                m_ExperimentManager.RecordModifier("SCALING", false);
            }
        }
    }

    private void MoveManipulator()
    {
        if (!isTranslating && !isRotating)
        {
            Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitHandPos;
            if (connectingVector.magnitude >= ManipulationMode.DISTANCETHRESHOLD)
            {
                float axisUpDotCV = Vector3.Dot(m_InitDir.normalized, connectingVector.normalized);
                if (Mathf.Abs(axisUpDotCV) < 0.5)   // if angle > 45deg
                {
                    isRotating = true;

                    Transform handleAxis = m_Handle.parent;
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
            TranslateManipulator();

        if (isRotating)
            RotateManipulator();

        m_ROSPublisher.PublishMoveArm();
    }

    private void TranslateManipulator()
    {
        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_InitPos;
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, m_InitDir);

        if (m_isScaling)
            projectedConnectingVector *= ManipulationMode.SCALINGFACTOR;

        Vector3 movement = m_InitPos + projectedConnectingVector - m_InitDir - m_Manipulator.transform.position;
        Vector3 position = m_Manipulator.transform.position + movement;

        //if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        //    position = PositionSnapping(position);

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, m_Manipulator.transform.rotation);
    }

    private void RotateManipulator()
    {
        //Vector3 currentVector = m_Handle.position - m_Manipulator.transform.position;
        float previousAngle = Vector3.SignedAngle(m_InitDir, m_PreviousDir, m_PlaneNormal);

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Manipulator.transform.position;
        Vector3 targetVector = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        float targetAngle = Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);

        if (m_isScaling)
            targetAngle *= ManipulationMode.SCALINGFACTOR;
        else
            targetAngle = RotationSnapping(targetVector);

        float movement = targetAngle - previousAngle;

        Quaternion rotation = Quaternion.AngleAxis(movement, m_PlaneNormal) * m_Manipulator.transform.rotation;
        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);

        m_PreviousDir = Quaternion.AngleAxis(movement, m_PlaneNormal) * m_PreviousDir;
    }

    private float RotationSnapping(Vector3 targetVector)
    {
        //if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        //{
        //    Transform focusObjectPose = m_InteractableObjects.m_FocusObject.transform;

        //    float angleToFocObjX = Vector3.Angle(targetVector, focusObjectPose.right);
        //    float angleToFocObjY = Vector3.Angle(targetVector, focusObjectPose.up);
        //    float angleToFocObjZ = Vector3.Angle(targetVector, focusObjectPose.forward);

        //    if (Mathf.Abs(90.0f - angleToFocObjX) < ManipulationMode.ANGLETHRESHOLD)
        //        targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.right.normalized);

        //    if (Mathf.Abs(90.0f - angleToFocObjY) < ManipulationMode.ANGLETHRESHOLD)
        //        targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.up.normalized);

        //    if (Mathf.Abs(90.0f - angleToFocObjZ) < ManipulationMode.ANGLETHRESHOLD)
        //        targetVector = Vector3.ProjectOnPlane(targetVector, focusObjectPose.forward.normalized);

        //    Vector3 connectingVector = m_Manipulator.transform.position - focusObjectPose.position;
            
        //    float angle = Vector3.Angle(m_Manipulator.transform.right, connectingVector);
        //    if (angle < 90.0f)
        //    {
        //        Vector3 currentVector = m_Handle.position - m_Manipulator.transform.position;
        //        float dotProd = Vector3.Dot(m_Manipulator.transform.right.normalized, currentVector.normalized);
        //        if (Mathf.Abs(dotProd) < 0.5f)
        //        {
        //            float angleToConVec = Vector3.Angle(targetVector, connectingVector);

        //            if (Mathf.Abs(90.0f - angleToConVec) < ManipulationMode.ANGLETHRESHOLD)
        //                targetVector = Vector3.ProjectOnPlane(targetVector, connectingVector);
        //        }
        //        else
        //        {
        //            Vector3 norm1 = Vector3.ProjectOnPlane(Vector3.forward, connectingVector);
        //            Vector3 norm2 = Vector3.Cross(connectingVector.normalized, norm1.normalized);

        //            float angleToNorm1 = Vector3.Angle(targetVector, norm1);
        //            float angleToNorm2 = Vector3.Angle(targetVector, norm2);

        //            if (Mathf.Abs(90.0f - angleToNorm1) < ManipulationMode.ANGLETHRESHOLD)
        //                targetVector = Vector3.ProjectOnPlane(targetVector, norm1);

        //            if (Mathf.Abs(90.0f - angleToNorm2) < ManipulationMode.ANGLETHRESHOLD)
        //                targetVector = Vector3.ProjectOnPlane(targetVector, norm2);
        //        }
        //    }
        //}
        //else
        //{
            float angleToX = Vector3.Angle(targetVector, Vector3.right);
            float angleToY = Vector3.Angle(targetVector, Vector3.up);
            float angleToZ = Vector3.Angle(targetVector, Vector3.forward);

            if (Mathf.Abs(90.0f - angleToX) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.right);

            if (Mathf.Abs(90.0f - angleToY) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.up);

            if (Mathf.Abs(90.0f - angleToZ) < ManipulationMode.ANGLETHRESHOLD)
                targetVector = Vector3.ProjectOnPlane(targetVector, Vector3.forward);
        //}

        return Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);
    }

    public void Flash(bool value)
    {
        foreach(SDOFHandle handle in gameObject.GetComponentsInChildren<SDOFHandle>())
        {
            handle.Flash(value);
        }
    }

    public Hand InteractingHand()
    {
        return m_InteractingHand;
    }

    public Hand OtherHand()
    {
        return m_OtherHand;
    }
}