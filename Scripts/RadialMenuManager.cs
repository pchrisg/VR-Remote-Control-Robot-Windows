using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RadialMenuManager : MonoBehaviour
{
    [Header("Actions")]
    public SteamVR_Action_Boolean m_Touch = null;
    public SteamVR_Action_Boolean m_Press = null;
    public SteamVR_Action_Vector2 m_TouchPos = null;

    [Header("Scene Objects")]
    [SerializeField] private RadialMenu radialMenu = null;

    private void Awake()
    {
        m_Touch.onChange += Touch;
        m_Press.onStateUp += PressRelease;
        m_TouchPos.onAxis += Position;
    }

    private void OnDestroy()
    {
        m_Touch.onChange -= Touch;
        m_Press.onStateUp -= PressRelease;
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

    private void PressRelease(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        //radialMenu.ActivateHighlightedSection();
    }
}
