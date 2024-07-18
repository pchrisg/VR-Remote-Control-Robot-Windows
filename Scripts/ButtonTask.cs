using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ButtonTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_ButtonPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    private ExperimentManager m_ExperimentManager = null;
    private InteractableObjects m_InteractableObjects = null;
    private GameObject m_ObjectsContainer = null;

    private readonly List<Pressable> m_Buttons = new ();
    private readonly List<Collider> m_Obstacles = new();

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_ObjectsContainer = m_ExperimentManager.m_Objects;
    }

    public void Setup(bool value)
    {
        if (value)
            StartCoroutine(SetupTask());
        else
            StartCoroutine(DestroyAllObjects());
    }

    private IEnumerator SetupTask()
    {
        UnityAction action = new (() => m_ExperimentManager.RecordTime());

        //Buttons
        GameObject button = Instantiate(m_ButtonPrefab);
        button.name = "button_1";
        button.transform.position = new(-0.5f, 0.025f, -0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "1";
        var pressable = button.GetComponentInChildren<Pressable>();
        pressable.SetEvent(action);
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        button = Instantiate(m_ButtonPrefab);
        button.name = "button_2";
        button.transform.position = new(0.5f, 0.025f, -0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "2";
        pressable = button.GetComponentInChildren<Pressable>();
        pressable.SetEvent(action);
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        button = Instantiate(m_ButtonPrefab);
        button.name = "button_3";
        button.transform.position = new(0.5f, 0.025f, 0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "3";
        pressable = button.GetComponentInChildren<Pressable>();
        pressable.SetEvent(action);
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        button = Instantiate(m_ButtonPrefab);
        button.name = "button_4";
        button.transform.position = new(-0.5f, 0.025f, 0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "4";
        pressable = button.GetComponentInChildren<Pressable>();
        pressable.SetEvent(new UnityAction(()=> m_ExperimentManager.SaveData()));
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        //Obstacles
        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1_1";
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.63f, -0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.8f, 0.1f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if(collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1_2";
        obstacle.transform.SetPositionAndRotation(new(0.045f, 0.225f, -0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.68f, 0.45f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_2_1";
        obstacle.transform.SetPositionAndRotation(new(0.4f, 0.6f, 0.01f), Quaternion.Euler(0.0f, 90.0f, -15.0f));
        obstacle.transform.localScale = new Vector3(0.5f, 0.15f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_2_2";
        obstacle.transform.SetPositionAndRotation(new(0.4f, 0.25f, 0.0f), Quaternion.Euler(0.0f, 90.0f, -15.0f));
        obstacle.transform.localScale = new Vector3(0.6f, 0.15f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3_1";
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.63f, 0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.8f, 0.1f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3_2";
        obstacle.transform.SetPositionAndRotation(new(0.25f, 0.2f, 0.42f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.1f, 0.4f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3_3";
        obstacle.transform.SetPositionAndRotation(new(-0.05f, 0.5f, 0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.1f, 0.15f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3_4";
        obstacle.transform.SetPositionAndRotation(new(-0.05f, 0.15f, 0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.1f, 0.3f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_3_5";
        obstacle.transform.SetPositionAndRotation(new(-0.32f, 0.2f, 0.35f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.1f, 0.4f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        foreach (var collider in obstacle.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_Obstacles.Add(collider);

        yield return new WaitForFixedUpdate();

        m_InteractableObjects.AddAllInteractableObjects(m_Obstacles);
    }

    private IEnumerator DestroyAllObjects()
    {
        m_InteractableObjects.RemoveAllInteractableObjects();
        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);

        if (m_ObjectsContainer != null && m_ObjectsContainer.transform.childCount > 0)
        {
            for (var i = m_ObjectsContainer.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_ObjectsContainer.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }

        m_Buttons.Clear();
        m_Obstacles.Clear();
    }

    public void ResetTask()
    {
        foreach(var button in m_Buttons)
            button.ResetButton();

        m_InteractableObjects.AddAllInteractableObjects(m_Obstacles);
    }
}