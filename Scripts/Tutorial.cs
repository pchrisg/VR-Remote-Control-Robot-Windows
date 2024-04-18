using UnityEngine;
using ManipulationModes;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace TutorialStages
{
    public enum Stage
    {
        WAIT,
        START,
        SIMPLEDIRECT,
        DIRECT,
        SDOF,
        PRACTICE
    };
}

public class Tutorial : MonoBehaviour
{
    [Header("Stage")]
    public Stage stage = Stage.WAIT;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_GhostManipulator = null;
    [SerializeField] private GameObject m_Barrel = null;
    [SerializeField] private GameObject m_Target = null;
    [SerializeField] private GameObject m_Obstacle = null;

    [Header("Sprites")]
    [SerializeField] private Sprite[] m_Sprites = new Sprite[2];

    // Scripts
    private ExperimentManager m_ExperimentManager = null;

    private ManipulationMode m_ManipulationMode = null;
    private SimpleDirectManipulation m_SimpleDirect = null;
    private ConstrainedDirectManipulation m_Direct = null;
    private SDOFManipulation m_SDOF = null;

    private Manipulator m_Manipulator = null;

    private ControllerHints m_ControllerHints = null;
    private InteractableObjects m_InteractableObjects = null;

    // Game Objects
    private GameObject m_Objects = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    // Timer
    private Timer m_Timer = null;
    private readonly float m_TimeLimit = 300.0f;

    private Coroutine m_ActiveCoroutine = null;
    private readonly List<Text> m_Text = new();
    private readonly List<SpriteRenderer> m_SpriteRenderers = new();
    private AudioSource m_AudioSource = null;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_SimpleDirect = m_Manipulator.GetComponent<SimpleDirectManipulation>();
        m_Direct = m_Manipulator.GetComponent<ConstrainedDirectManipulation>();
        m_SDOF = m_Manipulator.transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();

        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_Objects = gameObject.transform.parent.GetComponent<ExperimentManager>().m_Objects;
        
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        foreach (var text in gameObject.GetComponentsInChildren<Text>())
            m_Text.Add(text);

        foreach (var renderer in gameObject.GetComponentsInChildren<SpriteRenderer>())
            m_SpriteRenderers.Add(renderer);
        ChangeSprite(null);

        m_AudioSource = gameObject.GetComponentInChildren<AudioSource>();
    }

    private void OnDisable()
    {
        stage = Stage.WAIT;
        ChangeText("Instructions");
        ChangeSprite(null);

        if (m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);

        m_ActiveCoroutine = null;

        StartCoroutine(DestroyAllObjects());
    }

    private void Update()
    {
        if (stage == Stage.START)
        {
            if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
                stage = Stage.SIMPLEDIRECT;
            if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
                stage = Stage.DIRECT;
            if (m_ManipulationMode.mode == Mode.SDOF)
                stage = Stage.SDOF;
        }

        if (stage == Stage.SIMPLEDIRECT)
            m_ActiveCoroutine ??= StartCoroutine(SimpleDirect());

        if (stage == Stage.DIRECT)
            m_ActiveCoroutine ??= StartCoroutine(Direct());

        if (stage == Stage.SDOF)
            m_ActiveCoroutine ??= StartCoroutine(SDOF());

        if (stage == Stage.PRACTICE)
        {
            m_ActiveCoroutine ??= StartCoroutine(Practice());

            if (m_Timer.TimeExhausted() || m_ExperimentManager.m_Continue)
            {
                m_Timer.StopTimer();
                m_ExperimentManager.m_Continue = false;

                ResetTutorial();
            }
        }
            
    }

    public void Setup(bool value)
    {
        gameObject.SetActive(value);
    }

    public void ResetTutorial()
    {
        stage = Stage.WAIT;
        ChangeText("Instructions");
        ChangeSprite(null);

        if (m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);

        m_ActiveCoroutine = null;

        StartCoroutine(DestroyAllObjects());
    }

    private void ChangeText(string instruction)
    {
        foreach (var text in m_Text)
            text.text = instruction;
    }

    private void ChangeSprite(Sprite sprite)
    {
        foreach (var renderer in m_SpriteRenderers)
            renderer.sprite = sprite;
    }

    private IEnumerator SimpleDirect()
    {
        //#######################
        string text = "Simple Direct Manipulation\n\n" +
                      "To move the robot with this technique, you need both hands\n" +
                      "Pull the trigger button with your less dominant hand. This activates control\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        //yield return new WaitUntil(() => m_SimpleDirect.ActivationHand() != null);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        text = "Move the Robot\n\n" +
               "The manipulator will now follow the movement of your dominant hand\n\n" +
               "For better control, place your dominant hand over the manipulator before you activate control\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Task\n\n" +
               "Place the manipulator on the target";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(0.3f, 0.4f, -0.4f), Quaternion.Euler(new(0.0f, -115.0f, 90.0f)));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        Destroy(ghost);

        //#######################
        text = "Gripper Control\n\n" +
               "To control the gripper, first activate control like you did before\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        //yield return new WaitUntil(() => m_SimpleDirect.ActivationHand() != null);

        text = "Gripper Control\n\n" +
               "Now grab the other trigger to close the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowSqueezeHint(m_SimpleDirect.InteractingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Gripper Control\n\n" +
               "Grab it again to open the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowSqueezeHint(m_RightHand, false);
        m_ControllerHints.ShowSqueezeHint(m_LeftHand, false);

        //#######################
        text = "Task\n\n" +
               "Pick up the barrel";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject barrel = Instantiate(m_Barrel);
        barrel.transform.SetParent(m_Objects.transform);
        barrel.GetComponent<Barrel>().SetPosition(new(-0.4f, 0.058f, -0.4f));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Task\n\n" +
               "Place the barrel on the X";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject obstacle = Instantiate(m_Obstacle);
        obstacle.transform.SetParent(m_Objects.transform);
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

        GameObject target = Instantiate(m_Target);
        target.transform.SetParent(m_Objects.transform);
        target.GetComponent<Target>().SetPosition(new(0.4f, 0.5f, -0.4f));
        target.GetComponent<Target>().CheckDistance(barrel.transform);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        Destroy(target);
        Destroy(barrel);
        Destroy(obstacle);
        
        stage = Stage.PRACTICE;

        m_ActiveCoroutine = null;
    }

    private IEnumerator Direct()
    {
        //#######################
        string text = "Constrained Direct Manipulation\n\n" +
                      "To move the robot with this technique, you need both hands\n" +
                      "Pull the trigger button with your less dominant hand. This activates control\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        //yield return new WaitUntil(() => m_Direct.ActivationHand() != null);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        text = "Move the Robot\n\n" +
               "The manipulator will now follow the movement of your dominant hand\n\n" +
               "For better control, place your dominant hand over the manipulator before you activate control\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Task\n\n" +
               "Place the manipulator on the target";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(0.3f, 0.4f, -0.4f), Quaternion.Euler(new(0.0f, -115.0f, 90.0f)));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        Destroy(ghost);

        //#######################
        text = "Gripper Control\n\n" +
               "To control the gripper, first activate control like you did before\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        //yield return new WaitUntil(() => m_Direct.ActivationHand() != null);

        text = "Gripper Control\n\n" +
               "Now grab the other trigger to close the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowSqueezeHint(m_Direct.InteractingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Gripper Control\n\n" +
               "Grab it again to open the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowSqueezeHint(m_RightHand, false);
        m_ControllerHints.ShowSqueezeHint(m_LeftHand, false);

        //#######################
        text = "Scaling\n\n" +
               "This technique can be scaled for precise movement if you want\n\n" +
               "Begin by activating the control like before";
        ChangeText(text);
        m_AudioSource.Play();

        //yield return new WaitUntil(() => m_Direct.ActivationHand() != null);

        text = "Scaling\n\n" +
               "Now squeeze the grip button on that same controller to activate scaling\n\n" +
               "Release the grip to stop scaling";
        ChangeText(text);
        m_AudioSource.Play();

        //m_ControllerHints.ShowTriggerHint(m_Direct.ActivationHand(), true);
        //m_ControllerHints.ShowGripHint(m_Direct.ActivationHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        //#######################
        text = "Snapping\n\n" +
               "Additionally, you can activate snapping to make sure that the manipulator is always pointing down\n\n" +
               "Begin by activating the control like before";
        ChangeText(text);
        m_AudioSource.Play();

        //yield return new WaitUntil(() => m_Direct.ActivationHand() != null);

        text = "Snapping\n\n" +
               "Now squeeze the grip button on the other controller and make sure the manipulator is pointing downwards\n\n" +
               "Release the grip to stop snapping";
        ChangeText(text);
        m_AudioSource.Play();

        //m_ControllerHints.ShowTriggerHint(m_Direct.ActivationHand(), true);
        m_ControllerHints.ShowGripHint(m_Direct.InteractingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        //#######################
        text = "Collision Objects\n\n" +
               "You can select objects that you want the robot to avoid while controlling it\n\n" +
               "We call these collision objects";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject obstacle = Instantiate(m_Obstacle);
        obstacle.transform.SetParent(m_Objects.transform);
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Collision Objects\n\n" +
               "Start by selecting the collision objects tool\n\n" +
               "With your right hand, touch the trackpad, navigate the curser to collision objects tool, and then press the trackpad down";
        ChangeText(text);
        m_AudioSource.Play();

        ChangeSprite(m_Sprites[0]);

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.COLOBJCREATOR);

        m_ControllerHints.ShowTrackpadHint(false);

        ChangeSprite(null);

        text = "Collision Objects\n\n" +
               "Now squeeze the grip button of either controller and select the obstacle\n\n" +
               "All collision objects turn red once selected";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        text = "Collision Objects\n\n" +
               "Not only can you turn obstacles into collision objects, but the table and glass box can also be turned into collision objects";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Deselect the collision objects tool\n\n" +
               "With your right hand, touch the trackpad, navigate the curser to collision objects tool, and then press the trackpad down";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT);

        m_ControllerHints.ShowTrackpadHint(false);

        text = "Collision Objects\n\n" +
               "Now try to move the robot to crash into the obstacle\n\n" +
               "It should avoid the obstacle";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Attachable Objects\n\n" +
               "Sometimes you want to pick up objects, but not crash into them while moving the robot\n\n" +
               "We call these attachable objects";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject barrel = Instantiate(m_Barrel);
        barrel.transform.SetParent(m_Objects.transform);
        barrel.GetComponent<Barrel>().SetPosition(new(-0.4f, 0.058f, -0.4f));

        text = "Attachable Objects\n\n" +
               "Start by selecting the attachable objects tool\n\n" +
               "With your right hand, touch the trackpad, navigate the curser to attachable objects tool, and then press the trackpad down";
        ChangeText(text);
        m_AudioSource.Play();

        ChangeSprite(m_Sprites[1]);

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.ATTOBJCREATOR);

        m_ControllerHints.ShowTrackpadHint(false);

        ChangeSprite(null);

        text = "Attachable Objects\n\n" +
               "Now squeeze the grip button of either controller and select the barrel\n\n" +
               "All attachable objects turn green once selected";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        text = "Deselect the attachable objects tool\n\n" +
               "With your right hand, touch the trackpad, navigate the curser to attachable objects tool, and then press the trackpad down";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT);

        m_ControllerHints.ShowTrackpadHint(false);

        text = "Attachable Objects\n\n" +
               "Now try to move the robot to crash into the barrel\n\n" +
               "It should avoid it";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Attachable Objects\n\n" +
               "Now try to pick up the barrel";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Focus Objects\n\n" +
               "All attachable objects can be turned into focus objects\n\n" +
               "But there can only be one focus object at a time";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Focus Objects\n\n" +
               "Now squeeze the grip button of either controller and select the barrel\n\n" +
               "Focus objects turn yellow once selected";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        text = "Focus Objects\n\n" +
               "Alright, now pick up the barrel, but this time, activate snapping\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        //yield return new WaitUntil(() => m_Direct.ActivationHand() != null);

        m_ControllerHints.ShowGripHint(m_Direct.InteractingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        //#######################
        text = "Task\n\n" +
               "Place the barrel on the X";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject target = Instantiate(m_Target);
        target.transform.SetParent(m_Objects.transform);
        target.GetComponent<Target>().SetPosition(new Vector3(0.4f, 0.5f, -0.4f));
        target.GetComponent<Target>().CheckDistance(barrel.transform);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_InteractableObjects.RemoveAllInteractableObjects();

        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count() == 0);

        Destroy(target);
        Destroy(barrel);
        Destroy(obstacle);

        stage = Stage.PRACTICE;

        m_ActiveCoroutine = null;
    }

    private IEnumerator SDOF()
    {
        //#######################
        string text = "Separated Degrees of Freedom Manipulation\n\n" +
                      "To move the robot with this technique, you interact with the balls at the ends of the displayed axes";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Separated Degrees of Freedom\n\n" +
               "Reach out with either hand and hover one of the balls (handles)\n\n" +
               "While hovering, pull the trigger button to grab the handle";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_SDOF.Flash(true);

        yield return new WaitUntil(() => m_SDOF.InteractingHand() != null);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_SDOF.Flash(false);

        text = "Translation\n" +
               "While grabbing the Handle, move your hand towards or away from the manipulator to translate it in that direction\n\n" +
               "Rotation\n" +
               "While grabbing the Handle, move your hand around the manipulator to rotate it in that direction";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Task\n\n" +
               "Place the manipulator on the target";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(0.3f, 0.4f, -0.4f), Quaternion.Euler(new(0.0f, -115.0f, 90.0f)));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        Destroy(ghost);

        //#######################
        text = "Gripper Control\n\n" +
               "To control the gripper, first make sure that you aren\'t hovering any of the handles\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text += "Now grab one of the triggers to activate gripping and squeeze the other to open and close the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        //#######################
        text = "Scaling\n" +
               "First start by grabbing a handle\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_SDOF.Flash(true);

        yield return new WaitUntil(() => m_SDOF.InteractingHand() != null);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_SDOF.Flash(false);

        text += "Now squeeze the grip button of the other controller to activate scaling";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_SDOF.InteractingHand(), true);
        //m_ControllerHints.ShowGripHint(m_SDOF.OtherHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        //#######################
        text = "Task\n\n" +
               "Pick up the barrel";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject barrel = Instantiate(m_Barrel);
        barrel.transform.SetParent(m_Objects.transform);
        barrel.GetComponent<Barrel>().SetPosition(new(-0.4f, 0.058f, -0.4f));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Task\n\n" +
               "Place the barrel on the X";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject obstacle = Instantiate(m_Obstacle);
        obstacle.transform.SetParent(m_Objects.transform);
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

        GameObject target = Instantiate(m_Target);
        target.transform.SetParent(m_Objects.transform);
        target.GetComponent<Target>().SetPosition(new(0.4f, 0.5f, -0.4f));
        target.GetComponent<Target>().CheckDistance(barrel.transform);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        Destroy(target);
        Destroy(barrel);
        Destroy(obstacle);

        stage = Stage.PRACTICE;

        m_ActiveCoroutine = null;
    }

    private IEnumerator Practice()
    {
        string text = "Practice\n\n" +
                      "Place the barrel on the X as many times as you can\n\n" +
                      "Take this time to practice what you just learnt";
        ChangeText(text);
        m_AudioSource.Play();

        m_Timer.StartTimer(m_TimeLimit);

        GameObject barrel = Instantiate(m_Barrel);
        barrel.transform.SetParent(m_Objects.transform);

        GameObject target = Instantiate(m_Target);
        target.transform.SetParent(m_Objects.transform);
        target.GetComponent<Target>().CheckDistance(barrel.transform);

        List<GameObject> obstacles = new();
        for (var i = 0; i < 4; i++)
        {
            GameObject obstacle = Instantiate(m_Obstacle);
            obstacle.transform.SetParent(m_Objects.transform);
            obstacles.Add(obstacle);
        }
        obstacles[0].transform.position = new(-0.45f, 0.0f, 0.0f);
        obstacles[1].transform.position = new(0.0f, 0.0f, -0.45f);
        obstacles[2].transform.position = new(0.45f, 0.0f, 0.0f);

        obstacles[3].transform.SetPositionAndRotation(new(0.0f, 0.5f, 0.4f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacles[3].transform.localScale = new(0.3f, 1.0f, 0.025f);

        while (true)
        {
            for(var i = 0; i < 3; i++)
            {
                obstacles[i].transform.localScale = new(Random.Range(0.15f, 0.25f), Random.Range(0.25f, 1.0f), 0.025f);
                obstacles[i].transform.SetPositionAndRotation(new(obstacles[i].transform.position.x, obstacles[i].transform.localScale.y/2.0f, obstacles[i].transform.position.z),
                                                              Quaternion.Euler(new(0.0f, Random.value * 180.0f, 0.0f)));
            }

            int rand = Random.Range(0, 2);
            barrel.GetComponent<Barrel>().SetPosition(new(Random.Range(-0.5f, -0.22f), 0.058f, rand == 0 ? Random.Range(-0.5f, -0.22f) : Random.Range(0.22f, 0.5f)));

            rand = Random.Range(0, 2);
            target.GetComponent<Target>().SetPosition(new(Random.Range(0.22f, 0.5f), 0.5f, rand == 0 ? Random.Range(-0.5f, -0.22f) : Random.Range(0.22f, 0.5f)));

            yield return new WaitUntil(() => !barrel.GetComponent<Barrel>().IsMoving() && target.GetComponent<Target>().IsInBounds(barrel.transform));

            if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
            {
                m_InteractableObjects.RemoveAllInteractableObjects();
                yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator DestroyAllObjects()
    {
        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            m_InteractableObjects.RemoveAllInteractableObjects();
            yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);
        }

        if (m_Objects != null && m_Objects.transform.childCount > 0)
        {
            for (var i = m_Objects.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }
    }
}