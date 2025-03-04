using System.Collections.Generic;
using UnityEngine;
using ManipulationModes;

public class RadialMenu : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private Transform m_SelectionTransform = null;
    [SerializeField] private Transform m_CursorTransform = null;
    //[SerializeField] private SpriteRenderer m_PlanRobSR = null;
    [SerializeField] private SpriteRenderer m_ActiveSR = null;
    //[SerializeField] private SpriteRenderer m_GripperSR = null;
    [SerializeField] private SpriteRenderer m_SelectionSR = null;
    [SerializeField] private SpriteRenderer m_NullSelectionSR = null;

    [Header("Events")]
    [SerializeField] private RadialSection east = null;
    [SerializeField] private RadialSection west = null;
    /*[SerializeField] private RadialSection north = null;
    [SerializeField] private RadialSection south = null;
    [SerializeField] private RadialSection northeast = null;
    [SerializeField] private RadialSection southeast = null;
    [SerializeField] private RadialSection southwest = null;
    [SerializeField] private RadialSection northwest = null;*/
    private List<RadialSection> m_RadialSections = null;

    [Header("Sprites")]
    [SerializeField] private Sprite[] m_Sprites = new Sprite[4];

    private ManipulationMode m_ManipulationMode = null;

    private Mode m_MenuMode = Mode.CONSTRAINEDDIRECT;
    private Vector2 m_TouchPosition = Vector2.zero;
    
    private RadialSection m_HighlightedSection = null;

    private Color m_ShowColor = new(1.0f, 1.0f, 0.0f, 1.0f);
    private Color m_HideColor = new(0.0f, 0.0f, 0.0f, 0.0f);
    private Color m_BlockColor = new(1.0f, 1.0f, 1.0f, 1.0f);


    private readonly float degreeIncrement = 180.0f;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        CreateAndSetupSections();
    }

    private void Update()
    {
        SetCursorPosition();

        if (m_TouchPosition.magnitude < 0.35f)
        {
            m_SelectionSR.color = m_HideColor;
            m_NullSelectionSR.color = m_ShowColor;

            m_HighlightedSection = null;
        }
        else
        {
            m_SelectionSR.color = m_ShowColor;
            m_NullSelectionSR.color = m_BlockColor;

            float rotation = GetDegree(m_TouchPosition);
            SetSelectionRotation(rotation);
            SetSeletedEvent(rotation);
        }

        //if (m_MenuMode != m_ManipulationMode.mode)
        //    SetSectionIcons();
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void CreateAndSetupSections()
    {
        m_RadialSections = new List<RadialSection>()
        {
            /*,
            northeast,
            southeast,
            southwest,
            northwest*/
            east,
            west
        };

        for (int i = 0; i < 2; i++)
            m_RadialSections[i].iconRenderer.sprite = m_Sprites[i];

        SetSectionIcons();

        //m_PlanRobSR.sprite = null;
        m_ActiveSR.sprite = null;
    }

    private void SetSectionIcons()
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            m_MenuMode = Mode.CONSTRAINEDDIRECT;
            m_ActiveSR.sprite = null;

            for (int i = 0; i < 2; i++)
                m_RadialSections[i].iconRenderer.sprite = m_Sprites[i];
        }

        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
        {
            m_MenuMode = Mode.COLOBJCREATOR;
            m_ActiveSR.sprite = m_Sprites[0];

            m_RadialSections[0].iconRenderer.sprite = m_Sprites[2];
        }

        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_MenuMode = Mode.ATTOBJCREATOR;
            m_ActiveSR.sprite = m_Sprites[1];

            m_RadialSections[1].iconRenderer.sprite = m_Sprites[3];
        }
    }

    /*private void SetSectionIcons()
    {
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
        {
            m_MenuMode = Mode.SIMPLEDIRECT;
            m_ActiveSR.sprite = null;

            SetPlanRobIcon();
            SetGripIcon();
        }
        else
        {
            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = m_Sprites[i];
            }
        }

        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            m_MenuMode = Mode.DIRECT;

            m_ActiveSR.sprite = null;

            SetPlanRobIcon();
            SetGripIcon();
        }
        if (m_ManipulationMode.mode == Mode.SDOF)
        {
            m_MenuMode = Mode.SDOF;

            m_RadialSections[1].iconRenderer.sprite = m_Sprites[7];
            m_RadialSections[5].iconRenderer.sprite = null;
            
            m_ActiveSR.sprite = m_Sprites[1];

            SetPlanRobIcon();
            SetGripIcon();
        }
        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_MenuMode = Mode.ATTOBJCREATOR;

            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[2].iconRenderer.sprite = m_Sprites[8];
            
            m_ActiveSR.sprite = m_Sprites[2];
        }
        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
        {
            m_MenuMode = Mode.COLOBJCREATOR;

            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[3].iconRenderer.sprite = m_Sprites[9];
            
            m_ActiveSR.sprite = m_Sprites[3];
        }
        if (m_ManipulationMode.mode == Mode.RAILCREATOR)
        {
            m_MenuMode = Mode.RAILCREATOR;

            m_RadialSections[0].iconRenderer.sprite = null;
            m_RadialSections[1].iconRenderer.sprite = null;
            m_RadialSections[4].iconRenderer.sprite = null;
            m_RadialSections[5].iconRenderer.sprite = m_Sprites[11];
            m_MenuMode = Mode.RAILCREATOR;
            m_ActiveSR.sprite = m_Sprites[5];
        }
        if (m_ManipulationMode.mode == Mode.RAIL)
        {
            m_RadialSections[1].iconRenderer.sprite = null;
            m_RadialSections[5].iconRenderer.sprite = m_Sprites[12];
            m_MenuMode = Mode.RAIL;
            m_ActiveSR.sprite = m_Sprites[11];

            SetPlanRobIcon();
            SetGripIcon();
        }
    }*/

    /*private void SetPlanRobIcon()
    {
        //isPlanning = m_PlanningRobot.isPlanning;

        if (!isPlanning)
        {
            m_RadialSections[0].iconRenderer.sprite = m_Sprites[0];
            m_PlanRobSR.sprite = null;

            m_RadialSections[4].iconRenderer.sprite = m_Sprites[4];
        }
        else
        {
            m_RadialSections[0].iconRenderer.sprite = m_Sprites[6];
            m_PlanRobSR.sprite = m_Sprites[0];

            m_RadialSections[4].iconRenderer.sprite = null;
        }
    }*/

    /*private void SetGripIcon()
    {
        isGripping = m_GripperControl.isGripping;

        if (!isGripping)
        {
            m_RadialSections[4].iconRenderer.sprite = m_Sprites[4];
            m_GripperSR.sprite = null;

            m_RadialSections[0].iconRenderer.sprite = m_Sprites[0];
        }
        else
        {
            m_RadialSections[4].iconRenderer.sprite = m_Sprites[10];
            m_GripperSR.sprite = m_Sprites[4];

            m_RadialSections[0].iconRenderer.sprite = null;
        }
    }*/

    private float GetDegree(Vector2 direction)
    {
        float value = Mathf.Atan2(direction.y, direction.x);
        value *= Mathf.Rad2Deg;

        if (value < 0)
            value += 360.0f;

        return value;
    }

    private void SetCursorPosition()
    {
        m_CursorTransform.localPosition = m_TouchPosition;
    }

    private void SetSelectionRotation(float newRotation)
    {
        float snappedRotation = SnapRotation(newRotation);
        m_SelectionTransform.localEulerAngles = new Vector3(0, 0, -snappedRotation);
    }

    private float SnapRotation(float rotation)
    {
        return GetNearestIncrement(rotation) * degreeIncrement;
    }

    private int GetNearestIncrement(float rotation)
    {
        return Mathf.RoundToInt(rotation / degreeIncrement);
    }

    private void SetSeletedEvent(float currentRotation)
    {
        int index = GetNearestIncrement(currentRotation);

        if (index == 2)
            index = 0;

        m_HighlightedSection = m_RadialSections[index];
    }

    public void SetTouchPosition(Vector2 newValue)
    {
        m_TouchPosition = newValue;
    }

    public void ActivateHighlightedSection()
    {
        if(m_HighlightedSection != null)
        {
            m_HighlightedSection.onPress.Invoke();

            //if (isPlanning != m_PlanningRobot.isPlanning)
            //    SetPlanRobIcon();

            //if (isGripping != m_GripperControl.isGripping)
            //    SetGripIcon();

            if (m_MenuMode != m_ManipulationMode.mode)
                SetSectionIcons();
        }
    }
}
