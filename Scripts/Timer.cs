using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] private Text m_Timer;
    private string mm, ss;

    [HideInInspector]public float m_TimeLeft;

    void Start()
    {
        mm = (0.0f).ToString("0");
        ss = (0.0f).ToString("00");
    }

    void Update()
    {
        if (m_TimeLeft > 0.0f)
        {
            m_TimeLeft -= Time.deltaTime;
            mm = Mathf.Floor((m_TimeLeft / 60.0f) % 60.0f).ToString("0");
            ss = (m_TimeLeft % 60).ToString("00");
            m_Timer.text = mm + ":" + ss;
        }
        else
            m_Timer.text = "0:00";
    }
}
