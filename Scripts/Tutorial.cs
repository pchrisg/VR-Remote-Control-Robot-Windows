using System.Collections;
using UnityEngine;
using ManipulationOptions;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Tutorial : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private ControllerHints m_ControllerHints = null;
    private GameObject m_Objects = null;

    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    private int stage = 0;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();
        m_Objects = gameObject.transform.parent.GetComponent<ExperimentManager>().m_Objects;

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void OnDisable()
    {
        DestroyAllObjects();
    }

    private void Update()
    {
        
    }

    private void DestroyAllObjects()
    {
        if (m_Objects != null && m_Objects.transform.childCount > 0)
        {
            for (var i = m_Objects.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if (value)
            ResetTutorial();
    }

    public void ResetTutorial()
    {
        if(m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
        {

        }
        else
        {
            m_ManipulationMode.ToggleDirect();


        }
    }
}