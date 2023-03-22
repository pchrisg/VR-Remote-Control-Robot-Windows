using System;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;

public class CollisionHandling : MonoBehaviour
{
    [Header("Materials")]
    public Material m_InBoundsMaterial = null;
    public Material m_OutOfBoundsMaterial = null;
    public Material m_CollidingMaterial = null;
    public Material m_EludingMaterial = null;
    public Material m_AttachedMaterial = null;
    public Material m_FocusObjectMaterial = null;

    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;

    public bool m_IsPlayableArea = false;
    [HideInInspector] public bool m_isAttachable = false;
    [HideInInspector] public bool m_isAttached = false;
    [HideInInspector] public bool m_isDeleteAble = false;

    private static readonly string[] m_FingerNames = {
        "HandColliderRight(Clone)/fingers/finger_index_2_r",
        "HandColliderLeft(Clone)/fingers/finger_index_2_r" };
    private Collider m_RightIndex = null;
    private Collider m_LeftIndex = null;
    private Collider[] m_EndEffector = null;
    private Collider[] m_UR5 = null;
    private Collider[] m_Gripper = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector").transform.Find("palm").GetComponentsInChildren<Collider>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();
        m_Gripper = GameObject.FindGameObjectWithTag("Gripper").GetComponentsInChildren<Collider>();

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    }

    private void Start()
    {
        Invoke("GetFingerColliders", 0.5f);
    }

    private void GetFingerColliders()
    {
        m_RightIndex = Player.instance.transform.Find(m_FingerNames[0]).GetComponent<Collider>();
        m_LeftIndex = Player.instance.transform.Find(m_FingerNames[1]).GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isColliding = false;

        if (m_ManipulationMode.mode == Mode.GRIPPER && m_isAttachable)
        {
            isColliding = false;

            foreach (var collider in m_Gripper)
            {
                if (other == collider)
                {
                    isColliding = true;
                    break;
                }
            }

            if (isColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_AttachedMaterial;

                m_isAttached = true;
            }
        }

        else if(!m_isAttached)
        {
            foreach (var collider in m_EndEffector)
            {
                if (other == collider)
                {
                    isColliding = true;
                    break;
                }
            }

            if (!isColliding)
            {
                foreach (Collider collider in m_UR5)
                {
                    if (other == collider)
                    {
                        isColliding = true;
                        break;
                    }
                }
            }

            if (isColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();

                if (!m_IsPlayableArea)
                {
                    renderer.material = m_CollidingMaterial;
                    GameObject.FindGameObjectWithTag("EndEffector").GetComponent<AudioSource>().Play();
                }
                else
                    renderer.material = m_InBoundsMaterial;
            }
        }

        if (m_isDeleteAble && ((m_ManipulationMode.mode == Mode.COLOBJCREATOR && !m_isAttachable) ||
                               (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && m_isAttachable)))
        {
            if ((other == m_RightIndex && m_Grip.GetState(Player.instance.rightHand.handType)) ||
                (other == m_LeftIndex && m_Grip.GetState(Player.instance.leftHand.handType)))
            {
                if (m_CollisionObjects.m_FocusObject == gameObject)
                    m_CollisionObjects.m_FocusObject = null;
                GameObject.Destroy(gameObject);
            }
        }

        if (m_isAttachable && m_ManipulationMode.mode != Mode.ATTOBJCREATOR)
        {
            if ((other == m_RightIndex && m_Grip.GetState(Player.instance.rightHand.handType)) ||
                (other == m_LeftIndex && m_Grip.GetState(Player.instance.leftHand.handType)))
            {
                if (m_CollisionObjects.m_FocusObject == null)
                {
                    m_CollisionObjects.m_FocusObject = gameObject;

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    renderer.material = m_FocusObjectMaterial;
                }

                else if (m_CollisionObjects.m_FocusObject == gameObject)
                {
                    m_CollisionObjects.m_FocusObject = null;

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    renderer.material = m_EludingMaterial;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool isntColliding = false;

        if (m_ManipulationMode.mode == Mode.GRIPPER && m_isAttached)
        {
            isntColliding = false;

            foreach (var collider in m_Gripper)
            {
                if (other == collider)
                {
                    isntColliding = true;
                    break;
                }
            }

            if (isntColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjects.m_FocusObject == gameObject ? m_FocusObjectMaterial : m_EludingMaterial;

                m_isAttached = false;
            }
        }

        else if(!m_isAttached)
        {
            foreach (var collider in m_EndEffector)
            {
                if (other == collider)
                {
                    isntColliding = true;
                    break;
                }
            }

            if (!isntColliding)
            {
                foreach (Collider collider in m_UR5)
                {
                    if (other == collider)
                    {
                        isntColliding = true;
                        break;
                    }
                }
            }

            if (isntColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();

                if (!m_IsPlayableArea)
                    renderer.material = m_CollisionObjects.m_FocusObject == gameObject ? m_FocusObjectMaterial : m_EludingMaterial;

                else
                {
                    renderer.material = m_OutOfBoundsMaterial;
                    GameObject.FindGameObjectWithTag("EndEffector").GetComponent<AudioSource>().Play();
                }
            }
        }
    }
}