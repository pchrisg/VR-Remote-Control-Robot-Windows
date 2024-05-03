using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ManipulationModes;
using Valve.VR.InteractionSystem;

public class ExperimentManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_Glassbox = null;
    public GameObject m_Objects = null;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantNumber = string.Empty;
    [SerializeField] private bool m_ResetHeight = false;

    [Header("Technique")]
    public Mode m_Technique = Mode.IDLE;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;
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
    private StackingTask m_Task = null;

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
    private class PlacedBarrel
    {
        public string name;
        public float grabTime;
        public int grabCollisions;
        public float placeTime;
        public int placeCollisions;
    };

    private readonly List<PlacedBarrel> m_PlacedBarrels = new();

    private int m_KnockOverCount = 0;
    private readonly List<float> m_KnockOverTimes = new();

    // Time
    private float m_TimeInDirMan = 0.0f;
    private float m_TimeInColObj = 0.0f;
    private float m_TimeInAttObj = 0.0f;

    // Interactions
    private int m_InteractionCount = 0;
    // Scaling and Snapping
    private int m_ScalingCount = 0;
    private int m_SnappingCount = 0;
    private int m_FocusObjectCount = 0;
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

                // Data Gathering
                if (m_Timer.SplitTime() != 0.0f)
                {
                    if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
                        m_TimeInDirMan += Time.deltaTime;
                    if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
                        m_TimeInColObj += Time.deltaTime;
                    if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
                        m_TimeInAttObj += Time.deltaTime;
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
        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);
        yield return new WaitForSeconds(1.0f);

        m_Table.SetActive(false);
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");
        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();

        m_Table.SetActive(true);
        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);
    }

    private void ClearData()
    {
        // Times
        m_PlacedBarrels.Clear();
        m_TimeInDirMan = 0.0f;
        m_TimeInColObj = 0.0f;
        m_TimeInAttObj = 0.0f;

        // Interactions
        m_InteractionCount = 0;
        m_SnappingCount = 0;
        m_ScalingCount = 0;
        m_FocusObjectCount = 0;
        m_InteractionDescriptions.Clear();

        // Errors
        m_CollisionsCount = 0;
        m_ErrorDescriptions.Clear();

        // Knockovers
        m_KnockOverCount = 0;
        m_KnockOverTimes.Clear();
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

            m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_ManipulationMode.mode.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "\n");
        }
    }

    public void RecordCollision(string description)
    {
        if (m_Running)
        {
            m_CollisionsCount++;
            m_ErrorDescriptions.Add(m_ParticipantNumber + "," + m_Technique.ToString() + "," + m_Timer.SplitTime().ToString() + "," + description);
        }
    }

    public void RecordGrabTime(string name)
    {
        bool isNew = true;
        foreach (var barrel in m_PlacedBarrels)
        {
            if (barrel.name == name)
            {
                isNew = false;
                break;
            }
        }

        if (isNew)
        {
            PlacedBarrel barrel = new()
            {
                name = name,
                grabTime = m_Timer.SplitTime(),
                grabCollisions = m_CollisionsCount,
                placeTime = 0.0f,
                placeCollisions = 0 
            };

            m_PlacedBarrels.Add(barrel);
        }
    }

    public void RecordPlaceTime(string name)
    {
        if (m_Running)
        {
            if (m_PlacedBarrels[^1].name == name)
            {
                if (m_PlacedBarrels[^1].placeTime == 0.0f)
                {
                    m_PlacedBarrels[^1].placeTime = m_Timer.SplitTime();
                    m_PlacedBarrels[^1].placeCollisions = m_CollisionsCount;
                }
            }
        }
    }

    public void RecordKnockOver()
    {
        m_KnockOverCount++;
        m_KnockOverTimes.Add(m_Timer.SplitTime());
    }

    public void RecordModifier(string modifier, bool value)
    {
        if (m_Running)
        {
            if (value)
            {
                if (modifier == "SCALING")
                    m_ScalingCount++;

                if (modifier == "SNAPPING")
                    m_SnappingCount++;
            }

            m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_ManipulationMode.mode.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "," + modifier + "\n");
        }
    }

    public void RecordFocusObject(string barrel, bool value)
    {
        if (m_Running)
        {
            if (value)
                m_FocusObjectCount++;

            m_InteractionDescriptions.Add(m_ParticipantNumber + "," + m_ManipulationMode.mode.ToString() + "," + m_Timer.SplitTime().ToString() + "," + value.ToString() + "," + "FOCUS," + barrel + "\n");
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
        string path = m_FilePathName + "Data\\" + m_ParticipantNumber + m_Technique.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create first file
        var sr = File.CreateText(path);

        //participant number, technique, barrel1, barrel2, barrel3, barrel4, barrel5, totaltime, interaction count, collision count, knockover count, snapping count, scaling count, focus object count, collision obj time, attachable obj time, direct time, knockover times
        string dataCSV = m_ParticipantNumber +
                         "," + m_Technique.ToString();

        if (m_PlacedBarrels.Count > 0)
            dataCSV += "," + m_PlacedBarrels[0].name +
                       "," + m_PlacedBarrels[0].grabTime.ToString() +
                       "," + m_PlacedBarrels[0].grabCollisions.ToString() +
                       "," + (m_PlacedBarrels[0].placeTime - m_PlacedBarrels[0].grabTime).ToString() +
                       "," + (m_PlacedBarrels[0].placeCollisions - m_PlacedBarrels[0].grabCollisions).ToString();
        else
            dataCSV += ",,0.0,0,0.0,0";

        for (var i = 1; i < 5; i++)
        {
            if (m_PlacedBarrels.Count > i)
            {
                dataCSV += "," + m_PlacedBarrels[i].name +
                           "," + (m_PlacedBarrels[i].grabTime - m_PlacedBarrels[i - 1].placeTime).ToString() +
                           "," + (m_PlacedBarrels[i].grabCollisions - m_PlacedBarrels[i - 1].placeCollisions).ToString();

                if (m_PlacedBarrels[i].placeTime > 0.0f)
                    dataCSV += "," + (m_PlacedBarrels[i].placeTime - m_PlacedBarrels[i].grabTime).ToString() +
                               "," + (m_PlacedBarrels[i].placeCollisions - m_PlacedBarrels[i].grabCollisions).ToString();
                else
                    dataCSV += ",0.0,0";
            }
            else
                dataCSV += ",,0.0,0,0.0,0";
        }

        dataCSV += ",Total Time:";
        if (m_PlacedBarrels.Count == 5 && m_PlacedBarrels[4].placeTime > 0.0f)
            dataCSV += "," + m_PlacedBarrels[4].placeTime.ToString();
        else
            dataCSV += "," + m_TimeLimit.ToString();

        // Interaction and Collision Count
        dataCSV += ",TotalCollisions:," + m_CollisionsCount.ToString() +
                   ",Interactions:," + m_InteractionCount.ToString();

        if (m_Technique != Mode.SIMPLEDIRECT)
            dataCSV += ",Snapping:," + m_SnappingCount.ToString() +
                       ",Scaling:," + m_ScalingCount.ToString();
        else
            dataCSV += ",Snapping:,null" +
                       ",Scaling:,null";

        if (m_Technique == Mode.CONSTRAINEDDIRECT)
            dataCSV += ",FocusObjs:," + m_FocusObjectCount.ToString() +
                       ",ColObjTime:," + m_TimeInColObj.ToString() +
                       ",AttObjTime:," + m_TimeInAttObj.ToString() +
                       ",IntTime:," + m_TimeInDirMan.ToString();
        else
            dataCSV += ",FocusObjs:,null" +
                       ",ColObjTime:,null" +
                       ",AttObjTime:,null" +
                       ",IntTime:,null";


        dataCSV += ",KnockOvers:," + m_KnockOverCount.ToString();
        foreach (var time in m_KnockOverTimes)
            dataCSV += "," + time.ToString();

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

        //change path for interaction descriptions
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Technique.ToString() + "INTERACTIONS.csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create second file
        sr = File.CreateText(path);

        //participant number, technique, time, value
        dataCSV = string.Empty;

        foreach (var grab in m_InteractionDescriptions)
            dataCSV += grab;

        if (m_PlacedBarrels.Count == 5)
            dataCSV += m_PlacedBarrels[4].placeTime.ToString();
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

        //participant number, technique, time, description
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
}