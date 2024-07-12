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
    private ExperimentManager m_ExperimentManager = null;
    private AudioSource m_AudioSource = null;

    private Renderer[] m_Renderers = null;
    private readonly Material[] m_OriginalMat = { null, null };
    private readonly Material[] m_TransparentMat = { null, null };
    private readonly Material[] m_HighlightMat = { null, null };

    float m_CollisionTime = 0.0f;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_AudioSource = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<AudioSource>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();

        m_OriginalMat[0] = gameObject.transform.Find("Visuals").GetComponentInChildren<Renderer>().material;
        m_TransparentMat[0] = new(m_CollidingMat)
        {
            color = new(m_OriginalMat[0].color.r, m_OriginalMat[0].color.g, m_OriginalMat[0].color.b, 0.3f)
        };
        m_HighlightMat[0] = new(m_CollidingMat)
        {
            color = new(m_OriginalMat[0].color.r, m_OriginalMat[0].color.g, 0.0f, m_OriginalMat[0].color.a)
        };

        foreach (var renderer in m_Renderers)
        {
            if (renderer.materials.Count() > 1)
            {
                m_OriginalMat[1] = renderer.materials[1];
                m_TransparentMat[1] = new(m_CollidingMat)
                {
                    color = new(m_OriginalMat[1].color.r, m_OriginalMat[1].color.g, m_OriginalMat[1].color.b, 0.3f)
                };
                m_HighlightMat[1] = new(m_CollidingMat)
                {
                    color = new(m_OriginalMat[1].color.r, 1.0f, m_OriginalMat[1].color.b, m_OriginalMat[1].color.a)
                };
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Moveable") && Time.time - m_CollisionTime >= m_ROSPublisher.m_TimePenalty)
        {
            m_CollisionTime = Time.time;
            string description = gameObject.name + ",collided with," + other.name + "\n";

            if (m_ExperimentManager != null)
                m_ExperimentManager.RecordCollision(description);

            print(Time.time.ToString() + " Enter - " + description);

            Material[] colidingMat = { m_CollidingMat, m_CollidingMat };
            SetColor(colidingMat);

            m_AudioSource.clip = m_CollisionClip;
            m_AudioSource.Play();

            if (!m_ROSPublisher.IsLocked())
                m_ROSPublisher.PublishEmergencyStop();
        }
    }

    //private void OnTriggerStay(Collider other)
    //{
    //    if (!other.CompareTag("Attachable"))
    //    {
    //        if (!m_ROSPublisher.IsLocked() && Time.time - m_CollisionTime >= m_ROSPublisher.m_LockedTime * 2.0f)
    //        {
    //            m_CollisionTime = Time.time;
    //            print(Time.time.ToString() + " Stay - " + gameObject.name + " collided with " + other.name);
    //        }
    //    }
    //}

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Attachable"))
            SetColor(m_OriginalMat);
    }

    public void ChangeAppearance(int material)
    {
        switch (material)
        {
            case 1:
                SetColor(m_OriginalMat);
                break;

            case 2:
                SetColor(m_TransparentMat);
                break;

            case 3:
                SetColor(m_HighlightMat);
                break;

            default:
                break;
        }
    }

    private void SetColor(Material[] material)
    {
        foreach (Renderer renderer in m_Renderers)
        {
            if (renderer.materials.Count() == 1)
                renderer.material = material[0];
            else
                renderer.materials = material;
        }
    }
}