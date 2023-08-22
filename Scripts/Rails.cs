using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Rails : MonoBehaviour
{
    public struct Rail
    {
        public GameObject rail;
        public Vector3 start;
        public Vector3 end;
    };
    public List<Rail> m_Rails = new List<Rail>();

    private void OnDisable()
    {
        foreach (Transform child in gameObject.GetComponentInChildren<Transform>())
        {
            if (child.GetComponent<CapsuleCollider>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        RemoveAllRails();
    }

    public void AddRail(GameObject rail)
    {
        Vector3 offset = rail.transform.up.normalized * rail.transform.localScale.y;

        Rail newRail = new Rail
        {
            rail = rail, 
            start = rail.transform.position - offset, 
            end = rail.transform.position + offset
        };

        m_Rails.Add(newRail);
    }

    public void RemoveLastRail()
    {
        if(m_Rails.Any())
        {
            Destroy(m_Rails.Last().rail);

            m_Rails.Remove(m_Rails.Last());
        }
    }

    public void RemoveAllRails()
    {
        while (m_Rails.Any())
            RemoveLastRail();
    }
}