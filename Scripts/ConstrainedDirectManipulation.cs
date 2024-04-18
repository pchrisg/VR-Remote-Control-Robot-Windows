using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

[RequireComponent(typeof(Interactable))]

public class ConstrainedDirectManipulation : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private ExperimentManager m_ExperimentManager = null;

    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Grip = null;
    private int m_GripCount = 0;
    
    private Interactable m_Interactable = null;
    
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;
    private bool m_isInteracting = false;

    private Vector3 m_InitPos = new();
    private GameObject m_GhostObject = null;
    
    private bool m_isScaling = false;
    private bool m_isSnapping = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Interactable = GetComponent<Interactable>();

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
        if (m_isInteracting)// && m_ExperimentManager.IsRunning())
            MoveManipulator();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        if (!m_isInteracting)
            m_InteractingHand = hand;
    }

    private void TriggerGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)// && m_ExperimentManager.IsRunning())
        {
            if (!m_isInteracting && m_InteractingHand != null && fromSource == m_InteractingHand.handType && m_InteractingHand.IsStillHovering(m_Interactable))
            {
                m_isInteracting = true;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                m_GhostObject = new GameObject("GhostObject");
                m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                m_GhostObject.transform.SetParent(m_InteractingHand.transform);

                m_LeftHand.GetComponent<Hand>().Hide();
                m_RightHand.GetComponent<Hand>().Hide();
            }
        }
    }

    private void TriggerReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            if (m_isInteracting && m_InteractingHand != null && fromSource == m_InteractingHand.handType)
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                Destroy(m_GhostObject);

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();
            }
        }
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            m_GripCount++;

            switch (m_GripCount)
            {
                case 1:
                    m_isSnapping = true;

                    m_ExperimentManager.RecordModifier("SNAPPING", m_isSnapping);
                    break;

                case 2:
                    m_isScaling = true;
                    m_InitPos = gameObject.transform.position;
                    gameObject.GetComponent<Manipulator>().IsScaling(true);

                    m_ExperimentManager.RecordModifier("SCALING", m_isScaling);
                    break;

                default:
                    break;
            }
        }
    }

    private void GripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            m_GripCount--;

            switch (m_GripCount)
            {
                case 0:
                    m_isSnapping = false;

                    m_ExperimentManager.RecordModifier("SNAPPING", m_isSnapping);
                    break;

                case 1:
                    m_isScaling = false;
                    gameObject.GetComponent<Manipulator>().IsScaling(false);

                    m_ExperimentManager.RecordModifier("SCALING", m_isScaling);
                    break;

                default:
                    break;
            }

            if (m_isInteracting)
            {
                m_isInteracting = false;
                m_ManipulationMode.IsInteracting(m_isInteracting);

                Destroy(m_GhostObject);

                m_LeftHand.GetComponent<Hand>().Show();
                m_RightHand.GetComponent<Hand>().Show();
            }
        }
    }

    private void MoveManipulator()
    {
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
                if (m_isSnapping)
                {
                    position = PositionSnapping(m_InteractableObjects.m_FocusObject.transform);
                    rotation = LookAtFocusObject(position, m_InteractableObjects.m_FocusObject.transform);
                }
            }

            else if (m_isSnapping)
                rotation = RotationSnapping();
        }

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
        m_ROSPublisher.PublishMoveArm();
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

    private Quaternion LookAtFocusObject(Vector3 position, Transform focusObject)
    {
        Vector3 right = position - focusObject.position;
        Vector3 up;
        Vector3 forward;

        //// manipulator above focus object
        //float angle = Vector3.Angle(right, focusObject.up);
        //if (angle < ManipulationMode.ANGLETHRESHOLD)
        //{
        //    up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, focusObject.up);
        //    forward = Vector3.Cross(focusObject.up.normalized, up.normalized);

        //    return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        //}

        // manipulator facing focus object
        //angle = Vector3.Angle(m_GhostObject.transform.right, right);
        //if (angle < ManipulationMode.ANGLETHRESHOLD)
        //{
            up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, right);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
        //}

        //return m_GhostObject.transform.rotation;
    }

    private Quaternion RotationSnapping()
    {
        //snap to y axis
        Vector3 up = Vector3.ProjectOnPlane(m_GhostObject.transform.up, Vector3.up);
        Vector3 forward = Vector3.Cross(Vector3.up, up.normalized);

        return m_GhostObject.transform.rotation = Quaternion.LookRotation(forward, up);
    }

    public Hand InteractingHand()
    {
        return m_InteractingHand;
    }
}