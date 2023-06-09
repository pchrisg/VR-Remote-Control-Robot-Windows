using UnityEngine;
using ManipulationOptions;

public class CollisionHandling : MonoBehaviour
{
    [Header("Materials")]
    public Material m_OriginalMat = null;
    public Material m_CollidingMat = null;
    public Material m_AttachedMat = null;
    public Material m_FocusObjectMat = null;

    private ManipulationMode m_ManipulationModeScript = null;
    private Gripper m_GripperScript = null;
    private EndEffector m_EndEffectorScript = null;
    private CollisionObjects m_CollisionObjectsScript = null;

    [HideInInspector] public bool m_isAttachable = false;
    [HideInInspector] public bool m_isAttached = false;
    
    private Collider[] m_EndEffectorColliders = null;
    private Collider[] m_UR5Colliders = null;
    private Collider[] m_RobotiqColliders = null;

    private bool isCreating = false;

    private void Awake()
    {
        m_ManipulationModeScript = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_GripperScript = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<Gripper>();
        m_EndEffectorScript = GameObject.FindGameObjectWithTag("EndEffector").GetComponent<EndEffector>();
        m_CollisionObjectsScript = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        

        m_EndEffectorColliders = m_GripperScript.transform.Find("palm").GetComponentsInChildren<Collider>();
        m_UR5Colliders = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();
        m_RobotiqColliders = GameObject.FindGameObjectWithTag("Robotiq").GetComponentsInChildren<Collider>();
    }

    private void OnDestroy()
    {
        gameObject.GetComponent<Renderer>().material = m_OriginalMat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_GripperScript.isGripping && m_isAttachable)
        {
            bool otherIsRobot = false;

            foreach (var collider in m_RobotiqColliders)
            {
                if (other == collider)
                {
                    otherIsRobot = true;
                    break;
                }
            }

            if (otherIsRobot)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_AttachedMat;
                m_GripperScript.SetObjGripSize();

                m_isAttached = true;
            }
        }

        else if (!m_isAttached)
        {
            bool otherIsEndEffector = false;

            foreach (var collider in m_EndEffectorColliders)
            {
                if (other == collider)
                {
                    otherIsEndEffector = true;
                    break;
                }
            }

            /*if (!otherIsEndEffector)
            {
                foreach (Collider collider in m_UR5Colliders)
                {
                    if (other == collider)
                    {
                        otherIsRobot = true;
                        break;
                    }
                }
            }*/

            if (otherIsEndEffector)
            {
                print("colliding");
                Renderer renderer = gameObject.GetComponent<Renderer>();

                renderer.material = m_CollidingMat;
                m_EndEffectorScript.Colliding();
                m_GripperScript.Collide();
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
        if (m_GripperScript.isGripping && m_isAttached)
        {
            bool otherIsRobot = false;

            foreach (var collider in m_RobotiqColliders)
            {
                if (other == collider)
                {
                    otherIsRobot = true;
                    break;
                }
            }

            if (otherIsRobot)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjectsScript.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;

                m_GripperScript.ResetAttObjSize();
                m_isAttached = false;
            }
        }

        else if (!m_isAttached)
        {
            bool otherIsEndEffector = false;

            foreach (var collider in m_EndEffectorColliders)
            {
                if (other == collider)
                {
                    otherIsEndEffector = true;
                    break;
                }
            }

            /*if (!otherIsEndEffector)
            {
                foreach (Collider collider in m_UR5Colliders)
                {
                    if (other == collider)
                    {
                        otherIsRobot = true;
                        break;
                    }
                }
            }*/

            if (otherIsEndEffector)
            {
                print("not colliding");
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.material = m_CollisionObjectsScript.m_FocusObject == gameObject ? m_FocusObjectMat : m_OriginalMat;
                m_EndEffectorScript.NotColliding();
            }
        }
    }

    private void Update()
    {
        if(isCreating != m_CollisionObjectsScript.isCreating)
        {
            if (!m_CollisionObjectsScript.isCreating)
                gameObject.GetComponent<Renderer>().material = m_OriginalMat;

            else
            {
                if (m_ManipulationModeScript.mode == Mode.COLOBJCREATOR && !m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_CollidingMat;

                else if (m_ManipulationModeScript.mode == Mode.ATTOBJCREATOR && m_isAttachable)
                    gameObject.GetComponent<Renderer>().material = m_AttachedMat;
            }
            isCreating = m_CollisionObjectsScript.isCreating;
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