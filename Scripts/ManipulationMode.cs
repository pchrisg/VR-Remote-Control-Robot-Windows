
using UnityEngine;
using Valve.VR;

public class ManipulationMode : MonoBehaviour
{
    [SerializeField] private GameObject m_Manipulator;
    [SerializeField] private GameObject m_WidgetPrefab;
    //[SerializeField] private ManipulatorPublisher m_rosPublisher;

    private Planner m_Planner;

    private bool m_SDOFManipulation;
    private GameObject m_Widget;

    // Variables required for Controller Actions
    private SteamVR_Action_Boolean m_Trackpad;
    private SteamVR_Action_Boolean m_Menu;
    private SteamVR_Action_Boolean m_Grip;

    void Start()
    {
        m_Menu = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressMenu");
        m_Menu.AddOnStateDownListener(MenuPressed, SteamVR_Input_Sources.Any);
        m_Trackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressTrackpad");
        m_Trackpad.AddOnStateDownListener(TrackpadPressed, SteamVR_Input_Sources.Any);
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Grip.AddOnStateDownListener(GripGrabbed, SteamVR_Input_Sources.Any);

        m_SDOFManipulation = false;
        m_Planner = gameObject.GetComponent<Planner>();
    }

    private void MenuPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        Debug.Log("MenuPressed");
        if (!m_Planner.isPlanning)
        {
            m_Planner.SetUpPlanningRobot();
        }
        else
        {
            m_Planner.DestroyPlanningRobot();
        }
    }

    private void TrackpadPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        Debug.Log("TrackpadPressed");
        if (m_Planner.isPlanning)
            m_Planner.ExecuteTrajectory();
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        Debug.Log("GripGrabbed");
        m_SDOFManipulation = !m_SDOFManipulation;
        if (m_SDOFManipulation)
        {
            SetUpWidget();
        }
        else
        {
            m_Manipulator.transform.parent = null;
            Destroy(m_Widget);
        }
    }

    private void SetUpWidget()
    {
        m_Widget = Instantiate(m_WidgetPrefab, m_Manipulator.transform.position, m_Manipulator.transform.rotation);
        m_Manipulator.transform.SetParent(m_Widget.transform);
    }

    /*void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            Debug.Log("Space pressed");
            PublishTrajectoryPlanner();
        }
    }*/
}