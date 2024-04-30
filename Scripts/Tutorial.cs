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
    private RobotAssistant m_RobotAssitant = null;

    private ManipulationMode m_ManipulationMode = null;
    private SDOFManipulation m_SDOF = null;
    private GripperControl m_GripperControl = null;

    private GameObject m_Robotiq = null;

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
        m_RobotAssitant = GameObject.FindGameObjectWithTag("RobotAssistant").GetComponent<RobotAssistant>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_SDOF = m_Manipulator.transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();
        m_GripperControl = m_Manipulator.GetComponent<GripperControl>();

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

        StartCoroutine(DestroyAllObjectsCoroutine());
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
            m_ActiveCoroutine ??= StartCoroutine(SimpleDirectCoroutine());

        if (stage == Stage.DIRECT)
            m_ActiveCoroutine ??= StartCoroutine(ConstrainedDirectCoroutine());

        if (stage == Stage.SDOF)
            m_ActiveCoroutine ??= StartCoroutine(SDOFCoroutine());

        if (stage == Stage.PRACTICE)
        {
            m_ActiveCoroutine ??= StartCoroutine(PracticeCoroutine());

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

        StartCoroutine(DestroyAllObjectsCoroutine());
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

    private IEnumerator ControllerCoroutine()
    {
        string text = "Learn the Controller\n\n" +
                      "The \"Trigger\" buttons are used to move the robot, and open and close the gripper\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        text += "Look at the controllers in your hands and pull the trigger buttons";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger || m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        text = "Trigger Buttons\n\n" +
               "Notice that when you pull the trigger button the hands make a grabbing gesture\n\n" +
               "Sometimes I might say to \"grab\" an object, what I mean is to place your hand over the object and \"pull the trigger button\"";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        if (m_ManipulationMode.mode != Mode.SIMPLEDIRECT)
        {
            text = "Learn the Controller\n\n" +
                   "The \"Grip\" buttons are used to give more control when moving the the robot";

            if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
                text += ", and to select objects";

            text += "\n\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;

            text += "Look at the controllers in your hands and squeeze the grip buttons\n\n" +
                   "The grip buttons should be pressed with your middle fingers";
            ChangeText(text);
            m_AudioSource.Play();

            m_ControllerHints.ShowGripHint(m_RightHand, true);
            m_ControllerHints.ShowGripHint(m_LeftHand, true);

            yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

            m_ControllerHints.ShowGripHint(m_RightHand, false);
            m_ControllerHints.ShowGripHint(m_LeftHand, false);

            text = "Grip Buttons\n\n" +
                   "Notice that when you squeeze the grip button the hands make a pointing gesture\n\n";

            if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
                text += "Sometimes I might say to \"select\" an object. What I mean is to \"pull the grip button\" and touch the object with your index finger";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;
        }

        if (m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
        {
            text = "Learn the Controller\n\n" +
                   "The \"Trackpad\" is used to select and deselect tools from a menu\n\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;

            text += "Look at the controller in your right hand and touch the trackpad";
            ChangeText(text);
            m_AudioSource.Play();

            m_ControllerHints.ShowTrackpadHint(true);

            yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trackpad);

            m_ControllerHints.ShowTrackpadHint(false);

            text = "Trackpad\n\n" +
                   "Notice that when you touch the trackpad, a radial menu appears with two options\n\n" +
                   "To select an option, slide the cursor over the option and press the trackpad button\n\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ManipulationMode.mode != Mode.CONSTRAINEDDIRECT);

            text += "To deselect an option, slide the cursor over the option and press the trackpad button again";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT);

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;
        }

        text = "Please turn and face the robot to learn the technique";
        ChangeText(text);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;
    }

    private IEnumerator RobotFeedbackCoroutine()
    {
        m_ExperimentManager.m_TeachRobotFeedback = false;

        //#######################
        string text = "Robot Control\n\n" +
                      "When you move the manipulator, you are telling the robot where you want its gripper to be\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        text += "Control the robot to the indicated position";
        ChangeText(text);

        GameObject ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(-0.11f, 0.39f, -0.49f), Quaternion.Euler(0.0f, -90.0f, 90.0f));

        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        Destroy(ghost);

        m_ExperimentManager.m_AllowUserControl = false;

        text = "Robot Movement\n\n" +
               "Notice that the robot moves much slower than the manipulator\n\n" +
               "Keep that in mind when controlling the robot, and give the robot time to catch up to the manipulator before moving it again";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Robot Movement\n\n" +
               "The robot has 6 joints that move independently\n\n" +
               "I will highlight each joint and display what movement they allow";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Base\" of the robot rotates the whole arm to the left or right";
        ChangeText(text);
        m_AudioSource.Play();

        int modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 0, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 0));

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Shoulder\" of the robot rotates it up or down";
        ChangeText(text);
        m_AudioSource.Play();

        modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 1, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 1));

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Elbow\" of the robot rotates it up or down";
        ChangeText(text);
        m_AudioSource.Play();

        modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 2, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 2));

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Wrist 1\" of the robot rotates the gripper up or down";
        ChangeText(text);
        m_AudioSource.Play();

        modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 3, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 3));

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Wrist 2\" of the robot rotates the gripper right or left";
        ChangeText(text);
        m_AudioSource.Play();

        modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 4, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 4));

        //#######################
        text = "Robot Movement\n\n" +
               "The \"Wrist 3\" of the robot rotates the fingers of gripper to the right or left";
        ChangeText(text);
        m_AudioSource.Play();

        modifier = 1;
        while (!m_ExperimentManager.m_Continue)
        {
            yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 5, modifier));
            modifier *= -1;
        }
        m_ExperimentManager.m_Continue = false;
        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 5));

        //#######################
        text = "Robot Movement\n\n" +
               "Each joint has a physical limit to how much it can rotate.\n\n" +
               "The robot arm also has a physical limit to its reach\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        m_ExperimentManager.m_AllowUserControl = true;

        text += "Place the manipulator on the indicated position";
        ChangeText(text);
        m_AudioSource.Play();

        ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(-0.1f, 0.2f, 0.0f), Quaternion.Euler(0.0f, -90.0f, 90.0f));

        yield return new WaitUntil(() => CheckObjectPose(m_Manipulator.gameObject, ghost));

        m_ExperimentManager.m_AllowUserControl = false;

        text = "Movement Feedback\n\n" +
               "If the manipulator turns red, that means that the robot is incapable of reaching that position either due to its limits or because it will collide with itself\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        m_ExperimentManager.m_AllowUserControl = true;

        text += "If this happens, move the manipulator back to the robot gripper and move it again slowly in the direction that you want it to go\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        text += "If the robot is close to one of its limits, a robot assistant will appear to guide you in the direction that you need to move the maniplator\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        ghost.transform.position = new Vector3(-0.4f, 0.2f, -0.4f);

        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        //#######################
        text = "Movement Feedback\n\n" +
               "Control the robot to hit the obstacle infront of you\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject obstacle = Instantiate(m_Obstacle);
        obstacle.transform.SetParent(m_Objects.transform);
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

        ghost.transform.position = new Vector3(0.0f, 0.2f, -0.4f);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        text = "Movement Feedback\n\n" +
               "If the robot hits an object, the part of the robot that is colliding will turn red and you will hear a thump sound\n\n" +
               "An emergency stop command will be issued, and the robot will not move for 2 seconds";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        Destroy(obstacle);

        //#######################
        text = "Explore robot movement\n\n" +
               "Control the robot to the indicated positions to explore what it can do\n\n" +
               "Take a second to look at the robot's orientation at each position";
        ChangeText(text);
        m_AudioSource.Play();

        ghost.transform.SetPositionAndRotation(new(-0.11f, 0.39f, -0.49f), Quaternion.Euler(0.0f, -90.0f, 90.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        ghost.transform.SetPositionAndRotation(new(0.19f, 0.7f, 0.0f), Quaternion.Euler(0.0f, -180.0f, 90.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        ghost.transform.SetPositionAndRotation(new(0.48f, 0.13f, 0.4f), Quaternion.Euler(0.0f, 90.0f, 10.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        ghost.transform.SetPositionAndRotation(new(0.4f, 0.4f, -0.04f), Quaternion.Euler(0.0f, -90.0f, 90.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        ghost.transform.SetPositionAndRotation(new(0.5f, 0.2f, -0.5f), Quaternion.Euler(0.0f, 0.0f, 90.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        ghost.transform.SetPositionAndRotation(new(-0.11f, 0.39f, -0.49f), Quaternion.Euler(0.0f, -90.0f, 90.0f));
        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        Destroy(ghost);
    }

    private IEnumerator SimpleDirectCoroutine()
    {
        //#######################
        m_ExperimentManager.m_AllowUserControl = false;
        yield return StartCoroutine(ControllerCoroutine());
        m_ExperimentManager.m_AllowUserControl = true;

        //#######################
        string text;
        if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
            text = "Simple Direct Manipulation\n\n";
        else
            text = "Constrained Direct Manipulation\n\n";

        text += "Reach out with either hand and hover the manipulator\n\n" +
                "When hovering, a highlight appears around the manipulator \n\n" +
                "While hovering, pull the trigger button to grab the manipulator";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_Manipulator.Flash(true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_Manipulator.Flash(false);

        text = "Move the Robot\n\n" +
               "The manipulator will now follow the movement of your hand and the robot follows the movement of the manipulator\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Task\n\n" +
               "Control the robot to the indicated position";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject ghost = Instantiate(m_GhostManipulator);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(0.3f, 0.4f, -0.4f), Quaternion.Euler(new(0.0f, -130.0f, 90.0f)));

        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        Destroy(ghost);

        //#######################
        if (m_ExperimentManager.m_TeachRobotFeedback)
            yield return StartCoroutine(RobotFeedbackCoroutine());

        //#######################
        text = "Gripper Control\n\n" +
               "To operate the gripper, simply pull the trigger button on the other controller\n\n" +
               "Each pull of the trigger alternates between closing and opening the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_GripperControl.GrippingHand() != null);

        m_ControllerHints.ShowTriggerHint(m_GripperControl.GrippingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        if(m_ManipulationMode.mode == Mode.CONSTRAINEDDIRECT)
            yield break;

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

    private IEnumerator ConstrainedDirectCoroutine()
    {
        //#######################
        yield return StartCoroutine(SimpleDirectCoroutine());

        //#######################
        string text = "Snapping\n\n" +
                      "When moving the manipulator, it is very difficult to keep the gripper pointing downwards\n\n" +
                      "That's where snapping comes in";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Snapping\n\n" +
               "To activate snapping, you just have to squeeze ONE of the grip buttons\n\n" +
               "Then, as you move the manipulator, it will always be snapped to point downwards\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        if (m_ControllerHints.handStatus.right.grip && m_ControllerHints.handStatus.left.grip)
        {
            text += "Only squeeze 1 trigger!";
            ChangeText(text);
            m_AudioSource.Play();
        }    

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Scaling\n\n" +
               "When the robot is close to where you want it to be, but not quite, try scaling for precise control";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Scaling\n\n" +
               "To activate scaling, you just have to squeeze BOTH of the grip buttons\n\n" +
               "Then, as you move the manipulator, the manipulator will move a fraction of the distance\n\n" +
               "Also, rotation is disabled when scaling";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip && m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        //#######################
        text = "Collision Objects\n\n" +
               "While controlling the robot, you want to make sure not to collide with objects\n\n" +
               "Why not select the objects that you want the robot to avoid?";
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
               "All collision objects turn red once selected\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        text += "Not only can you turn obstacles into collision objects, but the table and glove box can also be turned into collision objects";
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
               "What if you want the robot to avoid crashing into an object but you want to be able to pick it up?\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject barrel = Instantiate(m_Barrel);
        barrel.transform.SetParent(m_Objects.transform);
        barrel.GetComponent<Barrel>().SetPosition(new(-0.4f, 0.058f, -0.4f));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

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

        //#######################
        text = "Focus Objects\n\n" +
               "Now that you have an attachable object in the scene, you can tell the robot to \"focus\" on that object";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Focus Objects\n\n" +
               "Just squeeze the grip button of either controller and select the barrel\n\n" +
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
               "The object turned yellow, but nothing happened\n\n" +
               "Nothing will happen until you try to pick up the barrel and activate snapping";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

        text = "Focus Objects\n\n" +
               "Now, instead of snapping downwards, the robot snaps to the focus object";

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

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

    private IEnumerator SDOFCoroutine()
    {
        //#######################
        yield return StartCoroutine(ControllerCoroutine());

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

        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));
        m_ExperimentManager.m_Continue = false;

        Destroy(ghost);

        //#######################
        if (m_ExperimentManager.m_TeachRobotFeedback)
            yield return StartCoroutine(RobotFeedbackCoroutine());

        //#######################
        text = "Gripper Control\n\n" +
               "To operate the gripper, simply pull the trigger button on the other controller\n\n" +
               "Each pull of the trigger alternates between closing and opening the gripper";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_GripperControl.GrippingHand() != null);

        m_ControllerHints.ShowTriggerHint(m_GripperControl.GrippingHand(), true);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);

        //#######################
        text = "Snapping\n\n" +
               "When rotating the manipulator, it is very difficult to get the manipulator in line with the world\n\n" +
               "That's where snapping comes in";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Snapping\n\n" +
               "To activate snapping, you just have to squeeze ONE of the grip buttons\n\n" +
               "Then, as you rotate the manipulator, it will snap to the world axes\n\n";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        if (m_ControllerHints.handStatus.right.grip && m_ControllerHints.handStatus.left.grip)
        {
            text += "Only squeeze 1 trigger!";
            ChangeText(text);
            m_AudioSource.Play();
        }

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Scaling\n\n" +
               "When the robot is close to where you want it to be, but not quite, try scaling for precise control";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

        text = "Scaling\n\n" +
               "To activate scaling, you just have to squeeze\n" +
               "ONE of the grip buttons when translating\n" +
               "BOTH of the grip buttons when rotating\n\n" +
               "Then, as you move the manipulator, the manipulator will move a fraction of the distance";
        ChangeText(text);
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.IsInteracting());

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip && m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue == true);
        m_ExperimentManager.m_Continue = false;

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

    private IEnumerator PracticeCoroutine()
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

    private IEnumerator DestroyAllObjectsCoroutine()
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

    private bool CheckObjectPose(GameObject Object, GameObject refObj)
    {
        if (Vector3.Distance(Object.transform.position, refObj.transform.position) < ManipulationMode.DISTANCETHRESHOLD &&
            Quaternion.Angle(Object.transform.rotation, refObj.transform.rotation) < ManipulationMode.ANGLETHRESHOLD)
            return true;
        else
            return false;
    }
}