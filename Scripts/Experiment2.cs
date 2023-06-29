using UnityEngine;

public class Experiment2 : MonoBehaviour
{
    [Header("SceneObjects")]
    [SerializeField] private GameObject m_Objects = null;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_TargetPrefab = null;
    [SerializeField] private GameObject m_DominoPrefab = null;

    private Experiment2ConditionChecker m_Ex2ConCheck = null;
    private ExperimentManager m_ExperimentManager = null;

    private void Awake()
    {
        m_ExperimentManager = transform.parent.GetComponent<ExperimentManager>();
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if (value)
            ResetExperiment();
    }

    private void OnDisable()
    {
        DestroyAllObjects();
    }

    private void Update()
    {
        if (m_Ex2ConCheck != null && m_Ex2ConCheck.allObjectsInside)
            m_ExperimentManager.SaveData();
    }

    private void DestroyAllObjects()
    {
        if (m_Objects.transform.childCount > 0)
        {
            for (var i = m_Objects.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void ResetExperiment()
    {
        DestroyAllObjects();

        GameObject target = Instantiate(m_TargetPrefab);
        target.transform.position = new Vector3(0.0f, 0.375f, -0.45f);
        target.transform.SetParent(m_Objects.transform);

        m_Ex2ConCheck = target.GetComponent<Experiment2ConditionChecker>();
        m_Ex2ConCheck.m_NumberOfBarrels = 4;

        GameObject domino = Instantiate(m_DominoPrefab);
        domino.transform.position = new Vector3(-0.5f, 0.075f, -0.5f);
        domino.transform.SetParent(m_Objects.transform);
        domino = Instantiate(m_DominoPrefab);
        domino.transform.position = new Vector3(-0.5f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);
        domino = Instantiate(m_DominoPrefab);
        domino.transform.position = new Vector3(0.5f, 0.075f, -0.5f);
        domino.transform.SetParent(m_Objects.transform);
        domino = Instantiate(m_DominoPrefab);
        domino.transform.position = new Vector3(0.0f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);
        domino = Instantiate(m_DominoPrefab);
        domino.transform.position = new Vector3(0.5f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);
    }
}