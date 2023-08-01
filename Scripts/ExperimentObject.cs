using UnityEngine;

public class ExperimentObject : MonoBehaviour
{
    private readonly string[] m_FingerLinkNames = {
        "finger_middle_link_0",
        "finger_1_link_0",
        "finger_2_link_0"};
    private Collider[] m_FingerMColliders = null;
    private Collider[] m_Finger1Colliders = null;
    private Collider[] m_Finger2Colliders = null;

    private int fingerMTouching = 0;
    private int finger1Touching = 0;
    private int finger2Touching = 0;

    private Vector3 m_ReleasePosition = Vector3.zero;

    [HideInInspector] public bool isMoving = false;

    private void Awake()
    {
        GameObject robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_FingerMColliders = robotiq.transform.Find(m_FingerLinkNames[0]).GetComponentsInChildren<Collider>();
        m_Finger1Colliders = robotiq.transform.Find(m_FingerLinkNames[1]).GetComponentsInChildren<Collider>();
        m_Finger2Colliders = robotiq.transform.Find(m_FingerLinkNames[2]).GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (isMoving && fingerMTouching == 0 && finger1Touching == 0 && finger2Touching == 0)
        {
            if (gameObject.transform.position == m_ReleasePosition)
            {
                m_ReleasePosition = Vector3.zero;
                isMoving = false;
            }
            else
                m_ReleasePosition = gameObject.transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (var collider in m_FingerMColliders)
        {
            if (other == collider)
                fingerMTouching++;
        }

        foreach (var collider in m_Finger1Colliders)
        {
            if (other == collider)
                finger1Touching++;
        }

        foreach (var collider in m_Finger2Colliders)
        {
            if (other == collider)
                finger2Touching++;
        }

        if (fingerMTouching != 0 || finger1Touching != 0 || finger2Touching != 0)
            isMoving = true;
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (var collider in m_FingerMColliders)
        {
            if (other == collider)
                fingerMTouching--;
        }

        foreach (var collider in m_Finger1Colliders)
        {
            if (other == collider)
                finger1Touching--;
        }

        foreach (var collider in m_Finger2Colliders)
        {
            if (other == collider)
                finger2Touching--;
        }

        if (fingerMTouching == 0 && finger1Touching == 0 && finger2Touching == 0)
            m_ReleasePosition = gameObject.transform.position;
    }
}
