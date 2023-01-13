using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class RailCreator : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private GameObject m_Manipulator = null;
    [SerializeField] private ManipulationMode m_ManipulationMode;

    [Header("Prefab")]
    [SerializeField] private GameObject m_RailPrefab = null;

    private Rails m_Rails = null;

    private GameObject m_Rail = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private Hand m_InteractingHand = null;
    private Vector3 m_Pivot = Vector3.zero;
    private const float THRESHOLD = 0.05f;

    private void Awake()
    {
        m_Rails = gameObject.transform.parent.GetComponent<Rails>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Grip.onStateDown += SetInteractingHand;
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
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
        m_Grip.onStateDown -= SetInteractingHand;
        m_Trigger.onStateDown -= SetRail;
    }

    private void Update()
    {
        if(m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            if (m_Grip.GetState(m_RightHand.handType) || m_Grip.GetState(m_LeftHand.handType))
            {
                MakeRail();
            }
            else
                m_InteractingHand = null;
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void SetInteractingHand(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(m_InteractingHand == null)
        {
            if (m_Grip.GetState(m_RightHand.handType))
                m_InteractingHand = m_RightHand;
            else
                m_InteractingHand = m_LeftHand;
        }

        if (m_InteractingHand == m_RightHand)
            m_InteractingHand = m_LeftHand;
        else
            m_InteractingHand = m_RightHand;
    }

    private void SetRail(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_Rail != null)
        {
            m_Rail = null;
            m_Pivot = Vector3.zero;
        }
    }

    private void MakeRail()
    {
        Transform index = m_InteractingHand.skeleton.indexTip;

        if(m_Pivot == Vector3.zero)
        {
            Transform lastChild = m_Rails.GetLastChild();

            if (lastChild.position == gameObject.GetComponentInParent<Transform>().position)
                m_Pivot = m_Manipulator.transform.position;
            else
                m_Pivot = lastChild.position + (lastChild.up * lastChild.localScale.y);
        }

        Vector3 connectingVector = index.position - m_Pivot;
        float distance = Vector3.Distance(m_Pivot, index.position);

        if (m_Rail == null)
        {
            m_Rail = GameObject.Instantiate(m_RailPrefab);
            m_Rail.transform.SetParent(gameObject.transform.parent);
        }

        m_Rail.transform.position = m_Pivot + connectingVector / 2.0f;
        m_Rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, connectingVector);
        m_Rail.transform.localScale = new Vector3(0.0025f, distance/2, 0.0025f);

        Snapping(connectingVector);
    }

    private void Snapping(Vector3 connectingVector)
    {
        float CosAngle = Vector3.Dot(Vector3.Normalize(connectingVector), gameObject.transform.up);
        if (Mathf.Abs(CosAngle) < THRESHOLD)
        {
            Vector3 index = m_Pivot + connectingVector;
            Vector3 projectedPoint = index - (CosAngle * connectingVector.magnitude) * gameObject.transform.up;
            Vector3 projectedConnectingVector = projectedPoint - m_Pivot;

            m_Rail.transform.position = m_Pivot + projectedConnectingVector / 2.0f;
            m_Rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, projectedConnectingVector);
        }

        if (1 - Mathf.Abs(CosAngle) < THRESHOLD)
        {
            Vector3 projectedConnectingVector = (CosAngle * connectingVector.magnitude) * gameObject.transform.up;

            m_Rail.transform.position = m_Pivot + projectedConnectingVector / 2.0f;
            m_Rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, projectedConnectingVector);
        }
    }
}