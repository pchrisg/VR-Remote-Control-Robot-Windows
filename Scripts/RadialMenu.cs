using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RadialMenu : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private Transform m_SelectionTransform = null;
    [SerializeField] private Transform m_CursorTransform = null;
    [SerializeField] private SpriteRenderer m_SRPlanRob = null;
    [SerializeField] private SpriteRenderer m_SRActiveMode = null;

    [Header("Sprites")]
    [SerializeField] private Sprite[] sprites = new Sprite[13];

    [Header("Events")]
    [SerializeField] private RadialSection north = null;
    [SerializeField] private RadialSection northeast = null;
    [SerializeField] private RadialSection southeast = null;
    [SerializeField] private RadialSection south = null;
    [SerializeField] private RadialSection southwest = null;
    [SerializeField] private RadialSection northwest = null;

    private ManipulationMode m_ManipulationMode = null;
    private PlanningRobot m_PlanningRobot = null;

    private ManipulationOptions.Mode m_MenuMode = ManipulationOptions.Mode.DIRECT;
    private Vector2 m_TouchPosition = Vector2.zero;
    private List<RadialSection> m_RadialSections = null;
    private RadialSection m_HighlightedSection = null;
    private bool isPlanning = false;

    private readonly float degreeIncrement = 60.0f;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();
        CreateAndSetupSections();
    }

    private void Update()
    {
        Vector2 direction = Vector2.zero + m_TouchPosition;
        float rotation = GetDegree(direction);

        SetCursorPosition();
        SetSelectionRotation(rotation);
        SetSeletedEvent(rotation);

        if (m_MenuMode != m_ManipulationMode.mode)
            SetSectionIcons();
    }

    private void CreateAndSetupSections()
    {
        m_RadialSections = new List<RadialSection>()
        {
            north,
            northeast,
            southeast,
            south,
            southwest,
            northwest
        };

        TogglePlanRob();
        SetSectionIcons();

        m_SRPlanRob.sprite = null;
        m_SRActiveMode.sprite = null;
    }

    private void SetSectionIcons()
    {
        if(m_ManipulationMode.mode == ManipulationOptions.Mode.DIRECT)
        {
            for(int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = sprites[i];
            }
            m_MenuMode = ManipulationOptions.Mode.DIRECT;
            m_SRActiveMode.sprite = null;
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.SDOF)
        {
            for (int i = 1; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[1].iconRenderer.sprite = sprites[7];
            m_MenuMode = ManipulationOptions.Mode.SDOF;
            m_SRActiveMode.sprite = sprites[1];
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.ATTOBJCREATOR)
        {
            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[2].iconRenderer.sprite = sprites[8];
            m_MenuMode = ManipulationOptions.Mode.ATTOBJCREATOR;
            m_SRActiveMode.sprite = sprites[2];
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.COLOBJCREATOR)
        {
            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[3].iconRenderer.sprite = sprites[9];
            m_MenuMode = ManipulationOptions.Mode.COLOBJCREATOR;
            m_SRActiveMode.sprite = sprites[3];
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.GRIPPER)
        {
            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[4].iconRenderer.sprite = sprites[10];
            m_MenuMode = ManipulationOptions.Mode.GRIPPER;
            m_SRActiveMode.sprite = sprites[4];
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.RAILCREATOR)
        {
            for (int i = 0; i < 6; i++)
            {
                m_RadialSections[i].iconRenderer.sprite = null;
            }
            m_RadialSections[5].iconRenderer.sprite = sprites[11];
            m_MenuMode = ManipulationOptions.Mode.RAILCREATOR;
            m_SRActiveMode.sprite = sprites[5];
        }
        if (m_ManipulationMode.mode == ManipulationOptions.Mode.RAIL)
        {
            m_RadialSections[0].iconRenderer.sprite = sprites[0];
            m_RadialSections[5].iconRenderer.sprite = sprites[12];
            m_MenuMode = ManipulationOptions.Mode.RAIL;
            m_SRActiveMode.sprite = sprites[11];
        }
    }

    private void TogglePlanRob()
    {
        if(!m_PlanningRobot.isPlanning)
        {
            m_RadialSections[0].iconRenderer.sprite = sprites[0];
            m_SRPlanRob.sprite = null;
        }
        else
        {
            m_RadialSections[0].iconRenderer.sprite = sprites[6];
            m_SRPlanRob.sprite = sprites[0];
        }

        isPlanning = m_PlanningRobot.isPlanning;
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }
    
    private float GetDegree(Vector2 direction)
    {
        float value = Mathf.Atan2(direction.x, direction.y);
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

        if (index == 6)
            index = 0;

        m_HighlightedSection = m_RadialSections[index];
    }

    public void SetTouchPosition(Vector2 newValue)
    {
        m_TouchPosition = newValue;
    }

    public void ActivateHighlightedSection()
    {
        m_HighlightedSection.onPress.Invoke();

        if (isPlanning != m_PlanningRobot.isPlanning)
            TogglePlanRob();

        if (m_MenuMode != m_ManipulationMode.mode)
            SetSectionIcons();
    }
}
