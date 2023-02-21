using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ActionResultUnity = RosMessageTypes.Moveit.MoveGroupActionResultMsg;

public class ResultSubscriber : MonoBehaviour
{
    private string m_StatusTopic = null;
    private ROSConnection m_Ros = null;

    [Header("Scene Object")]
    [SerializeField] private GameObject m_test = null;

    [Header("Materials")]
    [SerializeField] private Material m_LightGrey = null;
    [SerializeField] private Material m_OutOfBoundsMaterial = null;

    private void Awake()
    {
        m_StatusTopic = "/move_group/result";
        m_Ros = ROSConnection.GetOrCreateInstance();
    }

    private void Start()
    {
        m_Ros.Subscribe<ActionResultUnity>(m_StatusTopic, CheckResult);
    }

    private void OnDestroy()
    {
        m_Ros.Unsubscribe(m_StatusTopic);
    }

    private void CheckResult(ActionResultUnity message)
    {
        print(message.status.text);
        if(message.status.text == "No motion plan found. No execution attempted.")
        {
            m_test.GetComponent<Renderer>().material = m_OutOfBoundsMaterial;
        }
        else
        {
            m_test.GetComponent<Renderer>().material = m_LightGrey;
        }
    }
}
