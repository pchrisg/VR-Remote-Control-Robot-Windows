using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;
using ManipulationModes;

public class InteractableObjects : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_CollidingMat = null;
    [SerializeField] private Material m_AttachedMat = null;
    [SerializeField] private Material m_FocusObjectMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;

    private static readonly string[] m_FingerNames = {
        "HandColliderRight(Clone)/fingers/finger_index_2_r",
        "HandColliderLeft(Clone)/fingers/finger_index_2_r" };

    private int m_Id = 0;

    [HideInInspector] public GameObject m_FocusObject = null;

    public struct IObject
    {
        public Transform parent;
        public GameObject gameObj;
    }

    public List<IObject> m_InteractableObjects = new();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
    }

    private void Start()
    {
        Invoke(nameof(SetFingerColliderScript), 0.5f);   //Adds CollisionObjectCreator to fingers
    }

    private void SetFingerColliderScript()
    {
        GameObject rightIndex = Player.instance.transform.Find(m_FingerNames[0]).gameObject;
        rightIndex.AddComponent<InteractableObjectCreator>();
        rightIndex.GetComponent<InteractableObjectCreator>().hand = Player.instance.rightHand;

        GameObject leftIndex = Player.instance.transform.Find(m_FingerNames[1]).gameObject;
        leftIndex.AddComponent<InteractableObjectCreator>();
        leftIndex.GetComponent<InteractableObjectCreator>().hand = Player.instance.leftHand;
    }

    public void IsCreating(bool value)
    {
        foreach (var iObj in m_InteractableObjects)
            iObj.gameObj.GetComponent<CollisionHandling>().IsCreating(value);
    }

    public void AddInteractableObject(Collider collider)
    {
        bool isAttachable = false;
        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
            isAttachable = true;

        collider.AddComponent<CollisionHandling>();
        collider.GetComponent<CollisionHandling>().SetupCollisionHandling(isAttachable, m_CollidingMat, m_AttachedMat, m_FocusObjectMat);

        collider.AddComponent<InteractableObject>();
        collider.GetComponent<InteractableObject>().AddInteractableObject(isAttachable, GetFreeID().ToString(), collider);

        IObject iObj = new()
        {
            parent = collider.transform.parent,
            gameObj = collider.gameObject
        };

        m_InteractableObjects.Add(iObj);
        collider.transform.SetParent(gameObject.transform);
    }

    public void RemoveInteractableObject(Collider collider)
    {
        foreach (var iObj in m_InteractableObjects)
        {
            if (iObj.gameObj == collider.gameObject)
            {
                RemoveInteractableObject(iObj);
                break;
            }
        }
    }

    public void RemoveInteractableObject(IObject iObj)
    {
        Destroy(iObj.gameObj.GetComponent<CollisionHandling>());
        iObj.gameObj.GetComponent<InteractableObject>().RemoveInteractableObject();
        Destroy(iObj.gameObj.GetComponent<InteractableObject>());

        iObj.gameObj.transform.SetParent(iObj.parent);
        m_InteractableObjects.Remove(iObj);
    }

    public void RemoveAllInteractableObjects()
    {
        foreach (var iObj in m_InteractableObjects)
            RemoveInteractableObject(iObj);
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }

    public void SetFocusObject(Collider collider)
    {
        if (m_FocusObject == null)
        {
            m_FocusObject = collider.gameObject;
            m_FocusObject.GetComponent<CollisionHandling>().SetAsFocusObject(true);
        }

        else if (m_FocusObject == collider.gameObject)
        {
            m_FocusObject.GetComponent<CollisionHandling>().SetAsFocusObject(false);
            m_FocusObject = null;
        }
    }

    public void SetFocusObject(GameObject focusObject = null)
    {
        m_FocusObject = focusObject;
    }

    public Quaternion LookAtFocusObject(Vector3 position, Transform initPose, Vector3 connectingVector = new())
    {
        if (m_FocusObject == null)
            return initPose.rotation;

        Vector3 right = position - m_FocusObject.transform.position;
        Vector3 up;
        Vector3 forward;

        // manipulator above focus object
        float angle = Vector3.Angle(right, m_FocusObject.transform.up);
        if (angle < 0.1f)
        {
            right = Vector3.Project(initPose.right, m_FocusObject.transform.up);
            up = Vector3.ProjectOnPlane(initPose.up, m_FocusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        // manipulator to the right/left of focus object
        angle = Vector3.Angle(right, m_FocusObject.transform.right);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(initPose.up, m_FocusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        // manipulator infront/behind focus object
        angle = Vector3.Angle(right, m_FocusObject.transform.forward);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(initPose.up, m_FocusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        // manipulator facing focus object
        if (m_ManipulationMode.mode == Mode.DIRECT)
        {
            angle = Vector3.Angle(initPose.right, right);
            if (angle < ManipulationMode.ANGLETHRESHOLD)
            {
                up = Vector3.Cross(initPose.forward, right);
                angle = Vector3.Angle(up, Vector3.up);
                up = angle <= 90 ? Vector3.up : -Vector3.up;

                forward = Vector3.Cross(right.normalized, up.normalized);
                up = Vector3.Cross(forward.normalized, right.normalized);

                return Quaternion.LookRotation(forward, up);
            }
        }

        else if (connectingVector != Vector3.zero)
        {
            right = position - m_FocusObject.transform.position;

            angle = Vector3.Angle(connectingVector, right);
            if (angle < ManipulationMode.ANGLETHRESHOLD || 180.0f - angle < ManipulationMode.ANGLETHRESHOLD)
            {
                up = Vector3.Cross(initPose.forward, right);
                angle = Vector3.Angle(up, Vector3.up);
                up = angle <= 90 ? Vector3.up : -Vector3.up;

                forward = Vector3.Cross(right.normalized, up.normalized);
                up = Vector3.Cross(forward.normalized, right.normalized);

                return Quaternion.LookRotation(forward, up);
            }
        }

        return initPose.rotation;
    }
}