using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR.InteractionSystem;
using FeedBackModes;

public class ExperimentManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_GlassBox = null;
    public GameObject m_Objects = null;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantNumber = string.Empty;
    [SerializeField] private bool m_ResetHeight = false;

    [Header("Technique")]
    public Mode m_FeedbackMode = Mode.NONE;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;
    [SerializeField] private bool m_AddColObjs = false;
    private readonly List<Collider> m_SceneObjects = new ();
    public bool m_AllowUserControl = false;
    public bool m_ShowHints = false;

    [Header("Setup")]
    [SerializeField] private bool m_SetupTutorial = false;
    [SerializeField] private bool m_SetupTask = false;

    [Header("Reset")]
    [SerializeField] private bool m_Reset = false;

    [Header("Start/Stop")]
    public bool m_TeachRobotFeedback = false;
    [SerializeField] private bool m_Start = false;

    [Header("Tutorial Control")]
    public bool m_Continue = false;

    [Header("Status")]
    [SerializeField] private string m_Active = string.Empty;
    [SerializeField] private string m_Status = string.Empty;

    // Scripts
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private Timer m_Timer = null;

    // Tutorial
    private Tutorial m_Tutorial = null;

    // Experiment
    private ButtonTask m_Task = null;

    // Settings
    private readonly float m_TimeLimit = 600.0f;

    // Control
    private bool m_HintsActive = false;
    private bool m_TutorialActive = false;
    private bool m_TaskActive = false;
    private bool m_Running = false;

    // File Path
    private readonly string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\Experiment2\\";

    // ## Data to Store ##
    private readonly List<float> m_PressTimes = new ();

    // Interactions
    private int m_InteractionCount = 0;
    private readonly List<string> m_InteractionDescriptions = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Tutorial = transform.Find("Tutorial").GetComponent<Tutorial>();
        m_Task = transform.Find("Task").GetComponent<ButtonTask>();

        m_Table.SetActive(false);
        m_GlassBox.SetActive(false);
        m_Objects.SetActive(false);

        m_Active = "None";
        m_Status = "Standby";

        foreach (var collider in m_Table.transform.Find("TableTop").GetComponents<Collider>())
            if (collider.isTrigger)
                m_SceneObjects.Add(collider);
        foreach (var collider in m_GlassBox.GetComponentsInChildren<Collider>())
            if (collider.isTrigger)
                m_SceneObjects.Add(collider);
    }

    private void Start()
    {
        Invoke(nameof(ResetRobotPose), 1.0f);
    }

    private void Update()
    {
        // Reset Player Height
        if (m_ResetHeight)
        {
            m_ResetHeight = false;
            ResetHeight();
        }

        if (m_ShowHints != m_HintsActive)
        {
            m_HintsActive = m_ShowHints;
            m_ManipulationMode.ShowHints(m_ShowHints);
        }

        // Reset Robot Pose
        if (m_ResetRobotPose)
        {
            m_ResetRobotPose = false;
            ResetRobotPose();
        }

        // Reset Robot Pose
        if (m_AddColObjs)
        {
            m_AddColObjs = false;
            AddCollisionObjects();
        }

        // If no tutorial or task is running
        if (!m_Running)
        {
            // Setup tutorial
            if (m_SetupTutorial)
            {
                m_SetupTutorial = false;

                if (!m_TutorialActive)
                {
                    m_Task.Setup(false);
                    m_TaskActive = false;

                    m_Tutorial.Setup(true);
                    m_TutorialActive = true;

                    m_Active = "Tutorial";
                    m_Status = "Standby";
                }
            }

            // Setup task
            if (m_SetupTask)
            {
                m_SetupTask = false;

                if (!m_TaskActive)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;

                    m_Task.Setup(true);
                    m_TaskActive = true;

                    m_Active = "Experiment";
                    m_Status = "Standby";
                }
            }

            // Reset the experiment
            if (m_Reset)
            {
                m_Reset = false;

                if (m_TutorialActive || m_TaskActive)
                {
                    if (m_TutorialActive)
                        m_Tutorial.ResetTutorial();
                    if (m_TaskActive)
                        m_Task.ResetTask();
                }
            }

            // Start the experiment
            if (m_Start)
            {
                if (!m_TutorialActive && !m_TaskActive)
                    m_Start = false;

                else
                {
                    m_Running = true;
                    m_AllowUserControl = true;
                    m_Status = "Running";
                    m_Timer.ResetTimer();

                    if (m_TutorialActive)
                        m_Tutorial.stage = TutorialStages.Stage.START;

                    else
                        ClearData();
                }
            }
        }

        // if tutorial or experiment are running
        else
        {
            // if tutorial stopped
            if (m_TutorialActive && !m_Start)
            {
                m_Running = false;
                m_AllowUserControl = false;
                m_Status = "Stopped";
                m_Tutorial.ResetTutorial();
            }

            // if experiment
            if (m_TaskActive)
            {
                // if time exhausted
                if (m_Timer.TimeExhausted())
                {
                    print("Time's up!");
                    SaveData();
                }

                // if experiment stopped
                if (!m_Start)
                {
                    m_Running = false;
                    m_AllowUserControl = false;
                    m_Status = "Stopped";
                    m_Timer.StopTimer();
                    m_Task.ResetTask();
                }
            }
        }
    }

    private void ResetHeight()
    {
        float cameraHeight = GameObject.FindGameObjectWithTag("MainCamera").transform.localPosition.y;

        float heightDiff = cameraHeight - 1.68f;

        Player.instance.transform.position = new(0.0f, -0.972f - heightDiff, 0.0f);
    }

    private void ResetRobotPose()
    {
        StartCoroutine(ResetRobotPoseRoutine());

        foreach (EmergencyStop emergencyStop in GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<EmergencyStop>())
            emergencyStop.ChangeAppearance(1);
    }

    private IEnumerator ResetRobotPoseRoutine()
    {
        m_InteractableObjects.RemoveAllInteractableObjects();
        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);
        yield return new WaitForSeconds(1.0f);

        m_Table.SetActive(false);
        m_GlassBox.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");
        //GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();

        m_Table.SetActive(true);
        m_GlassBox.SetActive(true);
        m_Objects.SetActive(true);
    }

    private void AddCollisionObjects()
    {
        m_InteractableObjects.AddAllInteractableObjects(m_SceneObjects);
    }

    private void ClearData()
    {
        // Times
        m_PressTimes.Clear();

        // Interactions
        m_InteractionCount = 0;
        m_InteractionDescriptions.Clear();
    }

    public void RecordInteraction(bool value)
    {
        if (m_TaskActive && m_Running)
        {
            if (value)
            {
                if (m_InteractionCount == 0)
                    m_Timer.StartTimer(m_TimeLimit);

                m_InteractionCount++;
            }

            m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_FeedbackMode.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "\n");
        }
    }

    public void RecordTime()
    {
        m_PressTimes.Add(m_Timer.SplitTime());
    }

    public void SaveData()
    {
        if (m_Running)
        {
            RecordTime();
            m_Timer.StopTimer();
            ResetRobotPose();

            m_Start = false;
            m_Running = false;
            m_AllowUserControl = false;
            m_TaskActive = false;
            m_Task.Setup(false);
            m_ShowHints = false;

            m_Active = "None";
            m_Status = "Finished";

            StartCoroutine(WriteToFileRoutine());
        }
    }

    private IEnumerator WriteToFileRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        // Set path for data
        string path = m_FilePathName + "Data\\" + m_ParticipantNumber + m_FeedbackMode.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create first file
        var sr = File.CreateText(path);

        //participant number, feedback mode, button1, button2, button3, button4, totaltime, interaction count
        string dataCSV = m_ParticipantNumber +
                         "," + m_FeedbackMode.ToString();

        if (m_PressTimes.Count > 0)
            dataCSV += "," + m_PressTimes[0].ToString();
        else
            dataCSV += ",0.0";

        for (var i = 1; i < 4; i++)
        {
            if (m_PressTimes.Count > i)
                dataCSV += "," + (m_PressTimes[i] - m_PressTimes[i - 1]).ToString();
            else
                dataCSV += ",0.0";
        }

        dataCSV += ",Total Time:";
        if (m_PressTimes.Count == 4)
            dataCSV += "," + m_PressTimes[3].ToString();
        else
            dataCSV += "," + m_TimeLimit.ToString();

        // Interaction and Collision Count
        dataCSV += ",Interactions:," + m_InteractionCount.ToString();

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo _ = new(path) { IsReadOnly = true };

        //Close first file
        sr.Close();

        print("Data saved to file: " + path);

        yield return new WaitForSeconds(0.5f);


        //change path for interaction descriptions
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_FeedbackMode.ToString() + "INTERACTIONS.csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create second file
        sr = File.CreateText(path);

        //participant number, feedback mode, time, value
        dataCSV = string.Empty;

        foreach (var grab in m_InteractionDescriptions)
            dataCSV += grab;

        if (m_PressTimes.Count == 4)
            dataCSV += m_PressTimes[3].ToString();
        else
            dataCSV += m_TimeLimit.ToString();

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        _ = new(path) { IsReadOnly = true };

        //Close file
        sr.Close();

        print("Data saved to file: " + path);

        yield return new WaitForSeconds(0.5f);
    }
}