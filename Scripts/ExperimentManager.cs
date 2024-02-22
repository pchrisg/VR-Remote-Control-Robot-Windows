using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ManipulationModes;

public class ExperimentManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_Glassbox = null;
    public GameObject m_Objects = null;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantName = string.Empty;
    public Mode m_Technique = Mode.IDLE;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;

    [Header("Setup")]
    [SerializeField] private bool m_SetupTutorial = false;
    [SerializeField] private bool m_SetupTask1 = false;
    [SerializeField] private bool m_SetupTask2 = false;

    [Header("Reset")]
    [SerializeField] private bool m_Reset = false;

    [Header("Start/Stop")]
    [SerializeField] private bool m_Start = false;

    [Header("Status")]
    [SerializeField] private string m_Active = string.Empty;
    [SerializeField] private string m_Status = string.Empty;

    private bool m_TutorialActive = false;
    private bool m_Task1Active = false;
    private bool m_Task2Active = false;
    private bool m_Running = false;

    [HideInInspector] public const float ERRORTHRESHOLD = 0.05f; //5cm

    //Tutorial
    private Tutorial m_Tutorial = null;

    // Experiments
    private BarrelTask m_Task1 = null;
    private DominoTask m_Task2 = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Timer m_Timer = null;

    private readonly float m_TimeLimit = 600.0f;

    // File Path
    private readonly string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\";

    /// Data to Store
    // times
    private float m_TimeInDirMan = 0.0f;
    private float m_TimeInSDOFMan = 0.0f;
    private float m_TimeInRailCre = 0.0f;
    private float m_TimeInRailMan = 0.0f;
    private float m_TimeInColObj = 0.0f;
    private float m_TimeInAttObj = 0.0f;
    private float m_TimeTaken = 0.0f;

    // objects placed within bounds with their errors
    private string m_PlacedObjects = string.Empty;


    // Number of Grabs
    private bool isInteracting = false;
    private int m_GrabCount = 0;
    private List<string> m_GrabDescriptions = new();

    // Errors
    private int m_CollisionsCount = 0;
    private int m_OutOfBoundsCount = 0;
    private List<string> m_ErrorDescriptions = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Tutorial = transform.Find("Tutorial").GetComponent<Tutorial>();
        m_Task1 = transform.Find("Task1").GetComponent<BarrelTask>();
        m_Task2 = transform.Find("Task2").GetComponent<DominoTask>();

        //m_Table.SetActive(false);
        //m_Glassbox.SetActive(false);
        //m_Objects.SetActive(false);

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
                m_SetupTask1 = false;
                m_SetupTask2 = false;

                if (!m_TutorialActive)
                {
                    m_Task2.Setup(false);
                    m_Task2Active = false;

                    m_Task1.Setup(false);
                    m_Task1Active = false;

                    m_Tutorial.Setup(true);
                    m_TutorialActive = true;

                    m_Active = "Tutorial";
                    m_Status = "Standby";
                }
            }
                
            // Both tasks can't be active at same time
            if (m_SetupTask1 && m_SetupTask2)
            {
                m_SetupTask1 = false;
                m_SetupTask2 = false;
            }

            // Setup task 1
            if (m_SetupTask1)
            {
                m_SetupTask1 = false;

                if (!m_Task1Active)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;

                    m_Task2.Setup(false);
                    m_Task2Active = false;

                    m_Task1.Setup(true);
                    m_Task1Active = true;

                    m_Active = "Barrel Task";
                    m_Status = "Standby";
                }
            }

            // Setup experiment 2
            if (m_SetupTask2)
            {
                m_SetupTask2 = false;

                if (!m_Task2Active)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;

                    m_Task1.Setup(false);
                    m_Task1Active = false;

                    m_Task2.Setup(true);
                    m_Task2Active = true;

                    m_Active = "Domino Task";
                    m_Status = "Standby";
                }
            }

            // Reset the experiment
            if (m_Reset)
            {
                m_Reset = false;

                if (m_TutorialActive || m_Task1Active || m_Task2Active)
                {
                    if (m_TutorialActive)
                        m_Tutorial.ResetTutorial();
                    if (m_Task1Active)
                        m_Task1.ResetTask();
                    if (m_Task2Active)
                        m_Task2.ResetTask();

                    ResetRobotPose();
                }
            }

            // Start the experiment
            if (m_Start)
            {
                if (!m_TutorialActive && !m_Task1Active && !m_Task2Active)
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
            if (m_Task1Active || m_Task2Active)
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

                // Data Gathering
                if (m_ManipulationMode.mode == Mode.DIRECT)
                    m_TimeInDirMan += Time.deltaTime;
                if (m_ManipulationMode.mode == Mode.SDOF)
                    m_TimeInSDOFMan += Time.deltaTime;
                if (m_ManipulationMode.mode == Mode.RAILCREATOR)
                    m_TimeInRailCre += Time.deltaTime;
                if (m_ManipulationMode.mode == Mode.RAIL)
                    m_TimeInRailMan += Time.deltaTime;
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
        //m_Table.SetActive(false);
        //m_Glassbox.SetActive(false);
        //m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();

        yield return new WaitForSeconds(1.0f);

        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();
        //m_Table.SetActive(true);
        //m_Glassbox.SetActive(true);
        //m_Objects.SetActive(true);

        yield return null;
    }

    private void StartExperiment()
    {
        m_Timer.StartTimer(m_TimeLimit);

        m_TimeInDirMan = 0.0f;
        m_TimeInSDOFMan = 0.0f;
        m_TimeInRailCre = 0.0f;
        m_TimeInRailMan = 0.0f;
        m_TimeInColObj = 0.0f;
        m_TimeInAttObj = 0.0f;
    }

    private void RecordGrabTime(bool value)
    {
        m_GrabDescriptions.Add(m_Timer.SplitTime().ToString() + "," + value.ToString() + "\n");
    }

    public void AddPlacedObject(string name, float posErr, float rotErr)
    {
        m_PlacedObjects += m_Timer.SplitTime().ToString() + ", " + name + ", " + posErr.ToString() + ", " + rotErr.ToString() + "\n";
    }

    public void SplitTime()
    {
    }

    public void RecordCollision(string description)
    {
        if (m_Running)
        {
            m_CollisionsCount++;
            m_ErrorDescriptions.Add(m_Timer.SplitTime().ToString() + "," + description);
        }
    }

    public void RecordOutOfBounds()
    {
        if (m_Running)
        {
            m_OutOfBoundsCount++;
            m_ErrorDescriptions.Add(m_Timer.SplitTime().ToString() + ", Out of Bounds \n");
        }
    }

    public void SaveData()
    {
        if (m_Running)
        {
            if (m_Task1Active)
            {
                m_Task1Active = false;
                m_Task1.Setup(false);
            }

            else if (m_Task2Active)
            {
                m_Task2Active = false;
                m_Task2.Setup(false);
            }

            m_TimeTaken = m_Timer.SplitTime();

            m_Start = false;
            m_Running = false;
            m_Status = "Finished";
            m_Timer.StopTimer();

            //StartCoroutine(WriteToFile());
        }
    }

    /*IEnumerator WriteToFile()
    {
        yield return new WaitForSeconds(1.0f);

        string technique = string.Empty;
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
            technique = "_Simple";
        else
            technique = "_Ours";

        string path = m_FilePathName + m_Active + m_ParticipantNumber + technique + ".csv";
        m_Active = "None";

        if (File.Exists(path))
            File.Delete(path);

        // Create file
        var sr = File.CreateText(path);

        string dataCSV = "Placed Objects:\n Time, Name, Positional Error, Rotational Error \n";
        dataCSV += m_PlacedObjects + " \n";
        dataCSV += "Direct, SDOF, Rail Creator, Rail, ColObj, AttObj, Time Taken \n";
        dataCSV += m_TimeInDirMan.ToString() + ", "
                + m_TimeInSDOFMan.ToString() + ", "
                + m_TimeInRailCre.ToString() + ", "
                + m_TimeInRailMan.ToString() + ", "
                + m_TimeInColObj.ToString() + ", "
                + m_TimeInAttObj.ToString() + ", "
                + m_TimeTaken.ToString() + " \n\n";

        dataCSV += "Number of collisions:, " + m_CollisionsCount.ToString() + " \n";

        foreach (var collision in m_CollisionDescriptions)
            dataCSV += collision;

        // Write to File
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo fInfo = new(path)
        {
            IsReadOnly = true
        };

        //Close file
        sr.Close();

        yield return new WaitForSeconds(0.5f);

        //open the file
        //Application.OpenURL(path);

        //change path for grab descriptions
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Perspective.ToString() + m_Appearance.ToString() + "GRAB.csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create second file
        sr = File.CreateText(path);

        //collision descriptions
        dataCSV = string.Empty;

        foreach (var grab in m_GrabDescriptions)
            dataCSV += grab;

        dataCSV += m_Button4Time.ToString();

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
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Perspective.ToString() + m_Appearance.ToString() + "ERROR.csv";

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
    }*/
}