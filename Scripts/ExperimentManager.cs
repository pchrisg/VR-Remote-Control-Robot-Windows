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

    [Header("Participant")]
    [SerializeField] private string m_ParticipantName = string.Empty;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;

    [Header("Setup")]
    [SerializeField] private bool m_SetupTutorial = false;
    [SerializeField] private bool m_SetupExperiment1 = false;
    [SerializeField] private bool m_SetupExperiment2 = false;

    [Header("Reset")]
    [SerializeField] private bool m_ResetTutorial = false;
    [SerializeField] private bool m_ResetExperiment = false;

    [Header("Start")]
    [SerializeField] private bool m_StartTutorial = false;
    [SerializeField] private bool m_StartExperiment = false;

    [Header("Status")]
    [SerializeField] private bool m_TutorialActive = false;
    [SerializeField] private bool m_Experiment1Active = false;
    [SerializeField] private bool m_Experiment2Active = false;
    [SerializeField] private bool m_Running = false;

    [HideInInspector] public const float ERRORTHRESHOLD = 0.05f; //5cm

    //Tutorial
    private Tutorial m_Tutorial = null;

    // Experiments
    private Experiment1 m_Experiment1 = null;
    private Experiment2 m_Experiment2 = null;

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

    // collisions - updated by EmergencyStop script
    [HideInInspector] public int m_CollisionsCount = 0;
    [HideInInspector] public List<string> m_CollisionDescriptions = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Tutorial = transform.Find("Tutorial").GetComponent<Tutorial>();
        m_Experiment1 = transform.Find("Experiment1").GetComponent<Experiment1>();
        m_Experiment2 = transform.Find("Experiment2").GetComponent<Experiment2>();

        m_Table.SetActive(false);
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);
    }

    private void Start()
    {
        Invoke("ResetRobotPose", 1.0f);

        m_Timer.m_TimeLimit = m_TimeLimit;
        m_Timer.m_TimePassed = m_TimeLimit;
        m_Timer.m_Text.text = "Ready";
    }

    private void Update()
    {
        // Reset Robot Pose
        if (m_ResetRobotPose)
        {
            m_ResetRobotPose = false;
            ResetRobotPose();
        }

        // If no tutorial or experiment are running
        if (!m_Running)
        {
            // Setup tutorial
            if (m_SetupTutorial)
            {
                m_SetupTutorial = false;
                m_SetupExperiment1 = false;
                m_SetupExperiment2 = false;

                if (!m_TutorialActive)
                {
                    m_Experiment2.Setup(false);
                    m_Experiment2Active = false;

                    m_Experiment1.Setup(false);
                    m_Experiment1Active = false;

                    m_Tutorial.Setup(true);
                    m_TutorialActive = true;
                }
            }

            // Reset tutorial
            if (m_ResetTutorial)
            {
                m_ResetTutorial = false;

                if (m_TutorialActive)
                {
                    m_Tutorial.ResetTutorial();
                    ResetRobotPose();
                }
            }

            // Start the tutorial
            if (m_StartTutorial)
            {
                if (!m_TutorialActive)
                    m_StartTutorial = false;

                else
                {
                    m_Running = true;
                    m_Tutorial.stage = TutorialStages.Stage.START;
                }
            }
                
            // Both experiments can't be active at same time
            if (m_SetupExperiment1 && m_SetupExperiment2)
            {
                m_SetupExperiment1 = false;
                m_SetupExperiment2 = false;
            }

            // Setup experiment 1
            if (m_SetupExperiment1)
            {
                m_SetupExperiment1 = false;

                if (!m_Experiment1Active)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;

                    m_Experiment2.Setup(false);
                    m_Experiment2Active = false;

                    m_Experiment1.Setup(true);
                    m_Experiment1Active = true;
                }
            }

            // Setup experiment 2
            if (m_SetupExperiment2)
            {
                m_SetupExperiment2 = false;

                if (!m_Experiment2Active)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;

                    m_Experiment1.Setup(false);
                    m_Experiment1Active = false;

                    m_Experiment2.Setup(true);
                    m_Experiment2Active = true;
                }
            }

            // Reset the experiment
            if (m_ResetExperiment)
            {
                m_ResetExperiment = false;

                if (m_Experiment1Active || m_Experiment2Active)
                {
                    if (m_Experiment1Active)
                        m_Experiment1.ResetExperiment();
                    if (m_Experiment2Active)
                        m_Experiment2.ResetExperiment();

                    ResetRobotPose();
                }
            }

            // Start the experiment
            if (m_StartExperiment)
            {
                if (!m_Experiment1Active && !m_Experiment2Active)
                    m_StartExperiment = false;

                else
                {
                    m_Running = true;
                    StartExperiment();
                }
            }
        }
        // if tutorial or experiment are running
        else if (m_Running)
        {
            // if tutorial stopped
            if (m_TutorialActive && !m_StartTutorial)
                m_Running = false;

            // if experiment
            if (m_Experiment1Active || m_Experiment2Active)
            {
                // if time exhausted
                if (m_Timer.m_TimePassed == m_TimeLimit)
                    SaveData();

                // if experiment stopped
                if (!m_StartExperiment)
                {
                    m_Running = false;

                    m_Timer.m_TimePassed = m_TimeLimit;
                    m_Timer.m_Text.text = "Ready";
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

    public void AddPlacedObject(string name, float posErr, float rotErr)
    {
        m_PlacedObjects += m_Timer.m_TimePassed.ToString() + ", " + name + ", " + posErr.ToString() + ", " + rotErr.ToString() + "\n";
    }

    private void StartExperiment()
    {
        m_Timer.m_TimePassed = 0.0f;

        m_TimeInDirMan = 0.0f;
        m_TimeInSDOFMan = 0.0f;
        m_TimeInRailCre = 0.0f;
        m_TimeInRailMan = 0.0f;
        m_TimeInColObj = 0.0f;
        m_TimeInAttObj = 0.0f;
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

        yield return new WaitForSeconds(0.5f);

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPosition();

        m_Table.SetActive(true);
        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);

        yield return null;
    }

    public void SaveData()
    {
        if (m_Running)
        {
            m_TimeTaken = m_Timer.m_TimePassed;

            m_StartExperiment = false;
            m_Running = false;
            m_Timer.m_TimePassed = m_TimeLimit;
            m_Timer.m_Text.text = "End";

            StartCoroutine(WriteToFile());
        }
    }

    IEnumerator WriteToFile()
    {
        yield return new WaitForSeconds(1.0f);

        string experiment = string.Empty;
        if (m_Experiment1Active)
        {
            m_Experiment1Active = false;
            m_Experiment1.Setup(false);
            experiment = "PicknPlace\\";
        }

        else if (m_Experiment2Active)
        {
            m_Experiment2Active = false;
            m_Experiment2.Setup(false);
            experiment = "Stacking\\";
        }

        string technique = string.Empty;
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
            technique = "_Simple";
        else
            technique = "_Ours";

        string path = m_FilePathName + experiment + m_ParticipantName + technique + ".csv";

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
    }
}
