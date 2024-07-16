using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ButtonTask : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject m_ButtonPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    private ExperimentManager m_ExperimentManager = null;
    private InteractableObjects m_InteractableObjects = null;
    private GameObject m_ObjectsContainer = null;

    private readonly List<Pressable> m_Buttons = new ();
    private readonly List<BoxCollider> m_Obstacles = new();

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

    //private void Update()
    //{
        
    //}

    private void SetupTask()
    {
        //Buttons
        GameObject button = Instantiate(m_ButtonPrefab);
        button.name = "button_1";
        button.transform.position = new(-0.5f, 0.025f, -0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "1";
        var pressable = button.GetComponentInChildren<Pressable>();
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        button = Instantiate(m_ButtonPrefab);
        button.name = "button_2";
        button.transform.position = new(0.5f, 0.025f, -0.5f);
        button.transform.SetParent(m_ObjectsContainer.transform);
        button.GetComponentInChildren<TextMeshProUGUI>().text = "2";
        pressable = button.GetComponentInChildren<Pressable>();
        pressable.m_isMultiPress = false;
        m_Buttons.Add(pressable);

        //Obstacles
        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.name = "obstacle_1";
        obstacle.transform.SetPositionAndRotation(new(0.045f, 0.228f, -0.4f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle.transform.localScale = new Vector3(0.68f, 0.45f, 0.025f);
        obstacle.transform.SetParent(m_ObjectsContainer.transform);
        m_Obstacles.Add(obstacle.GetComponent<BoxCollider>());

        GameObject obstacle2 = Instantiate(m_ObstaclePrefab);
        obstacle2.name = "obstacle_2";
        obstacle2.transform.SetPositionAndRotation(new(0.045f, 0.616f, -0.2f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
        obstacle2.transform.localScale = new Vector3(0.68f, 0.1f, 0.025f);
        obstacle2.transform.SetParent(m_ObjectsContainer.transform);
        m_Obstacles.Add(obstacle2.GetComponent<BoxCollider>());

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