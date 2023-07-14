using UnityEngine;

public class Experiment1 : MonoBehaviour
{
    [Header("SceneObjects")]
    [SerializeField] private GameObject m_Objects = null;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_CratePrefab = null;
    [SerializeField] private GameObject m_BarrelPrefab = null;

    private ExperimentManager m_ExperimentManager = null;

    private void Awake()
    {
        m_ExperimentManager = transform.parent.GetComponent<ExperimentManager>();
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if(value)
            ResetExperiment();
    }

    private void OnDisable()
    {
        DestroyAllObjects();
    }

    private void DestroyAllObjects()
    {
        if (m_Objects.transform.childCount > 0)
        {
            for(var i = m_Objects.transform.childCount - 1; i >= 0 ; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void ResetExperiment()
    {
        DestroyAllObjects();

        GameObject crate = Instantiate(m_CratePrefab);
        crate.transform.position = new Vector3(0.0f, 0.0f, -0.45f);
        crate.transform.SetParent(m_Objects.transform);

        GameObject barrel = Instantiate(m_BarrelPrefab);
        barrel.transform.position = new Vector3(-0.5f, 0.075f, -0.5f);
        barrel.transform.SetParent(m_Objects.transform);
        barrel = Instantiate(m_BarrelPrefab);
        barrel.transform.position = new Vector3(-0.5f, 0.075f, 0.5f);
        barrel.transform.SetParent(m_Objects.transform);
        barrel = Instantiate(m_BarrelPrefab);
        barrel.transform.position = new Vector3(0.5f, 0.075f, -0.5f);
        barrel.transform.SetParent(m_Objects.transform);
        barrel = Instantiate(m_BarrelPrefab);
        barrel.transform.position = new Vector3(0.5f, 0.075f, 0.5f);
        barrel.transform.SetParent(m_Objects.transform);
    }
}