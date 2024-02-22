using UnityEngine;
using ManipulationModes;

namespace ManipulationModes
{
    public enum Mode
    {
        IDLE,
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
    public Mode mode = Mode.IDLE;
    //private Mode m_PrevMode = Mode.IDLE;

    [Header("Scene Objects")]
    [SerializeField] private SDOFWidget m_SDOFWidget = null;
    //[SerializeField] private RailCreator m_RailCreator = null;

    private Manipulator m_Manipulator = null;
    private CollisionObjects m_CollisionObjects = null;
    private ExperimentManager m_ExperimentManager = null;

    [HideInInspector] public const float ANGLETHRESHOLD = 5.0f;     //5deg
    [HideInInspector] public const float DISTANCETHRESHOLD = 0.03f; //3cm
    [HideInInspector] public const float SCALINGFACTOR = 0.25f;     //25%

    //private PlanningRobot m_PlanningRobot = null;
    //private GripperControl m_GripperControl = null;

    public bool isInteracting = false;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        //m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        //m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
    }

    private void Update()
    {
        if (mode != m_ExperimentManager.m_Technique)
        {
            if (mode == Mode.IDLE)
            {
                if (m_ExperimentManager.m_Technique == Mode.SIMPLEDIRECT)
                    mode = Mode.SIMPLEDIRECT;
                else if (m_ExperimentManager.m_Technique == Mode.DIRECT)
                    ToggleDirect();
                else if (m_ExperimentManager.m_Technique == Mode.SDOF)
                    ToggleSDOF();
            }
            else if (mode == Mode.SIMPLEDIRECT)
            {
                if (m_ExperimentManager.m_Technique == Mode.DIRECT)
                    ToggleDirect();
                else if (m_ExperimentManager.m_Technique == Mode.SDOF)
                    ToggleSDOF();
            }
            else if (mode == Mode.DIRECT)
            {
                if (m_ExperimentManager.m_Technique == Mode.SIMPLEDIRECT)
                {
                    ToggleDirect();
                    mode = Mode.SIMPLEDIRECT;
                }
                else if (m_ExperimentManager.m_Technique == Mode.SDOF)
                {
                    ToggleDirect();
                    ToggleSDOF();
                }
            }
            else if (mode == Mode.SDOF)
            {
                if (m_ExperimentManager.m_Technique == Mode.SIMPLEDIRECT)
                {
                    ToggleSDOF();
                    mode = Mode.SIMPLEDIRECT;
                }
                else if (m_ExperimentManager.m_Technique == Mode.DIRECT)
                {
                    ToggleSDOF();
                    ToggleDirect();
                }
            }
        }
    }

    public void ToggleDirect()
    {
        /*//if (m_PlanningRobot.isPlanning)
        //    m_PlanningRobot.Show(false);

        //if (mode == Mode.SDOF)
        //    m_SDOFWidget.Show(false);

        //else if (mode == Mode.ATTOBJCREATOR || mode == Mode.COLOBJCREATOR)
        //    m_CollisionObjects.isCreating = false;

        //else if (mode == Mode.RAILCREATOR)
        //{
        //    m_RailCreator.Show(false);
        //    m_RailCreator.RemoveAllRails();
        //}

        //else if (mode == Mode.RAIL)
        //    m_RailCreator.RemoveAllRails();*/

        if (mode == Mode.IDLE || mode == Mode.SIMPLEDIRECT)
            mode = Mode.DIRECT;
        else if (mode == Mode.DIRECT)
        {

            mode = Mode.IDLE;
        }
    }
    
    public void ToggleSDOF()
    {
        /*if(mode == Mode.DIRECT)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_SDOFWidget.Show(true);
            mode = Mode.SDOF;
        }

        else if (mode == Mode.SDOF)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_SDOFWidget.Show(false);
            mode = Mode.DIRECT;
        }*/

        if (mode == Mode.IDLE || mode == Mode.SIMPLEDIRECT)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_SDOFWidget.Show(true);
            mode = Mode.SDOF;
        }
        else if (mode == Mode.SDOF)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_SDOFWidget.Show(false);
            mode = Mode.IDLE;
        }
    }

    public void ToggleAttachableObjectCreator()
    {
        if (mode == Mode.DIRECT)
        {
            //if (m_PlanningRobot.isPlanning)
            //    m_PlanningRobot.Show(false);

            //if (m_GripperControl.isGripping)
            //    m_GripperControl.Show(false);

            m_CollisionObjects.isCreating = true;

            //m_PrevMode = mode;
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
            //if (m_PlanningRobot.isPlanning)
            //    m_PlanningRobot.Show(false);

            //if (m_GripperControl.isGripping)
            //    m_GripperControl.Show(false);

            m_CollisionObjects.isCreating = true;

            //m_PrevMode = mode;
            mode = Mode.COLOBJCREATOR;
        }

        else if (mode == Mode.COLOBJCREATOR)
        {
            m_CollisionObjects.isCreating = false;
            mode = Mode.DIRECT;
        }
    }

    /*public void TogglePlanner()
    {
        if(!m_GripperControl.isGripping &&
           (mode == Mode.SIMPLEDIRECT ||
           mode == Mode.DIRECT ||
           mode == Mode.SDOF ||
           mode == Mode.RAIL))
        {
            if(m_PlanningRobot.isPlanning)
            {
                m_Manipulator.ResetPositionAndRotation();
                m_PlanningRobot.Show(false);
            }
            else
                m_PlanningRobot.Show(true);
        }
    }*/

    /*public void ToggleGripper()
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
    }*/

    /*public void ToggleRailCreator()
    {
        if (mode == Mode.DIRECT)
        {
            //if (m_PlanningRobot.isPlanning)
            //    m_PlanningRobot.Show(false);

            //if (m_GripperControl.isGripping)
            //    m_GripperControl.Show(false);

            m_Manipulator.ResetPositionAndRotation();
            m_RailCreator.Show(true);
            mode = Mode.RAILCREATOR;
        }

        else if (mode == Mode.RAILCREATOR)
        {
            m_Manipulator.ResetPosition();
            m_RailCreator.Show(false);
            mode = Mode.RAIL;
        }

        else if (mode == Mode.RAIL)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_RailCreator.RemoveAllRails();
            mode = Mode.DIRECT;
        }
    }*/
}