using UnityEngine;

public class Barrel : MonoBehaviour
{
    private Vector3 m_PreviousPosition = new();
    private bool m_isMoving = false;

    private void Awake()
    {
        m_PreviousPosition = gameObject.transform.position;
    }

    private void Update()
    {
        if (!m_isMoving && m_PreviousPosition != gameObject.transform.position)
            m_isMoving = true;

        if (m_isMoving)
        {
            if (m_PreviousPosition == gameObject.transform.position)
            {
                if (gameObject.GetComponent<CollisionHandling>() == null || (gameObject.GetComponent<CollisionHandling>() != null && !gameObject.GetComponent<CollisionHandling>().m_isAttached))
                {
                    gameObject.transform.SetPositionAndRotation(new(gameObject.transform.position.x, 0.058f, gameObject.transform.position.z), Quaternion.Euler(0.0f, 0.0f, 0.0f));
                    m_isMoving = false;
                }
            }
            else
                m_PreviousPosition = gameObject.transform.position;
        }
    }
}
