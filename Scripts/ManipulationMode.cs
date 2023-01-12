using UnityEngine;
using Valve.VR;

public class ManipulationMode : MonoBehaviour
{

    [SerializeField] private PlanningRobot m_PlanningRobot = null;
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private Rails m_Rails = null;
    [SerializeField] private CollisionObjects m_CollisionObjects= null;

    [HideInInspector]
    public enum Mode
    {
        DIRECT,
        SDOF,
        COLLISIONBOX,
        RAIL
    }
    [HideInInspector] public Mode mode = Mode.DIRECT;

    private bool m_SDOFManipulating = false;
    private bool m_RailManipulating = false;
    private bool m_CreatingCollisionObjects = false;

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
        //Debug.Log("GripGrabbed");
        
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

    public void ToggleRails()
    {
        m_RailManipulating = !m_RailManipulating;
        m_Rails.Show(m_RailManipulating);
    }

    public void ToggleCollisionBoxes()
    {
        m_CreatingCollisionObjects = !m_CreatingCollisionObjects;
        m_CollisionObjects.Show(m_CreatingCollisionObjects);
    }
}