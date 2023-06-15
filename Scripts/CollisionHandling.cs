using UnityEngine;
using ManipulationOptions;

public class CollisionHandling : MonoBehaviour
{
    [Header("Materials")]
    public Material m_OriginalMat = null;
    public Material m_CollidingMat = null;
    public Material m_AttachedMat = null;
    public Material m_FocusObjectMat = null;

    private ManipulationMode m_ManipulationModeScript = null;
    private Gripper m_GripperScript = null;
    private EndEffector m_EndEffectorScript = null;
    private CollisionObjects m_CollisionObjectsScript = null;

    [HideInInspector] public bool m_isAttachable = false;
    [HideInInspector] public bool m_isAttached = false;
    
    private Collider[] m_EndEffectorColliders = null;
    private string[] m_FingerLinkNames = {
        "finger_middle_link_0",
        "finger_1_link_0",
        "finger_2_link_0"};
    private Collider[] m_FingerMColliders = null;
    private Collider[] m_Finger1Colliders = null;
    private Collider[] m_Finger2Colliders = null;

    private bool isCreating = false;
    public int fingerMTouching = 0;
    public int finger1Touching = 0;
    public int finger2Touching = 0;

    private void Awake()
    {
        m_ManipulationModeScript = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_GripperScript = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<Gripper>();
        m_EndEffectorScript = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<EndEffector>();
        m_CollisionObjectsScript = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        

        m_EndEffectorColliders = m_GripperScript.transform.Find("palm").GetComponentsInChildren<Collider>();
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
        if (isCreating != m_CollisionObjectsScript.isCreating)
        {
            if (!m_CollisionObjectsScript.isCreating)
                gameObject.GetComponent<Renderer>().material = m_OriginalMat;

            else
            {
                if (m_ManipulationModeScript.mode == Mode.COLOBJCREATOR && !m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_CollidingMat;

                else if (m_ManipulationModeScript.mode == Mode.ATTOBJCREATOR && m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_AttachedMat;
            }
            isCreating = m_CollisionObjectsScript.isCreating;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_isAttached)
        {
            bool otherIsEndEffector = false;
            foreach (var collider in m_EndEffectorColliders)
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
                m_EndEffectorScript.Colliding();
                m_GripperScript.Collide();
            }
        }

        if (m_GripperScript.isGripping && m_isAttachable && !m_isAttached)
        {
            foreach (var collider in m_FingerMColliders)
            {
                if(other == collider)
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

            if (fingerMTouching > 0 && finger1Touching > 0 && finger2Touching > 0)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_AttachedMat;
                m_GripperScript.SetObjGripSize();

                gameObject.GetComponent<CollisionObject>().AttachCollisionObject();

                m_isAttached = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!m_isAttached)
        {
            bool otherIsEndEffector = false;

            foreach (var collider in m_EndEffectorColliders)
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
                renderer.material = m_CollisionObjectsScript.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;
                m_EndEffectorScript.NotColliding();
            }
        }

        if (m_isAttached)
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

            if (fingerMTouching == 0 || finger1Touching == 0 || finger2Touching == 0)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjectsScript.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;

                m_GripperScript.ResetAttObjSize();

                gameObject.GetComponent<CollisionObject>().DetachCollisionObject();
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