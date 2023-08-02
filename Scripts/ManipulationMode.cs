using UnityEngine;
using Valve.VR;
using ManipulationModes;

namespace ManipulationModes
{
    public enum Mode
    {
        SIMPLEDIRECT,
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
    [Header("Technique")]
    public bool m_SwitchTechnique = false;
    public Mode mode = Mode.DIRECT;

    [HideInInspector] public const float ANGLETHRESHOLD = 5.0f;     //5deg
    [HideInInspector] public const float DISTANCETHRESHOLD = 0.05f; //5cm
    [HideInInspector] public const float SCALINGFACTOR = 0.25f;     //25%

    [Header("Scene Objects")]
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    [SerializeField] private RailCreator m_RailCreator = null;

    private PlanningRobot m_PlanningRobot = null;
    private CollisionObjects m_CollisionObjects = null;
    private GripperControl m_GripperControl = null;

    private SteamVR_Action_Boolean m_Grip = null;

    public bool isInteracting = false;

    private void Awake()
    {
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();

        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Grip.onStateDown += GripGrabbed;
    }

    private void Update()
    {
        if (m_SwitchTechnique)
        {
            if (mode == Mode.SIMPLEDIRECT)
                mode = Mode.DIRECT;
            else
            {
                ToggleDirect();
                mode = Mode.SIMPLEDIRECT;
            }

            m_SwitchTechnique = false;
        }
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

    public void ToggleDirect()
    {
        if (m_PlanningRobot.isPlanning)
            m_PlanningRobot.Show(false);

        if (m_GripperControl.isGripping)
            m_GripperControl.Show(false);

        if (mode == Mode.SDOF)
            m_SDOFWidget.Show(false);

        else if (mode == Mode.ATTOBJCREATOR || mode == Mode.COLOBJCREATOR)
            m_CollisionObjects.isCreating = false;

        else if (mode == Mode.RAILCREATOR)
        {
            m_RailCreator.Show(false);
            m_RailCreator.Clear();
        }

        else if (mode == Mode.RAIL)
            m_RailCreator.Clear();

        mode = Mode.DIRECT;
    }

    public void TogglePlanner()
    {
        if(!m_GripperControl.isGripping &&
           (mode == Mode.SIMPLEDIRECT ||
           mode == Mode.DIRECT ||
           mode == Mode.SDOF ||
           mode == Mode.RAIL))
        {
            if(m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show(false);
            else
                m_PlanningRobot.Show(true);
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
                m_PlanningRobot.Show(false);

            if (m_GripperControl.isGripping)
                m_GripperControl.Show(false);

            m_CollisionObjects.isCreating = true;
            mode = Mode.ATTOBJCREATOR;
        }

        else if (mode == Mode.ATTOBJCREATOR)
        {
            m_CollisionObjects.isCreating = false;
            mode = Mode.DIRECT;
        }
    }

    public void ToggleCollisionObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show(false);

            if (m_GripperControl.isGripping)
                m_GripperControl.Show(false);

            m_CollisionObjects.isCreating = true;
            mode = Mode.COLOBJCREATOR;
        }

        else if (mode == Mode.COLOBJCREATOR)
        {
            m_CollisionObjects.isCreating = false;
            mode = Mode.DIRECT;
        }
    }

    public void ToggleGripper()
    {
        if (!m_PlanningRobot.isPlanning &&
           (mode == Mode.SIMPLEDIRECT ||
           mode == Mode.DIRECT ||
           mode == Mode.SDOF ||
           mode == Mode.RAIL))
        {
            if (m_GripperControl.isGripping)
                m_GripperControl.Show(false);
            else
                m_GripperControl.Show(true);
        }
    }

    public void ToggleRailCreator()
    {
        if (mode == Mode.DIRECT)
        {
            if (m_PlanningRobot.isPlanning)
                m_PlanningRobot.Show(false);

            if (m_GripperControl.isGripping)
                m_GripperControl.Show(false);

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