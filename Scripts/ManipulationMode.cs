
using UnityEngine;
using Valve.VR;

public class ManipulationMode : MonoBehaviour
{
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;

    //private Planner m_Planner;

    private bool m_Planning;
    private bool m_SDOFManipulating;

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

        m_SDOFManipulating = false;
    }

    private void MenuPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        Debug.Log("MenuPressed");
        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.ExecuteTrajectory();
    }

    private void TrackpadPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        //Debug.Log("TrackpadPressed");
        
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        Debug.Log("GripGrabbed");
        
    }

    public void TogglePlanner()
    {
        m_Planning = !m_Planning;
        m_PlanningRobot.Show(m_Planning);
    }

    public void ToggleWidget()
    {
        m_SDOFManipulating = !m_SDOFManipulating;
        m_SDOFWidget.Show(m_SDOFManipulating);
    }

    public void ToggleCollisionBoxes()
    {

    }
}