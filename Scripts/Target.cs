using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    Material m_Material = null;

    private Color m_WithinBounds = new(1.0f, 1.0f, 0.0f, 0.3f);
    private Color m_CloseToBounds = new(1.0f, 1.0f, 1.0f, 0.3f);
    private Color m_FarFromBounds = new(0.0f, 0.0f, 0.0f, 0.0f);

    private void Awake()
    {
        m_Material = gameObject.GetComponent<Renderer>().material;
    }

    private void OnDestroy()
    {
        m_Material.color = m_FarFromBounds;
    }

    public void WithinBounds()
    {
        m_Material.color = m_WithinBounds;
    }

    public void CloseToBounds()
    {
        m_Material.color = m_CloseToBounds;
    }

    public void FarFromBounds()
    {
        m_Material.color = m_FarFromBounds;
    }
}