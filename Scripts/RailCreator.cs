using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

public class RailCreator : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_RailPrefab = null;

    [Header("Material")]
    [SerializeField] private Material m_RailMat;

    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;
    private CollisionObjects m_CollisionObjects = null;
    private Rails m_Rails = null;

    private Interactable m_Interactable = null;
    private Transform m_Manipulator = null;
    private Transform m_Robotiq = null;
    private GameObject m_NewRail = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_InteractingHand = null;
    private bool isInteracting = false;

    private GameObject m_GhostObject = null;
    private Vector3 m_Pivot = Vector3.zero;
    private bool locked = false;

    //Colors
    private Color m_DefaultColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    private Color m_Y_AxisColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color m_XZ_PlaneColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);
    private Color m_FocusObjectColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").transform;
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;
        m_Interactable = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Interactable>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Rails = gameObject.transform.parent.GetComponent<Rails>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void OnDisable()
    {
        if(m_NewRail != null)
        {
            Destroy(m_NewRail);
            m_NewRail = null;
            m_Pivot = Vector3.zero;
        }
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            m_ManipulationMode.isInteracting = isInteracting;
            if (!isInteracting && !locked)
            {
                if( m_Trigger.GetStateDown(Player.instance.rightHand.handType) ||
                    m_Trigger.GetStateDown(Player.instance.leftHand.handType))
                    TriggerGrabbed();
            }
            else
            {
                if (m_Trigger.GetStateUp(m_InteractingHand.handType))
                    TriggerReleased();

                else
                {
                    if (m_Trigger.GetState(m_InteractingHand.handType))
                        RotateRail();
                }
            }
        }
    }

    private void TriggerGrabbed()
    {
        if (m_Trigger.GetState(Player.instance.rightHand.handType) && Player.instance.rightHand.IsStillHovering(m_Interactable))
            m_InteractingHand = Player.instance.rightHand;

        else if (m_Trigger.GetState(Player.instance.leftHand.handType) && Player.instance.leftHand.IsStillHovering(m_Interactable))
            m_InteractingHand = Player.instance.leftHand;

        if (m_InteractingHand == null)
            RemoveLastRail();

        else
        {
            m_GhostObject = new GameObject("GhostObject");
            m_GhostObject.transform.SetPositionAndRotation(m_Manipulator.position, m_Manipulator.rotation);
            m_GhostObject.transform.SetParent(m_InteractingHand.transform);

            if (m_NewRail == null)
                MakeRail();

            isInteracting = true;
        }
    }

    private void RemoveLastRail()
    {
        if (m_Rails.m_Rails.Any())
        {
            Vector3 position = m_Rails.m_Rails.Last().start;
            Quaternion rotation = m_Manipulator.rotation;

            if (m_Rails.m_Rails.Count == 1)
                rotation = m_Robotiq.rotation;

            else if(m_CollisionObjects.m_FocusObject != null)
            {
                Vector3 connectingVector = m_Rails.m_Rails[^2].start - position;
                rotation = m_CollisionObjects.LookAtFocusObject(position, m_Manipulator, connectingVector);
            }

            m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);

            m_Rails.RemoveLastRail();
            m_PlanningRobot.DeleteLastTrajectory();
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
        m_PlanningRobot.Show(value);
    }

    private void MakeRail()
    {
        if (m_Rails.m_Rails.Any())
            m_Pivot = m_Rails.m_Rails.Last().end;

        else
            m_Pivot = m_Manipulator.position;

        m_NewRail = GameObject.Instantiate(m_RailPrefab);
        m_NewRail.transform.SetParent(gameObject.transform.parent);

        RotateRail();
    }

    private void RotateRail()
    {
        Vector3 connectingVector = m_GhostObject.transform.position - m_Pivot;

        Vector3 position = Snapping(connectingVector);
        Quaternion rotation = m_Manipulator.rotation;

        if (m_CollisionObjects.m_FocusObject != null)
            rotation = m_CollisionObjects.LookAtFocusObject(position, m_Manipulator, connectingVector);

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
    }

    private Vector3 Snapping(Vector3 connectingVector)
    {
        Color currentColor = m_DefaultColor;
        Vector3 projectedConnectingVector = connectingVector;
        bool loop = false;

        // if focus object exists
        if (m_CollisionObjects.m_FocusObject != null)
        {
            Transform focObjPose = m_CollisionObjects.m_FocusObject.transform;

            // vector from pivot to focus object
            Vector3 pivotToFocObj = focObjPose.position - m_Pivot;

            float angle = Vector3.Angle(connectingVector, pivotToFocObj);
            // if close to vector connecting pivot to focus object
            if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
            {
                projectedConnectingVector = Vector3.Project(connectingVector, pivotToFocObj);
                loop = true;
            }
            else
            {
                // vector from focus object to ghost object
                Vector3 focObjToGhostObj = m_GhostObject.transform.position - focObjPose.position;

                angle = Vector3.Angle(focObjToGhostObj, focObjPose.up);
                // if inline with focus object Y axis
                if (angle < ManipulationMode.ANGLETHRESHOLD)
                    projectedConnectingVector = focObjPose.position + Vector3.Project(focObjToGhostObj, focObjPose.up) - m_Pivot;

                angle = Vector3.Angle(focObjToGhostObj, focObjPose.right);
                // if inline with focus object X axis
                if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                    projectedConnectingVector = focObjPose.position + Vector3.Project(focObjToGhostObj, focObjPose.right) - m_Pivot;

                angle = Vector3.Angle(focObjToGhostObj, focObjPose.forward);
                // if inline with focus object Z axis
                if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                    projectedConnectingVector = focObjPose.position + Vector3.Project(focObjToGhostObj, focObjPose.forward) - m_Pivot;

                //if not already snapping
                if(projectedConnectingVector == connectingVector)
                {
                    angle = Vector3.Angle(connectingVector, focObjPose.up);
                    // if close to focus object Y axis
                    if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                        projectedConnectingVector = Vector3.Project(connectingVector, focObjPose.up);

                    angle = Vector3.Angle(connectingVector, focObjPose.right);
                    // if close to focus object X axis
                    if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                        projectedConnectingVector = Vector3.Project(connectingVector, focObjPose.right);

                    angle = Vector3.Angle(connectingVector, focObjPose.forward);
                    // if close to focus object Z axis
                    if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                        projectedConnectingVector = Vector3.Project(connectingVector, focObjPose.forward);
                }
            }
        }

        if (projectedConnectingVector != connectingVector)
            currentColor = m_FocusObjectColor;
        else
        {
            // if close to starting pivot
            if (m_Rails.m_Rails.Any() && (m_GhostObject.transform.position - m_Rails.m_Rails[0].start).magnitude < ManipulationMode.DISTANCETHRESHOLD)
            {
                projectedConnectingVector = m_Rails.m_Rails[0].start - m_Pivot;
                currentColor = m_FocusObjectColor;
                loop = true;
            }
            else
            {
                float angle = Vector3.Angle(connectingVector, Vector3.up);
                // if close to world Y axis
                if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
                {
                    projectedConnectingVector = Vector3.Project(connectingVector, Vector3.up);
                    currentColor = m_Y_AxisColor;
                }

                // if close too world XZ plane
                if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
                {
                    projectedConnectingVector = Vector3.ProjectOnPlane(connectingVector, Vector3.up);
                    currentColor = m_XZ_PlaneColor;
                }
            }
        }

        // if color changed
        if(currentColor != m_NewRail.GetComponent<Renderer>().material.color)
        {
            m_NewRail.GetComponent<Renderer>().material.color = currentColor;

            if(loop)
                m_RailMat.color = m_FocusObjectColor;
            else
                m_RailMat.color = m_DefaultColor;
        }

        m_NewRail.transform.SetPositionAndRotation(GetPosition(projectedConnectingVector), GetRotation(projectedConnectingVector));
        m_NewRail.transform.localScale = GetScale(projectedConnectingVector);

        return m_Pivot + projectedConnectingVector;
    }

    private Vector3 GetPosition(Vector3 connectingVector)
    {
        return m_Pivot + connectingVector * 0.5f;
    }

    private Quaternion GetRotation(Vector3 connectingVector)
    {
        return Quaternion.FromToRotation(Vector3.up, connectingVector);
    }

    private Vector3 GetScale(Vector3 connectingVector)
    {
        return new Vector3(0.0025f, connectingVector.magnitude * 0.5f, 0.0025f);
    }

    private void TriggerReleased()
    {
        if (m_NewRail != null)
        {
            m_RailMat.color = m_DefaultColor;
            m_NewRail.GetComponent<Renderer>().material = m_RailMat;
            m_Rails.AddRail(m_NewRail);

            Vector3 poseOffset = m_Manipulator.Find("Pose").transform.position - m_Manipulator.position;
            StartCoroutine(RequestTrajectories(poseOffset, m_NewRail.transform));

            Destroy(m_GhostObject);
            m_NewRail = null;
            isInteracting = false;
            m_Pivot = Vector3.zero;
            m_InteractingHand = null;
        }
    }

    IEnumerator RequestTrajectories(Vector3 poseOffset, Transform rail)
    {
        locked = true;

        float stepSize = 0.05f;
        Vector3 step = rail.transform.up * stepSize;

        Vector3 railStart = rail.position - rail.up * rail.localScale.y;
        Vector3 railEnd = rail.position + rail.up * rail.localScale.y;

        Vector3 trajStartPos = railStart;
        Vector3 trajEndPos = trajStartPos + step;

        while ((trajEndPos - railStart).magnitude < rail.localScale.y)
        {
            m_PlanningRobot.RequestTrajectory(trajStartPos + poseOffset, trajEndPos + poseOffset, false);

            trajStartPos = trajEndPos;
            trajEndPos += step;

            yield return new WaitForSeconds(0.08f);
        }
        m_PlanningRobot.RequestTrajectory(trajStartPos + poseOffset, railEnd + poseOffset, true);

        locked = false;
    }

    public void RemoveAllRails()
    {
        m_Rails.RemoveAllRails();
    }
}