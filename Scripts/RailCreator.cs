using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using System.Collections;

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
    private Transform m_ManipulatorPose = null;
    private GameObject m_NewRail = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_InteractingHand = null;
    private bool isInteracting = false;

    private GameObject m_GhostObject = null;
    private Vector3 m_Pivot = Vector3.zero;

    //Colors
    private Color m_DefaultColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    private Color m_Y_AxisColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color m_XZ_PlaneColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);
    private Color m_FocusObjectColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").transform;
        m_ManipulatorPose = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("Pose").transform;
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
        m_Manipulator.GetComponent<Manipulator>().ResetPosition();
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            m_ManipulationMode.isInteracting = isInteracting;
            if (!isInteracting)
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

        if (m_InteractingHand != null)
        {
            m_GhostObject = new GameObject("GhostObject");
            m_GhostObject.transform.SetPositionAndRotation(m_Manipulator.position, m_Manipulator.rotation);
            m_GhostObject.transform.SetParent(m_InteractingHand.transform);

            if (m_NewRail == null)
                MakeRail();

            isInteracting = true;
        }
        else if(m_Rails.GetLastChild() != m_Rails.transform)
        {
            Transform lastChild = m_Rails.GetLastChild();

            Vector3 position = position = lastChild.position - (lastChild.up.normalized * lastChild.localScale.y);
            Quaternion rotation = m_Manipulator.rotation;
            
            m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);

            Destroy(lastChild.gameObject);
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
        Transform lastChild = m_Rails.GetLastChild();

        if (lastChild.position == gameObject.transform.parent.position)
            m_Pivot = m_Manipulator.position;
        else
            m_Pivot = lastChild.position + lastChild.up.normalized * lastChild.localScale.y;

        m_NewRail = GameObject.Instantiate(m_RailPrefab);
        m_NewRail.transform.SetParent(gameObject.transform.parent);

        RotateRail();
    }

    private void RotateRail()
    {
        Vector3 connectingVector = m_GhostObject.transform.position - m_Pivot;

        Vector3 position = Snapping(connectingVector);
        Quaternion rotation = m_Manipulator.transform.rotation;

        m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
    }

    private Vector3 Snapping(Vector3 connectingVector)
    {
        Color currentColor = m_DefaultColor;
        Vector3 projectedConnectingVector = connectingVector;

        float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;

        // if close too XZ plane
        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            projectedConnectingVector = Vector3.ProjectOnPlane(connectingVector, Vector3.up);
            currentColor = m_XZ_PlaneColor;
        }
        // if close to y axis
        if (angle < ManipulationMode.ANGLETHRESHOLD ||
            Mathf.Abs(180.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            projectedConnectingVector = Vector3.Project(connectingVector, Vector3.up);
            currentColor = m_Y_AxisColor;
        }
        // if close to start
        if (m_Rails.rails.Length > 1 &&
            (m_GhostObject.transform.position - m_Rails.rails[0].start).magnitude < ManipulationMode.DISTANCETHRESHOLD)
        {
            projectedConnectingVector = m_Rails.rails[0].start - m_Pivot;
            currentColor = m_FocusObjectColor;
        }

        // if focus object exists
        if (m_CollisionObjects.m_FocusObject != null)
        {
            // if close to focus object
            if ((m_GhostObject.transform.position - m_CollisionObjects.m_FocusObject.transform.position).magnitude < ManipulationMode.DISTANCETHRESHOLD)
            {
                projectedConnectingVector = m_CollisionObjects.m_FocusObject.transform.position - m_Pivot;
                currentColor = m_FocusObjectColor;
            }
        }

        // if color changed
        if(currentColor != m_NewRail.GetComponent<Renderer>().material.color)
        {
            m_NewRail.GetComponent<Renderer>().material.color = currentColor;

            if (currentColor == m_FocusObjectColor)
                m_RailMat.color = m_FocusObjectColor;
            else if (m_RailMat.color == m_FocusObjectColor)
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

            StartCoroutine(RequestTrajectories());
        }
    }

    IEnumerator RequestTrajectories()
    {
        Vector3 poseOffset = m_ManipulatorPose.position - m_Manipulator.position;
        
        Vector3 direction = m_NewRail.transform.up;
        float stepSize = 0.05f;
        Vector3 startPosition = m_Pivot;
        Vector3 endPosition = m_Pivot + direction * stepSize;

        while ((endPosition - m_Pivot).magnitude < (m_Manipulator.position - m_Pivot).magnitude)
        {
            m_PlanningRobot.RequestTrajectory(startPosition + poseOffset, endPosition + poseOffset);

            yield return new WaitForSeconds(0.08f);

            startPosition = endPosition;
            endPosition += direction * stepSize;
        }
        m_PlanningRobot.RequestTrajectory(startPosition + poseOffset, m_Manipulator.position + poseOffset);

        GameObject.Destroy(m_GhostObject);

        m_NewRail = null;
        isInteracting = false;
        m_Pivot = Vector3.zero;
        m_InteractingHand = null;

        yield return null;
    }

    public void Clear()
    {
        Transform rails = m_Rails.GetComponent<Transform>();
        if (rails.childCount > 1)
        {
            for (int i = rails.childCount - 1; i > 0; i--)
            {
                GameObject rail = rails.GetChild(i).gameObject;
                Destroy(rail);
            }
        }
        m_Rails.RemoveAllRails();
    }
}