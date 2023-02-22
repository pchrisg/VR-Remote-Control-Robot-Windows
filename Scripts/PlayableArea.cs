using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayableArea : MonoBehaviour
{
    private Collider m_Manipulator = null;
    public bool m_IsPlayableArea = false;

    [Header("Materials")]
    public Material m_InBoundsMaterial = null;
    public Material m_OutOfBoundsMaterial = null;
    public Material m_CollidingMaterial = null;
    public Material m_EludingMaterial = null;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponentInChildren<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (other == m_Manipulator)
        {
            if (!m_IsPlayableArea)
                renderer.material = m_CollidingMaterial;
            else
                renderer.material = m_InBoundsMaterial;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (other == m_Manipulator)
        {
            if (!m_IsPlayableArea)
                renderer.material = m_EludingMaterial;
            else
                renderer.material = m_OutOfBoundsMaterial;
        }
    }
}