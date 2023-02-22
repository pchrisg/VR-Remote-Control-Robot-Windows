using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RadialMenuManager : MonoBehaviour
{
    private SteamVR_Action_Boolean m_Touch = null;
    private SteamVR_Action_Vector2 m_TouchPos = null;

    [Header("Scene Objects")]
    [SerializeField] private RadialMenu radialMenu = null;

    private void Awake()
    {
        m_Touch = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchTrackpad");
        m_TouchPos = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("TouchPosition");
        m_Touch.onChange += Touch;
        m_TouchPos.onAxis += Position;
    }

    private void OnDestroy()
    {
        m_Touch.onChange -= Touch;
        m_TouchPos.onAxis -= Position;
    }

    private void Position(SteamVR_Action_Vector2 fromAction, SteamVR_Input_Sources fromSource, Vector2 axis, Vector2 delta)
    {
        radialMenu.SetTouchPosition(axis);
    }

    private void Touch(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (!newState)
            radialMenu.ActivateHighlightedSection();
        radialMenu.Show(newState);
    }
}
