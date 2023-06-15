using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Experiment1 : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject m_Glassbox = null;
    [SerializeField] private GameObject m_Objects = null;

    private ROSPublisher m_ROSPublisher = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();

        m_Glassbox.SetActive(false);
        m_Objects.SetActive(false);
    }

    private void Start()
    {
        Invoke("ResetExperiment", 1.0f);
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

        GameObject.FindGameObjectWithTag("EndEffector").GetComponent<EndEffector>().ResetPosition();

        m_Glassbox.SetActive(true);
        m_Objects.SetActive(true);

        yield return null;
    }
}