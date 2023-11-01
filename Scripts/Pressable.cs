using UnityEngine;
using UnityEngine.Events;

public class Pressable : MonoBehaviour
{
    [Header ("Scene Objects")]
    [SerializeField] private GameObject m_Presser = null;
    [SerializeField] private Transform m_MovingPart = null;

    [Header ("Events")]
    [SerializeField] private UnityEvent m_OnButtonDown;
    [SerializeField] private UnityEvent m_OnButtonUp;
    [SerializeField] private UnityEvent m_OnButtonIsPressed;

    [Header("Properties")]
    [SerializeField] private bool m_isMultiPress = false;
    [Range(0, 1)]
    [SerializeField] private float m_EngageAtPercent = 0.95f;
    [Range(0, 1)]
    [SerializeField] private float m_DisengageAtPercent = 0.9f;

    private AudioSource m_AudioSource = null;
    private Vector3 m_LocalMoveDistance = new Vector3(0, -0.4f, 0);

    private bool m_Engaged = false;
    private bool m_ButtonDown = false;
    private bool m_ButtonUp = false;

    private Vector3 m_StartPosition;
    private Vector3 m_EndPosition;

    private Vector3 m_PresserEnteredPosition;
    private Collider[] m_PresserColliders = null;

    private void Awake()
    {
        m_AudioSource = gameObject.GetComponentInParent<AudioSource>();
        m_PresserColliders = m_Presser.GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        if (m_MovingPart == null && this.transform.childCount > 0)
            m_MovingPart = this.transform.GetChild(0);

        m_StartPosition = m_MovingPart.localPosition;
        m_EndPosition = m_StartPosition + m_LocalMoveDistance;
        m_PresserEnteredPosition = m_EndPosition;
    }

    private void OnTriggerStay(Collider other)
    {
        bool isPresser = false;

        foreach (var collider in m_PresserColliders)
        {
            if (other == collider)
            {
                isPresser = true;
                break;
            }
        }

        if(isPresser & (m_isMultiPress || !m_Engaged))
        {
            bool wasEngaged = m_Engaged;

            float currentDistance = Vector3.Distance(m_MovingPart.parent.InverseTransformPoint(m_Presser.transform.position), m_EndPosition);
            float enteredDistance = Vector3.Distance(m_PresserEnteredPosition, m_EndPosition);

            if (currentDistance > enteredDistance)
            {
                enteredDistance = currentDistance;
                m_PresserEnteredPosition = m_MovingPart.parent.InverseTransformPoint(m_Presser.transform.position);
            }

            float distanceDifference = enteredDistance - currentDistance;

            float lerp = Mathf.InverseLerp(0, m_LocalMoveDistance.magnitude, distanceDifference);

            if (lerp > m_EngageAtPercent)
                m_Engaged = true;
            else if (lerp < m_DisengageAtPercent)
                m_Engaged = false;

            m_MovingPart.localPosition = Vector3.Lerp(m_StartPosition, m_EndPosition, lerp);

            InvokeEvents(wasEngaged, m_Engaged);
        }
    }

    private void InvokeEvents(bool wasEngaged, bool isEngaged)
    {
        m_ButtonDown = wasEngaged == false && isEngaged == true;
        m_ButtonUp = wasEngaged == true && isEngaged == false;

        if (m_ButtonDown && m_OnButtonDown != null)
            m_OnButtonDown.Invoke();
        if (m_ButtonUp && m_OnButtonUp != null)
            m_OnButtonUp.Invoke();
        if (isEngaged && m_OnButtonIsPressed != null)
            m_OnButtonIsPressed.Invoke();
    }

    private void OnButtonDown()
    {
        m_AudioSource.Play();
        ColorSelf(Color.cyan);
    }

    private void OnButtonUp()
    {
        ColorSelf(Color.white);
    }

    private void ColorSelf(Color newColor)
    {
        m_MovingPart.GetComponent<Renderer>().material.color = newColor;
    }

    public void ResetButton()
    {
        ColorSelf(Color.white);
        m_Engaged = false;
        m_ButtonDown = false;
        m_ButtonUp = false;

        m_MovingPart.localPosition = m_StartPosition;
    }
}