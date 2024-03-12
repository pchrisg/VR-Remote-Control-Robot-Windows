using Unity.VisualScripting;
using UnityEngine;

public class CollisionHandling : MonoBehaviour
{
    //Scripts
    private Manipulator m_Manipulator = null;
    private GripperControl m_GripperControl = null;
    private InteractableObjects m_InteractableObjects = null;

    //Properties
    public bool m_isAttachable = false;
    private Material m_OriginalMat = null;
    private Material m_CollidingMat = null;
    private Material m_AttachedMat = null;
    private Material m_FocusObjectMat = null;

    //Colliders
    private Collider[] m_ManipulatorColliders = null;

    private int m_ManipulatorTouching = 0;

    private readonly string[] m_FingerLinkNames = {
        "finger_middle_link_0",
        "finger_1_link_0",
        "finger_2_link_0"};
    private Collider[] m_FingerMColliders = null;
    private Collider[] m_Finger1Colliders = null;
    private Collider[] m_Finger2Colliders = null;

    private int m_FingerMTouching = 0;
    private int m_Finger1Touching = 0;
    private int m_Finger2Touching = 0;

    //States
    private bool m_isCreating = false;
    private bool m_isColliding = false;
    private bool m_isFocusObject = false;
    public bool m_isAttached = false;

    private void Awake()
    {
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
        m_GripperControl = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<GripperControl>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();

        m_ManipulatorColliders = m_Manipulator.transform.Find("palm").GetComponentsInChildren<Collider>();
        GameObject robotiq = GameObject.FindGameObjectWithTag("Robotiq");
        m_FingerMColliders = robotiq.transform.Find(m_FingerLinkNames[0]).GetComponentsInChildren<Collider>();
        m_Finger1Colliders = robotiq.transform.Find(m_FingerLinkNames[1]).GetComponentsInChildren<Collider>();
        m_Finger2Colliders = robotiq.transform.Find(m_FingerLinkNames[2]).GetComponentsInChildren<Collider>();
    }

    private void OnDestroy()
    {
        m_isCreating = false;
        m_isColliding = false;
        m_isAttached = false;
        m_isFocusObject = false;

        SetMaterial();
    }

    private void OnTriggerEnter(Collider other)
    {
        bool colliderFound = false;

        foreach (var collider in m_FingerMColliders)
        {
            if (other == collider)
            {
                m_FingerMTouching++;
                colliderFound = true;
                break;
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_Finger1Colliders)
            {
                if (other == collider)
                {
                    m_Finger1Touching++;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_Finger2Colliders)
            {
                if (other == collider)
                {
                    m_Finger2Touching++;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_ManipulatorColliders)
            {
                if (other == collider)
                {
                    m_ManipulatorTouching++;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (m_isAttachable && !m_isAttached)
        {
            if (m_FingerMTouching > 0 && (m_Finger1Touching > 0 || m_Finger2Touching > 0))
            {
                m_isAttached = true;
                m_GripperControl.PlayAttachSound();
                m_Manipulator.Colliding(false);

                if (m_isFocusObject)
                    m_InteractableObjects.SetFocusObject();

                SetMaterial();
            }
        }

        if (!m_isAttached && !m_isColliding)
        {
            if (m_ManipulatorTouching > 0)
            {
                m_isColliding = true;
                m_Manipulator.Colliding(true);

                SetMaterial();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool colliderFound = false;

        foreach (var collider in m_FingerMColliders)
        {
            if (other == collider)
            {
                m_FingerMTouching--;
                colliderFound = true;
                break;
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_Finger1Colliders)
            {
                if (other == collider)
                {
                    m_Finger1Touching--;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_Finger2Colliders)
            {
                if (other == collider)
                {
                    m_Finger2Touching--;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (!colliderFound)
        {
            foreach (var collider in m_ManipulatorColliders)
            {
                if (other == collider)
                {
                    m_ManipulatorTouching--;
                    colliderFound = true;
                    break;
                }
            }
        }

        if (m_isAttached)
        {
            if (m_FingerMTouching == 0)
            {
                m_isAttached = false;
                m_GripperControl.PlayDetachSound();

                if (m_isFocusObject)
                    m_InteractableObjects.SetFocusObject(gameObject);

                SetMaterial();
            }
        }

        if (!m_isAttached && m_isColliding)
        {
            if (m_ManipulatorTouching == 0)
            {
                m_isColliding = false;
                m_Manipulator.Colliding(false);

                SetMaterial();
            }
        }
    }

    private void SetMaterial()
    {
        Material mat = m_OriginalMat;

        if (m_isCreating)
        {
            if (m_isAttachable)
                mat = m_AttachedMat;
            else
                mat = m_CollidingMat;
        }

        if (m_isFocusObject)
            mat = m_FocusObjectMat;

        if (m_isColliding)
            mat = m_CollidingMat;

        if (m_isAttached)
            mat = m_AttachedMat;

        if (gameObject.GetComponent<Renderer>() != null)
            gameObject.GetComponent<Renderer>().material = mat;

        else if (gameObject.GetComponentsInChildren<Renderer>() != null)
        {
            foreach (var child in gameObject.GetComponentsInChildren<Renderer>())
                child.GetComponent<MaterialChanger>().ChangeMat(mat);
        }
    }

    public void IsCreating(bool value)
    {
        m_isCreating = value;
        SetMaterial();
    }

    public void SetupCollisionHandling(bool isAttachable, Material collidingMat, Material attachedMat, Material focusObjectMat)
    {
        m_isCreating = true;

        if(gameObject.GetComponent<Renderer>() != null)
            m_OriginalMat = gameObject.GetComponent<Renderer>().material;

        else if (gameObject.GetComponentsInChildren<Renderer>() != null)
        {
            foreach (var child in gameObject.GetComponentsInChildren<Renderer>())
                child.AddComponent<MaterialChanger>();
        }

        m_CollidingMat = collidingMat;

        m_isAttachable = isAttachable;
        if (isAttachable)
        {
            m_AttachedMat = attachedMat;
            m_FocusObjectMat = focusObjectMat;
        }

        SetMaterial();
    }

    public void SetAsFocusObject(bool isFocusObject)
    {
        m_isFocusObject = isFocusObject;

        SetMaterial();
    }
}