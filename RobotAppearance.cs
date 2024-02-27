using UnityEngine;
using Valve.VR.InteractionSystem;

public class RobotAppearance : MonoBehaviour
{
    private enum Appearance
    {
        TRANSPARENT,
        OPAQUE
    };

    [Header("Distance")]
    [SerializeField] private float m_ChangeDistance = 0.0f;

    [Header("Materials")]
    [SerializeField] private Material m_TransparentMat = null;

    private GameObject m_UR5 = null;
    private GameObject m_Robotiq = null;
    private Collider m_Player = null;

    private Appearance m_Appearance = Appearance.OPAQUE;

    private void Awake()
    {
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_Player = Player.instance.headCollider;
    }

    private void Update()
    {
        float distance = Vector2.Distance(new(m_UR5.transform.position.x, m_UR5.transform.position.z), new(m_Player.transform.position.x, m_Player.transform.position.z));

        if (distance <= m_ChangeDistance && m_Appearance == Appearance.OPAQUE)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance(m_TransparentMat);

            foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();

            m_Appearance = Appearance.TRANSPARENT;
        }
        else if (distance > m_ChangeDistance && m_Appearance == Appearance.TRANSPARENT)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();

            m_Appearance = Appearance.OPAQUE;
        }
    }
}
