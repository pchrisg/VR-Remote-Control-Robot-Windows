using UnityEngine;
using ManipulationModes;
using Valve.VR.InteractionSystem;
using Unity.VisualScripting;

namespace ManipulationModes
{
    public enum Mode
    {
        IDLE,
        SIMPLEDIRECT,
        CONSTRAINEDDIRECT,
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

    private Manipulator m_Manipulator = null;
    private SDOFManipulation m_SDOFManipulation = null;
    private InteractableObjects m_InteractableObjects = null;
    private ExperimentManager m_ExperimentManager = null;

    private bool m_isInteracting = false;

    [HideInInspector] public const float ANGLETHRESHOLD = 10.0f;     //10deg
    [HideInInspector] public const float DISTANCETHRESHOLD = 0.03f; //3cm
    [HideInInspector] public const float SCALINGFACTOR = 0.25f;     //25%

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_SDOFManipulation = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
    }

    private void Update()
    {
        if (mode != m_ExperimentManager.m_Technique)
        {
            if (mode == Mode.IDLE)
            {
                if (m_ExperimentManager.m_Technique == Mode.SIMPLEDIRECT)
                    mode = Mode.SIMPLEDIRECT;
                else if (m_ExperimentManager.m_Technique == Mode.CONSTRAINEDDIRECT)
                    ToggleDirect();
                else if (m_ExperimentManager.m_Technique == Mode.SDOF)
                    ToggleSDOF();
            }
            else if (mode == Mode.SIMPLEDIRECT)
            {
                if (m_ExperimentManager.m_Technique == Mode.CONSTRAINEDDIRECT)
                    ToggleDirect();
                else if (m_ExperimentManager.m_Technique == Mode.SDOF)
                    ToggleSDOF();
            }
            else if (mode == Mode.CONSTRAINEDDIRECT)
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
                else if (m_ExperimentManager.m_Technique == Mode.CONSTRAINEDDIRECT)
                {
                    ToggleSDOF();
                    ToggleDirect();
                }
            }

            m_Manipulator.ResetPositionAndRotation();
        }
    }

    public void IsInteracting(bool isInteracting)
    {
        m_isInteracting = isInteracting;
        m_ExperimentManager.RecordInteraction(m_isInteracting);
    }

    public bool IsInteracting()
    {
        return m_isInteracting;
    }

    public void ToggleDirect()
    {
        if (mode == Mode.IDLE || mode == Mode.SIMPLEDIRECT)
            mode = Mode.CONSTRAINEDDIRECT;

        else if (mode == Mode.CONSTRAINEDDIRECT)
            mode = Mode.IDLE;
    }
    
    public void ToggleSDOF()
    {
        if (mode == Mode.IDLE || mode == Mode.SIMPLEDIRECT)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_Manipulator.GetComponent<Interactable>().highlightOnHover = false;
            m_SDOFManipulation.Show(true);
            mode = Mode.SDOF;
        }
        else if (mode == Mode.SDOF)
        {
            m_Manipulator.ResetPositionAndRotation();
            m_Manipulator.GetComponent<Interactable>().highlightOnHover = true;
            m_SDOFManipulation.Show(false);
            mode = Mode.IDLE;
        }
    }

    public void ToggleAttachableObjectCreator()
    {
        if (mode == Mode.CONSTRAINEDDIRECT)
        {
            m_isInteracting = true;
            m_InteractableObjects.IsCreating(true);
            mode = Mode.ATTOBJCREATOR;
        }

        else if (mode == Mode.ATTOBJCREATOR)
        {
            m_isInteracting = false;
            m_InteractableObjects.IsCreating(false);
            mode = Mode.CONSTRAINEDDIRECT;
        }
    }

    public void ToggleCollisionObjectCreator()
    {
        if (mode == Mode.CONSTRAINEDDIRECT)
        {
            m_isInteracting = true;
            m_InteractableObjects.IsCreating(true);
            mode = Mode.COLOBJCREATOR;
        }

        else if (mode == Mode.COLOBJCREATOR)
        {
            m_isInteracting = false;
            m_InteractableObjects.IsCreating(false);
            mode = Mode.CONSTRAINEDDIRECT;
        }
    }
}