using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Experiment2ConditionChecker : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_DominoMat = null;
    [SerializeField] private Material m_PlacedDominoMat = null;

    private ExperimentManager m_ExperimentManager = null;

    private List<GameObject> m_Dominoes = new List<GameObject>();
    private List<GameObject> m_PlacedDominoes = new List<GameObject>();

    private readonly int m_NumberOfDominoes = 5;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        gameObject.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 1.0f, 0.4f);
    }

    private void Update()
    {
        if (m_Dominoes.Any())
        {
            if (!m_PlacedDominoes.Contains(m_Dominoes.Last()))
            {
                m_PlacedDominoes.Add(m_Dominoes.Last());
                StartCoroutine(AddDomino(m_PlacedDominoes.Last()));
            }

            if (m_Dominoes.Count == m_NumberOfDominoes)
                StartCoroutine(EndExperiment(m_Dominoes.Last()));
        }
    }

    IEnumerator AddDomino(GameObject domino)
    {
        yield return new WaitUntil(() => domino.GetComponent<ExperimentObject>().isMoving == false);

        Vector2 targetPosition = new Vector2(gameObject.transform.position.x, gameObject.transform.position.z);
        Vector2 dominoPosition = new Vector2(domino.transform.position.x, domino.transform.position.z);

        float posErr = Vector2.Distance(targetPosition, dominoPosition);
        float rotErr = Quaternion.Angle(gameObject.transform.rotation, domino.transform.rotation);

        m_ExperimentManager.AddPlacedObject(domino.name, posErr, rotErr);
    }

    IEnumerator EndExperiment(GameObject domino)
    {
        yield return new WaitUntil(() => domino.GetComponent<ExperimentObject>().isMoving == false);
        yield return new WaitForSeconds(1.0f);

        m_ExperimentManager.SaveData();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Moveable")
        {
            if (m_Dominoes.Contains(other.gameObject))
                return;

            m_Dominoes.Add(other.gameObject);

            other.GetComponent<Renderer>().material = m_PlacedDominoMat;
            if (other.GetComponent<CollisionHandling>() != null && other.GetComponent<CollisionHandling>().m_isAttachable == true)
                other.GetComponent<CollisionHandling>().m_OriginalMat = m_PlacedDominoMat;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Moveable")
        {
            if (m_Dominoes.Contains(other.gameObject))
                m_Dominoes.Remove(other.gameObject);

            other.GetComponent<Renderer>().material = m_DominoMat;
            if (other.GetComponent<CollisionHandling>() != null && other.GetComponent<CollisionHandling>().m_isAttachable == true)
                other.GetComponent<CollisionHandling>().m_OriginalMat = m_DominoMat;

            if (!m_Dominoes.Any())
                gameObject.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 1.0f, 0.4f);
        }
    }
}
