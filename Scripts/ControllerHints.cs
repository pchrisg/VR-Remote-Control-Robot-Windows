using System;
using System.Collections;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ControllerHints : MonoBehaviour
{
    // SteamVR Actions
    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private SteamVR_Action_Boolean m_Trackpad = null;
    private SteamVR_Action_Boolean m_TouchRight = null;
    private SteamVR_Action_Boolean m_TouchLeft = null;

    private Coroutine m_RightCoroutine = null;
    private Coroutine m_LeftCoroutine = null;

    private void Awake()
    {
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trackpad = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchTrackpad");
        m_TouchRight = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchRight");
        m_TouchLeft = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TouchLeft");
    }

    private IEnumerator ShowTextHints(Hand hand, ISteamVR_Action_In action, string text, Func<bool> lambda)
    {
        ControllerButtonHints.HideAllTextHints(hand);
        bool active = false;
        while (true)
        {
            if (action.GetActive(hand.handType))
            {
                if(!lambda())
                {
                    if (!active)
                    {
                        ControllerButtonHints.ShowTextHint(hand, action, text);
                        active = true;
                    }
                }
                else
                {
                    ControllerButtonHints.HideTextHint(hand, action);
                    active = false;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ShowHint(Hand hand, bool value, SteamVR_Action_Boolean action, String text)
    {
        if (value)
        {
            if (hand == Player.instance.rightHand)
            {
                if (m_RightCoroutine != null)
                    StopCoroutine(m_RightCoroutine);

                m_RightCoroutine = StartCoroutine(ShowTextHints(hand, action, text, () => action.GetState(hand.handType)));
            }
            else
            {
                if (m_LeftCoroutine != null)
                    StopCoroutine(m_LeftCoroutine);

                m_LeftCoroutine = StartCoroutine(ShowTextHints(hand, action, text, () => action.GetState(hand.handType)));
            }
        }
        else
        {
            if (hand == Player.instance.rightHand)
            {
                if (m_RightCoroutine != null)
                    StopCoroutine(m_RightCoroutine);

                m_RightCoroutine = null;
            }
            else
            {
                if (m_LeftCoroutine != null)
                    StopCoroutine(m_LeftCoroutine);

                m_LeftCoroutine = null;
            }

            ControllerButtonHints.HideTextHint(hand, action);
        }
    }

    public void ShowGripHint(Hand hand, bool value)
    {
        ShowHint(hand, value, m_Grip, "Grab the Grip Button");
    }

    public void ShowTriggerHint(Hand hand, bool value)
    {
        ShowHint(hand, value, m_Trigger, "Grab the Trigger Button");
    }

    public void ShowSqueezeHint(Hand hand, bool value)
    {
        ShowHint(hand, value, m_Trigger, "Squeeze the Trigger Button");
    }

    public void ShowTrackpadHint(bool value)
    {
        ShowHint(Player.instance.rightHand, value, m_Trackpad, "Touch the Trackpad");
    }

    public void ShowTouchRightHint(bool value)
    {
        ShowHint(Player.instance.leftHand, value, m_TouchRight, "Touch the Trackpad on the right");
    }

    public void ShowTouchLeftHint(bool value)
    {
        ShowHint(Player.instance.leftHand, value, m_TouchLeft, "Touch the Trackpad on the left");
    }
}