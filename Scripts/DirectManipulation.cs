using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class DirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_ActivationHand = null;
    private Hand m_InteractingHand = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private Vector3 m_PreviousPosition = new();
    private Quaternion m_PreviousRotation = new();

    private Transform m_Robotiq = null;
    private Transform m_ManipulatorPose = null;

    private float m_TotalDistance = 0.0f;
    private float m_PreviousDistance = 0.0f;

    private float m_TotalAngle = 0.0f;
    private float m_PreviousAngle = 0.0f;

    private Vector3 m_InitPos = new();
    private GameObject m_GhostObject = null;
    private bool m_isInteracting = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

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

        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;
        m_ManipulatorPose = gameObject.transform.Find("Pose");
    }

    private void Update()
    {
        if (m_isInteracting)
            MoveManipulator();
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (!m_isInteracting && m_ActivationHand == null)
            {
                m_isInteracting = true;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                if (fromSource == m_LeftHand.handType)
                {
                    m_ActivationHand = m_LeftHand;
                    m_InteractingHand = m_RightHand;
                }
                else
                {
                    m_ActivationHand = m_RightHand;
                    m_InteractingHand = m_LeftHand;
                }

                m_PreviousPosition = m_InteractingHand.transform.position;
                m_PreviousRotation = m_InteractingHand.transform.rotation;

                m_LeftHand.GetComponent<Hand>().Hide();
                m_RightHand.GetComponent<Hand>().Hide();

                m_GhostObject = new GameObject("GhostObject");
                m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);

                m_TotalDistance = 0.0f;
                m_PreviousDistance = Vector3.Distance(m_Robotiq.position, m_ManipulatorPose.position);
                m_TotalAngle = 0.0f;
                m_PreviousAngle = Quaternion.Angle(m_Robotiq.rotation, m_ManipulatorPose.rotation);
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
            {
                m_ActivationHand = null;
                Destroy(m_GhostObject);
                m_ROSPublisher.PublishMoveArm();
            }

            if (!m_Trigger.GetState(m_RightHand.handType) && !m_Trigger.GetState(m_LeftHand.handType))
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();
            }
        }
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
            {
                if (m_GhostObject != null)
                    m_InitPos = m_GhostObject.transform.position;
            }
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            if (m_ActivationHand != null && fromSource == m_ActivationHand.handType)
            {
                if (m_GhostObject != null)
                    m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            }
        }
    }

    private void MoveManipulator()
    {
        Vector3 fromToPosition = m_InteractingHand.transform.position - m_PreviousPosition;
        Quaternion fromToRotation = m_InteractingHand.transform.rotation * Quaternion.Inverse(m_PreviousRotation);

        m_GhostObject.transform.position += fromToPosition;
        m_GhostObject.transform.rotation = fromToRotation * m_GhostObject.transform.rotation;

        Vector3 position = m_GhostObject.transform.position;
        Quaternion rotation = m_GhostObject.transform.rotation;

        if (m_Grip.GetState(m_ActivationHand.handType))
        {
            Vector3 connectingVector = m_GhostObject.transform.position - m_InitPos;
            position = m_InitPos + connectingVector * ManipulationMode.SCALINGFACTOR;
            rotation = gameObject.transform.rotation;
        }
        else
        {
            if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
            {
                position = PositionSnapping();
                rotation = m_InteractableObjects.LookAtFocusObject(position, m_GhostObject.transform);
            }

            if (rotation == m_GhostObject.transform.rotation)
                rotation = RotationSnapping();
        }

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
        m_ROSPublisher.PublishMoveArm();

        float distance = Vector3.Distance(m_Robotiq.position, m_ManipulatorPose.position);
        float deltaDistance = distance - m_PreviousDistance;
        if (deltaDistance > 0)
        {
            m_TotalDistance += deltaDistance;
            if (m_TotalDistance > ManipulationMode.DISTANCETHRESHOLD)
                m_ROSPublisher.PublishStopArm();
        }
        else
            m_TotalDistance = 0.0f;

        float angle = Quaternion.Angle(m_Robotiq.rotation, m_ManipulatorPose.rotation);
        float deltaAngle = angle - m_PreviousAngle;
        if (deltaAngle > 0)
        {
            m_TotalAngle += deltaAngle;
            if (m_TotalAngle > ManipulationMode.ANGLETHRESHOLD)
                m_ROSPublisher.PublishStopArm();
        }
        else
            m_TotalAngle = 0.0f;

        m_PreviousDistance = Vector3.Distance(m_Robotiq.position, position + transform.TransformVector(m_ManipulatorPose.localPosition));
        m_PreviousAngle = Quaternion.Angle(m_Robotiq.rotation, rotation * m_ManipulatorPose.localRotation);
        
        m_PreviousPosition = m_InteractingHand.transform.position;
        m_PreviousRotation = m_InteractingHand.transform.rotation;
    }

    private Vector3 PositionSnapping()
    {
        Transform focusObject = m_InteractableObjects.m_FocusObject.transform;
        Vector3 connectingVector = m_GhostObject.transform.position - focusObject.position;

        float snappingThreshold = ManipulationMode.ANGLETHRESHOLD * 2.0f;
        float angle = Vector3.Angle(connectingVector, focusObject.up);
        if (angle < snappingThreshold)
            return focusObject.position + Vector3.Project(connectingVector, focusObject.up);

        angle = Vector3.Angle(connectingVector, focusObject.right);
        if (angle < snappingThreshold || 180.0f - angle < snappingThreshold)
            return focusObject.position + Vector3.Project(connectingVector, focusObject.right);

        angle = Vector3.Angle(connectingVector, focusObject.forward);
        if (angle < snappingThreshold || 180.0f - angle < snappingThreshold)
            return focusObject.position + Vector3.Project(connectingVector, focusObject.forward);

        return m_GhostObject.transform.position;
    }

    private Quaternion RotationSnapping()
    {
        float angle = Vector3.Angle(m_GhostObject.transform.right, Vector3.up);

        if (angle < ManipulationMode.ANGLETHRESHOLD)
        {
            //snap to y axis
            Vector3 right = Vector3.Project(m_GhostObject.transform.right, Vector3.up);
            Vector3 up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, Vector3.up);
            Vector3 forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }
        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            // snap to xz plane
            Vector3 forward = Vector3.ProjectOnPlane(m_GhostObject.transform.forward, Vector3.up);
            angle = Vector3.Angle(m_GhostObject.transform.up, Vector3.up);
            Vector3 up = angle <= 90 ? Vector3.up : -Vector3.up;

            return Quaternion.LookRotation(forward, up);
        }

        return m_GhostObject.transform.rotation;
    }
}