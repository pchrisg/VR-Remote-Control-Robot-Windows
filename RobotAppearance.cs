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
    [SerializeField] private float m_Distance = 0.0f;

    [Header("Materials")]
    [SerializeField] private Material m_TransparentMat = null;

    private GameObject m_UR5 = null;
    private GameObject m_Robotiq = null;
    private Player m_Player = null;

    private Appearance m_Appearance = Appearance.OPAQUE;

    private void Awake()
    {
        m_UR5 = GameObject.FindGameObjectWithTag("robot");
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_Player = Player.instance;
    }

    private void Update()
    {
        if (Vector3.Distance(m_UR5.transform.position, m_Player.transform.position) <= m_Distance && m_Appearance == Appearance.OPAQUE)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance(m_TransparentMat);

            foreach (var joint in m_Robotiq.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();

            m_Appearance = Appearance.TRANSPARENT;
        }
        else if (Vector3.Distance(m_UR5.transform.position, m_Player.transform.position) > m_Distance && m_Appearance == Appearance.TRANSPARENT)
        {
            foreach (var joint in m_UR5.GetComponentsInChildren<EmergencyStop>())
                joint.ChangeAppearance();

            m_Appearance = Appearance.OPAQUE;
        }
    }
}
