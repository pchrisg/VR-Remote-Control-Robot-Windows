using UnityEngine;

public class CollisionHandling : MonoBehaviour
{
    [HideInInspector] public Material m_OriginalMat = null;
    [HideInInspector] public Material m_CollidingMat = null;
    [HideInInspector] public Material m_AttachedMat = null;
    [HideInInspector] public Material m_FocusObjectMat = null;

    [HideInInspector] public bool m_isAttachable = false;
    [HideInInspector] public bool m_isAttached = false;

    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;
    private CollisionObjects m_CollisionObjects = null;

    private Collider[] m_ManipulatorColliders = null;
    private string[] m_FingerLinkNames = {
        "finger_middle_link_0",
        "finger_1_link_0",
        "finger_2_link_0"};
    private Collider[] m_FingerMColliders = null;
    private Collider[] m_Finger1Colliders = null;
    private Collider[] m_Finger2Colliders = null;

    private bool isCreating = false;
    private int fingerMTouching = 0;
    private int finger1Touching = 0;
    private int finger2Touching = 0;

    private Vector3 m_ReleasePosition = Vector3.zero;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        

        m_ManipulatorColliders = m_Manipulator.transform.Find("palm").GetComponentsInChildren<Collider>();
        GameObject robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_FingerMColliders = robotiq.transform.Find(m_FingerLinkNames[0]).GetComponentsInChildren<Collider>();
        m_Finger1Colliders = robotiq.transform.Find(m_FingerLinkNames[1]).GetComponentsInChildren<Collider>();
        m_Finger2Colliders = robotiq.transform.Find(m_FingerLinkNames[2]).GetComponentsInChildren<Collider>();
    }

    private void OnDestroy()
    {
        gameObject.GetComponent<Renderer>().material = m_OriginalMat;
    }

    private void Update()
    {
        if(m_ReleasePosition != Vector3.zero)
        {
            if (gameObject.transform.position == m_ReleasePosition)
            {
                gameObject.GetComponent<CollisionObject>().AddCollisionObject();
                m_ReleasePosition = Vector3.zero;
            }
            else
                m_ReleasePosition = gameObject.transform.position;
        }

        if (isCreating != m_CollisionObjects.isCreating)
        {
            if (!m_CollisionObjects.isCreating)
                gameObject.GetComponent<Renderer>().material = m_OriginalMat;

            else
            {
                if (m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_AttachedMat;

                if (!m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_CollidingMat;
            }
            isCreating = m_CollisionObjects.isCreating;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_isAttached)
        {
            bool otherIsEndEffector = false;
            foreach (var collider in m_ManipulatorColliders)
            {
                if (other == collider)
                {
                    otherIsEndEffector = true;
                    break;
                }
            }

            if (otherIsEndEffector)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();

                renderer.material = m_CollidingMat;
                m_Manipulator.Colliding(true);
            }
        }

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

        if (m_GripperControl.isGripping && m_isAttachable && !m_isAttached)
        {
            if (fingerMTouching > 0 && (finger1Touching > 0 || finger2Touching > 0))
            {
                m_isAttached = true;

                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_AttachedMat;

                m_Manipulator.Colliding(false);
                m_GripperControl.Attach();

                if (gameObject == m_CollisionObjects.m_FocusObject)
                {
                    m_CollisionObjects.SetFocusObject(null);
                }

                gameObject.GetComponent<CollisionObject>().RemoveCollisionObject();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!m_isAttached)
        {
            bool otherIsEndEffector = false;

            foreach (var collider in m_ManipulatorColliders)
            {
                if (other == collider)
                {
                    otherIsEndEffector = true;
                    break;
                }
            }

            if (otherIsEndEffector)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = gameObject == m_CollisionObjects.m_FocusObject ? m_FocusObjectMat : m_OriginalMat;
                m_Manipulator.Colliding(false);
            }
        }

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

        if (m_isAttached)
        {
            if (fingerMTouching == 0)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjects.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;

                m_GripperControl.Detach();

                m_ReleasePosition = gameObject.transform.position;
                m_isAttached = false;
            }
        }
    }

    public void SetFocusObject(bool isFocusObj)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (isFocusObj)
            renderer.material = m_FocusObjectMat;
        else
            renderer.material = m_OriginalMat;
    }
}