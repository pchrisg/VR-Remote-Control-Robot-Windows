using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject m_Player = null;
    [SerializeField] private GameObject m_Table = null;
    [SerializeField] private GameObject m_Objects = null;
    [SerializeField] private GameObject m_TPPGuides = null;
    [SerializeField] private GameObject m_FVPGuides = null;

    [Header("Materials")]
    [SerializeField] private Material m_GlassMat = null;
    [SerializeField] private Material m_PlanRobMat = null;

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
    [SerializeField] private bool m_InPosition = false;
    [SerializeField] private bool m_Start = false;

    [Header("Status")]
    [SerializeField] private string m_Active = string.Empty;
    [SerializeField] private string m_Status = string.Empty;

    //Scripts
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;

    //GameObjects
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
    private float m_Button1Time = 0.0f;
    private float m_Button2Time = 0.0f;
    private float m_Button3Time = 0.0f;
    private float m_Button4Time = 0.0f;

    // Number of Grabs
    private bool isInteracting = false;
    private int m_GrabCount = 0;

    // Collisions - updated by EmergencyStop script
    [HideInInspector] public int m_CollisionsCount = 0;
    [HideInInspector] public List<string> m_CollisionDescriptions = new();

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
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
        Invoke(nameof(ResetRobotPose), 1.0f);
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
            if(m_SetupTutorial || m_SetupTask)
            {
                m_InPosition = false;
                ShowRobot(false);
                m_Manipulator.ShowManipulator(false);
                m_Table.SetActive(false);
                m_Objects.SetActive(false);
                SetupPerspective();

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
                // Setup task
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
                if ((!m_TutorialActive && !m_TaskActive) || !m_InPosition)
                    m_Start = false;

                else
                {
                    m_FVPGuides.SetActive(false);
                    m_TPPGuides.SetActive(false);

                    m_Table.SetActive(true);

                    if (m_TutorialActive)
                        m_Tutorial.StartTutorial();

                    if (m_TaskActive)
                        StartTask();

                    m_Running = true;
                    m_Status = "Running";
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

                m_Tutorial.ResetTutorial();
            }

            // if experiment
            if (m_TaskActive)
            {
                if(isInteracting != m_ManipulationMode.isInteracting)
                {
                    isInteracting = !isInteracting;
                    if (isInteracting)
                    {
                        if(m_GrabCount == 0)
                            m_Timer.StartTimer();

                        m_GrabCount++;
                    }
                }

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
        m_Table.SetActive(true);
        m_Objects.SetActive(true);
        StartCoroutine(ResetPose());
    }

    IEnumerator ResetPose()
    {
        bool tableActive = m_Table.activeSelf;
        bool objectsActive = m_Objects.activeSelf;

        m_Table.SetActive(false);
        m_Objects.SetActive(false);

        m_ROSPublisher.PublishResetPose();

        yield return new WaitForSeconds(1.0f);

        m_Manipulator.ResetPositionAndRotation();

        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        if (tableActive)
            m_Table.SetActive(true);
        if (objectsActive)
            m_Objects.SetActive(true);
    }

    private void Setup(bool value)
    {
        if (value)
        {
            m_Objects.SetActive(true);
            ResetTask();
        }

        m_Objects.SetActive(false);
    }

    private void SetupPerspective()
    {
        if (m_Perspective == Perspective.FRONTVIEW)
        {
            m_Player.transform.SetPositionAndRotation(new Vector3(0.0f, -0.972f, -0.1f), Quaternion.Euler(0.0f, 0.0f, 0.0f));
            m_FVPGuides.SetActive(true);
        }
        else if (m_Perspective == Perspective.THIRDPERSON)
        {
            m_Player.transform.SetPositionAndRotation(new Vector3(0.0f, -0.972f, 0.0f), Quaternion.Euler(0.0f, 180.0f, 0.0f));
            m_TPPGuides.SetActive(true);
        }
    }

    private void SetupAppearance()
    {
        if (m_Appearance == Appearance.OPAQUE)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();
        }
        else if (m_Appearance == Appearance.TRANSPARENT)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance(m_GlassMat);

            foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();
        }
        else if (m_Appearance == Appearance.INVISIBLE)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance(m_PlanRobMat);

            foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();
        }
    }

    public void ShowRobot(bool value)
    {
        if (value)
            SetupAppearance();
        else
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance(m_PlanRobMat);
        }
    }

    private void ResetTask()
    {
        foreach (var button in gameObject.GetComponentsInChildren<Pressable>())
            button.ResetButton();
    }

    private void StartTask()
    {
        m_Objects.SetActive(true);
        ShowRobot(true);
        m_Manipulator.ShowManipulator(true);
    }

    public void SnapTime()
    {
        if (m_Button1Time == 0.0f)
            m_Button1Time = m_Timer.GetTime();
        else if (m_Button2Time == 0.0f)
            m_Button2Time = m_Timer.GetTime();
        else if (m_Button3Time == 0.0f)
            m_Button3Time = m_Timer.GetTime();
    }

    public void SaveData()
    {
        if (m_Running)
        {
            if (m_TaskActive)
            {
                m_InPosition = false;
                m_TaskActive = false;
                Setup(false);
            }

            m_Button4Time = m_Timer.GetTime();

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

        //set path
        string path = m_FilePathName + "Data\\" + m_ParticipantNumber + m_Perspective.ToString() + m_Appearance.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create first file
        var sr = File.CreateText(path);

        //participant number, perspective, appearance, button1, button2, button3, button4, total time, number of grabs, collision number
        string dataCSV = m_ParticipantNumber + "," +
                         m_Perspective.ToString() + "," + 
                         m_Appearance.ToString() + "," +
                         m_Button1Time.ToString() + "," +
                         (m_Button2Time - m_Button1Time).ToString() + "," +
                         (m_Button3Time - m_Button2Time).ToString() + "," +
                         (m_Button4Time - m_Button3Time).ToString() + "," +
                         m_Button4Time.ToString() + "," +
                         m_GrabCount.ToString() + "," +
                         m_CollisionsCount;

        // Write to file
        sr.WriteLine(dataCSV);

        //Change to Readonly
        FileInfo fInfo = new(path)
        {
            IsReadOnly = true
        };

        //Close first file
        sr.Close();

        yield return new WaitForSeconds(0.5f);

        //open the file
        //Application.OpenURL(path);

        //change path
        path = m_FilePathName + "Descriptions\\" + m_ParticipantNumber + m_Perspective.ToString() + m_Appearance.ToString() + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        // Create second file
        sr = File.CreateText(path);

        //collision descriptions
        dataCSV = string.Empty;

        foreach (var collision in m_CollisionDescriptions)
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

        yield return new WaitForSeconds(0.5f);

        m_Button1Time = 0.0f;
        m_Button2Time = 0.0f;
        m_Button3Time = 0.0f;
        m_Button4Time = 0.0f;

        m_GrabCount = 0;

        m_CollisionsCount = 0;
        m_CollisionDescriptions.Clear();
    }
}