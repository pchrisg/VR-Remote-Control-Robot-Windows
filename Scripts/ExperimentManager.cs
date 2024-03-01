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
    private bool m_TaskActive = false;
    private bool m_Running = false;

    // File Path
    private readonly string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\";

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

    // objects placed within bounds with their errors
    private string m_PlacedObjects = string.Empty;

    // Number of Grabs
    private bool isInteracting = false;
    private int m_InteractionCount = 0;
    private List<string> m_InteractionDescriptions = new();

    // Errors
    private int m_CollisionsCount = 0;
    private List<string> m_ErrorDescriptions = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
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
        Invoke("ResetRobotPose", 1.0f);
    }

    private void Update()
    {
        // Reset Robot Pose
        if (m_ResetRobotPose)
        {
            m_ResetRobotPose = false;
            ResetRobotPose();
        }

        // If no tutorial or task is running
        if (!m_Running)
        {
            // Setup tutorial
            if (m_SetupTutorial)
            {
                m_SetupTutorial = false;
                m_SetupTask = false;

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

                    ResetRobotPose();
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
                        StartExperiment();
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

                if (isInteracting != m_ManipulationMode.isInteracting)
                {
                    isInteracting = !isInteracting;

                    RecordInteractionTime(isInteracting);

                    if (isInteracting)
                    {
                        if (m_InteractionCount == 0)
                            m_Timer.StartTimer(m_TimeLimit);

                        m_InteractionCount++;
                    }
                }

                // if experiment stopped
                if (!m_Start)
                {
                    m_Running = false;
                    m_Status = "Stopped";

                    m_Timer.StopTimer();
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
        StartCoroutine(ResetPose());
    }

    IEnumerator ResetPose()
    {
        m_Table.SetActive(false);
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();

        yield return new WaitForSeconds(1.0f);

        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();
        m_Table.SetActive(true);
        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);

        yield return null;
    }

    private void StartExperiment()
    {
        m_Timer.StartTimer(m_TimeLimit);

        m_TimeInDirMan = 0.0f;
        m_TimeInColObj = 0.0f;
        m_TimeInAttObj = 0.0f;
    }

    public void SplitTime()
    {
        if (m_Barrel1Time == 0.0f)
            m_Barrel1Time = m_Timer.SplitTime();
        else if (m_Barrel2Time == 0.0f)
            m_Barrel2Time = m_Timer.SplitTime();
        else if (m_Barrel3Time == 0.0f)
            m_Barrel3Time = m_Timer.SplitTime();
        else if (m_Barrel4Time == 0.0f)
            m_Barrel4Time = m_Timer.SplitTime();
    }

    private void RecordInteractionTime(bool value)
    {
        m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_Technique.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "\n");
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
            if (m_TaskActive)
            {
                m_TaskActive = false;
                m_Task.Setup(false);
            }

            if(!m_Timer.TimeExhausted())
                m_Barrel5Time = m_Timer.SplitTime();

            m_Start = false;
            m_Running = false;
            m_Status = "Finished";
            m_Timer.StopTimer();

            StartCoroutine(WriteToFile());
        }
    }

    IEnumerator WriteToFile()
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
            if(m_Barrel4Time == 0.0f)
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

        if(m_Technique == Mode.DIRECT)
        {
            dataCSV += "," + m_TimeInAttObj.ToString() +
                    "," + m_TimeInColObj.ToString() +
                    "," + m_TimeInDirMan.ToString();
        }

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo fInfo = new(path)
        {
            IsReadOnly = true
        };

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
        fInfo = new(path)
        {
            IsReadOnly = true
        };

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
        fInfo = new(path)
        {
            IsReadOnly = true
        };

        //Close file
        sr.Close();

        print("Data saved to file: " + path);

        yield return new WaitForSeconds(0.5f);
    }
}