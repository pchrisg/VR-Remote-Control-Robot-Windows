using System.Collections;
using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ManipulatorMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private InteractableObjects m_InteractableObjects = null;
    private Indicator m_Indicator = null;

    private Transform m_Robotiq = null;

    //Colors
    private Color m_DefaultColor = new(0.2f, 0.2f, 0.2f, 8.0f);
    private Color m_ScalingColor = new(0.8f, 0.8f, 0.8f, 8.0f);
    private Color m_CollidingColor = new(1.0f, 0.0f, 0.0f, 8.0f);
    private Color m_InvisColor = new(0.2f, 0.2f, 0.2f, 0.0f);
    private Color m_FlashingColor = new(1.0f, 1.0f, 0.0f, 0.5f);

    Coroutine activeCouroutine = null;

    private bool m_isScaling = false;

    private Coroutine m_ActiveCoroutine = null;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_Indicator = gameObject.transform.Find("Indicator").GetComponent<Indicator>();

        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;

        m_ManipulatorMat.color = m_DefaultColor;

        ShowManipulator(false);
    }

    private void OnDestroy()
    {
        m_ManipulatorMat.color = m_DefaultColor;
    }

    private void Update()
    {
        if (m_ManipulationMode.mode == ManipulationModes.Mode.CONSTRAINEDDIRECT)
            CheckSnapping();
    }

    public void ResetPositionAndRotation()
    {
        if (activeCouroutine != null)
            StopCoroutine(activeCouroutine);

        activeCouroutine = StartCoroutine(ResetPositionAndRotationRoutine());
    }

    private IEnumerator ResetPositionAndRotationRoutine()
    {
        ShowManipulator(false);

        yield return new WaitForSeconds(0.3f);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
        ShowManipulator(true);
    }

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
        if (value)
            m_ManipulatorMat.color = m_CollidingColor;
        else
        {
            if (m_isScaling)
                m_ManipulatorMat.color = m_ScalingColor;
            else
                m_ManipulatorMat.color = m_DefaultColor;
        }
    }

    public void IsScaling(bool value)
    {
        m_isScaling = value;

        if (value)
            m_ManipulatorMat.color = m_ScalingColor;
        else
            m_ManipulatorMat.color = m_DefaultColor;
    }

    public void ShowManipulator(bool value)
    {
        if (m_ManipulationMode.mode != ManipulationModes.Mode.CONSTRAINEDDIRECT)
            m_Indicator.Show(false);
        else
            m_Indicator.Show(value);

        if (value)
            m_ManipulatorMat.color = m_DefaultColor;
        else
            m_ManipulatorMat.color = m_InvisColor;
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
            m_ManipulatorMat.color = m_DefaultColor;
        }
    }

    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            if (m_ManipulationMode.IsInteracting())
                m_ManipulatorMat.color = m_DefaultColor;
            else
            {
                m_ManipulatorMat.color = m_FlashingColor;
                yield return new WaitForSeconds(1.0f);

                m_ManipulatorMat.color = m_DefaultColor;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}