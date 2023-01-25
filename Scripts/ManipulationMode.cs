using UnityEngine;
using Valve.VR;
using ManipulationOptions;

namespace ManipulationOptions
{
    public enum Mode
    {
        DIRECT,
        SDOF,
        RAIL,
        AABBCREATOR,
        RAILCREATOR
    };
}

public class ManipulationMode : MonoBehaviour
{

    [SerializeField] private PlanningRobot m_PlanningRobot = null;
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private RailCreator m_RailCreator = null;
    [SerializeField] private CollisionObjectCreator m_CollisionObjectCreator = null;

    //[HideInInspector]
    public Mode mode = Mode.DIRECT;

    // Variables required for Controller Actions
    private SteamVR_Action_Boolean m_Trackpad = null;
    private SteamVR_Action_Boolean m_Menu = null;
    private SteamVR_Action_Boolean m_Grip = null;

    private void Awake()
    {
        m_Menu = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressMenu");
        m_Trackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressTrackpad");
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");

        m_Menu.onStateDown += MenuPressed;
        m_Trackpad.onStateDown += TrackpadPressed;
        m_Grip.onStateDown += GripGrabbed;
    }

    private void OnDestroy()
    {
        m_Menu.onStateDown -= MenuPressed;
        m_Trackpad.onStateDown -= TrackpadPressed;
        m_Grip.onStateDown -= GripGrabbed;
    }

    private void MenuPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.ExecuteTrajectory();
    }

    private void TrackpadPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        //Debug.Log("TrackpadPressed");
        
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        //Debug.Log("GripGrabbed");
        
    }

    public void TogglePlanner()
    {
        m_PlanningRobot.isPlanning = !m_PlanningRobot.isPlanning;
        m_PlanningRobot.Show();
    }

    public void ToggleWidget()
    {
        if(mode == Mode.DIRECT)
        {
            mode = Mode.SDOF;
            m_SDOFWidget.Show(true);
            
        }
        else if (mode == Mode.SDOF)
        {
            m_SDOFWidget.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleRailCreator()
    {
        if (mode == Mode.DIRECT)
        {
            m_RailCreator.Show(true);
            mode = Mode.RAILCREATOR;
        }
        else if (mode == Mode.RAILCREATOR)
        {
            m_RailCreator.Show(false);
            mode = Mode.RAIL;
        }
        else if(mode == Mode.RAIL)
        {
            m_RailCreator.Clear();
            mode = Mode.DIRECT;
        }
    }

    public void ToggleCollisionObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            m_CollisionObjectCreator.Show(true);
            mode = Mode.AABBCREATOR;
        }
        else if (mode == Mode.AABBCREATOR)
        {
            m_CollisionObjectCreator.Show(false);
            mode = Mode.DIRECT;
        }
    }
}