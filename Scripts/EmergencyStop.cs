/**
 * Added to each robot joint
 */

using System.Linq;
using UnityEngine;

public class EmergencyStop : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_CollidingMat = null;

    [Header("Sounds")]
    [SerializeField] private AudioClip m_CollisionClip = null;

    private ROSPublisher m_ROSPublisher = null;
    private Experiment1Manager m_ExperimentManager = null;
    private AudioSource m_AudioSource = null;

    private Renderer[] m_Renderers = null;
    private Material m_OriginalMat1 = null;
    private Material m_OriginalMat2 = null;

    //For Experiment1
    private Material m_AppearanceMat = null;

    float m_CollisionTime = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment1").GetComponent<Experiment1Manager>();
        m_AudioSource = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<AudioSource>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();

        m_OriginalMat1 = gameObject.transform.Find("Visuals").GetComponentInChildren<Renderer>().material;

        foreach (var renderer in m_Renderers)
        {
            if (renderer.materials.Count() > 1)
                m_OriginalMat2 = renderer.materials[1];
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent != null && !other.transform.parent.CompareTag("Moveable"))
        {
            m_CollisionTime = Time.time;
            string description = gameObject.name + ",collided with," + other.transform.parent.gameObject.name + "\n";

            if (m_ExperimentManager != null)
                m_ExperimentManager.RecordCollision(description);
            
            print(Time.time.ToString() + "Enter - " + description);

            foreach (Renderer renderer in m_Renderers)
            {
                if (renderer.materials.Count() == 1)
                    renderer.material = m_CollidingMat;
                else
                {
                    Material[] mats = { m_CollidingMat, m_CollidingMat };
                    renderer.materials = mats;
                }
            }

            m_AudioSource.clip = m_CollisionClip;
            m_AudioSource.Play();

            m_ROSPublisher.PublishEmergencyStop();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent != null && !other.transform.parent.CompareTag("Moveable"))
        {
            if (Time.time - m_CollisionTime >= m_ROSPublisher.m_LockedTime * 2.0f)
            {
                m_CollisionTime = Time.time;
                print(Time.time.ToString() + "Stay - " + gameObject.name + " collided with " + other.transform.parent.gameObject.name);

                m_ROSPublisher.PublishEmergencyStop();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.parent != null && !other.transform.parent.CompareTag("Moveable"))
            ResetColor();
    }

    public void ChangeAppearance(Material newMaterial = null)
    {
        m_AppearanceMat = newMaterial;

        ResetColor();
    }

    public void ResetColor()
    {
        foreach (Renderer renderer in m_Renderers)
        {
            if (m_AppearanceMat != null)
            {
                if (renderer.materials.Count() == 1)
                    renderer.material = m_AppearanceMat;
                else
                {
                    Material[] mats = { m_AppearanceMat, m_AppearanceMat };
                    renderer.materials = mats;
                }
            }
            else
            {
                if (renderer.materials.Count() == 1)
                    renderer.material = m_OriginalMat1;
                else
                {
                    Material[] mats = { m_OriginalMat1, m_OriginalMat2 };
                    renderer.materials = mats;
                }
            }
        }
    }
}