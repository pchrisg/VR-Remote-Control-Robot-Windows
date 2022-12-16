
using UnityEngine;
using Valve.VR;

public class ManipulationMode : MonoBehaviour
{
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;

    //private bool m_Planning = false;
    private bool m_SDOFManipulating = false;

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
        m_PlanningRobot.isPlanning = !m_PlanningRobot.isPlanning;
        m_PlanningRobot.Show();
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