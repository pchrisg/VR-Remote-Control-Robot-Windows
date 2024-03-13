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
    private Color m_DefaultColor = new(0.2f, 0.2f, 0.2f, 1.0f);
    private Color m_CollidingColor = new(1.0f, 0.0f, 0.0f, 1.0f);
    private Color m_InvisColor = new(0.2f, 0.2f, 0.2f, 0.0f);
    private Color m_Y_AxisColor = new(0.0f, 1.0f, 0.0f, 0.5f);
    private Color m_XZ_PlaneColor = new(1.0f, 0.0f, 1.0f, 0.5f);
    private Color m_FocusObjectColor = new(1.0f, 1.0f, 0.0f, 0.5f);

    public bool isColliding = false;

    Coroutine activeCouroutine = null;

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
        if (m_ManipulationMode.mode != ManipulationModes.Mode.SIMPLEDIRECT)
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

        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => m_ROSPublisher.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
        ShowManipulator(true);
    }

    private void CheckSnapping()
    {
        Color color = m_InvisColor;

        float angle = Vector3.Angle(gameObject.transform.right.normalized, Vector3.up.normalized);
        if (angle < 0.1f)
            color = m_Y_AxisColor;

        if (Mathf.Abs(90.0f - angle) < 0.1f)
            color = m_XZ_PlaneColor;

        if (m_InteractableObjects.m_FocusObject != null && !m_InteractableObjects.m_FocusObject.GetComponent<CollisionHandling>().m_isAttached)
        {
            Vector3 connectingVector = gameObject.transform.position - m_InteractableObjects.m_FocusObject.transform.position;
            angle = Vector3.Angle(gameObject.transform.right.normalized, connectingVector.normalized);

            if (angle < 0.1f)
                color = m_FocusObjectColor;
        }

        m_Indicator.SetColour(color);
    }

    public void Colliding(bool value)
    {
        isColliding = value;

        if (isColliding)
            m_ManipulatorMat.color = m_CollidingColor;
        else
            m_ManipulatorMat.color = m_DefaultColor;
    }

    public void ShowManipulator(bool value)
    {
        m_Indicator.Show(value);

        if (value)
            m_ManipulatorMat.color = m_DefaultColor;
        else
            m_ManipulatorMat.color = m_InvisColor;
    }
}