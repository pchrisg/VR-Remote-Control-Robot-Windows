using System.Collections;
using TMPro;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PlanningFeedback : MonoBehaviour
{
    private RobotFeedback m_RobotFeedback = null;

    private Transform m_Wrist2 = null;
    private Transform m_Player = null;

    private GameObject m_Canvas = null;
    private TextMeshPro m_Text = null;
    private Coroutine m_ActiveCoroutine = null;

    private void Awake()
    {
        m_RobotFeedback = GameObject.FindGameObjectWithTag("RobotFeedback").GetComponent<RobotFeedback>();

        m_Wrist2 = GameObject.FindGameObjectWithTag("Robotiq").transform.parent.parent;
        m_Player = Player.instance.headCollider.transform;

        m_Canvas = gameObject.transform.GetChild(0).gameObject;
        m_Text = gameObject.GetComponentInChildren<TextMeshPro>();
        m_Canvas.SetActive(false);
    }

    private void Update()
    {
        gameObject.transform.SetPositionAndRotation(m_Wrist2.position + Vector3.up * 0.16f, Quaternion.LookRotation(gameObject.transform.position - m_Player.position, Vector3.up));
    }

    private void Flash(bool value)
    {
        m_Text.text = "* Planning *";

        if (value)
            m_ActiveCoroutine ??= StartCoroutine(FlashCoroutine());

        else
        {
            if (m_ActiveCoroutine != null)
                StopCoroutine(m_ActiveCoroutine);

            m_ActiveCoroutine = null;

            m_Canvas.SetActive(false);
        }
    }

    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            m_Canvas.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            m_Canvas.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TimedOut(bool value)
    {
        if (value)
        {
            if (m_RobotFeedback.IsColliding() || m_RobotFeedback.IsOutOfBounds())
            {
                m_Canvas.SetActive(true);
                m_Text.text = "!!! Unattainable Goal !!!";
            }
            else
                Flash(value);
        }
        else
            Flash(value);
    }
}