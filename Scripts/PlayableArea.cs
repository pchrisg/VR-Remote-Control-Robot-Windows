using System;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PlayableArea : MonoBehaviour
{
    [Header("Materials")]
    public Material m_InBoundsMaterial = null;
    public Material m_OutOfBoundsMaterial = null;
    public Material m_CollidingMaterial = null;
    public Material m_EludingMaterial = null;

    private Collider[] m_EndEffector = null;
    private Collider[] m_UR5 = null;
    public bool m_IsPlayableArea = false;
    [HideInInspector] public bool m_isDeleteAble = false;
    [HideInInspector] public CollisionObjectCreator m_ColObjCreator = null;

    private static readonly string[] m_ChildNames = {
        "HandColliderRight(Clone)/fingers/finger_index_2_r",
        "HandColliderLeft(Clone)/fingers/finger_index_2_r" };
    private Collider m_RightIndex = null;
    private Collider m_LeftIndex = null;

    private void Awake()
    {
        m_EndEffector = GameObject.FindGameObjectWithTag("EndEffector").transform.Find("palm").GetComponentsInChildren<Collider>();
        m_UR5 = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        Invoke("GetFingerColliders", 0.5f);
    }

    private void GetFingerColliders()
    {
        m_RightIndex = Player.instance.transform.Find(m_ChildNames[0]).GetComponent<Collider>();
        m_LeftIndex = Player.instance.transform.Find(m_ChildNames[1]).GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        bool change = false;

        foreach (var collider in m_EndEffector)
        {
            if (other == collider)
            {
                change = true;
                break;
            }
        }

        if(!change)
        {
            foreach (Collider collider in m_UR5)
            {
                if (other == collider)
                {
                    change = true;
                    break;
                }
            }
        }

        if(change)
        {
            if (!m_IsPlayableArea)
            {
                renderer.material = m_CollidingMaterial;
                m_EndEffector[0].GetComponent<AudioSource>().Play();
            }
            else
                renderer.material = m_InBoundsMaterial;
        }

        if((other == m_RightIndex || other == m_LeftIndex) && m_isDeleteAble && m_ColObjCreator.m_KillFinger != String.Empty)
        {
            if (other == m_RightIndex && m_ColObjCreator.m_KillFinger == "right")
                GameObject.Destroy(gameObject);

            if (other == m_LeftIndex && m_ColObjCreator.m_KillFinger == "left")
                GameObject.Destroy(gameObject);
        }
    }

    /*private void OnTriggerStay(Collider other)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (!m_IsPlayableArea && renderer.material != m_CollidingMaterial)
        {
            bool change = false;

            if (other == m_Manipulator)
                change = true;

            else
            {
                foreach (Collider collider in m_UR5)
                {
                    if (other == collider)
                    {
                        change = true;
                        break;
                    }
                }
            }

            if (change)
            {
                if (!m_IsPlayableArea)
                    renderer.material = m_CollidingMaterial;
            }
        }
    }*/

    private void OnTriggerExit(Collider other)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        bool change = false;

        foreach (var collider in m_EndEffector)
        {
            if (other == collider)
            {
                change = true;
                break;
            }
        }

        if(!change)
        {
            foreach (Collider collider in m_UR5)
            {
                if (other == collider)
                {
                    change = true;
                    break;
                }
            }
        }

        if (change)
        {
            if (!m_IsPlayableArea)
                renderer.material = m_EludingMaterial;
            else
            {
                renderer.material = m_OutOfBoundsMaterial;
                m_EndEffector[0].GetComponent<AudioSource>().Play();
            }
        }
    }
}