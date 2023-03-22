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

    private GameObject m_EndEffector = null;
    private GameObject m_Rail = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;

    private Vector3 m_Pivot = Vector3.zero;
    private readonly float m_AngleThreshold = 5.0f;
    private readonly float m_DistanceThreshold = 0.05f;

    private void Awake()
    {
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector");
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Rails = gameObject.transform.parent.GetComponent<Rails>();

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trigger.onStateDown += SetRail;

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void OnDisable()
    {
        if(m_Rail != null)
        {
            Destroy(m_Rail);
            m_Rail = null;
            m_Pivot = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= SetRail;
    }

    private void Update()
    {
        if(m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            if (m_InteractingHand == null)
            {
                if (m_Grip.GetState(m_RightHand.handType))
                    m_InteractingHand = m_RightHand;
                else if (m_Grip.GetState(m_LeftHand.handType))
                    m_InteractingHand = m_LeftHand;
            }
            else
            {
                if (m_Grip.GetState(m_InteractingHand.handType))
                    MakeRail();

                else
                    m_InteractingHand = null;
            }
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void MakeRail()
    {
        Transform indexFinger = m_InteractingHand.skeleton.indexTip;

        if(m_Pivot == Vector3.zero)
        {
            Transform lastChild = m_Rails.GetLastChild();

            if (lastChild.position == gameObject.transform.parent.position)
                m_Pivot = m_EndEffector.transform.position;
            else
                m_Pivot = lastChild.position + (lastChild.up.normalized * lastChild.localScale.y);
        }

        Vector3 connectingVector = indexFinger.position - m_Pivot;

        if (m_Rail == null)
        {
            m_Rail = GameObject.Instantiate(m_RailPrefab);
            m_Rail.transform.SetParent(gameObject.transform.parent);
        }

        m_Rail.transform.SetPositionAndRotation(m_Pivot + connectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, connectingVector));
        m_Rail.transform.localScale = new Vector3(0.0025f, connectingVector.magnitude * 0.5f, 0.0025f);

        Snapping(indexFinger, connectingVector);
    }

    private void Snapping(Transform indexFinger, Vector3 connectingVector)
    {
        m_RailMat.color = new Color(200.0f, 200.0f, 200.0f);
        m_Rail.GetComponent<Renderer>().material.color = new Color(200.0f, 200.0f, 200.0f);
        float angle = Mathf.Acos(Vector3.Dot(connectingVector.normalized, Vector3.up.normalized)) * Mathf.Rad2Deg;
        if (Mathf.Abs(90.0f - angle) < m_AngleThreshold)
        {
            Vector3 projectedConnectingVector = Vector3.ProjectOnPlane(connectingVector, Vector3.up);
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 0.0f, 255.0f);
        }

        if (angle < m_AngleThreshold || Mathf.Abs(180.0f - angle) < m_AngleThreshold)
        {
            Vector3 projectedConnectingVector = Vector3.Project(connectingVector, Vector3.up);
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.GetComponent<Renderer>().material.color = new Color(0.0f, 255.0f, 0.0f);
        }

        if (m_Rails.rails.Length > 1 && (indexFinger.position - m_Rails.rails[0].start).magnitude < m_DistanceThreshold)
        {
            Vector3 projectedConnectingVector = m_Rails.rails[0].start - m_Pivot;
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.transform.localScale = new Vector3(0.0025f, projectedConnectingVector.magnitude * 0.5f, 0.0025f);
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 255.0f, 0.0f);
            m_RailMat.color = new Color(255.0f, 255.0f, 0.0f);
        }

        if (m_CollisionObjects.m_FocusObject != null && (indexFinger.position - m_CollisionObjects.m_FocusObject.transform.position).magnitude < m_DistanceThreshold)
        {
            Vector3 projectedConnectingVector = m_CollisionObjects.m_FocusObject.transform.position - m_Pivot;
            m_Rail.transform.SetPositionAndRotation(m_Pivot + projectedConnectingVector * 0.5f, Quaternion.FromToRotation(Vector3.up, projectedConnectingVector));
            m_Rail.transform.localScale = new Vector3(0.0025f, projectedConnectingVector.magnitude * 0.5f, 0.0025f);
            m_Rail.GetComponent<Renderer>().material.color = new Color(255.0f, 255.0f, 0.0f);
            m_RailMat.color = new Color(255.0f, 255.0f, 0.0f);
        }
    }

    private void SetRail(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            if (m_Rail != null)
            {
                m_RailMat.color = new Color(200.0f, 200.0f, 200.0f);
                m_Rails.AddRail(m_Rail);

                m_Rail.GetComponent<Renderer>().material = m_RailMat;
                m_Rail = null;
                m_Pivot = Vector3.zero;
            }
            else if (m_Rails.GetLastChild() != m_Rails.GetComponent<Transform>())
            {
                GameObject lastChild = m_Rails.GetLastChild().gameObject;
                Destroy(lastChild);
                m_Rails.RemoveLastRail();
            }
        }
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