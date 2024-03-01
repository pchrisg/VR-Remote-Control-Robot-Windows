using UnityEngine;

public class StackingTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_TargetPrefab = null;
    [SerializeField] private GameObject m_BarrelPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    private GameObject m_Objects = null;

    private void Awake()
    {
        m_Objects = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>().m_Objects;
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if (value)
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
            for (var i = m_Objects.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void ResetTask()
    {
        DestroyAllObjects();

        //Target
        GameObject target = Instantiate(m_TargetPrefab);
        target.transform.position = new Vector3(0.0f, 0.075f, -0.45f);
        target.transform.SetParent(m_Objects.transform);

        //Dominoes
        GameObject domino = Instantiate(m_BarrelPrefab);
        domino.name = "domino_1";
        domino.transform.position = new Vector3(-0.5f, 0.075f, -0.5f);
        domino.transform.SetParent(m_Objects.transform);

        domino = Instantiate(m_BarrelPrefab);
        domino.name = "domino_2";
        domino.transform.position = new Vector3(-0.5f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);

        domino = Instantiate(m_BarrelPrefab);
        domino.name = "domino_3";
        domino.transform.position = new Vector3(0.5f, 0.075f, -0.5f);
        domino.transform.SetParent(m_Objects.transform);

        domino = Instantiate(m_BarrelPrefab);
        domino.name = "domino_4";
        domino.transform.position = new Vector3(0.0f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);

        domino = Instantiate(m_BarrelPrefab);
        domino.name = "domino_5";
        domino.transform.position = new Vector3(0.5f, 0.075f, 0.5f);
        domino.transform.SetParent(m_Objects.transform);

        //Obstacles
        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1";
        obstacle.transform.position = new Vector3(-0.069f, 0.15f, -0.237f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, -168.0f, 0.0f);
        obstacle.transform.localScale = new Vector3(0.467f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_2";
        obstacle.transform.position = new Vector3(-0.273f, 0.1f, 0.281f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, -160.0f, 0.0f);
        obstacle.transform.localScale = new Vector3(0.247f, 0.2f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3";
        obstacle.transform.position = new Vector3(-0.34f, 0.253f, 0.051f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, 142, 0.0f);
        obstacle.transform.localScale = new Vector3(0.097f, 0.506f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_4";
        obstacle.transform.position = new Vector3(0.35f, 0.116f, -0.284f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, -105, 0.0f);
        obstacle.transform.localScale = new Vector3(0.689f, 0.232f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_5";
        obstacle.transform.position = new Vector3(-0.39f, 0.127f, -0.19f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, -117, 0.0f);
        obstacle.transform.localScale = new Vector3(0.955f, 0.254f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_6";
        obstacle.transform.position = new Vector3(-0.124f, 0.326f, 0.43f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, 90, 0.0f);
        obstacle.transform.localScale = new Vector3(0.367f, 0.652f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_7";
        obstacle.transform.position = new Vector3(0.323f, 0.15f, 0.245f);
        obstacle.transform.rotation *= Quaternion.Euler(0.0f, -103, 0.0f);
        obstacle.transform.localScale = new Vector3(0.733f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_Objects.transform);
    }
}