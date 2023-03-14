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
        RAILCREATOR,
        COLOBJCREATOR,
        GRIPPER,
        ATTOBJCREATOR
    };
}

public class ManipulationMode : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private RailCreator m_RailCreator = null;
    [SerializeField] private CollisionObjectCreator m_ColObjCreator = null;

    [Header ("Mode")]
    public Mode mode = Mode.DIRECT;

    private PlanningRobot m_PlanningRobot = null;

    private SteamVR_Action_Boolean m_Trackpad = null;
    private SteamVR_Action_Boolean m_Menu = null;
    private SteamVR_Action_Boolean m_Grip = null;

    private void Awake()
    {
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();

        m_Menu = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressMenu");
        m_Trackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressTrackpad");
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");

        m_Menu.onStateDown += MenuPressed;
    }

    private void OnDestroy()
    {
        m_Menu.onStateDown -= MenuPressed;
    }

    private void MenuPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources any)
    {
        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.ExecuteTrajectory();
    }

    public void TogglePlanner()
    {
        if(mode != Mode.ATTOBJCREATOR && mode != Mode.COLOBJCREATOR && mode != Mode.GRIPPER && mode != Mode.RAILCREATOR)
            m_PlanningRobot.Show();
    }

    public void ToggleWidget()
    {
        if(mode == Mode.DIRECT)
        {
            m_SDOFWidget.Show(true);
            mode = Mode.SDOF;
        }

        else if (mode == Mode.SDOF)
        {
            m_SDOFWidget.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleAttachmentObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            //m_ColObjCreator.Show(true);
            mode = Mode.ATTOBJCREATOR;
        }

        else if (mode == Mode.ATTOBJCREATOR)
        {
            //m_ColObjCreator.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleCollisionObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            m_ColObjCreator.Show(true);
            mode = Mode.COLOBJCREATOR;
        }

        else if (mode == Mode.COLOBJCREATOR)
        {
            m_ColObjCreator.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleGripper()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            //m_ColObjCreator.Show(true);
            mode = Mode.GRIPPER;
        }

        else if (mode == Mode.GRIPPER)
        {
            //m_ColObjCreator.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleRailCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            m_RailCreator.Show(true);
            mode = Mode.RAILCREATOR;
        }

        else if (mode == Mode.RAILCREATOR)
        {
            m_RailCreator.Show(false);
            mode = Mode.RAIL;
        }

        else if (mode == Mode.RAIL)
        {
            m_RailCreator.Clear();
            mode = Mode.DIRECT;
        }
    }
}