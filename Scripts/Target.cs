using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Target : MonoBehaviour
{
    private Vector2 m_Position = new();
    private Material m_Material = null;
    private Transform m_Barrel = null;

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

    private void Update()
    {
        if (m_Barrel != null)
        {
            if (Vector2.Distance(m_Position, new(m_Barrel.position.x, m_Barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
                WithinBounds();
            else if (Vector2.Distance(m_Position, new(m_Barrel.position.x, m_Barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD * 2)
                CloseToBounds();
            else
                FarFromBounds();
        }
        else if (m_Material.color != m_FarFromBounds)
            FarFromBounds();
    }

    private void WithinBounds()
    {
        m_Material.color = m_WithinBounds;
    }

    private void CloseToBounds()
    {
        m_Material.color = m_CloseToBounds;
    }

    private void FarFromBounds()
    {
        m_Material.color = m_FarFromBounds;
    }

    public void SetPosition(Vector3 position)
    {
        gameObject.transform.position = position;
        m_Position = new(position.x, position.z);
    }

    public bool CheckDistance(Transform barrel)
    {
        if (m_Barrel != barrel)
            m_Barrel = barrel;

        return IsInBounds(barrel);
    }

    public bool IsInBounds(Transform barrel)
    {
        if (barrel == null)
            return false;

        if (Vector2.Distance(m_Position, new(barrel.position.x, barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
            return true;
        else
            return false;
    }
}