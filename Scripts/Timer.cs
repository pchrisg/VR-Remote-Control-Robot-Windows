using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private Text m_Text = null;

    private float m_TimeElapsed = 0.0f;
    private float m_TimeLimit = 0.0f;
    

    private string m_Mins = string.Empty;
    private string m_Secs = string.Empty;

    private bool m_Running = false;
    private bool m_TimeExhausted = false;

    private void Awake()
    {
        m_Text = gameObject.GetComponentInChildren<Text>();
    }

    private void Start()
    {
        m_Mins = (0.0f).ToString("0");
        m_Secs = (0.0f).ToString("00");
        m_Text.text = "Ready";
    }

    private void Update()
    {
        if (m_Running)
        {
            if (m_TimeLimit == 0.0f || m_TimeElapsed < m_TimeLimit)
            {
                m_TimeElapsed += Time.deltaTime;
                m_Mins = Mathf.Floor((m_TimeElapsed / 60.0f) % 60.0f).ToString("0");
                m_Secs = (m_TimeElapsed % 60).ToString("00");
                m_Text.text = m_Mins + ":" + m_Secs;
            }
            else
                StopTimer();
        }
    }

    public void ResetTimer()
    {
        m_Running = false;
        m_TimeExhausted = false;
        m_TimeElapsed = 0.0f;
        m_TimeLimit = 0.0f;
        m_Text.text = "Ready";
    }

    public void StartTimer(float timeLimit = 0.0f)
    {
        m_TimeLimit = timeLimit;
        m_Running = true;
    }

    public float SplitTime()
    {
        return m_TimeElapsed;
    }

    public void StopTimer()
    {
        if (m_TimeLimit != 0.0f && m_TimeElapsed >= m_TimeLimit)
        {
            m_TimeExhausted = true;
            m_TimeElapsed = m_TimeLimit;
        }

        m_Text.text = "Stop";
        m_Running = false;
    }

    public bool TimeExhausted()
    {
        return m_TimeExhausted;
    }
}