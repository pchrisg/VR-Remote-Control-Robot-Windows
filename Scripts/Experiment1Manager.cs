using System.Collections;
using System.IO;
using UnityEngine;

public class Experiment1Manager : MonoBehaviour
{
    public enum Perspective
    {
        THIRDPERSON,
        FRONTVIEW
    }

    public enum Appearance
    {
        OPAQUE,
        TRANSPARENT,
        INVISIBLE
    }

    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_Objects = null;

    [Header("Materials")]
    [SerializeField] private Material m_Glass = null;
    [SerializeField] private Material m_PlanningRobot = null;

    [Header("Participant")]
    [SerializeField] private string m_ParticipantNumber = string.Empty;

    [Header("Perspective")]
    [SerializeField] private Perspective m_Perspective = Perspective.FRONTVIEW;

    [Header("Appearance")]
    [SerializeField] private Appearance m_Appearance = Appearance.OPAQUE;

    [Header("Robot Control")]
    [SerializeField] private bool m_ResetRobotPose = false;

    [Header("Setup")]
    [SerializeField] private bool m_SetupTutorial;
    [SerializeField] private bool m_SetupTask;

    [Header("Reset")]
    [SerializeField] private bool m_Reset = false;

    [Header("Start/Stop")]
    [SerializeField] private bool m_Start = false;

    [Header("Status")]
    [SerializeField] private string m_Active = string.Empty;
    [SerializeField] private string m_Status = string.Empty;

    private GameObject m_UR5 = null;
    private GameObject m_Robotiq = null;

    private bool m_TutorialActive = false;
    private bool m_TaskActive = false;
    private bool m_Running = false;

    //Tutorial
    private Tutorial1 m_Tutorial = null;

    private ROSPublisher m_ROSPublisher = null;
    private Timer m_Timer = null;

    // File Path
    private readonly string m_FilePathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiments\\Experiment1\\";

    // Time
    private float m_2ndButtonTime = 0.0f;
    private float m_3rdButtonTime = 0.0f;

    private void Awake()
    {
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_Tutorial = gameObject.transform.Find("Tutorial").GetComponent<Tutorial1>();
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Table.SetActive(false);
        m_Objects.SetActive(false);

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

        if(!m_Running)
        {
            // Setup tutorial
            if (m_SetupTutorial)
            {
                m_SetupTutorial = false;
                m_SetupTask = false;

                if (!m_TutorialActive)
                {
                    Setup(false);
                    m_TaskActive = false;

                    m_Tutorial.Setup(true);
                    m_TutorialActive = true;

                    m_Active = "Tutorial";
                    m_Status = "Standby";
                }
            }
            // Setup task 1
            if (m_SetupTask)
            {
                m_SetupTask = false;

                if (m_TutorialActive)
                {
                    m_Tutorial.Setup(false);
                    m_TutorialActive = false;
                }

                Setup(true);
                m_TaskActive = true;

                m_Active = m_Perspective.ToString() + m_Appearance.ToString();
                m_Status = "Standby";
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
                        ResetTask();

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

                    //if (m_TutorialActive)
                    //    m_Tutorial.stage = TutorialStages.Stage.START;
                }
            }
        }
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
                // if experiment stopped
                if (!m_Start)
                {
                    m_Running = false;
                    m_Status = "Stopped";

                    m_Timer.StopTimer();
                    ResetTask();
                }
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
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();

        yield return new WaitForSeconds(1.0f);

        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPositionAndRotation();
        m_Table.SetActive(true);

        yield return null;
    }

    private void Setup(bool value)
    {
        m_Objects.SetActive(value);

        if (value)
        {
            if (m_Perspective == Perspective.FRONTVIEW)
            {

            }
            else if (m_Perspective == Perspective.THIRDPERSON)
            {

            }

            if (m_Appearance == Appearance.OPAQUE)
            {
                foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                    joint.ChangeAppearance();
            }
            else if (m_Appearance == Appearance.TRANSPARENT)
            {
                foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                    joint.ChangeAppearance(m_Glass);

                foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                    joint.ChangeAppearance();
            }
            else if (m_Appearance == Appearance.INVISIBLE)
            {
                foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                    joint.ChangeAppearance(m_PlanningRobot);

                foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                    joint.ChangeAppearance();
            }

            ResetTask();
        }
    }

    private void ResetTask()
    {
        foreach (var button in gameObject.GetComponentsInChildren<Button>())
            button.ResetButton();
    }

    public void StartTimer()
    {
        m_Timer.StartTimer();
    }

    public void SnapTime()
    {
        m_2ndButtonTime = m_Timer.GetTime();
    }

    public void SaveData()
    {
        if (m_Running)
        {
            if (m_TaskActive)
            {
                m_TaskActive = false;
                Setup(false);
            }

            m_3rdButtonTime = m_Timer.GetTime();

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

        string path = m_FilePathName + m_ParticipantNumber + m_Perspective.ToString() + m_Appearance.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create file
        var sr = File.CreateText(path);

        string dataCSV = m_ParticipantNumber + "," +
                         m_Perspective.ToString() + "," + 
                         m_Appearance.ToString() + "," +
                         m_2ndButtonTime.ToString() + "," + 
                         (m_3rdButtonTime - m_2ndButtonTime).ToString() + "," + 
                         m_3rdButtonTime.ToString();

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
