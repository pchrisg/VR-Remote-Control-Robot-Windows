using UnityEngine;
using ManipulationModes;
using Valve.VR.InteractionSystem;
using TutorialStages;
using System.Collections;
using UnityEngine.UI;

namespace TutorialStages
{
    public enum Stage
    {
        WAIT,
        READY,
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
    public Stage stage = Stage.READY;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_GhostManipulator = null;
    [SerializeField] private GameObject m_Cube = null;
    [SerializeField] private GameObject m_X = null;

    [Header("Sprites")]
    [SerializeField] private Sprite[] m_Sprites = new Sprite[7];

    // 
    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;
    private ManipulationMode m_ManipulationMode = null;
    private ControllerHints m_ControllerHints = null;
    private GameObject m_Objects = null;
    private GameObject m_Robotiq = null;

    // Hands
    private Hand m_LeftHand = null;
    private Hand m_RightHand = null;

    // Timer
    private Timer m_Timer = null;
    private readonly float m_TimeLimit = 300.0f;

    private Coroutine m_ActiveCoroutine = null;
    private Text m_Text = null;
    private AudioSource m_AudioSource = null;
    private SpriteRenderer m_SpriteRenderer = null;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_ControllerHints = gameObject.GetComponent<ControllerHints>();
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

    private void Start()
    {
        m_Timer.m_TimeLimit = m_TimeLimit;
        m_Timer.m_TimePassed = m_TimeLimit;
    }

    private void OnDisable()
    {
        DestroyAllObjects();
        stage = Stage.READY;
        
        if(m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);

        if(m_Manipulator != null)
            m_Manipulator.Flash(false);

        m_ActiveCoroutine = null;

        m_Timer.m_TimePassed = 600.0f;
    }

    private void Update()
    {
        if (stage == Stage.START)
        {
            if (m_ManipulationMode.mode == Mode.SIMPLEDIRECT)
                stage = Stage.SIMPLEDIRECT;
            else if (m_ManipulationMode.mode == Mode.DIRECT)
                stage = Stage.DIRECT;
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

        if (stage == Stage.PRACTICE)
        {
            if (m_ActiveCoroutine == null)
                m_ActiveCoroutine = StartCoroutine(Practice());
            else if (m_Timer.m_TimePassed >= m_TimeLimit)
            {
                StopCoroutine(m_ActiveCoroutine);
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
                      "Reach out and grab the manipulator with your right hand";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);
        m_Manipulator.Flash(true);

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

        m_Manipulator.Flash(false);
        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        GameObject.Destroy(target);

        GameObject cube = Instantiate(m_Cube);
        cube.transform.SetParent(m_Objects.transform);
        cube.transform.SetPositionAndRotation(new Vector3(-0.4f, 0.05f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 120.0f, 0.0f)));

        GameObject x = Instantiate(m_X);
        x.transform.SetParent(m_Objects.transform);
        x.transform.SetPositionAndRotation(new Vector3(0.4f, 0.0001f, -0.4f), Quaternion.Euler(new Vector3(0.0f, 0.0f, 0.0f)));

        m_SpriteRenderer.sprite = m_Sprites[0];
        m_Text.text = "Select Gripper Control\n\n" +
                      "Touch the trackpad and select the gripper control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil( () => m_GripperControl.isGripping);

        m_ControllerHints.ShowTrackpadHint(false);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Gripper Control\n\n" +
                      "Grab the right trigger to activate gripper control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger);

        m_Text.text += "\n\nControl the gripper by slowly squeezing the left trigger\n\n" +
                       "(You can also activate gripper control by grabbing the left trigger and control it with the right trigger)\n\n" +
                       "Now grab the cube with the gripper";
        m_AudioSource.Play();

        m_ControllerHints.ShowSqueezeHint(m_LeftHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_RightHand, false);
        m_ControllerHints.ShowSqueezeHint(m_LeftHand, false);

        yield return new WaitUntil(() => cube.GetComponent<ExperimentObject>().isMoving == true);

        m_Text.text = "Deselect Gripper Control\n\n" +
                      "Touch the trackpad and deselect the gripper control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => !m_GripperControl.isGripping);

        m_ControllerHints.ShowTrackpadHint(false);

        m_Text.text = "Move the Cube\n\n" +
                      "Move the cube over the target X";
        m_AudioSource.Play();

        yield return new WaitUntil(() => CheckVec2Distance(cube, x));

        m_SpriteRenderer.sprite = m_Sprites[0];
        m_Text.text = "Select Gripper Control\n\n" +
                      "Touch the trackpad and select the gripper control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => m_GripperControl.isGripping);

        m_ControllerHints.ShowTrackpadHint(false);

        m_SpriteRenderer.sprite = null;
        m_Text.text = "Gripper Control\n\n" +
                      "Release the cube by activating gripper control\n\n" +
                      "(only grab one trigger)";
        m_AudioSource.Play();

        m_ControllerHints.ShowTriggerHint(m_LeftHand, true);
        m_ControllerHints.ShowTriggerHint(m_RightHand, true);

        yield return new WaitUntil(() => m_ControllerHints.handStatus.right.trigger || m_ControllerHints.handStatus.left.trigger);

        m_ControllerHints.ShowTriggerHint(m_LeftHand, false);
        m_ControllerHints.ShowTriggerHint(m_RightHand, false);

        yield return new WaitUntil(() => CheckVec2Distance(cube, x) && cube.GetComponent<ExperimentObject>().isMoving == false);

        m_Text.text = "Deselect Gripper Control";
        m_AudioSource.Play();

        m_ControllerHints.ShowTrackpadHint(true);

        yield return new WaitUntil(() => !m_GripperControl.isGripping);

        m_ControllerHints.ShowTrackpadHint(false);

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
        yield return null;
    }

    private IEnumerator Practice()
    {
        m_Timer.m_TimePassed = 0.0f;

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

    private bool CheckVec3Distance(GameObject first, GameObject second)
    {
        if (Vector3.Distance(first.transform.position, second.transform.position) < ManipulationMode.DISTANCETHRESHOLD)
            return true;
        else
            return false;
    }

    private bool CheckVec2Distance(GameObject first, GameObject second)
    {
        if (Vector2.Distance(new Vector2(first.transform.position.x, first.transform.position.z), new Vector2(second.transform.position.x, second.transform.position.z)) < ManipulationMode.DISTANCETHRESHOLD)
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
        stage = Stage.READY;
        DestroyAllObjects();
    }
}