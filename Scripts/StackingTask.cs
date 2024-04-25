using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class StackingTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_TargetPrefab = null;
    [SerializeField] private GameObject m_BarrelPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    private ExperimentManager m_ExperimentManager = null;
    private InteractableObjects m_InteractableObjects = null;

    private GameObject m_ObjectsContainer = null;
    private readonly List<Transform> m_Barrels = new();
    private readonly List<string> m_PlacedBarrels = new();

    private GameObject m_Target = null;

    private bool m_BarrelMoved = false;
    private bool m_WithinBounds = false;
    private float m_Timer = 0.0f;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_ObjectsContainer = m_ExperimentManager.m_Objects;
    }

    public void Setup(bool value)
    {
        if (value)
            SetupTask();
        else
            StartCoroutine(DestroyAllObjects());
    }

    private void Update()
    {
        Transform movingBarrel = null;
        foreach (Transform barrel in m_Barrels)
        {
            if (barrel.GetComponent<Barrel>().IsMoving())
            {
                movingBarrel = barrel;

                if(m_BarrelMoved == false)
                {
                    m_BarrelMoved = true;
                    m_ExperimentManager.RecordGrabTime(barrel.gameObject.name);
                    print(barrel.gameObject.name + " grabbed");
                }

                break;
            }
        }

        if (m_BarrelMoved)
        {
            if (movingBarrel != null)
                m_WithinBounds = m_Target.GetComponent<Target>().CheckDistance(movingBarrel);
            else
            {
                m_BarrelMoved = false;

                bool knockedOver = false;
                foreach (Transform barrel in m_Barrels)
                {
                    if (m_Target.GetComponent<Target>().IsInBounds(barrel))
                    {
                        if (!m_PlacedBarrels.Contains(barrel.gameObject.name))
                            m_PlacedBarrels.Add(barrel.gameObject.name);
                    }
                    else
                    {
                        if (m_PlacedBarrels.Contains(barrel.gameObject.name))
                        {
                            m_PlacedBarrels.Remove(barrel.gameObject.name);
                            knockedOver = true;
                        }
                    }
                }

                if (m_WithinBounds)
                {
                    m_WithinBounds = false;
                    m_ExperimentManager.RecordPlaceTime(m_PlacedBarrels.Last());
                }

                if (knockedOver)
                    m_ExperimentManager.RecordKnockOver();

                if (m_PlacedBarrels.Count() == 5)
                    m_Timer = 3.0f;
            }
        }

        if (m_Timer != 0.0f)
        {
            m_Timer -= Time.deltaTime;

            if(m_Timer <= 0)
            {
                m_Timer = 0.0f;
                if (m_PlacedBarrels.Count() == 5)
                    m_ExperimentManager.SaveData();
            }
        }
    }

    private void SetupTask()
    {
        //Target
        m_Target = Instantiate(m_TargetPrefab);
        m_Target.transform.SetParent(m_ObjectsContainer.transform);
        m_Target.GetComponent<Target>().SetPosition(new Vector3(0.0f, 0.5f, -0.45f));

        //Barrels
        GameObject barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_1";
        barrel.GetComponent<Barrel>().SetPosition(new(-0.5f, 0.058f, -0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        barrel.GetComponentInChildren<TextMeshProUGUI>().text = "1";
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_2";
        barrel.GetComponent<Barrel>().SetPosition(new(0.5f, 0.058f, -0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        barrel.GetComponentInChildren<TextMeshProUGUI>().text = "2";
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_3";
        barrel.GetComponent<Barrel>().SetPosition(new(0.067f, 0.058f, 0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        barrel.GetComponentInChildren<TextMeshProUGUI>().text = "3";
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_4";
        barrel.GetComponent<Barrel>().SetPosition(new(0.5f, 0.058f, 0.5f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        barrel.GetComponentInChildren<TextMeshProUGUI>().text = "4";
        m_Barrels.Add(barrel.transform);

        barrel = Instantiate(m_BarrelPrefab);
        barrel.name = "barrel_5";
        barrel.GetComponent<Barrel>().SetPosition(new(-0.44f, 0.058f, 0.4f));
        barrel.transform.SetParent(m_ObjectsContainer.transform);
        barrel.GetComponentInChildren<TextMeshProUGUI>().text = "5";
        m_Barrels.Add(barrel.transform);

        //Obstacles
        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1";
        obstacle.transform.SetPositionAndRotation(new(-0.069f, 0.15f, -0.237f), Quaternion.Euler(0.0f, -168.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.467f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_2";
        obstacle.transform.SetPositionAndRotation(new(-0.351f, 0.828f, 0.036f), Quaternion.Euler(0.0f, -40f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.7f, 0.34f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3";
        obstacle.transform.SetPositionAndRotation(new(-0.39f, 0.26f, 0.026f), Quaternion.Euler(0.0f, 113.16f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.3f, 0.52f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_4";
        obstacle.transform.SetPositionAndRotation(new(0.388f, 0.116f, -0.428f), Quaternion.Euler(0.0f, -105, 0.0f));
        obstacle.transform.localScale = new Vector3(0.4f, 0.232f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_5";
        obstacle.transform.SetPositionAndRotation(new(-0.269f, 0.127f, -0.428f), Quaternion.Euler(0.0f, -117, 0.0f));
        obstacle.transform.localScale = new Vector3(0.4f, 0.254f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_6";
        obstacle.transform.SetPositionAndRotation(new(-0.107f, 0.326f, 0.43f), Quaternion.Euler(0.0f, 90, 0.0f));
        obstacle.transform.localScale = new Vector3(0.4f, 0.652f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_7";
        obstacle.transform.SetPositionAndRotation(new(0.352f, 0.25f, 0.473f), Quaternion.Euler(0.0f, -92, 0.0f));
        obstacle.transform.localScale = new Vector3(0.2f, 0.5f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_9";
        obstacle.transform.SetPositionAndRotation(new(0.478f, 0.5f, 0.069f), Quaternion.Euler(0.0f, -22.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.3f, 1.0f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_10";
        obstacle.transform.SetPositionAndRotation(new(0.06f, 0.5259f, 0.485f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.3f, 0.25f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
    }    

    private IEnumerator DestroyAllObjects()
    {
        yield return new WaitUntil( () => m_InteractableObjects.m_InteractableObjects.Count == 0);

        if (m_ObjectsContainer != null && m_ObjectsContainer.transform.childCount > 0)
        {
            for (var i = m_ObjectsContainer.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_ObjectsContainer.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }

        m_Barrels.Clear();
        m_PlacedBarrels.Clear();
    }

    public void ResetTask()
    {
        m_PlacedBarrels.Clear();

        foreach (var barrel in m_Barrels)
            barrel.GetComponent<Barrel>().ResetPosition();
    }
}