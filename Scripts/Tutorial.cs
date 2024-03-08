using UnityEngine;
using ManipulationModes;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;
using System.Linq;

namespace TutorialStages
{
    public enum Stage
    {
        WAIT,
        START,
        SIMPLEDIRECT,
        DIRECT,
        SDOF,
        RAIL,
        RAILCREATOR,
        COLOBJCREATOR,
        ATTOBJCREATOR,
        PRACTICE
    };
}

public class Tutorial : MonoBehaviour
{
    [Header("Stage")]
    public Stage stage = Stage.WAIT;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_GhostManipulator = null;
    [SerializeField] private GameObject m_Cube = null;
    [SerializeField] private GameObject m_X = null;
    [SerializeField] private GameObject m_Obstacle = null;

    [Header("Sprites")]
    [SerializeField] private Sprite[] m_Sprites = new Sprite[7];

    // Scripts
    private Manipulator m_Manipulator = null;
    private SDOFManipulation m_SDOFManipulation = null;
    private GripperControl m_GripperControl = null;
    private ManipulationMode m_ManipulationMode = null;
    private ControllerHints m_ControllerHints = null;
    private InteractableObjects m_InteractableObjects = null;

    // Game Objects
    private GameObject m_Objects = null;
    private GameObject m_Robotiq = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    // Timer
    private Timer m_Timer = null;
    private readonly float m_TimeLimit = 300.0f;

    private Coroutine m_ActiveCoroutine = null;
    private Coroutine m_PracObsCoroutine = null;
    private Text m_Text = null;
    private AudioSource m_AudioSource = null;
    private SpriteRenderer m_SpriteRenderer = null;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_SDOFManipulation = m_Manipulator.transform.Find("SDOFWidget").GetComponent<SDOFManipulation>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_Objects = gameObject.transform.parent.GetComponent<ExperimentManager>().m_Objects;
        m_Robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;

        m_Timer = GameObject.FindGameObjectWithTag("Timer").GetComponent<Timer>();

        m_Text = gameObject.GetComponentInChildren<Text>();
        m_AudioSource = gameObject.GetComponentInChildren<AudioSource>();
        m_SpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        m_SpriteRenderer.sprite = null;
    }

    private void OnDisable()
    {
        DestroyAllObjects();
        stage = Stage.WAIT;
        
        if(m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);

        //if(m_Manipulator != null)
        //    m_Manipulator.Flash(false);

        m_ActiveCoroutine = null;

        m_Timer.StopTimer();
    }

    private void Update()
    {
        if (stage == Stage.START)
        {
            if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
                stage = Stage.SIMPLEDIRECT;
            else if (m_ManipulationMode.mode == Mode.DIRECT)
                stage = Stage.ATTOBJCREATOR;
            else
                m_ManipulationMode.ToggleDirect();
        }

        if (stage == Stage.SIMPLEDIRECT || stage == Stage.DIRECT)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(Direct());
        }

        if (stage == Stage.SDOF)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(SDOF());
        }

        if (stage == Stage.RAIL)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(Rail());
        }

        if (stage == Stage.RAILCREATOR)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(RailCreator());
        }

        if (stage == Stage.COLOBJCREATOR)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(CollisionObject());
        }

        if (stage == Stage.ATTOBJCREATOR)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(AttachableObject());
        }

        if (stage == Stage.PRACTICE)
        {
            if (m_ActiveCoroutine == null)
            {
                m_ActiveCoroutine = StartCoroutine(Practice());
                m_PracObsCoroutine = StartCoroutine(PracticeObstacle());
            }
                
            else if (m_Timer.TimeExhausted())
            {
                StopCoroutine(m_ActiveCoroutine);
                StopCoroutine(m_PracObsCoroutine);
                DestroyAllObjects();
                stage = Stage.WAIT;
            }
        }
    }

    private IEnumerator Direct()
    {
        GameObject target = Instantiate(m_GhostManipulator);
        target.transform.SetParent(m_Objects.transform);
        target.transform.SetPositionAndRotation(new Vector3(0.3f, 0.4f, -0.4f), Quaternion.Euler(new Vector3(0.0f, -115.0f, 90.0f)));

        m_Text.text = "Move the Robot\n\n" +
                      "Reach out your right hand and hover the manipulator\n" +
                      "It will have a yellow highlight when you are hoveringit\n\n" +
                      "Once hovering, grab the manipulator\n\n";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        //m_Manipulator.Flash(true);

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);

        m_Text.text = "Move the Robot\n\n" +
                      "While grabbing the manipulator, move your hand";
        m_AudioSource.Play();

        if (stage == Stage.DIRECT)
        {
            m_Text.text += "\n\nScaled Movement\n\n" +
                           "While moving the manipulator, grab the left trigger";
            m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        }

        m_Text.text += "\n\nPlacement\n\n" +
                           "Place the manipulator on the target";

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target) && CheckRotation(m_Robotiq, target));

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);

        target.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.2f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 120.0f, 90.0f)));

        m_Text.text = "Move the Robot\n\n" +
                      "The manipulator can be grabbed with either hand";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);

        if (stage == Stage.DIRECT)
        {
            m_Text.text += "\n\nScaled Movement\n\n" +
                           "Grab the other trigger while moving the manipulator";
            m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        }

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target) && CheckRotation(m_Robotiq, target));

        //m_Manipulator.Flash(false);
        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        GameObject.Destroy(target);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 120.0f, 0.0f)));

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        m_Text.text = "Gripper Control\n\n" +
                      "Making sure that you are not hovering the manipulator\n\n" +
                      "Grab the left trigger to activate gripper control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_GripperControl.isGripping);

        m_Text.text += "\n\nControl the gripper by slowly squeezing the right trigger\n\n" +
                       "(You can also activate gripper control by grabbing the right trigger and control it with the left trigger)\n\n" +
                       "Now grab the cube with the gripper";
        m_AudioSource.Play();

        m_ControllerHints.ShowSqueezeHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowSqueezeHint(m_LeftHand, false);

        yield return new WaitUntil(() => cube.GetComponent<ExperimentObject>().isMoving == true);

        m_Text.text = "Move the Cube\n\n" +
                      "Move the cube over the target X";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec2Distance(cube, x));

        m_Text.text = "Gripper Control\n\n" +
                      "Release the cube by activating gripper control\n\n" +
                      "(only grab ONE trigger)";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger || m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_ControllerHints.ShowTriggerHint(m_RightHand, false);

        yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

        GameObject.Destroy(cube);
        GameObject.Destroy(x);

        if (stage == Stage.SIMPLEDIRECT)
            stage = Stage.PRACTICE;
        else if (stage == Stage.DIRECT)
            stage = Stage.SDOF;

        m_ActiveCoroutine = null;
    }

    private IEnumerator SDOF()
    {
        m_ControllerHints.ShowTrackpadHint(true);

        m_SpriteRenderer.sprite = m_Sprites[1];
        m_Text.text = "Select SDOF Manipulation\n\n" +
                      "Touch the trackpad and select Separated Defree of Freedom Manipulation";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.SDOF);

        m_ControllerHints.ShowTrackpadHint(false);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Separated Degrees of Freedom\n\n" +
                      "Reach out and grab one of the Handles";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_SDOFManipulation.Flash(true);

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);

        m_Text.text = "Separated Degrees of Freedom Translation\n\n" +
                      "While grabbing the Handle, move your hand towards or away from the manipulator" +
                      "\n\nScaled Movement\n\n" +
                      "While moving the Handle, grab the other trigger";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.trigger && m_ControllerHints.handStatus.right.trigger);

        m_Text.text = "Separated Degrees of Freedom\n\n" +
                      "Release the triggers";
        m_AudioSource.Play();

        yield return new WaitUntil(() => !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        m_Text.text = "Separated Degrees of Freedom Rotation\n\n" +
                      "Grab a handle and move your hand towards a handle of a different colour\n\n" +
                      "When rotating, the axes snap to the world coordinate system" +
                      "\n\nScaled Movement\n\n" +
                      "While moving the Handle, grab the other trigger";
        m_AudioSource.Play();
        
        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.trigger && m_ControllerHints.handStatus.right.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_SDOFManipulation.Flash(false);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 120.0f, 0.0f)));

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        m_Text.text = "Place the cube on the X\n\n" +
                      "Separated Degrees of Freedom Translation\n" +
                      "While grabbing the Handle, move your hand towards or away from the manipulator\n\n" +
                      "Separated Degrees of Freedom Rotation\n" +
                      "While grabbing the Handle, move your hand towards a handle of a different colour\n\n" +
                      "Scaled Movement\n" +
                      "While moving the Handle, grab the other trigger";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

        m_Text.text = "Deselect SDOF Manipulation\n\n" +
                      "Touch the trackpad and deselect SDOF manipulation";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        m_ControllerHints.ShowTrackpadHint(false);

        GameObject.Destroy(cube);
        GameObject.Destroy(x);

        stage = Stage.RAILCREATOR;

        m_ActiveCoroutine = null;
    }

    private IEnumerator RailCreator()
    {
        m_Text.text = "Resetting Robot Position\n\n" +
                      "Please wait while we reset the robot's position";

        GameObject ros = GameObject.FindGameObjectWithTag("ROS");
        ros.GetComponent<ROSPublisher>().PublishResetPose();

        yield return new WaitForSeconds(2.0f);
        yield return new WaitUntil(() => ros.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        m_Manipulator.ResetPositionAndRotation();
        m_ControllerHints.ShowTrackpadHint(true);

        m_SpriteRenderer.sprite = m_Sprites[2];
        m_Text.text = "Select Rail Creator\n\n" +
                      "Touch the trackpad and select the rail creator";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.RAILCREATOR);

        m_ControllerHints.ShowTrackpadHint(false);

        GameObject target = Instantiate(m_GhostManipulator);
        target.transform.SetParent(m_Objects.transform);
        target.transform.position = new Vector3(0.3f, 0.39f, -0.4f);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Rail Creator\n\n" +
                      "Place the manipulator on the target manipulator";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        //m_Manipulator.Flash(true);

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);

        m_Text.text += "\n\nThe Rail\n\n" +
                      "Notice a rail is created from the robot's gripper to the manipulator";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec3Distance(m_Manipulator.gameObject, target) && !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        //m_Manipulator.Flash(false);

        m_Text.text = "The Planning Robot\n\n" +
                      "The planning robot shows the path that the robot will take while moving along that rail";
        m_AudioSource.Play();

        yield return new WaitForSeconds(5.0f);

        target.transform.position = new Vector3(0.3f, 0.9f, -0.4f);

        m_Text.text = "Move the Manipulator\n\n" +
                      "Place the manipulator on the target manipulator again";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        //m_Manipulator.Flash(true);

        yield return new WaitUntil(() => CheckVec3Distance(m_Manipulator.gameObject, target) && !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        //m_Manipulator.Flash(false);

        m_Text.text = "The Planning Robot\n\n" +
                      "The manipulator will turn red and play a sound if the robot is incapable of moving to a location\n\n" +
                      "The planning robot will follow the rail up to the point that the robot can reach";
        m_AudioSource.Play();

        yield return new WaitForSeconds(2.0f);

        m_Text.text += "\n\nGrab the right trigger to continue";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger);

        m_Text.text = "Deleting a Rail\n\n" +
                      "To delete a rail, just grab the trigger while the hand is not hovering the manipulator\n\n" +
                      "Delete all of the rails";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.position = new Vector3(-0.4f, 0.05f, -0.4f);

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        target.transform.position = new Vector3(-0.4f, 0.39f, -0.4f);

        m_Text.text = "Plan a Robot Path\n\n" +
                      "Follow the target manipulators to create a path for placing the cube";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec3Distance(m_Manipulator.gameObject, target) && !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        target.transform.position = new Vector3(-0.4f, 0.2f, -0.4f);

        yield return new WaitUntil(() => CheckVec3Distance(m_Manipulator.gameObject, target) && !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        target.transform.position = new Vector3(0.4f, 0.25f, -0.4f);

        yield return new WaitUntil(() => CheckVec3Distance(m_Manipulator.gameObject, target) && !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        GameObject.Destroy(target);
        GameObject.Destroy(cube);
        GameObject.Destroy(x);

        stage = Stage.RAIL;

        m_ActiveCoroutine = null;
    }

    private IEnumerator Rail()
    {
        m_SpriteRenderer.sprite = m_Sprites[3];
        m_Text.text = "Select Rail Manipulation\n\n" +
                      "Touch the trackpad and select rail manipulation";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.RAIL);

        m_ControllerHints.ShowTrackpadHint(false);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Rail Manipulation\n\n" +
                      "Grab the manipulator and move it along the rail";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        //m_Manipulator.Flash(true);

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);

        m_Text.text += "\n\nScaled Movement\n\n" +
                      "Grab the trigger of the other hand to activate scaled movement";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.trigger && m_ControllerHints.handStatus.right.trigger);

        m_Text.text = "Rail Manipulation\n\n" +
                      "Now release the manipulator";
        m_AudioSource.Play();

        yield return new WaitUntil(() => !m_ControllerHints.handStatus.left.trigger && !m_ControllerHints.handStatus.right.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        //m_Manipulator.Flash(false);

        m_Text.text = "Rail Manipulation\n\n" +
                      "On your left controller, rest your thumb on the trackpad in the right direction";
        m_AudioSource.Play();

        m_ControllerHints.ShowTouchRightHint(true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.touchRight);

        m_ControllerHints.ShowTouchRightHint(false);

        m_Text.text = "Rail Manipulation\n\n" +
                      "Now rest your thumb on the trackpad in the left direction";
        m_AudioSource.Play();

        m_ControllerHints.ShowTouchLeftHint(true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.touchLeft);

        m_ControllerHints.ShowTouchLeftHint(false);

        m_Text.text = "Rail Manipulation\n\n" +
                      "To move forwards along the rail, touch right\n" +
                      "To move backwards along the rail, touch left\n\n" +
                      "Scaled Movement\n\n" +
                      "Grab the right trigger to scale the movement";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger);
        yield return new WaitUntil(() => !m_ControllerHints.handStatus.right.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.position = new Vector3(-0.4f, 0.05f, -0.4f);

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        m_Text.text = "Place the cube on the X\n\n" +
                      "Rail Manipulation\n" +
                      "Grab and move the manipulator OR\n" +
                      "Use the left directional pad to move the robot\n\n" +
                      "Scaled Movement\n" +
                      "Grab the trigger of the other hand to activate scaled movement";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

        m_Text.text = "Deselect Rail Manipulation\n\n" +
                      "Touch the trackpad and deselect Rail manipulation";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        m_ControllerHints.ShowTrackpadHint(false);

        GameObject.Destroy(cube);
        GameObject.Destroy(x);

        stage = Stage.COLOBJCREATOR;

        m_ActiveCoroutine = null;
    }

    private IEnumerator CollisionObject()
    {
        GameObject target = Instantiate(m_GhostManipulator);
        target.transform.SetParent(m_Objects.transform);
        target.transform.position = new Vector3(0.0f, 0.39f, -0.8f);

        m_Text.text = "Collision Objects\n\n" +
                      "Move the manipulator to the target";
        m_AudioSource.Play();

        GameObject ros = GameObject.FindGameObjectWithTag("ROS");
        yield return new WaitUntil(() => ros.GetComponent<ROSPublisher>().locked);

        Destroy(target);

        m_Text.text = "Collision Objects\n\n" +
                      "GAHH! You just smashed the glass of a very expensive glovebox \\(><)/\n\n" +
                      "Please contemplate what you've done while we reset the robot's position and fix the glass";
        m_AudioSource.Play();
        m_AudioSource.Play();
        m_AudioSource.Play();

        yield return new WaitUntil(() => !ros.GetComponent<ROSPublisher>().locked);

        ros.GetComponent<ROSPublisher>().PublishResetPose();

        yield return new WaitForSeconds(2.0f);
        yield return new WaitUntil(() => ros.GetComponent<ResultSubscriber>().m_RobotState == "IDLE");

        m_Manipulator.ResetPositionAndRotation();
        m_ControllerHints.ShowTrackpadHint(true);

        m_SpriteRenderer.sprite = m_Sprites[4];
        m_Text.text = "Collision Objects\n\n" +
                      "Right, let's try to not break anything else.\n" +
                      "Select the Collision Objects tool";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.COLOBJCREATOR);

        m_ControllerHints.ShowTrackpadHint(false);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Collision Objects\n\n" +
                      "Grab the grip button to create a pointing gesture";
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_LeftHand, true);
        m_ControllerHints.ShowGripHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.grip || m_ControllerHints.handStatus.left.grip);

        m_ControllerHints.ShowGripHint(m_LeftHand, false);
        m_ControllerHints.ShowGripHint(m_RightHand, false);

        m_Text.text += "\n\nSelect the glass that you don't want the robot to crash into";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Any());

        m_Text.text = "Collision Objects\n\n" +
                      "You can select as many collision objects as you want to avoid\n\n" +
                      "Try selecting another";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count > 1);
        yield return new WaitForSeconds(0.5f);

        m_Text.text = "Collision Objects\n\n" +
                      "Deselect the Collision Objects tool";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        m_ControllerHints.ShowTrackpadHint(false);

        target = Instantiate(m_GhostManipulator);
        target.transform.SetParent(m_Objects.transform);
        target.transform.position = new Vector3(0.0f, 0.39f, -0.8f);

        m_Text.text = "Collision Objects\n\n" +
                      "Now try to move the manipulator to the target again";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.isInteracting);
        yield return new WaitForSeconds(3.0f);

        m_Text.text = "Collision Objects\n\n" +
                      "So you still can't reach that target huh?\n" +
                      "Well at least you didn't smash the glass again \\(^^)/\n\n" +
                      "I'll set an achievable target for you";
        m_AudioSource.Play();

        target.transform.position = new Vector3(-0.4f, 0.39f, -0.4f);

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target));

        Destroy(target);

        m_Text.text = "Place the cube on the X\n\n" +
                      "AND DON'T HIT THE OBSTACLE";
        m_AudioSource.Play();

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        bool collided = true;
        while (collided)
        {
            GameObject cube = Instantiate(m_Cube);
            cube.transform.SetParent(m_Objects.transform);
            cube.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 120.0f, 0.0f)));

            GameObject obstacle = Instantiate(m_Obstacle);
            obstacle.transform.SetParent(m_Objects.transform);
            obstacle.transform.SetPositionAndRotation(new Vector3(0.0f, 0.45f, -0.35f), Quaternion.Euler(new Vector3(0.0f,90.0f,0.0f)));

            yield return new WaitUntil(() => obstacle == null || (CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false));

            if (obstacle != null)
                collided = false;
            else
            {
                m_Text.text = "Place the cube on the X\n\n" +
                              "Try using the collision objects tool\n\n" +
                              "The robot won't collide but the cube still can, be careful!";
                m_AudioSource.Play();

                yield return new WaitUntil(() => !m_ManipulationMode.isInteracting);
            }

            Destroy(cube);
            Destroy(obstacle);
        }

        Destroy(x);

        m_Text.text = "Well done! You're getting the hang of this!\n\n" +
                      "Deselect manipulation techniques you have selected";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        stage = Stage.ATTOBJCREATOR;

        m_ActiveCoroutine = null;
    }

    private IEnumerator AttachableObject()
    {
        m_SpriteRenderer.sprite = m_Sprites[5];
        m_Text.text = "Attachable Objects\n\n" +
                      "Select the Attachable Object tool";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.ATTOBJCREATOR);

        m_ControllerHints.ShowTrackpadHint(false);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.SetPositionAndRotation(new Vector3(0.0f, 0.05f, -0.5f), Quaternion.Euler(new Vector3(0.0f, 60.0f, 0.0f)));

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Attachable Objects\n\n" +
                      "Now select the cube like you did the glass\n\n" +
                      "When selected it will turn green";
        m_AudioSource.Play();

        m_ControllerHints.ShowGripHint(m_RightHand, true);
        m_ControllerHints.ShowGripHint(m_LeftHand, true);

        int count = m_InteractableObjects.m_InteractableObjects.Count;
        yield return new WaitUntil(() => m_InteractableObjects.m_InteractableObjects.Count > count);

        m_ControllerHints.ShowGripHint(m_RightHand, false);
        m_ControllerHints.ShowGripHint(m_LeftHand, false);

        m_Text.text = "Attachable Objects\n\n" +
                      "Just like collision objects, the robot will avoid crashing into attachable objects\n\n" +
                      "Now deselect the Attachable Object tool";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        m_Text.text = "Focus Objects\n\n" +
                      "Now the fun starts\n" +
                      "Select the attachable object to make it the object of focus in the scene\n\n" +
                      "When selected, it will turn yellow";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_InteractableObjects.m_FocusObject != null);

        GameObject target = Instantiate(m_GhostManipulator);
        target.transform.SetParent(m_Objects.transform);
        target.transform.SetPositionAndRotation(new Vector3(0.0f, 0.39f, -0.5f), Quaternion.Euler(new Vector3(0.0f, -120.0f, 90.0f)));

        m_Text.text = "Focus Objects\n\n" +
                      "With a focus object in the scene, the manipulator will snap to that object\n\n" +
                      "Let's give it a try";
        m_AudioSource.Play();

        

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target) && CheckRotation(m_Robotiq, target));

        m_Text.text = "Focus Objects\n\n" +
                      "The manipulator turns yellow when snapped to the focus object";
        m_AudioSource.Play();

        yield return new WaitForSeconds(1.0f);

        m_Text.text += "\n\nNow select SDOF manipulation";

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.SDOF);

        cube.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 10.0f, 0.0f)));
        target.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.39f, -0.4f), Quaternion.Euler(new Vector3(0.0f, -80.0f, 90.0f)));

        m_Text.text = "Focus Objects\n\n" +
                      "With a focus object, the axes snap to the focus object coordinate system\n\n" +
                      "Try placing the manipulator over the focus object";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target) && CheckRotation(m_Robotiq, target));

        m_Text.text = "Focus Objects\n\n" +
                      "Good good, now deselect SDOF and select the Rail creator";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.RAILCREATOR);

        cube.transform.SetPositionAndRotation(new Vector3(0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 45.0f, 0.0f)));
        target.transform.SetPositionAndRotation(new Vector3(0.4f, 0.39f, -0.4f), Quaternion.Euler(new Vector3(0.0f, -45.0f, 90.0f)));

        m_Text.text = "Focus Objects\n\n" +
                      "With a focus object, the manipulator will snap to the focus object's axes and to the invisible line that connects the focus object to the manipulator\n\n" +
                      "Rotation\n" +
                      "If snapped, the manipulator will rotate to face the focus object";
        m_AudioSource.Play();

        yield return new WaitForSeconds(10.0f);

        m_Text.text = "Focus Objects\n\n" +
                      "Create a path to place the manipulator over the cube\n\n" +
                      "Then select Rail manipulation from the menu";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.RAIL);

        m_Text.text = "Focus Objects\n\n" +
                      "Move the robot over the cube";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec3Distance(m_Robotiq, target) && CheckRotation(m_Robotiq, target));

        Destroy(target);

        m_Text.text = "Focus Objects\n\n" +
                      "Now deselect Rail manipulation from the menu";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        m_Text.text = "Attaching an Object\n\n" +
                      "Finally, let's grab the focus object to attach it";
        m_AudioSource.Play();

        cube.transform.SetPositionAndRotation(new Vector3(0.0f, 0.05f, -0.5f), Quaternion.Euler(new Vector3(0.0f, 30.0f, 0.0f)));

        yield return new WaitUntil(() => cube.GetComponent<CollisionHandling>().m_isAttached);

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.position = new Vector3(0.4f, 0.0001f, -0.4f);

        m_Text.text = "Attaching an Object\n\n" +
                      "When an attachable object becomes attached, it turns green and makes a sound\n\n" +
                      "Focus Object\n" +
                      "Grabbing a focus object automatically removes its focus object status\n\n" +
                      "Place the cube on the X";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

        Destroy(cube);
        Destroy(x);

        m_Text.text = "And that's it! Now get ready to practice\n\n" +
                      "Deselect any manipulation tools that you selected";
        m_AudioSource.Play();

        yield return new WaitUntil(() => m_ManipulationMode.mode == Mode.DIRECT);

        stage = Stage.PRACTICE;

        m_ActiveCoroutine = null;
    }

    private IEnumerator Practice()
    {
        m_Text.text = "Practice\n\n" +
                      "Place the cube on the X as many times as you can\n\n" +
                      "Take this time to practice what you just learnt";
        m_AudioSource.Play();

        m_Timer.StartTimer(m_TimeLimit);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        bool active = false;

        while (true)
        {
            if(!active)
            {
                active = true;

                Vector3 position = new Vector3(Random.Range(-0.5f, 0.1f), 0.05f, Random.Range(-0.5f, 0.5f));
                Quaternion rotation = Quaternion.Euler(new Vector3(0.0f, Random.value * 360.0f, 0.0f));
                cube.transform.SetPositionAndRotation(position, rotation);

                position = new Vector3(Random.Range(0.1f, 0.5f), 0.0001f, Random.Range(-0.5f, 0.5f));
                x.transform.position = position;

                yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

                active = false;
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator PracticeObstacle()
    {
        while (true)
        {
            GameObject obstacle = Instantiate(m_Obstacle);
            obstacle.transform.SetParent(m_Objects.transform);

            Vector3 position = new Vector3(Random.Range(-0.5f, 0.1f), 0.45f, Random.Range(-0.5f, 0.5f));
            Quaternion rotation = Quaternion.Euler(new Vector3(0.0f, Random.value * 360.0f, 0.0f));

            obstacle.transform.SetPositionAndRotation(position, rotation);

            yield return new WaitUntil(() => obstacle == null);

            Destroy(obstacle);
        }
    }

    private bool CheckVec3Distance(GameObject first, GameObject second)
    {
        if (Vector3.Distance(first.transform.position, second.transform.position) < ExperimentManager.ERRORTHRESHOLD)
            return true;
        else
            return false;
    }

    private bool CheckVec2Distance(GameObject first, GameObject second)
    {
        if (Vector2.Distance(new Vector2(first.transform.position.x, first.transform.position.z), new Vector2(second.transform.position.x, second.transform.position.z)) < ExperimentManager.ERRORTHRESHOLD)
            return true;
        else
            return false;
    }

    private bool CheckRotation(GameObject first, GameObject second)
    {
        if (Quaternion.Angle(first.transform.rotation, second.transform.rotation) < ManipulationMode.ANGLETHRESHOLD)
            return true;
        else
            return false;
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
    }

    public void ResetTutorial()
    {
        stage = Stage.WAIT;
        DestroyAllObjects();
    }
}