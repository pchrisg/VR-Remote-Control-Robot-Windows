using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class RadialMenuManager : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private SteamVR_Action_Boolean m_TouchTrackpad = null;
    private SteamVR_Action_Vector2 m_TouchPos = null;
    private SteamVR_Action_Boolean m_PressTrackpad = null;

    [Header("Scene Objects")]
    [SerializeField] private RadialMenu radialMenu = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();

        m_TouchTrackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchTrackpad");
        m_TouchPos = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("TouchPosition");
        m_PressTrackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PressTrackpad");
        m_TouchTrackpad.onChange += Touch;
        m_TouchPos.onAxis += Position;
        m_PressTrackpad.onStateDown += Click;
    }

    private void OnDestroy()
    {
        m_TouchTrackpad.onChange -= Touch;
        m_TouchPos.onAxis -= Position;
        m_PressTrackpad.onStateDown -= Click;
    }

    private void Start()
    {
        Invoke("SetRightHandAsParent", 0.5f);
    }

    private void SetRightHandAsParent()
    {
        Hand rightHand = Player.instance.rightHand;
        gameObject.transform.position = rightHand.transform.position;
        gameObject.transform.rotation = rightHand.transform.rotation;
        gameObject.transform.SetParent(rightHand.transform);
    }

    private void Position(SteamVR_Action_Vector2 fromAction, SteamVR_Input_Sources fromSource, Vector2 axis, Vector2 delta)
    {
        radialMenu.SetTouchPosition(axis);
    }

    private void Touch(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if(m_ManipulationMode.mode == Mode.DIRECT ||
           m_ManipulationMode.mode == Mode.ATTOBJCREATOR ||
           m_ManipulationMode.mode == Mode.COLOBJCREATOR)
            radialMenu.Show(newState);
    }

    private void Click(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        radialMenu.ActivateHighlightedSection();
    }
}
