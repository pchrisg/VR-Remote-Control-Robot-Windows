using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ManipulationOptions;

public class ExperimentManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Glassbox = null;
    [SerializeField] private GameObject m_Objects = null;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantName = string.Empty;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;

    [Header("Experiment Control")]
    [SerializeField] private bool m_RunExperiment = false;
    [SerializeField] private bool m_ResetExperiment = false;
    [SerializeField] private bool m_SetupExperiment1 = false;
    [SerializeField] private bool m_SetupExperiment2 = false;

    [Header("Experiment Status")]
    [SerializeField] private bool m_ExperimentActive = false;
    [SerializeField] private bool m_Experiment1Active = false;
    [SerializeField] private bool m_Experiment2Active = false;

    // Experiments
    private Experiment1 m_Experiment1 = null;
    private Experiment2 m_Experiment2 = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Timer m_Timer = null;

    private float m_TotalTime = 300.0f;

    // File Path
    private string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\";

    /// Data to Store
    // times
    private float m_TimeInDirMan = 0.0f;
    private float m_TimeInSDOFMan = 0.0f;
    private float m_TimeInRailCre = 0.0f;
    private float m_TimeInRailMan = 0.0f;
    private float m_TimeInColObj = 0.0f;
    private float m_TimeInAttObj = 0.0f;
    private float m_TimeTaken = 0.0f;

    // collisions updated by EmergencyStop script
    [HideInInspector] public int m_CollisionsCount = 0;
    [HideInInspector] public List<string> m_CollisionDescriptions = new List<string>();

    // number of objects placed updated by ConditionChecker scripts
    [HideInInspector] public int m_PlacedObjectsCount = 0;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Experiment1 = transform.Find("Experiment1").GetComponent<Experiment1>();
        m_Experiment2 = transform.Find("Experiment2").GetComponent<Experiment2>();

        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);
    }

    private void Start()
    {
        Invoke("ResetRobotPose", 1.0f);

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

        if (!m_ExperimentActive)
        {
            if (m_SetupExperiment1 && m_SetupExperiment2)
            {
                m_SetupExperiment1 = false;
                m_SetupExperiment2 = false;
            }

            if (m_SetupExperiment1)
            {
                m_SetupExperiment1 = false;

                if (!m_Experiment1Active)
                {
                    m_Experiment2.Setup(false);
                    m_Experiment2Active = false;

                    m_Experiment1.Setup(true);
                    m_Experiment1Active = true;
                }
            }

            if (m_SetupExperiment2)
            {
                m_SetupExperiment2 = false;

                if (!m_Experiment2Active)
                {
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

                if (m_Experiment1Active)
                    m_Experiment1.ResetExperiment();
                if (m_Experiment2Active)
                    m_Experiment2.ResetExperiment();

                ResetRobotPose();
            }

            if (m_RunExperiment)
            {
                StartExperiment();
                m_ExperimentActive = true;
            }
        }
        else
        {
            if(m_Timer.m_TimeLeft <= 0.0f)
                SaveData();

            if (!m_RunExperiment)
            {
                m_Timer.m_TimeLeft = 0.0f;
                m_Timer.m_Text.text = "Ready";
                m_ExperimentActive = false;
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

    private void StartExperiment()
    {
        m_Timer.m_TimeLeft = m_TotalTime;

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
        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();

        yield return new WaitForSeconds(1.0f);

        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        yield return new WaitForSeconds(0.5f);

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPosition();

        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);

        yield return null;
    }

    public void SaveData()
    {
        if (m_ExperimentActive)
        {
            m_TimeTaken = m_TotalTime - m_Timer.m_TimeLeft;
            m_RunExperiment = false;
            m_ExperimentActive = false;
            StartCoroutine(WriteToFile());
        }
    }

    IEnumerator WriteToFile()
    {
        yield return new WaitForSeconds(1.0f);

        string experiment = string.Empty;
        if (m_Experiment1Active)
        {
            m_Experiment1.Setup(false);
            experiment = "PicknPlace\\";
        }
            
        else if (m_Experiment2Active)
        {
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

        string dataCSV = "Number of Objects Placed:, " + m_PlacedObjectsCount.ToString() + " \n\n";
        dataCSV += "Direct, SDOF, Rail Creator, Rail, ColObj, AttObj, Time Taken \n";
        dataCSV += m_TimeInDirMan.ToString() + ", "
                + m_TimeInSDOFMan.ToString() + ", "
                + m_TimeInRailCre.ToString() + ", "
                + m_TimeInRailMan.ToString() + ", "
                + m_TimeInColObj.ToString() + ", "
                + m_TimeInAttObj.ToString() + ", "
                + m_TimeTaken.ToString() + " \n\n";

        dataCSV += "Number of collisions:, " + m_CollisionsCount.ToString() + " \n";

        foreach(var collision in m_CollisionDescriptions)
            dataCSV += collision;

        // Write to File
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo fInfo = new FileInfo(path);
        fInfo.IsReadOnly = true;

        //Close file
        sr.Close();

        yield return new WaitForSeconds(0.5f);

        //open the file
        //Application.OpenURL(path);

        m_Timer.m_TimeLeft = 0.0f;
        m_Timer.m_Text.text = "End";
    }
}
