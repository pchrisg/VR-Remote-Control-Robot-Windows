using System.Collections;
using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ManipulatorMat;

    private ManipulationMode m_ManipulationMode = null;

    private Transform m_Robotiq;

    //Colors
    private Color m_CurrentColor = new Color();
    private Color m_DefaultColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
    private Color m_CollidingColor = new Color(1.0f, 0.0f, 0.0f, 0.4f);
    private Color m_Y_AxisColor = new Color(0.0f, 1.0f, 0.0f, 0.4f);
    private Color m_XZ_PlaneColor = new Color(1.0f, 0.0f, 1.0f, 0.4f);
    private Color m_FocusObjectColor = new Color(1.0f, 1.0f, 0.0f, 0.4f);

    private bool isColliding = false;
    private Coroutine m_Flashing = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq").transform;

        m_CurrentColor = m_DefaultColor;
        m_ManipulatorMat.color = m_CurrentColor;
    }

    private void OnDestroy()
    {
        m_ManipulatorMat.color = m_DefaultColor;
    }

    private void Start()
    {
        Invoke("ResetPosition", 1.2f);
    }

    private void Update()
    {
        Color color = m_DefaultColor;

        if (isColliding)
            color = m_CollidingColor;
        else if (m_ManipulationMode.mode != ManipulationModes.Mode.SIMPLEDIRECT)
            color = CheckSnapping();

        if(color != m_CurrentColor)
        {
            m_CurrentColor = color;
            m_ManipulatorMat.color = m_CurrentColor;
        }
    }

    public void ResetPosition()
    {
        gameObject.GetComponent<ArticulationBody>().TeleportRoot(m_Robotiq.position, m_Robotiq.rotation);
    }

    private Color CheckSnapping()
    {
        float angle = Mathf.Acos(Vector3.Dot(transform.right, Vector3.up)) * Mathf.Rad2Deg;
        if (float.IsNaN(angle) || angle < 0.1f)
            return m_Y_AxisColor;

        if (Mathf.Abs(angle - 90.0f) < 0.1f)
            return m_XZ_PlaneColor;

        return m_DefaultColor;
    }

    public void Colliding(bool value)
    {
        isColliding = value;
    }

    public void Flash(bool value)
    {
        if (value)
        {
            if (m_Flashing == null)
                m_Flashing = StartCoroutine(Flashing());
        }
        else
        {
            if (m_Flashing != null)
                StopCoroutine(m_Flashing);
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