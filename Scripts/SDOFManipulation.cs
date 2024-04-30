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

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Grip = null;
    private int m_GripCount = 0;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;
    private Hand m_Otherand = null;
    private bool m_isInteracting = false;

    private Transform m_Handle = null;

    private Vector3 m_InitHandPos = new();

    private bool isTranslating = false;
    private Vector3 m_InitPos = new();

    private bool isRotating = false;
    private Vector3 m_InitDir = new();
    private Vector3 m_PlaneNormal = new();

    private bool m_isSnapping = false;
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
        if (m_isInteracting && m_ExperimentManager.m_AllowUserControl)
            MoveManipulator();
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    public void SetInteractingHand(Hand hand, Transform handle)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            m_InteractingHand = hand;
            if (hand == m_RightHand)
                m_Otherand = m_LeftHand;
            else
                m_Otherand = m_RightHand;

            m_Handle = handle;

            if (m_ExperimentManager.m_ShowHints)
            {
                ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Trigger, "Move Manipulator", false);
                ControllerButtonHints.ShowTextHint(m_Otherand, m_Trigger, "Operate Gripper", false);
            }
        }
    }

    public bool IsInteracting()
    {
        return m_isInteracting;
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF && m_ExperimentManager.m_AllowUserControl)
        {
            if (!m_isInteracting && m_InteractingHand != null && fromSource == m_InteractingHand.handType && m_InteractingHand.IsStillHovering(m_Handle.GetComponent<Interactable>()))
            {
                m_isInteracting = true;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                m_InitHandPos = m_InteractingHand.objectAttachmentPoint.position;

                m_LeftHand.GetComponent<Hand>().Hide();
                m_RightHand.GetComponent<Hand>().Hide();

                if (m_ExperimentManager.m_ShowHints)
                    ControllerButtonHints.HideTextHint(m_InteractingHand, m_Trigger);
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            if (m_isInteracting && m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                isTranslating = false;
                isRotating = false;

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();

                m_ROSPublisher.PublishMoveArm();

                if (m_ExperimentManager.m_ShowHints)
                {
                    ControllerButtonHints.HideTextHint(m_RightHand, m_Grip);
                    ControllerButtonHints.HideTextHint(m_LeftHand, m_Grip);
                    ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Trigger, "Move Manipulator", false);
                    ControllerButtonHints.ShowTextHint(m_Otherand, m_Trigger, "Operate Gripper", false);
                }
            }
        }
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            m_GripCount++;

            switch (m_GripCount)
            {
                case 1:
                    if (isTranslating)
                    {
                        m_isScaling = true;
                        m_Manipulator.IsScaling(true);

                        m_InitPos = m_Handle.position;

                        m_ExperimentManager.RecordModifier("SCALING", m_isScaling);

                        if (m_ExperimentManager.m_ShowHints && m_isInteracting)
                        {
                            ControllerButtonHints.HideTextHint(m_InteractingHand, m_Grip);
                            ControllerButtonHints.HideTextHint(m_Otherand, m_Grip);
                        }
                    }
                    else
                    {
                        m_isSnapping = true;

                        m_ExperimentManager.RecordModifier("SNAPPING", m_isSnapping);

                        if (m_ExperimentManager.m_ShowHints && m_isInteracting)
                        {
                            if (fromSource == m_InteractingHand.handType)
                            {
                                ControllerButtonHints.HideTextHint(m_InteractingHand, m_Grip);
                                ControllerButtonHints.ShowTextHint(m_Otherand, m_Grip, "Scaling", false);
                            }
                            else
                            {
                                ControllerButtonHints.HideTextHint(m_Otherand, m_Grip);
                                ControllerButtonHints.ShowTextHint(m_Otherand, m_Trigger, "Operate Gripper", false);
                                ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Scaling", false);
                            }
                        }
                    }
                    break;

                case 2:
                    if (isRotating)
                    {
                        m_isScaling = true;
                        m_InitDir = m_Handle.position - m_Manipulator.transform.position;
                        m_Manipulator.IsScaling(true);

                        m_ExperimentManager.RecordModifier("SCALING", m_isScaling);

                        if (m_ExperimentManager.m_ShowHints && m_isInteracting)
                        {
                            ControllerButtonHints.HideTextHint(m_InteractingHand, m_Grip);
                            ControllerButtonHints.HideTextHint(m_Otherand, m_Grip);
                        }
                    }
                    break;

                default:
                    break;
            }
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            m_GripCount--;

            switch (m_GripCount)
            {
                case 0:
                    if (m_isScaling)
                    {
                        m_isScaling = false;
                        m_Manipulator.IsScaling(false);

                        m_ExperimentManager.RecordModifier("SCALING", m_isScaling);
                    }
                    else
                    {
                        m_isSnapping = false;

                        m_ExperimentManager.RecordModifier("SNAPPING", m_isSnapping);
                    }
                    
                    break;

                case 1:

                    if (m_isSnapping)
                    {
                        m_isScaling = false;
                        m_Manipulator.IsScaling(false);

                        m_ExperimentManager.RecordModifier("SCALING", m_isScaling);
                    }
                    
                    break;

                default:
                    break;
            }

            if (m_isInteracting)
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                isTranslating = false;
                isRotating = false;

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();

                m_ROSPublisher.PublishMoveArm();

                if (m_ExperimentManager.m_ShowHints && m_InteractingHand != null)
                {
                    ControllerButtonHints.HideTextHint(m_RightHand, m_Grip);
                    ControllerButtonHints.HideTextHint(m_LeftHand, m_Grip);
                    ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Trigger, "Move Manipulator", false);
                    ControllerButtonHints.ShowTextHint(m_Otherand, m_Trigger, "Operate Gripper", false);
                }
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
                Transform handleAxis = m_Handle.parent;

                float angleToUp = Vector3.Angle(handleAxis.up, connectingVector);
                if (Mathf.Abs(90.0f - angleToUp) <= 45.0f)
                {
                    isRotating = true;

                    float angleToRight = Vector3.Angle(handleAxis.right, connectingVector);
                    float angleToForward = Vector3.Angle(handleAxis.forward, connectingVector);

                    if (Mathf.Abs(90.0f - angleToRight) < Mathf.Abs(90.0f - angleToForward))
                        m_PlaneNormal = handleAxis.right;
                    else
                        m_PlaneNormal = handleAxis.forward;

                    m_InitDir = m_Handle.position - m_Manipulator.transform.position;

                    m_ExperimentManager.RecordModifier("ROTATING", isRotating);

                    if (m_ExperimentManager.m_ShowHints)
                    {
                        ControllerButtonHints.ShowTextHint(m_InteractingHand, m_Grip, "Snapping", false);
                        ControllerButtonHints.ShowTextHint(m_Otherand, m_Grip, "Snapping", false);
                    }
                }
                else
                {
                    isTranslating = true;

                    m_InitPos = m_Handle.position;

                    m_ExperimentManager.RecordModifier("TRANSLATING", isTranslating);

                    if (m_ExperimentManager.m_ShowHints)
                    {
                        ControllerButtonHints.ShowTextHint(m_RightHand, m_Grip, "Scaling", false);
                        ControllerButtonHints.ShowTextHint(m_LeftHand, m_Grip, "Scaling", false);
                    }
                }
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
        Vector3 projectedConnectingVector = Vector3.Project(connectingVector, m_Handle.parent.up);
        Vector3 offset = m_Handle.position - m_Manipulator.transform.position;

        if (m_isSnapping || m_isScaling)
            projectedConnectingVector *= ManipulationMode.SCALINGFACTOR;

        Vector3 movement = m_InitPos + projectedConnectingVector - offset - m_Manipulator.transform.position;
        Vector3 position = m_Manipulator.transform.position + movement;

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, m_Manipulator.transform.rotation);
    }

    private void RotateManipulator()
    {
        Vector3 currentDirection = m_Handle.position - m_Manipulator.transform.position;
        float currentAngle = Vector3.SignedAngle(m_InitDir, currentDirection, m_PlaneNormal);

        Vector3 connectingVector = m_InteractingHand.objectAttachmentPoint.position - m_Manipulator.transform.position;
        Vector3 targetVector = Vector3.ProjectOnPlane(connectingVector, m_PlaneNormal);
        float targetAngle = Vector3.SignedAngle(m_InitDir, targetVector, m_PlaneNormal);

        if (m_isScaling)
            targetAngle *= ManipulationMode.SCALINGFACTOR;
        else if (m_isSnapping)
            targetAngle = RotationSnapping(targetVector);

        float movement = targetAngle - currentAngle;

        Quaternion rotation = Quaternion.AngleAxis(movement, m_PlaneNormal) * m_Manipulator.transform.rotation;
        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);
    }

    private float RotationSnapping(Vector3 targetVector)
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

    public bool IsTranslating()
    {
        return isTranslating;
    }
}