/**
 * Added to each robot joint
 */

using UnityEngine;

public class EmergencyStop : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_CollidingMat = null;

    [Header("Sounds")]
    [SerializeField] private AudioClip m_CollisionClip = null;

    private ROSPublisher m_ROSPublisher = null;
    private ExperimentManager m_ExperimentManager = null;
    private AudioSource m_AudioSource = null;

    private Renderer[] m_Renderers = null;
    private Material m_OriginalMat = null;

    float m_CollisionTime = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_AudioSource = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<AudioSource>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();
        m_OriginalMat = gameObject.transform.Find("Visuals").GetComponentInChildren<Renderer>().material;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent != null && other.transform.parent.tag != "Moveable")
        {
            m_CollisionTime = Time.time;
            string collisionDescription = gameObject.name + ", collided with, " + other.transform.parent.gameObject.name + " \n";

            m_ExperimentManager.m_CollisionsCount++;
            m_ExperimentManager.m_CollisionDescriptions.Add(collisionDescription);
            print(Time.time.ToString() + "Enter - " + collisionDescription);

            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_CollidingMat;

            m_AudioSource.clip = m_CollisionClip;
            m_AudioSource.Play();

            m_ROSPublisher.PublishEmergencyStop();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent != null && other.transform.parent.tag != "Moveable")
        {
            if (Time.time - m_CollisionTime >= m_ROSPublisher.m_LockedTime + 0.5f)
            {
                m_CollisionTime = Time.time;
                print(Time.time.ToString() + "Stay - " + gameObject.name + " collided with " + other.transform.parent.gameObject.name);

                m_ROSPublisher.PublishEmergencyStop();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.parent != null && other.transform.parent.tag != "Moveable")
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_OriginalMat;
        }
    }

}