using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class OutOfBounds : MonoBehaviour
{
    [SerializeField] Collider m_Manipulator;
    [SerializeField] bool m_InsideOutOfBounds;

    [SerializeField] private Material m_InBounds;
    [SerializeField] private Material m_OutOfBounds;
    private Renderer m_Renderer;

    private void Start()
    {
        m_Renderer = this.gameObject.GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == m_Manipulator)
        {
            if (m_InsideOutOfBounds)
            {
                m_Renderer.material = m_OutOfBounds;
            }
            else
            {
                m_Renderer.material = m_InBounds;
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other == m_Manipulator)
        {
            if (m_InsideOutOfBounds)
            {
                m_Renderer.material = m_InBounds;
            }
            else
            {
                m_Renderer.material = m_OutOfBounds;
            }
        }
    }
}
