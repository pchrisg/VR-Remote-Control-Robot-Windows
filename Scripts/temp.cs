using UnityEngine;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Tutorial1 : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject m_TeapotPrefab = null;
    [SerializeField] private GameObject m_GhostManipPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;
    [SerializeField] private GameObject m_ButtonPrefab = null;

    [Header("Stage")]
    [SerializeField] private Stage stage = Stage.WAIT;

    [SerializeField] private bool m_Continue = false;

    // Scripts
    private ExperimentManager m_ExperimentManager = null;
    private Manipulator m_Manipulator = null;
    private ControllerHints m_ControllerHints = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    // GameObjects
    private GameObject m_Teapot = null;
    private GameObject m_GhostManip = null;
    private GameObject m_Obstacle = null;
    private GameObject m_Button = null;

    private Coroutine m_ActiveCoroutine = null;
    private readonly List<Text> m_Text = new();
    private AudioSource m_AudioSource = null;

    private void Awake()
    {
        m_ExperimentManager = gameObject.GetComponentInParent<ExperimentManager>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();

        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        foreach (var text in gameObject.GetComponentsInChildren<Text>())
            m_Text.Add(text);
        m_AudioSource = gameObject.GetComponentInChildren<AudioSource>();
    }

    private void OnDisable()
    {
        if (m_Manipulator != null && m_Manipulator.GetComponent<SimpleDirectManipulation>() == null)
        {
            m_Manipulator.GetComponent<Interactable>().highlightOnHover = true;
            m_Manipulator.AddComponent<SimpleDirectManipulation>();
        }
    }

    private void Update()
    {
        if (stage == Stage.START)
            m_ActiveCoroutine ??= StartCoroutine(Learning());
    }

    public void Setup(bool value)
    {
        ResetTutorial();

        gameObject.SetActive(value);
    }

    public void ResetTutorial()
    {
        stage = Stage.WAIT;
        //m_ExperimentManager.ShowRobot(false);
        m_Manipulator.ShowManipulator(false);

        if (m_Teapot != null)
            Destroy(m_Teapot);

        if (m_GhostManip != null)
            Destroy(m_GhostManip);

        if (m_Obstacle != null)
            Destroy(m_Obstacle);

        if (m_Button != null)
            Destroy(m_Button);

        if (m_ActiveCoroutine != null)
        {
            StopCoroutine(m_ActiveCoroutine);
            m_ActiveCoroutine = null;
        }

        if (m_Manipulator != null)
            m_Manipulator.Flash(false);

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
        m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        string text = "Controller\n\n" +
                      "The only important controller button in this experiment is the trigger button\n\n" +
                      "Look at your controller and pull the trigger button";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger || m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        text = "Controller\n\n" +
               "Notice the hand makes a grabbing gesture when you pull the triller button\n\n" +
               "When I say \"grab\", what I mean is \"pull the trigger button\"";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_Teapot = Instantiate(m_TeapotPrefab);
        m_Teapot.transform.position = new Vector3(-0.1f, 0.29f, -0.49f);

        text = "Moving objects\n\n" +
               "Reach out your hand and grab the teapot\n\n" +
               "While grabbing the teapot, move your hand\n\n" +
               "Notice how the teapot follows your hand";
        ChangeText(text);
        m_AudioSource.Play();
        
        //m_Teapot.GetComponent<Teapot>().Flash(true);

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

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

        //m_ExperimentManager.ShowRobot(true);

        text = "Robot Control\n\n" +
               "This is the robot that you are controlling\n\n" +
               "When you move the manipulator, you are telling the robot where you want its end effector to be";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        text = "Robot Control\n\n" +
               "Notice that the robot moves much slower than the manipulator\n\n" +
               "Keep that in mind when controlling the robot and give the robot time to catch up to the manipulator";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_GhostManip = Instantiate(m_GhostManipPrefab);
        m_GhostManip.transform.SetPositionAndRotation(new Vector3(-0.1f,0.39f,-0.9f), Quaternion.Euler(0.0f,-90.0f,90.0f));

        text = "Movement Feedback\n\n" +
               "If the manipulator turns red and you hear a thump sound, that means that the robot is incapable of reaching that position\n\n" +
               "Move the manipulator to the indicated position";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_GhostManip.transform.position = new Vector3(-0.1f, 0.39f, -0.7f);

        text = "Movement Feedback\n\n" +
               "If this happens, just move the manipulator until the robot can reach it\n\n" +
               "Move the manipulator back in range of the robot";
        ChangeText(text);
        m_AudioSource.Play();

        m_GhostManip.transform.position = new Vector3(-0.1f, 0.39f, -0.4f);

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        m_Obstacle = Instantiate(m_ObstaclePrefab);
        m_Obstacle.transform.SetPositionAndRotation(new Vector3(0.2f,0.2f,-0.4f),Quaternion.Euler(0.0f,60.0f,0.0f));

        m_GhostManip.transform.position = new Vector3(0.4f, 0.39f, -0.4f);

        text = "Movement Feedback\n\n" +
               "If the robot hits an object, the part of the robot that is colliding will turn red and you will hear a thump sound\n\n" +
               "Move the robot to the indicated position";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        text = "Movement Feedback\n\n" +
               "When manipulating, try to avoid collisions as much as possible by guiding the robot around obstacles";
        ChangeText(text);

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        Destroy(m_GhostManip);
        Destroy(m_Obstacle);

        m_Button = Instantiate(m_ButtonPrefab);
        m_Button.transform.position = new Vector3(0.0f,0.025f,-0.45f);

        text = "Push the button\n\n" +
               "Use the manipulator to control the robot to push the button\n\n"+
               "The button will turn purple and make a sound when pushed successfully\n\n"; ;
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_Continue);
        m_Continue = false;

        Destroy(m_Button);

        stage = Stage.WAIT;

        m_ActiveCoroutine = null;
    }
}