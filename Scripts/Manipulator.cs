using System.Collections;
using UnityEngine;
using FeedBackModes;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ManipulatorMat = null;

    //private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private Indicator m_Indicator = null;
    //private RobotFeedback m_RobotFeedback = null;
    private ExperimentManager m_ExperimentManager = null;

    private Transform m_Robotiq = null;

    //Colors
    private Color m_ShowColor = new(0.2f, 0.2f, 0.2f, 0.5f);
    private Color m_ScalingColor = new(0.8f, 0.8f, 0.8f, 0.5f);
    private Color m_CollidingColor = new(1.0f, 0.0f, 0.0f, 0.5f);
    private Color m_HideColor = new(0.2f, 0.2f, 0.2f, 0.0f);
    private Color m_FlashingColor = new(1.0f, 1.0f, 0.0f, 0.5f);

    private bool m_isScaling = false;

    private Coroutine m_ActiveCoroutine = null;

    private bool m_isInteracting = false;

    private void Awake()
    {
        //m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_Indicator = gameObject.transform.Find("Indicator").GetComponent<Indicator>();
        //m_RobotFeedback = GameObject.FindGameObjectWithTag("RobotFeedback").GetComponent<RobotFeedback>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;

        m_ManipulatorMat.color = m_ShowColor;

        ShowManipulator(false);
    }

    private void OnDestroy()
    {
        m_ManipulatorMat.color = m_ShowColor;
    }

    private void Update()
    {
        if (m_isInteracting != m_ManipulationMode.IsInteracting())
        {
            m_isInteracting = m_ManipulationMode.IsInteracting();
            ShowManipulator(m_isInteracting);
        }

        if (m_isInteracting)
        {
            if (m_ManipulationMode.mode == ManipulationModes.Mode.CONSTRAINEDDIRECT)
                CheckSnapping();
        }
        else
            ResetPositionAndRotation();
        
    }

    public void ResetPositionAndRotation()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);

        //if (m_ActiveCoroutine != null)
        //    StopCoroutine(m_ActiveCoroutine);

        //m_ActiveCoroutine = StartCoroutine(ResetPositionAndRotationRoutine());
    }

    //private IEnumerator ResetPositionAndRotationRoutine()
    //{
    //    ShowManipulator(false);

    //    yield return new WaitForSeconds(0.3f);
    //    yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

    //    gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
    //    ShowManipulator(true);

    //    m_RobotFeedback.ResetPositionAndRotation();
    //}

    private void CheckSnapping()
    {
        m_Indicator.ChangeAppearance(1);

        float angle = Vector3.Angle(gameObject.transform.right.normalized, Vector3.up.normalized);
        if (angle < 0.1f)
            m_Indicator.ChangeAppearance(2);

        if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        {
            Vector3 connectingVector = gameObject.transform.position - m_InteractableObjects.m_FocusObject.transform.position;
            angle = Vector3.Angle(gameObject.transform.right.normalized, connectingVector.normalized);

            if (angle < 0.1f)
                m_Indicator.ChangeAppearance(3);
        }
    }

    public void IsColliding(bool value)
    {
        if (m_ExperimentManager.m_FeedbackMode == Mode.NONE)
        {
            if (value)
                m_ManipulatorMat.color = m_CollidingColor;
            else
            {
                if (m_isScaling)
                    m_ManipulatorMat.color = m_ScalingColor;
                else
                    m_ManipulatorMat.color = m_ShowColor;
            }
        }
    }

    public void IsScaling(bool value)
    {
        m_isScaling = value;

        if (value)
            m_ManipulatorMat.color = m_ScalingColor;
        else
            m_ManipulatorMat.color = m_ShowColor;
    }

    public void ShowManipulator(bool value)
    {
        if (m_ManipulationMode.mode != ManipulationModes.Mode.CONSTRAINEDDIRECT)
            m_Indicator.Show(false);
        else
            m_Indicator.Show(value);

        if (value)
            m_ManipulatorMat.color = m_ShowColor;
        else
            m_ManipulatorMat.color = m_HideColor;
    }

    public void Flash(bool value)
    {
        if (value)
            m_ActiveCoroutine ??= StartCoroutine(FlashCoroutine());

        else
        {
            if (m_ActiveCoroutine != null)
                StopCoroutine(m_ActiveCoroutine);

            m_ActiveCoroutine = null;
            m_ManipulatorMat.color = m_ShowColor;
        }
    }

    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            if (m_ManipulationMode.IsInteracting())
                m_ManipulatorMat.color = m_ShowColor;
            else
            {
                m_ManipulatorMat.color = m_FlashingColor;
                yield return new WaitForSeconds(1.0f);

                m_ManipulatorMat.color = m_ShowColor;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}