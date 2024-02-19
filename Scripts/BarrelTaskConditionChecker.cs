using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BarrelTaskConditionChecker : MonoBehaviour
{
    private ExperimentManager m_ExperimentManager = null;

    private List<GameObject> m_Barrels = new List<GameObject>();
    private List<GameObject> m_PlacedBarrels = new List<GameObject>();

    private readonly int m_NumberOfBarrels = 4;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
    }

    private void Update()
    {
        if (m_Barrels.Any())
        {
            if (!m_PlacedBarrels.Contains(m_Barrels.Last()))
            {
                m_PlacedBarrels.Add(m_Barrels.Last());
                StartCoroutine(AddBarrel(m_PlacedBarrels.Last()));
            }

            if (m_Barrels.Count == m_NumberOfBarrels)
                StartCoroutine(EndExperiment(m_Barrels.Last()));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Moveable")
        {
            if (m_Barrels.Contains(other.gameObject))
                return;

            m_Barrels.Add(other.gameObject);
        }
    }

    IEnumerator AddBarrel(GameObject barrel)
    {
        yield return new WaitUntil(() => barrel.GetComponent<ExperimentObject>().isMoving == false);

        m_ExperimentManager.AddPlacedObject(barrel.name, 0.0f, 0.0f);
    }

    IEnumerator EndExperiment(GameObject barrel)
    {
        yield return new WaitUntil(() => barrel.GetComponent<ExperimentObject>().isMoving == false);
        yield return new WaitForSeconds(1.0f);

        m_ExperimentManager.SaveData();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Moveable")
        {
            if (m_Barrels.Contains(other.gameObject))
                m_Barrels.Remove(other.gameObject);
        }
    }
}
