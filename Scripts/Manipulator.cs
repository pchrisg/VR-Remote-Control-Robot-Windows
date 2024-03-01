using System.Collections;
using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ManipulatorMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;

    private Transform m_Robotiq = null;

    //Colors
    private Color m_CurrentColor = new();
    private Color m_DefaultColor = new(0.2f, 0.2f, 0.2f, 1.0f);
    private Color m_CollidingColor = new(1.0f, 0.0f, 0.0f, 1.0f);
    private Color m_Y_AxisColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private Color m_XZ_PlaneColor = new(1.0f, 0.0f, 1.0f, 1.0f);
    private Color m_FocusObjectColor = new(1.0f, 1.0f, 0.0f, 1.0f);
    private Color m_InvisColor = new(0.2f, 0.2f, 0.2f, 0.0f);

    public bool isColliding = false;
    private Coroutine m_Flashing = null;

    Coroutine activeCouroutine = null;

    private bool isVisible = false;

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;

        m_CurrentColor = m_DefaultColor;
        m_ManipulatorMat.color = m_DefaultColor;

        ShowManipulator(false);
    }

    private void OnDestroy()
    {
        m_ManipulatorMat.color = m_DefaultColor;
    }

    private void Update()
    {
        if(isVisible)
        {
            Color color = m_DefaultColor;

            if (isColliding)
                color = m_CollidingColor;
            else if (m_ManipulationMode.mode != ManipulationModes.Mode.SIMPLEDIRECT)
                color = CheckSnapping(color);

            if(color != m_CurrentColor)
            {
                m_CurrentColor = color;
                m_ManipulatorMat.color = m_CurrentColor;
            }
        }
    }

    public void ResetPositionAndRotation()
    {
        if (activeCouroutine != null)
            StopCoroutine(activeCouroutine);

        activeCouroutine = StartCoroutine(ResetPose());
    }

    private IEnumerator ResetPose()
    {
        ShowManipulator(false);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
        ShowManipulator(true);
    }

    public void ResetPosition()
    {
        if (activeCouroutine != null)
            StopCoroutine(activeCouroutine);

        activeCouroutine = StartCoroutine(ResetPos());
    }

    private IEnumerator ResetPos()
    {
        ShowManipulator(false);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, gameObject.transform.rotation);
        ShowManipulator(true);
    }

    private Color CheckSnapping(Color color)
    {
        float angle = Vector3.Angle(gameObject.transform.right.normalized, Vector3.up.normalized);
        if (angle < 0.1f)
            color = m_Y_AxisColor;

        if (Mathf.Abs(90.0f - angle) < 0.1f)
            color = m_XZ_PlaneColor;

        if (m_CollisionObjects.m_FocusObject != null && !m_CollisionObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        {
            Vector3 connectingVector = gameObject.transform.position - m_CollisionObjects.m_FocusObject.transform.position;
            angle = Vector3.Angle(gameObject.transform.right.normalized, connectingVector.normalized);

            if (angle < 0.1f)
                color =  m_FocusObjectColor;
        }

        return color;
    }

    public void Colliding(bool value)
    {
        isColliding = value;
    }

    public void ShowManipulator(bool value)
    {
        if (value)
            m_ManipulatorMat.color = m_DefaultColor;
        else
            m_ManipulatorMat.color = m_InvisColor;

        isVisible = value;
    }

    public void Flash(bool value)
    {
        if (value)
            m_Flashing ??= StartCoroutine(Flashing());

        else
        {
            if (m_Flashing != null)
                StopCoroutine(m_Flashing);

            m_Flashing = null;
            m_ManipulatorMat.color = m_DefaultColor;
        }
    }

    private IEnumerator Flashing()
    {
        while (true)
        {
            if (m_ManipulationMode.isInteracting)
                m_ManipulatorMat.color = m_DefaultColor;
            else
            {
                m_ManipulatorMat.color = m_FocusObjectColor;
                yield return new WaitForSeconds(1.0f);
                m_ManipulatorMat.color = m_DefaultColor;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }
}