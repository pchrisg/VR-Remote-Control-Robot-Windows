
using System.Xml.Serialization;
using UnityEngine;

public class UR5Robot : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip m_MotionClip = null;

    private AudioSource m_AudioSource = null;
    private Transform m_Robotiq = null;
    private Manipulator m_Manipulator = null;
    private ResultSubscriber m_ResultSubscriber = null;

    private Vector3 m_PreviousPosition = new();
    private bool m_isMoving = false;
    private float m_ElapsedTime = 0.0f;

    private void Awake()
    {
        m_AudioSource = gameObject.GetComponent<AudioSource>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ResultSubscriber = GameObject.FindGameObjectWithTag("ROS").GetComponent<ResultSubscriber>();

        m_PreviousPosition = m_Robotiq.position;
    }

    private void Update()
    {
        if (!m_isMoving && Vector3.Distance(m_Robotiq.position, m_PreviousPosition) > 0.001f)
            m_isMoving = true;

        if (m_isMoving)
        {
            if (!m_AudioSource.isPlaying)
            {
                if (m_AudioSource.clip != m_MotionClip)
                    m_AudioSource.clip = m_MotionClip;

                if (!m_ResultSubscriber.isPlanExecuted)
                {
                    m_ResultSubscriber.isPlanExecuted = true;
                    //m_Manipulator.Colliding(false);
                }

                m_AudioSource.Play();
            }

            if (Vector3.Distance(m_Robotiq.position, m_PreviousPosition) < 0.001f)
            {
                m_isMoving = false;
                m_ElapsedTime = 0.1f;
            }
            else
                m_PreviousPosition = m_Robotiq.position;
        }
        else if (m_ElapsedTime != 0.0f)
        {
            m_ElapsedTime -= Time.deltaTime;
            
            if(m_ElapsedTime <= 0.0f)
            {
                m_ElapsedTime = 0.0f;
                if (m_AudioSource.isPlaying)
                    m_AudioSource.Stop();
            }
        }
    }
}
