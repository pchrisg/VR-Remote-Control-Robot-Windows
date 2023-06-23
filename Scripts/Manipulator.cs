using UnityEngine;

public class Manipulator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ManipulatorMat;

    private Transform m_Robotiq;

    //Colours
    private Color m_CurrentColor = new Color();
    private Color m_DefaultColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
    private Color m_CollidingColor = new Color(1.0f, 0.0f, 0.0f, 0.4f);
    private Color m_Y_AxisColor = new Color(0.0f, 1.0f, 0.0f, 0.4f);
    private Color m_XZ_PlaneColor = new Color(1.0f, 0.0f, 1.0f, 0.4f);
    private Color m_FocusObjectColor = new Color(1.0f, 1.0f, 0.0f, 0.4f);

    private bool isColliding = false;

    private void Awake()
    {
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
        Color color = m_CurrentColor;

        if (isColliding)
            color = m_CollidingColor;
        else
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

    public void Collide()
    {
        isColliding = true;
    }

    public void NotColliding()
    {
        isColliding = false;
    }
}