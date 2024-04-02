using UnityEngine;

public class Barrel : MonoBehaviour
{
    private Vector3 m_StartingPosition = new();
    private Vector3 m_PreviousPosition = new();

    private Collider[] m_RobotColliders = null;
    private int m_RobotPartsColliding = 0;

    private Material m_OriginalMat = null;
    private Material m_HighlightMat = null;

    [SerializeField] private bool m_ResetPosition = false;
    private bool m_isMoving = false;
    private bool m_isInBounds = false;

    private void Awake()
    {
        m_RobotColliders = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();

        m_OriginalMat = gameObject.GetComponent<Renderer>().material;
        m_HighlightMat = new(gameObject.GetComponent<Renderer>().material) {color = new(1.0f, 1.0f, 0.0f, 1.0f)};
    }

    private void Update()
    {
        if(m_ResetPosition)
        {
            m_ResetPosition = !m_ResetPosition;
            ResetPosition();
        }

        if (!m_isMoving && (Vector3.Distance(gameObject.transform.position, m_PreviousPosition) > 0.002f || m_RobotPartsColliding > 0))
            m_isMoving = true;

        if (m_isMoving)
        {
            if (Vector3.Distance(gameObject.transform.position, m_PreviousPosition) < 0.002f && m_RobotPartsColliding == 0)
            {
                m_isMoving = false;

                float angle = Vector3.Angle(gameObject.transform.up, Vector3.up);
                if (angle > 45)
                    gameObject.transform.SetPositionAndRotation(new(gameObject.transform.position.x, 0.058f, gameObject.transform.position.z), Quaternion.Euler(0.0f, 0.0f, 0.0f));
            }

            else
                m_PreviousPosition = gameObject.transform.position;
        }

        //if (!m_isMoving && (gameObject.GetComponent<CollisionHandling>() != null && !gameObject.GetComponent<CollisionHandling>().IsFocusObject()) &&
        //   ((m_isInBounds && gameObject.GetComponent<Renderer>().material != m_HighlightMat) || (!m_isInBounds && gameObject.GetComponent<Renderer>().material != m_OriginalMat)))
        //{
        //    ChangeMat();
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (var collider in m_RobotColliders)
        {
            if (other == collider)
                m_RobotPartsColliding++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (var collider in m_RobotColliders)
        {
            if (other == collider)
                m_RobotPartsColliding--;
        }
    }

    public void ResetPosition()
    {
        gameObject.transform.SetPositionAndRotation(m_StartingPosition, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        m_PreviousPosition = gameObject.transform.position;
    }

    public void SetStartingPosition(Vector3 position)
    {
        m_StartingPosition = position;
        ResetPosition();
    }

    //private void ChangeMat()
    //{
    //    if (m_isInBounds)
    //        gameObject.GetComponent<Renderer>().material = m_HighlightMat;
    //    else
    //        gameObject.GetComponent<Renderer>().material = m_OriginalMat;
    //}

    public bool IsMoving()
    {
        return m_isMoving;
    }

    public void IsInBounds(bool value)
    {
        m_isInBounds = value;
    }
}