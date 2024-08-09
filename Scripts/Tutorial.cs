using UnityEngine;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using FeedBackModes;

namespace TutorialStages
{
    public enum Stage
    {
        WAIT,
        START
    };
}

public class Tutorial : MonoBehaviour
{
    [Header("Stage")]
    public Stage stage = Stage.WAIT;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_GhostManipulatorPrefab = null;
    [SerializeField] private GameObject m_ButtonPrefab = null;
    [SerializeField] private GameObject m_ObstaclePrefab = null;

    // Scripts
    private ExperimentManager m_ExperimentManager = null;

    private ManipulationMode m_ManipulationMode = null;

    private GameObject m_Robotiq = null;

    private Manipulator m_Manipulator = null;

    private ControllerHints m_ControllerHints = null;
    private InteractableObjects m_InteractableObjects = null;

    // Game Objects
    private GameObject m_Objects = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    private Coroutine m_ActiveCoroutine = null;
    private readonly List<Text> m_Text = new();
    private readonly List<SpriteRenderer> m_SpriteRenderers = new();
    private AudioSource m_AudioSource = null;

    private void Awake()
    {
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();

        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_Objects = gameObject.transform.parent.GetComponent<ExperimentManager>().m_Objects;
        
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

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
            m_ActiveCoroutine ??= StartCoroutine(RobotFeedbackCoroutine());
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
                      "The \"Trigger\" buttons are used to move the robot\n\n";
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

        text = "Please turn and face the robot";
        ChangeText(text);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;
    }

    //private IEnumerator RobotFeedbackCoroutine()
    //{
    //    m_ExperimentManager.m_TeachRobotFeedback = false;

    //    //#######################
    //    string text = "Robot Control\n\n" +
    //                  "When you move the manipulator, you are telling the robot where you want its gripper to be\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    text += "Control the robot to the indicated position";
    //    ChangeText(text);

    //    GameObject ghost = Instantiate(m_GhostManipulator);
    //    ghost.transform.SetParent(m_Objects.transform);
    //    ghost.transform.SetPositionAndRotation(new(-0.11f, 0.39f, -0.49f), Quaternion.Euler(0.0f, -90.0f, 90.0f));

    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    Destroy(ghost);

    //    m_ExperimentManager.m_AllowUserControl = false;

    //    text = "Robot Movement\n\n" +
    //           "Notice that the robot moves much slower than the manipulator\n\n" +
    //           "Keep that in mind when controlling the robot, and give the robot time to catch up to the manipulator before moving it again";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The robot has 6 joints that move independently\n\n" +
    //           "I will highlight each joint and display what movement they allow";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Base\" of the robot rotates the whole arm to the left or right";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    int modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 0, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 0));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Shoulder\" of the robot rotates it up or down";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 1, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 1));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Elbow\" of the robot rotates it up or down";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 2, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 2));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Wrist 1\" of the robot rotates the gripper up or down";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 3, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 3));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Wrist 2\" of the robot rotates the gripper right or left";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 4, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 4));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "The \"Wrist 3\" of the robot rotates the fingers of gripper to the right or left";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    modifier = 1;
    //    while (!m_ExperimentManager.m_Continue)
    //    {
    //        yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(true, 5, modifier));
    //        modifier *= -1;
    //    }
    //    m_ExperimentManager.m_Continue = false;
    //    yield return StartCoroutine(m_RobotAssitant.DisplayJointMovement(false, 5));

    //    //#######################
    //    text = "Robot Movement\n\n" +
    //           "Each joint has a physical limit to how much it can rotate.\n\n" +
    //           "The robot arm also has a physical limit to its reach\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    m_ExperimentManager.m_AllowUserControl = true;

    //    text += "Place the manipulator on the indicated position";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    ghost = Instantiate(m_GhostManipulator);
    //    ghost.transform.SetParent(m_Objects.transform);
    //    ghost.transform.SetPositionAndRotation(new(-0.1f, 0.2f, 0.0f), Quaternion.Euler(0.0f, -90.0f, 90.0f));

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);

    //    m_ExperimentManager.m_AllowUserControl = false;

    //    text = "Movement Feedback\n\n" +
    //           "If the manipulator turns red, that means that the robot is incapable of reaching that position either due to its limits or because it will collide with itself\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    m_ExperimentManager.m_AllowUserControl = true;

    //    text += "If this happens, move the manipulator back to the robot gripper and move it again slowly in the direction that you want it to go\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    text += "If the robot is close to one of its limits, a robot assistant will appear to guide you in the direction that you need to move the maniplator\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    ghost.transform.position = new Vector3(-0.4f, 0.2f, -0.4f);

    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    //#######################
    //    text = "Movement Feedback\n\n" +
    //           "Control the robot to hit the obstacle infront of you\n\n";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    GameObject obstacle = Instantiate(m_ObstaclePrefab);
    //    obstacle.transform.SetParent(m_Objects.transform);
    //    obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
    //    obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

    //    ghost.transform.position = new Vector3(0.0f, 0.2f, -0.4f);

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    text = "Movement Feedback\n\n" +
    //           "If the robot hits an object, the part of the robot that is colliding will turn red and you will hear a thump sound\n\n" +
    //           "An emergency stop command will be issued, and the robot will not move for 2 seconds";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
    //    m_ExperimentManager.m_Continue = false;

    //    Destroy(obstacle);

    //    //#######################
    //    text = "Explore robot movement\n\n" +
    //           "Control the robot to the indicated positions to explore what it can do\n\n" +
    //           "Take a second to look at the robot's orientation at each position";
    //    ChangeText(text);
    //    m_AudioSource.Play();

    //    ghost.transform.SetPositionAndRotation(new(-0.11f, 0.39f, -0.49f), Quaternion.Euler(0.0f, -90.0f, 90.0f));
    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    ghost.transform.SetPositionAndRotation(new(0.19f, 0.7f, 0.0f), Quaternion.Euler(0.0f, -180.0f, 90.0f));
    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    ghost.transform.SetPositionAndRotation(new(0.48f, 0.13f, 0.4f), Quaternion.Euler(0.0f, 90.0f, 10.0f));
    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    ghost.transform.SetPositionAndRotation(new(0.5f, 0.2f, -0.5f), Quaternion.Euler(0.0f, 0.0f, 90.0f));
    //    yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

    //    Destroy(ghost);
    //}

    private IEnumerator MoveRobotCoroutine()
    {
        //#######################
        m_ExperimentManager.m_AllowUserControl = false;
        yield return StartCoroutine(ControllerCoroutine());
        m_ExperimentManager.m_AllowUserControl = true;

        //#######################
        string text = "Movement\n\n";

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

        GameObject ghost = Instantiate(m_GhostManipulatorPrefab);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(0.3f, 0.4f, -0.4f), Quaternion.Euler(new(0.0f, -130.0f, 90.0f)));

        yield return new WaitUntil(() => CheckObjectPose(m_Robotiq, ghost));

        Destroy(ghost);
    }

    private IEnumerator RobotFeedbackCoroutine()
    {
        //#######################
        if (m_ExperimentManager.m_TeachRobotControl)
            yield return StartCoroutine(MoveRobotCoroutine());

        string text;
        //#######################
        if (m_ExperimentManager.m_FeedbackMode == Mode.CONSTANT)
        {
             text = "Robot Feedback\n\n" +
                    "As you move the manipulator, a ghost robot immediately follows to show you the final orientation of the robot\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;
        }

        //#######################
        text = "Robot Limits\n\n" +
               "The robot arm has physical limits to its reach and rotation\n\n" +
               "If one of these limits is reached, an \"Unattainable Goal\" message is displayed\n";
        if (m_ExperimentManager.m_FeedbackMode == Mode.NONE)
            text += "and the manipulator will turn red.";
        else
            text += "and the robot feedback will turn red.";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject ghost = Instantiate(m_GhostManipulatorPrefab);
        ghost.transform.SetParent(m_Objects.transform);
        ghost.transform.SetPositionAndRotation(new(-0.12f, 0.2f, -0.2f), Quaternion.Euler(0.0f, -90.0f, 90.0f));

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        if (m_ExperimentManager.m_FeedbackMode != Mode.NONE)
        {
            text = "Movement Feedback\n\n" +
                   "As you approach a robot limit, that part of the robot will turn red and you will be instructed on how to move away from that limit\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;
        }
        else
        {
            text = "If this happens, move the manipulator away from its limit\n";
            ChangeText(text);
            m_AudioSource.Play();

            yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
            m_ExperimentManager.m_Continue = false;
        }

        ghost.transform.position = new Vector3(-0.2f, 0.15f, -0.422f);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        //#######################
        text = "Control the robot to collide with the obstacle\n";
        ChangeText(text);
        m_AudioSource.Play();

        GameObject obstacle = Instantiate(m_ObstaclePrefab);
        obstacle.transform.SetParent(m_Objects.transform);
        obstacle.transform.SetPositionAndRotation(new(0.0f, 0.15f, -0.422f), Quaternion.Euler(new(0.0f, 90.0f, 0.0f)));
        obstacle.transform.localScale = new(0.3f, 0.3f, 0.025f);

        yield return new WaitForFixedUpdate();
        m_InteractableObjects.AddInteractableObject(obstacle.GetComponent<BoxCollider>());

        ghost.transform.position = new Vector3(0.0f, 0.2f, -0.4f);

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        text = "Movement Feedback\n\n" +
               "The robot is programmed to avoid collisions,\n" +
               "so if you give it a command that will cause a collision,\n" +
               "an \"Unattainable Goal\" message is displayed \n";
        if (m_ExperimentManager.m_FeedbackMode == Mode.NONE)
            text += "and the manipulator will turn red.";
        else
            text += "and the robot feedback will indicate where the collision occurs.";
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        m_InteractableObjects.RemoveInteractableObject(obstacle.GetComponent<BoxCollider>());

        Destroy(ghost);
        Destroy(obstacle);

        if (m_ExperimentManager.m_TeachRobotControl)
            yield return StartCoroutine(Practice());

        stage = Stage.WAIT;

        m_ActiveCoroutine = null;
    }

    private IEnumerator DestroyAllObjectsCoroutine()
    {
        //m_InteractableObjects.RemoveAllInteractableObjects();
        //yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count == 0);

        if (m_Objects != null && m_Objects.transform.childCount > 0)
        {
            for (var i = m_Objects.transform.childCount - 1; i >= 0; i--)
            {
                GameObject obj = m_Objects.transform.GetChild(i).gameObject;
                Destroy(obj);
            }
        }

        yield break;
    }

    private IEnumerator Practice()
    {
        GameObject button = Instantiate(m_ButtonPrefab);
        button.transform.position = new Vector3(0.0f, 0.025f, -0.45f);

        string text = "Push the button\n\n" +
               "Use the manipulator to control the robot to push the button\n\n" +
               "The button will turn purple and make a sound when pushed successfully\n\n"; ;
        ChangeText(text);
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ExperimentManager.m_Continue);
        m_ExperimentManager.m_Continue = false;

        Destroy(button);
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