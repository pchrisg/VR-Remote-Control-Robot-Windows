using UnityEngine;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class Tutorial1 : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_TeapotPrefab = null;
    [SerializeField] private GameObject m_ButtonPrefab = null;

    [Header("Stage")]
    [SerializeField] private Stage stage = Stage.WAIT;

    [SerializeField] private bool m_Continue = false;

    // Scripts
    private Experiment1Manager m_ExperimentManager = null;
    private Manipulator m_Manipulator = null;
    private ManipulationMode m_ManipulationMode = null;
    private ControllerHints m_ControllerHints = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    // GameObjects
    GameObject m_Button = null;
    GameObject m_Teapot = null;

    private Coroutine m_ActiveCoroutine = null;
    private List<Text> m_Text = new List<Text>();
    private AudioSource m_AudioSource = null;

    private void Awake()
    {
        m_ExperimentManager = gameObject.GetComponentInParent<Experiment1Manager>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        foreach (var text in gameObject.GetComponentsInChildren<Text>())
            m_Text.Add(text);
        m_AudioSource = gameObject.GetComponentInChildren<AudioSource>();
    }

    private void OnDisable()
    {
        if (m_Button != null)
            GameObject.Destroy(m_Button);

        stage = Stage.WAIT;

        if (m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);

        if (m_Manipulator != null)
            m_Manipulator.Flash(false);

        m_ActiveCoroutine = null;
    }

    private void Update()
    {
        if (stage == Stage.START)
            m_ActiveCoroutine ??= StartCoroutine(Learning());
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);

        if (value)
            ResetTutorial();
        else
        {
            stage = Stage.WAIT;

            if (m_Manipulator.GetComponent<SimpleDirectManipulation>() == null)
            {
                m_Manipulator.GetComponent<Interactable>().highlightOnHover = true;
                m_Manipulator.AddComponent<SimpleDirectManipulation>();
            }
        }
    }

    public void ResetTutorial()
    {
        stage = Stage.WAIT;
        m_ExperimentManager.ShowRobot(false);
        m_Manipulator.ShowManipulator(false);

        if (m_Teapot != null)
            Destroy(m_Teapot);

        if (m_Button != null)
            Destroy(m_Button);

        if (m_Manipulator.GetComponent<SimpleDirectManipulation>() != null)
        {
            m_Manipulator.GetComponent<Interactable>().highlightOnHover = false;
            Destroy(m_Manipulator.GetComponent<SimpleDirectManipulation>());
        }
    }

    public void StartTutorial()
    {
        stage = Stage.START;
    }

    private void ChangeText(string instruction)
    {
        foreach(var text in m_Text)
            text.text = instruction;
    }

    private IEnumerator Learning()
    {
        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_Teapot = Instantiate(m_TeapotPrefab);
        m_Teapot.transform.SetPositionAndRotation(new Vector3(-0.49f, 0.29f, 0.1f), Quaternion.Euler(0.0f, 90.0f, 0.0f));

        string text = "Moving objects\n\n" +
                      "Reach out your hand and grab the teapot\n\n" +
                      "While grabbing the teapot, move your hand\n\n" +
                      "Notice how the teapot follows your hand";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_Teapot.GetComponent<Teapot>().Flash(true);

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        Destroy(m_Teapot);
        m_Manipulator.ShowManipulator(true);
        m_Manipulator.GetComponent<Interactable>().highlightOnHover = true;
        m_Manipulator.AddComponent<SimpleDirectManipulation>();

        text = "Robot Control\n\n" +
               "This is the manipulator aka the remote control for the robot\n\n" +
               "You can move it in the same way you moved the teapot before\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_ExperimentManager.ShowRobot(true);

        text = "Robot Control\n\n" +
               "This is the robot that you are controlling\n\n" +
               "When you move the manipulator, you are telling the robot where you want its end effector to be\n\n +" +
               "Notice that there is a small delay ";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        stage = Stage.WAIT;

        m_ActiveCoroutine = null;
    }
}
