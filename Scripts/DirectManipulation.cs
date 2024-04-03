using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class DirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private ExperimentManager m_ExperimentManager = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_ActivationHand = null;
    private Hand m_InteractingHand = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private Vector3 m_PreviousPosition = new();
    private Quaternion m_PreviousRotation = new();

    private Vector3 m_InitPos = new();
    private GameObject m_GhostObject = null;
    private bool m_isInteracting = false;
    private bool m_isScaling = false;
    private bool m_isSnapping = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
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
        if (m_isInteracting && m_ActivationHand != null)
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
                {
                    m_isScaling = true;
                    m_InitPos = m_GhostObject.transform.position;
                    m_ExperimentManager.RecordModifier("SCALING", true);
                }
            }

            if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            {
                m_isSnapping = true;
                m_ExperimentManager.RecordModifier("SNAPPING", true);
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
                {
                    m_isScaling = false;
                    m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                    m_ExperimentManager.RecordModifier("SCALING", false);
                }
            }

            if (m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            {
                m_isSnapping = false;
                m_ExperimentManager.RecordModifier("SNAPPING", false);
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

        if (m_isScaling)
        {
            Vector3 connectingVector = m_GhostObject.transform.position - m_InitPos;
            position = m_InitPos + connectingVector * ManipulationMode.SCALINGFACTOR;
            rotation = gameObject.transform.rotation;
        }
        else
        {
            if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
            {
                position = PositionSnapping(m_InteractableObjects.m_FocusObject.transform);
                rotation = LookAtFocusObject(position, m_InteractableObjects.m_FocusObject.transform);
            }

            else if (m_isSnapping)
                rotation = RotationSnapping();
        }

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
        m_ROSPublisher.PublishMoveArm();

        m_PreviousPosition = m_InteractingHand.transform.position;
        m_PreviousRotation = m_InteractingHand.transform.rotation;
    }

    private Vector3 PositionSnapping(Transform focusObject)
    {
        Vector3 connectingVector = m_GhostObject.transform.position - focusObject.position;

        float angle = Vector3.Angle(connectingVector, focusObject.up);
        if (angle < ManipulationMode.ANGLETHRESHOLD)
            return m_GhostObject.transform.position = focusObject.position + Vector3.Project(connectingVector, focusObject.up);

        angle = Vector3.Angle(connectingVector, focusObject.right);
        if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
            return m_GhostObject.transform.position = focusObject.position + Vector3.Project(connectingVector, focusObject.right);

        angle = Vector3.Angle(connectingVector, focusObject.forward);
        if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
            return m_GhostObject.transform.position = focusObject.position + Vector3.Project(connectingVector, focusObject.forward);

        return m_GhostObject.transform.position;
    }

    public Quaternion LookAtFocusObject(Vector3 position, Transform focusObject)
    {
        Vector3 right = position - focusObject.position;
        Vector3 up;
        Vector3 forward;

        // manipulator above focus object
        float angle = Vector3.Angle(right, focusObject.up);
        if (angle < ManipulationMode.ANGLETHRESHOLD)
        {
            right = Vector3.Project(m_GhostObject.transform.right, focusObject.up);
            up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, focusObject.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        // manipulator to the right/left of focus object
        angle = Vector3.Angle(right, focusObject.right);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(m_GhostObject.transform.up, focusObject.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        // manipulator infront/behind focus object
        angle = Vector3.Angle(right, focusObject.transform.forward);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(m_GhostObject.transform.up, focusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        // manipulator facing focus object
        //angle = Vector3.Angle(m_GhostObject.transform.right, right);
        //if (angle < ManipulationMode.ANGLETHRESHOLD)
        //{
        //    up = Vector3.Cross(m_GhostObject.transform.forward, right);
        //    angle = Vector3.Angle(up, Vector3.up);
        //    up = angle <= 90 ? Vector3.up : -Vector3.up;

        //    forward = Vector3.Cross(right.normalized, up.normalized);
        //    up = Vector3.Cross(forward.normalized, right.normalized);

        //    return Quaternion.LookRotation(forward, up);
        //}

        return m_GhostObject.transform.rotation;
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

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }
        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            Vector3 right = Vector3.ProjectOnPlane(m_GhostObject.transform.right, Vector3.up);
            Vector3 up = Vector3.Cross(m_GhostObject.transform.forward.normalized, right.normalized);

            angle = Vector3.Angle(up, Vector3.up);
            if (angle < ManipulationMode.ANGLETHRESHOLD || Mathf.Abs(180.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
                up = angle <= 90 ? Vector3.up : -Vector3.up;

            Vector3 forward = Vector3.Cross(right.normalized, up.normalized);

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        return m_GhostObject.transform.rotation;
    }
}