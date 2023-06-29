using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private string mm = string.Empty, ss = string.Empty;

    [HideInInspector] public Text m_Text = null;
    [HideInInspector] public float m_TimeLeft = 0.0f;

    private void Awake()
    {
        m_Text = gameObject.GetComponent<Text>();
    }

    private void Start()
    {
        mm = (0.0f).ToString("0");
        ss = (0.0f).ToString("00");
    }

    private void Update()
    {
        if (m_TimeLeft > 0.0f)
        {
            m_TimeLeft -= Time.deltaTime;
            mm = Mathf.Floor((m_TimeLeft / 60.0f) % 60.0f).ToString("0");
            ss = (m_TimeLeft % 60).ToString("00");
            m_Text.text = mm + ":" + ss;
        }
        else
            m_TimeLeft = 0.0f;
    }
}
