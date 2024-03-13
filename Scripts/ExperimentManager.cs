using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ManipulationModes;

public class ExperimentManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_Glassbox = null;
    public GameObject m_Objects = null;

    [Header("Technique")]
    public Mode m_Technique = Mode.IDLE;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantNumber = string.Empty;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;

    [Header("Setup")]
    [SerializeField] private bool m_SetupTutorial = false;
    [SerializeField] private bool m_SetupTask = false;

    [Header("Reset")]
    [SerializeField] private bool m_Reset = false;

    [Header("Start/Stop")]
    [SerializeField] private bool m_Start = false;

    [Header("Status")]
    [SerializeField] private string m_Active = string.Empty;
    [SerializeField] private string m_Status = string.Empty;

    //Scripts
    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private Timer m_Timer = null;

    //Tutorial
    private Tutorial m_Tutorial = null;

    // Experiment
    private StackingTask m_Task = null;

    //Settings
    [HideInInspector] public const float ERRORTHRESHOLD = 0.05f; //5cm
    private readonly float m_TimeLimit = 600.0f;

    //Control
    private bool m_TutorialActive = false;
    public bool m_TaskActive = false;
    private bool m_Running = false;

    // File Path
    private readonly string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\Experiment2\\";

    // ## Data to Store ##
    // Time
    private float m_Barrel1Time = 0.0f;
    private float m_Barrel2Time = 0.0f;
    private float m_Barrel3Time = 0.0f;
    private float m_Barrel4Time = 0.0f;
    private float m_Barrel5Time = 0.0f;
    private float m_TimeInDirMan = 0.0f;
    private float m_TimeInColObj = 0.0f;
    private float m_TimeInAttObj = 0.0f;

    // Number of Grabs
    private bool m_IsInteracting = false;
    private int m_InteractionCount = 0;
    private readonly List<string> m_InteractionDescriptions = new();

    // Errors
    private int m_CollisionsCount = 0;
    private readonly List<string> m_ErrorDescriptions = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Tutorial = transform.Find("Tutorial").GetComponent<Tutorial>();
        m_Task = transform.Find("Task").GetComponent<StackingTask>();

        m_Table.SetActive(false);
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);

        m_Active = "None";
        m_Status = "Standby";
    }

    private void Start()
    {
        Invoke(nameof(ResetRobotPose), 1.0f);
    }

    private void Update()
    {
        // Reset Robot Pose
        if (m_ResetRobotPose)
        {
            m_ResetRobotPose = false;

            m_InteractableObjects.RemoveAllInteractableObjects();
            ResetRobotPose();
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
                    m_InteractableObjects.RemoveAllInteractableObjects();
                    ResetRobotPose();

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
                    m_InteractableObjects.RemoveAllInteractableObjects();
                    ResetRobotPose();

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
                    m_InteractableObjects.RemoveAllInteractableObjects();
                    ResetRobotPose();

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
                    m_Status = "Running";

                    if (m_TutorialActive)
                        m_Tutorial.stage = TutorialStages.Stage.START;

                    else
                        ClearData();
                }
            }
        }

        // if tutorial or experiment are running
        else if (m_Running)
        {
            // if tutorial stopped
            if (m_TutorialActive && !m_Start)
            {
                m_Running = false;
                m_Status = "Stopped";
            }

            // if experiment
            if (m_TaskActive)
            {
                // if time exhausted
                if (m_Timer.TimeExhausted())
                    SaveData();

                // if experiment stopped
                if (!m_Start)
                {
                    m_Running = false;
                    m_Status = "Stopped";
                    m_Timer.StopTimer();
                }

                if (m_IsInteracting != m_ManipulationMode.IsInteracting())
                {
                    m_IsInteracting = !m_IsInteracting;
                    RecordInteractionTime(m_IsInteracting);

                    if (m_IsInteracting)
                    {
                        if (m_InteractionCount == 0)
                            m_Timer.StartTimer(m_TimeLimit);

                        m_InteractionCount++;
                    }
                }

                // Data Gathering
                if (m_ManipulationMode.mode == Mode.DIRECT)
                    m_TimeInDirMan += Time.deltaTime;
                if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
                    m_TimeInColObj += Time.deltaTime;
                if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
                    m_TimeInAttObj += Time.deltaTime;
            }
        }
    }

    private void ResetRobotPose()
    {
        StartCoroutine(ResetRobotPoseRoutine());
    }

    private IEnumerator ResetRobotPoseRoutine()
    {
        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);

        m_Table.SetActive(false);
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");
        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();

        m_Table.SetActive(true);
        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);
    }

    private void RecordInteractionTime(bool value)
    {
        m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_ManipulationMode.mode.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "\n");
    }

    private void ClearData()
    {
        // Times
        m_Barrel1Time = 0.0f;
        m_Barrel2Time = 0.0f;
        m_Barrel3Time = 0.0f;
        m_Barrel4Time = 0.0f;
        m_Barrel5Time = 0.0f;
        m_TimeInDirMan = 0.0f;
        m_TimeInColObj = 0.0f;
        m_TimeInAttObj = 0.0f;

        // Number of Grabs
        m_IsInteracting = false;
        m_InteractionCount = 0;
        m_InteractionDescriptions.Clear();

        // Errors
        m_CollisionsCount = 0;
        m_ErrorDescriptions.Clear();
    }

    private IEnumerator WriteToFileRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        // Set path for data
        string path = m_FilePathName + "Data\\" + m_ParticipantNumber + m_Technique.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create first file
        var sr = File.CreateText(path);

        //participant number, technique, barrel1, barrel2, barrel3, barrel4, barrel5, totaltime, number of interactions, number of collisions, attachable time, collision time, direct time
        string dataCSV = m_ParticipantNumber +
                "," + m_Technique.ToString() +
                "," + m_Barrel1Time.ToString();

        if (m_Barrel5Time == 0.0f)
        {
            if (m_Barrel4Time == 0.0f)
            {
                if (m_Barrel3Time == 0.0f)
                {
                    if (m_Barrel2Time == 0.0f)
                        dataCSV += "," + m_Barrel2Time.ToString();
                    else
                        dataCSV += "," + (m_Barrel2Time - m_Barrel1Time).ToString();

                    dataCSV += "," + m_Barrel3Time.ToString();
                }
                else
                {
                    dataCSV += "," + (m_Barrel2Time - m_Barrel1Time).ToString() +
                            "," + (m_Barrel3Time - m_Barrel2Time).ToString();
                }

                dataCSV += "," + m_Barrel4Time.ToString();
            }
            else
            {
                dataCSV += "," + (m_Barrel2Time - m_Barrel1Time).ToString() +
                        "," + (m_Barrel3Time - m_Barrel2Time).ToString() +
                        "," + (m_Barrel4Time - m_Barrel3Time).ToString();
            }

            dataCSV += "," + m_Barrel5Time.ToString() +
                    "," + m_TimeLimit.ToString();
        }
        else
        {
            dataCSV += "," + (m_Barrel2Time - m_Barrel1Time).ToString() +
                    "," + (m_Barrel3Time - m_Barrel2Time).ToString() +
                    "," + (m_Barrel4Time - m_Barrel3Time).ToString() +
                    "," + (m_Barrel5Time - m_Barrel4Time).ToString() +
                    "," + m_Barrel5Time.ToString();
        }

        dataCSV += "," + m_InteractionCount.ToString() +
                "," + m_CollisionsCount.ToString();

        if (m_Technique == Mode.DIRECT)
        {
            dataCSV += "," + m_TimeInAttObj.ToString() +
                    "," + m_TimeInColObj.ToString() +
                    "," + m_TimeInDirMan.ToString();
        }

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo _ = new(path) { IsReadOnly = true };

        //Close first file
        sr.Close();

        print("Data saved to file: " + path);

        yield return new WaitForSeconds(0.5f);

        //open the file
        //Application.OpenURL(path);

        //change path for grab descriptions
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Technique.ToString() + "INTERACTION.csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create second file
        sr = File.CreateText(path);

        //participant number, technique
        dataCSV = string.Empty;

        foreach (var grab in m_InteractionDescriptions)
            dataCSV += grab;

        if (m_Barrel5Time != 0.0f)
            dataCSV += m_Barrel5Time.ToString();
        else
            dataCSV += m_TimeLimit.ToString();

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        _ = new(path) { IsReadOnly = true };

        //Close file
        sr.Close();

        print("Data saved to file: " + path);

        //change path for error descriptions
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Technique.ToString() + "COLLISIONS.csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create third file
        sr = File.CreateText(path);

        //collision descriptions
        dataCSV = string.Empty;

        foreach (var collision in m_ErrorDescriptions)
            dataCSV += collision;

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        _ = new(path) { IsReadOnly = true };

        //Close file
        sr.Close();

        print("Data saved to file: " + path);

        yield return new WaitForSeconds(0.5f);
    }

    public void SplitTime(int barrelNumber)
    {
        if (barrelNumber == 1 && m_Barrel1Time == 0.0f)
            m_Barrel1Time = m_Timer.SplitTime();
        else if (barrelNumber == 2 && m_Barrel2Time == 0.0f)
            m_Barrel2Time = m_Timer.SplitTime();
        else if (barrelNumber == 3 && m_Barrel3Time == 0.0f)
            m_Barrel3Time = m_Timer.SplitTime();
        else if (barrelNumber == 4 && m_Barrel4Time == 0.0f)
            m_Barrel4Time = m_Timer.SplitTime();
        else if (barrelNumber == 5 && m_Barrel5Time == 0.0f)
            m_Barrel5Time = m_Timer.SplitTime();

        print("saved time " + barrelNumber);
    }

    public void RecordCollision(string description)
    {
        if (m_Running)
        {
            m_CollisionsCount++;
            m_ErrorDescriptions.Add(m_ParticipantNumber + "," + m_Technique.ToString() + "," + m_Timer.SplitTime().ToString() + "," + description);
        }
    }

    public void SaveData()
    {
        if (m_Running)
        {
            m_Timer.StopTimer();

            m_InteractableObjects.RemoveAllInteractableObjects();
            ResetRobotPose();

            m_Start = false;
            m_Running = false;
            m_TaskActive = false;
            m_Task.Setup(false);

            m_Active = "None";
            m_Status = "Finished";

            if (m_Timer.TimeExhausted())
                m_Barrel5Time = 0.0f;

            StartCoroutine(WriteToFileRoutine());
        }
    }
}