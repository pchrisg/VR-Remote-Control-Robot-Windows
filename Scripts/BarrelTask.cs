using UnityEngine;

public class BarrelTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_CratePrefab = null;
    [SerializeField] private GameObject m_BarrelPrefab = null;

    private GameObject m_Objects = null;

    private void Awake()
    {
        m_Objects = gameObject.transform.parent.GetComponent<ExperimentManager>().m_Objects;
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if(value)
            ResetTask();
    }

    private void OnDisable()
    {
        DestroyAllObjects();
    }

    private void DestroyAllObjects()
    {
        if (m_Objects != null && m_Objects.transform.childCount > 0)
        {
            for(var i = m_Objects.transform.childCount - 1; i >= 0 ; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void ResetTask()
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