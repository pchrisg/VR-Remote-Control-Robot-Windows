using UnityEngine;
using ManipulationOptions;

public class CollisionHandling : MonoBehaviour
{
    [Header("Materials")]
    public Material m_OriginalMat = null;
    public Material m_CollidingMat = null;
    public Material m_AttachedMat = null;
    public Material m_FocusObjectMat = null;

    private ManipulationMode m_ManipulationMode = null;
    private Gripper m_Gripper = null;
    private CollisionObjects m_CollisionObjects = null;

    [HideInInspector] public bool m_isAttachable = false;
    [HideInInspector] public bool m_isAttached = false;
    
    private Collider[] m_ColEndEffector = null;
    private Collider[] m_ColUR5 = null;
    private Collider[] m_ColGripper = null;

    private bool isCreating = false;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Gripper = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<Gripper>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();

        m_ColEndEffector = m_Gripper.transform.Find("palm").GetComponentsInChildren<Collider>();
        m_ColUR5 = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();
        m_ColGripper = GameObject.FindGameObjectWithTag("Robotiq").GetComponentsInChildren<Collider>();
    }

    private void OnDestroy()
    {
        gameObject.GetComponent<Renderer>().material = m_OriginalMat;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isColliding = false;

        if (m_Gripper.isGripping && m_isAttachable)
        {
            isColliding = false;

            foreach (var collider in m_ColGripper)
            {
                if (other == collider)
                {
                    isColliding = true;
                    break;
                }
            }

            if (isColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_AttachedMat;
                m_Gripper.SetObjGripSize();

                m_isAttached = true;
            }
        }

        else if (!m_isAttached)
        {
            foreach (var collider in m_ColEndEffector)
            {
                if (other == collider)
                {
                    isColliding = true;
                    break;
                }
            }

            if (!isColliding)
            {
                foreach (Collider collider in m_ColUR5)
                {
                    if (other == collider)
                    {
                        isColliding = true;
                        break;
                    }
                }
            }

            if (isColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();

                renderer.material = m_CollidingMat;
                m_Gripper.Collide();
            }
        }

        /*if (m_isDeleteAble && ((m_ManipulationMode.mode == Mode.COLOBJCREATOR && !m_isAttachable) ||
                               (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && m_isAttachable)))
        {
            if ((other == m_RightIndex && m_Grip.GetState(Player.instance.rightHand.handType)) ||
                (other == m_LeftIndex && m_Grip.GetState(Player.instance.leftHand.handType)))
            {
                if (m_CollisionObjects.m_FocusObject == gameObject)
                    m_CollisionObjects.m_FocusObject = null;
                GameObject.Destroy(gameObject);
            }
        }*/

        /*if (m_isAttachable && m_ManipulationMode.mode != Mode.ATTOBJCREATOR)
        {
            if ((other == m_RightIndex && m_Grip.GetState(Player.instance.rightHand.handType)) ||
                (other == m_LeftIndex && m_Grip.GetState(Player.instance.leftHand.handType)))
            {
                if (m_CollisionObjects.m_FocusObject == null)
                {
                    m_CollisionObjects.m_FocusObject = gameObject;

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    renderer.material = m_FocusObjectMaterial;

                    m_Gripper.GetComponent<DirectManipulation>().FocusObjectSelected();
                }

                else if (m_CollisionObjects.m_FocusObject == gameObject && m_ManipulationMode.mode != Mode.RAILCREATOR)
                {
                    m_CollisionObjects.m_FocusObject = null;

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    renderer.material = m_EludingMaterial;
                }
            }
        }*/
    }

    private void OnTriggerExit(Collider other)
    {
        bool isNotColliding = false;

        if (m_Gripper.isGripping && m_isAttached)
        {
            isNotColliding = false;

            foreach (var collider in m_ColGripper)
            {
                if (other == collider)
                {
                    isNotColliding = true;
                    break;
                }
            }

            if (isNotColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjects.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;

                m_Gripper.ResetAttObjSize();
                m_isAttached = false;
            }
        }

        else if (!m_isAttached)
        {
            foreach (var collider in m_ColEndEffector)
            {
                if (other == collider)
                {
                    isNotColliding = true;
                    break;
                }
            }

            if (!isNotColliding)
            {
                foreach (Collider collider in m_ColUR5)
                {
                    if (other == collider)
                    {
                        isNotColliding = true;
                        break;
                    }
                }
            }

            if (isNotColliding)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjects.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;
            }
        }
    }

    private void Update()
    {
        if(isCreating != m_CollisionObjects.isCreating)
        {
            if (!m_CollisionObjects.isCreating)
                gameObject.GetComponent<Renderer>().material = m_OriginalMat;

            else
            {
                if (m_ManipulationMode.mode == Mode.COLOBJCREATOR && !m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_CollidingMat;

                else if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR && m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_AttachedMat;
            }
            isCreating = m_CollisionObjects.isCreating;
        }
    }

    public void ToggleFocusObject(bool isFocusObj)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (isFocusObj)
            renderer.material = m_FocusObjectMat;
        else
            renderer.material = m_OriginalMat;
    }
}