using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{

    private string mm, ss;

    public Text timer;
    public float timeLeft;

    void Start()
    {
        mm = (0.0f).ToString("0");
        ss = (0.0f).ToString("00");
    }

    void Update()
    {
        if (timeLeft > 0.0f)
        {
            timeLeft -= Time.deltaTime;
            mm = Mathf.Floor((timeLeft / 60.0f) % 60.0f).ToString("0");
            ss = (timeLeft % 60).ToString("00");
            timer.text = mm + ":" + ss;
        }
        else
            timer.text = "0:00";
    }
}
