using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class RailCreator : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_RailPrefab = null;

    [Header("Material")]
    [SerializeField] private Material m_RailMat;

    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;
    private Rails m_Rails = null;

    private Transform m_Manipulator = null;
    private GameObject m_Rail = null;

    private SteamVR_Action_Boolean m_Trigger = null;

    public Hand m_InteractingHand = null;
    public bool isInteracting = false;

    private Vector3 m_Pivot = Vector3.zero;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").transform;
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Rails = gameObject.transform.parent.GetComponent<Rails>();

        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
    }

    private void OnDisable()
    {
        if(m_Rail != null)
        {
            Destroy(m_Rail);
            m_Rail = null;
            m_Pivot = Vector3.zero;
        }
        m_Manipulator.GetComponent<Manipulator>().ResetPosition();
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            m_InteractingHand = m_Manipulator.GetComponent<DirectManipulation>().m_InteractingHand;

            if (!isInteracting)
            {
                if( m_Trigger.GetStateDown(Player.instance.rightHand.handType) || 
                    m_Trigger.GetStateDown(Player.instance.leftHand.handType))
                    TriggerGrabbed();
            }

            if (isInteracting)
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
        if (m_InteractingHand != null && m_Trigger.GetState(m_InteractingHand.handType))
        {
            if (m_Rail == null)
                MakeRail();

            isInteracting = true;
        }

        else if(m_Rails.GetLastChild() != m_Rails.transform)
        {
            Vector3 position = m_Manipulator.position;
            Quaternion rotation = m_Manipulator.rotation;

            Transform lastChild = m_Rails.GetLastChild();

            position = lastChild.position - (lastChild.up.normalized * lastChild.localScale.y);

            Destroy(lastChild.gameObject);
            m_Rails.RemoveLastRail();

            m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(position, rotation);
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void MakeRail()
    {
        Transform lastChild = m_Rails.GetLastChild();

        if (lastChild.position == gameObject.transform.parent.position)
            m_Pivot = m_Manipulator.position;
        else
            m_Pivot = lastChild.position + (lastChild.up.normalized * lastChild.localScale.y);

        m_Rail = GameObject.Instantiate(m_RailPrefab);
        m_Rail.transform.SetParent(gameObject.transform.parent);

        RotateRail();
    }

    private void RotateRail()
    {
        Vector3 connectingVector = m_Manipulator.position - m_Pivot;

        m_Rail.transform.SetPositionAndRotation(m_Pivot + connectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, connectingVector));
        m_Rail.transform.localScale = new Vector3(0.0025f, connectingVector.magnitude * 0.5f, 0.0025f);

        Snapping();
    }

    private void Snapping()
    {
        Vector3 connectingVector = m_Manipulator.position - m_Pivot;

        m_RailMat.color = new Color(200.0f, 200.0f, 200.0f);
        m_Rail.GetComponent<Renderer>().material.color = new Color(200.0f, 200.0f, 200.0f);
        float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        if (Mathf.Abs(90.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            Vector3 projectedConnectingVector = Vector3.ProjectOnPlane(connectingVector, Vector3.up);
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 0.0f, 255.0f);
        }

        if (angle < ManipulationMode.ANGLETHRESHOLD ||
            Mathf.Abs(180.0f - angle) < ManipulationMode.ANGLETHRESHOLD)
        {
            Vector3 projectedConnectingVector = Vector3.Project(connectingVector, Vector3.up);
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.GetComponent<Renderer>().material.color = new Color(0.0f, 255.0f, 0.0f);
        }

        if (m_Rails.rails.Length > 1 &&
            (m_Manipulator.position - m_Rails.rails[0].start).magnitude < ManipulationMode.DISTANCETHRESHOLD)
        {
            Vector3 projectedConnectingVector = m_Rails.rails[0].start - m_Pivot;
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.transform.localScale = new Vector3(0.0025f, projectedConnectingVector.magnitude * 0.5f, 0.0025f);
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 255.0f, 0.0f);
            m_RailMat.color = new Color(255.0f, 255.0f, 0.0f);
        }

        if (m_CollisionObjects.m_FocusObject != null && 
            (m_Manipulator.position - m_CollisionObjects.m_FocusObject.transform.position).magnitude < ManipulationMode.DISTANCETHRESHOLD)
        {
            Vector3 projectedConnectingVector = m_CollisionObjects.m_FocusObject.transform.position - m_Pivot;
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.transform.localScale = new Vector3(0.0025f, projectedConnectingVector.magnitude * 0.5f, 0.0025f);
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 255.0f, 0.0f);
            m_RailMat.color = new Color(255.0f, 255.0f, 0.0f);
        }
    }

    private void TriggerReleased()
    {
        if (m_Rail != null)
        {
            m_RailMat.color = new Color(200.0f, 200.0f, 200.0f);
            m_Rails.AddRail(m_Rail);

            m_Rail.GetComponent<Renderer>().material = m_RailMat;
            m_Rail = null;
            m_Pivot = Vector3.zero;
        }

        isInteracting = false;
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