using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Experiment1 : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Glassbox = null;
    [SerializeField] private GameObject m_Objects = null;
    [SerializeField] private Timer m_Timer = null;
    [SerializeField] private ManipulationMode m_ManipulationMode = null;
    [SerializeField] private InsideCheck m_InsideChecker = null;

    private ROSPublisher m_ROSPublisher = null;

    private float m_TotalTime = 300.0f;

    // File Variables
    private string m_PathName = "C:\\Users\\Chris\\Dropbox\\Deg_PhD\\Experiment_01\\";
    [SerializeField] private string m_Filename = string.Empty;
    private bool writingToFile = false;

    // Data to Store
    private float m_TimeInDirMan = 0.0f;
    private float m_TimeInSDOFMan = 0.0f;
    private float m_TimeInRailCre = 0.0f;
    private float m_TimeInRailMan = 0.0f;
    private float m_TimeInColObj = 0.0f;
    private float m_TimeInAttObj = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();

        m_Timer.m_TimeLeft = m_TotalTime;

        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);
    }

    private void Start()
    {
        Invoke("ResetExperiment", 1.0f);
        m_InsideChecker.m_NumberOfBarrels = 1;
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.DIRECT)
            m_TimeInDirMan += Time.deltaTime;
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.SDOF)
            m_TimeInSDOFMan += Time.deltaTime;
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.RAILCREATOR)
            m_TimeInRailCre += Time.deltaTime;
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.RAIL)
            m_TimeInRailMan += Time.deltaTime;
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.COLOBJCREATOR)
            m_TimeInColObj += Time.deltaTime;
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.ATTOBJCREATOR)
            m_TimeInAttObj += Time.deltaTime;

        if (m_InsideChecker.allObjectsInside && !writingToFile)
        {
            writingToFile = true;
            StartCoroutine(WriteToFile());
        }
    }

    private void ResetExperiment()
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

        GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>().ResetPosition();

        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);

        yield return null;
    }

    IEnumerator WriteToFile()
    {
        string path = m_PathName + m_Filename + ".csv";

        if (File.Exists(path))
            File.Delete(path);

        //Create the file
        var sr = File.CreateText(path);

        string dataCSV = "Direct, SDOF, Rail Creator, Rail, ColObj, AttObj, Total Time \n";
        dataCSV += m_TimeInDirMan.ToString() + ", "
                + m_TimeInSDOFMan.ToString() + ", "
                + m_TimeInRailCre.ToString() + ", "
                + m_TimeInRailMan.ToString() + ", "
                + m_TimeInColObj.ToString() + ", "
                + m_TimeInAttObj.ToString() + ", "
                + (m_TotalTime - m_Timer.m_TimeLeft).ToString() + " \n";

        sr.WriteLine(dataCSV);

        //Change to Readonly
        //FileInfo fInfo = new FileInfo(path);
        //fInfo.IsReadOnly = true;

        //Close the file
        sr.Close();

        yield return new WaitForSeconds(0.5f);

        //open the file
        Application.OpenURL(path);
    }
}