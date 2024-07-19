/**
 * Added to each joint of the Robot Feedback
 */

using UnityEngine;
using FeedBackModes;
using System.Linq;

public class RobotFeedbackCollision : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_FeebackMat = null;
    private ExperimentManager m_ExperimentManager = null;
    private RobotFeedback m_RobotFeedback = null;

    private Renderer[] m_Renderers = null;
    private readonly Material[] m_OriginalMat = { null, null };
    private readonly Material[] m_DisplayErrorMat = { null, null };
    private readonly Material[] m_ShowMat = { null, null };

    private bool m_isColliding = false;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_RobotFeedback = GameObject.FindGameObjectWithTag("RobotFeedback").GetComponent<RobotFeedback>();

        m_Renderers = gameObject.transform.Find("Visuals").GetComponentsInChildren<Renderer>();

        m_OriginalMat[0] = m_FeebackMat;
        m_OriginalMat[1] = m_FeebackMat;
        m_DisplayErrorMat[0] = new(m_FeebackMat){color = new(1.0f , 0.0f, 0.0f, 0.5f)};
        m_DisplayErrorMat[1] = m_DisplayErrorMat[0];
        m_ShowMat[0] = new(m_FeebackMat){color = new(1.0f, 1.0f, 1.0f, 0.2f)};
        m_ShowMat[1] = m_ShowMat[0];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Moveable"))
        {
            m_isColliding = true;
            SetColor(m_DisplayErrorMat);
            m_RobotFeedback.AddCollidingPart(gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Moveable"))
        {
            m_isColliding = false;
            SetColor(m_OriginalMat);
            m_RobotFeedback.RemoveCollidingPart(gameObject);
        }
    }

    public bool IsColliding()
    {
        return m_isColliding;
    }

    public void ChangeAppearance(int material)
    {
        switch (material)
        {
            case 1:
                SetColor(m_OriginalMat);
                break;

            case 2:
                SetColor(m_DisplayErrorMat);
                break;

            case 3:
                SetColor(m_ShowMat);
                break;

            default:
                break;
        }
    }

    private void SetColor(Material[] material)
    {
        if (m_ExperimentManager.m_FeedbackMode != Mode.NONE)
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

    public void ResetColor()
    {
        SetColor(m_OriginalMat);
    }
}