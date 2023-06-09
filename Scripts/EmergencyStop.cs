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
    private AudioSource m_AudioSource = null;

    Renderer[] m_Renderers = null;
    Material m_OriginalMat = null;

    float m_CollisionTime = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_AudioSource = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<AudioSource>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();
        m_OriginalMat = gameObject.transform.Find("Visuals").GetComponentInChildren<Renderer>().material;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Moveable")
        {
            m_CollisionTime = Time.time;
            print(gameObject.name + " collided with " + collision.gameObject.name);

            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_CollidingMat;

            m_AudioSource.clip = m_CollisionClip;
            m_AudioSource.Play();

            m_ROSPublisher.PublishEmergencyStop();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag != "Moveable")
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.material = m_OriginalMat;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag != "Moveable")
        {
            if(Time.time - m_CollisionTime >= 5f)
            {
                m_CollisionTime = Time.time;
                print(gameObject.name + " still in collision with " + collision.gameObject.name);

                m_AudioSource.clip = m_CollisionClip;
                m_AudioSource.Play();

                m_ROSPublisher.PublishEmergencyStop();
            }
        }
    }
}