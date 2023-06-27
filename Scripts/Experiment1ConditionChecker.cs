using System.Collections.Generic;
using UnityEngine;

public class Experiment1ConditionChecker : MonoBehaviour
{
    private List<GameObject> m_Barrels = new List<GameObject>();
    
    [HideInInspector] public int m_NumberOfBarrels = 0;
    [HideInInspector] public bool allObjectsInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Moveable")
        {
            if (m_Barrels.Contains(other.gameObject))
                return;

            m_Barrels.Add(other.gameObject);

            if (m_Barrels.Count == m_NumberOfBarrels)
                allObjectsInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Moveable")
        {
            if (m_Barrels.Contains(other.gameObject))
                m_Barrels.Remove(other.gameObject);
            else
                print("Something very wrong");
        }
    }
}
