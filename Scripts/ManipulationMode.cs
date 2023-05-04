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
        ATTOBJCREATOR
    };
}

public class ManipulationMode : MonoBehaviour
{
    [HideInInspector] public const float ANGLETHRESHOLD = 5.0f;     //5deg
    [HideInInspector] public const float DISTANCETHRESHOLD = 0.05f; //5cm
    [HideInInspector] public const float SCALINGFACTOR = 0.25f;     //25%

    [Header("Scene Objects")]
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private RailCreator m_RailCreator = null;
    [SerializeField] private CollisionObjectCreator m_ColObjCreator = null;

    [Header ("Mode")]
    public Mode mode = Mode.DIRECT;

    private PlanningRobot m_PlanningRobot = null;
    private Gripper m_Gripper = null;

    private SteamVR_Action_Boolean m_Grip = null;

    private void Awake()
    {
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_Gripper = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<Gripper>();

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Grip.onStateDown += GripGrabbed;
    }

    private void OnDestroy()
    {
        m_Grip.onStateDown -= GripGrabbed;
    }

    private void GripGrabbed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.ExecuteTrajectory();
    }

    public void TogglePlanner()
    {
        if(!m_Gripper.isGripping &&
           (mode == Mode.DIRECT ||
           mode == Mode.SDOF ||
           mode == Mode.RAIL))
        {
            m_PlanningRobot.Show();
        }
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

    public void ToggleAttachableObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            if (m_Gripper.isGripping)
                m_Gripper.Show();

            m_ColObjCreator.Show(true);
            mode = Mode.ATTOBJCREATOR;
        }

        else if (mode == Mode.ATTOBJCREATOR)
        {
            m_ColObjCreator.Show(false);
            mode = Mode.DIRECT;
        }
    }

    public void ToggleCollisionObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            if (m_Gripper.isGripping)
                m_Gripper.Show();

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
        if (!m_PlanningRobot.isPlanning &&
           (mode == Mode.DIRECT ||
           mode == Mode.SDOF ||
           mode == Mode.RAIL))
        {
            m_Gripper.Show();
        }
    }

    public void ToggleRailCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show();

            if (m_Gripper.isGripping)
                m_Gripper.Show();

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