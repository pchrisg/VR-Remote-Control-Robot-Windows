using System.Collections.Generic;
using UnityEngine;

public class StackingTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_TargetPrefab = null;
    [SerializeField] private GameObject m_BarrelPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    private ExperimentManager m_ExperimentManager = null;

    private GameObject m_ObjectsContainer = null;
    private readonly List<Transform> m_Barrels = new();

    private Vector2 m_Target = new();
    private bool m_isHighlighted = false;

    private float m_Timer = 0.0f;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_ObjectsContainer = m_ExperimentManager.m_Objects;
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
        m_Barrels.Clear();
    }

    private void Update()
    {
        Transform movingBarrel = null;
        foreach (Transform barrel in m_Barrels)
        {
            if (barrel.GetComponent<Barrel>().m_isMoving)
            {
                movingBarrel = barrel;
                break;
            }
        }

        if(movingBarrel != null)
        {
            if (Vector2.Distance(m_Target, new(movingBarrel.position.x, movingBarrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
            {
                if(!m_isHighlighted)
                {
                    m_isHighlighted = true;
                    foreach (Transform barrel in m_Barrels)
                    {
                        if (Vector2.Distance(m_Target, new(barrel.position.x, barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
                            barrel.GetComponent<Barrel>().Highlight(true);
                    }
                }
            }
            else if (m_isHighlighted)
            {
                m_isHighlighted = false;
                foreach (Transform barrel in m_Barrels)
                    barrel.GetComponent<Barrel>().Highlight(false);
            }
        }
        else if (m_isHighlighted)
        {
            m_isHighlighted = false;

            int count = 0;
            foreach (Transform barrel in m_Barrels)
            {
                barrel.GetComponent<Barrel>().Highlight(false);

                if (Vector2.Distance(m_Target, new(barrel.position.x, barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
                    count++;
            }

            m_ExperimentManager.SplitTime(count);

            if (count == 5)
                m_Timer = 3.0f;
        }

        if (m_Timer != 0.0f)
        {
            m_Timer -= Time.deltaTime;

            if(m_Timer <= 0)
            {
                int count = 0;
                foreach (Transform barrel in m_Barrels)
                {
                    if (Vector2.Distance(m_Target, new(barrel.position.x, barrel.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
                        count++;
                }

                if (count == 5)
                    m_ExperimentManager.SaveData();
            }
        }
    }

    private void DestroyAllObjects()
    {
        if (m_ObjectsContainer != null && m_ObjectsContainer.transform.childCount > 0)
        {
            for (var i = m_ObjectsContainer.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_ObjectsContainer.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void ResetTask()
    {
        DestroyAllObjects();

        //Target
        GameObject target = Instantiate(m_TargetPrefab);
        target.transform.position = new Vector3(0.0f, 0.0f, -0.45f);
        target.transform.SetParent(m_ObjectsContainer.transform);
        m_Target = new(target.transform.position.x, target.transform.position.z);

        //Barrels
        GameObject barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_1";
        barrel.GetComponent<Barrel>().SetStartingPosition(new(-0.5f, 0.058f, -0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_2";
        barrel.GetComponent<Barrel>().SetStartingPosition(new(-0.44f, 0.058f, 0.4f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_3";
        barrel.GetComponent<Barrel>().SetStartingPosition(new(0.5f, 0.058f, -0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_4";
        barrel.GetComponent<Barrel>().SetStartingPosition(new(0.067f, 0.058f, 0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_5";
        barrel.GetComponent<Barrel>().SetStartingPosition(new(0.5f, 0.058f, 0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        m_Barrels.Add(barrel.transform);

        //Obstacles
        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1";
        obstacle.transform.SetPositionAndRotation(new(-0.069f, 0.15f, -0.237f), Quaternion.Euler(0.0f, -168.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.467f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_2";
        obstacle.transform.SetPositionAndRotation(new(-0.273f, 0.1f, 0.281f), Quaternion.Euler(0.0f, -160.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.247f, 0.2f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3";
        obstacle.transform.SetPositionAndRotation(new(-0.34f, 0.253f, 0.051f), Quaternion.Euler(0.0f, 142, 0.0f));
        obstacle.transform.localScale = new Vector3(0.097f, 0.506f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_4";
        obstacle.transform.SetPositionAndRotation(new(0.35f, 0.116f, -0.284f), Quaternion.Euler(0.0f, -105, 0.0f));
        obstacle.transform.localScale = new Vector3(0.689f, 0.232f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_5";
        obstacle.transform.SetPositionAndRotation(new(-0.39f, 0.127f, -0.19f), Quaternion.Euler(0.0f, -117, 0.0f));
        obstacle.transform.localScale = new Vector3(0.955f, 0.254f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_6";
        obstacle.transform.SetPositionAndRotation(new(-0.124f, 0.326f, 0.43f), Quaternion.Euler(0.0f, 90, 0.0f));
        obstacle.transform.localScale = new Vector3(0.367f, 0.652f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_7";
        obstacle.transform.SetPositionAndRotation(new(0.323f, 0.15f, 0.245f), Quaternion.Euler(0.0f, -103, 0.0f));
        obstacle.transform.localScale = new Vector3(0.733f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
    }
}