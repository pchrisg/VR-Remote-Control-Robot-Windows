using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class InteractableObjects : MonoBehaviour
{
    [Header("Materials")]
    public Material m_AttachedMat = null;
    public Material m_CollidingMat = null;
    public Material m_FocusObjectMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;

    private static readonly string[] m_FingerNames = {
        "HandColliderRight(Clone)/fingers/finger_index_2_r",
        "HandColliderLeft(Clone)/fingers/finger_index_2_r" };

    private int m_Id = 0;

    public bool isCreating = false;
    [HideInInspector] public GameObject m_FocusObject = null;

    public struct IObject
    {
        public Transform parent;
        public GameObject iObject;
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

    public void AddInteractableObject(GameObject obj)
    {
        IObject iObj = new()
        {
            parent = obj.transform.parent,
            iObject = obj
        };

        m_InteractableObjects.Add(iObj);
        obj.transform.SetParent(gameObject.transform);
    }

    public void RemoveInteractableObject(GameObject obj)
    {
        foreach (var iObj in m_InteractableObjects)
        {
            if (iObj.iObject == obj)
            {
                obj.transform.SetParent(iObj.parent);
                m_InteractableObjects.Remove(iObj);
                break;
            }
        }
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }

    public void SetFocusObject(GameObject focusObject = null)
    {
        m_FocusObject = focusObject;

        if (m_FocusObject != null && m_ManipulationMode.mode != ManipulationModes.Mode.SDOF)
        {
            Quaternion rotation = LookAtFocusObject(m_Manipulator.transform.position, m_Manipulator.transform);
            m_Manipulator.GetComponent<ArticulationBody>().TeleportRoot(m_Manipulator.transform.position, rotation);

            if (m_ManipulationMode.mode != ManipulationModes.Mode.RAILCREATOR)
                StartCoroutine(PublishMoveArm());
        }
    }

    private IEnumerator PublishMoveArm()
    {
        yield return new WaitForNextFrameUnit();

        m_ROSPublisher.PublishMoveArm();
    }

    public Quaternion LookAtFocusObject(Vector3 position, Transform initPose, Vector3 connectingVector = new Vector3())
    {
        if (m_FocusObject == null)
            return initPose.rotation;

        float angleToRight = Vector3.Angle(initPose.up, m_FocusObject.transform.right);
        float angleToForward = Vector3.Angle(initPose.up, m_FocusObject.transform.forward);

        if (180.0f - angleToRight < angleToRight)
            angleToRight = 180.0f - angleToRight;
        if (180.0f - angleToForward < angleToForward)
            angleToForward = 180.0f - angleToForward;

        Vector3 right = position - m_FocusObject.transform.position;
        Vector3 up = Vector3.zero;
        Vector3 forward = Vector3.zero;
        float angle = Vector3.Angle(right, m_FocusObject.transform.up);
        if (angle < 0.1f)
        {
            if (angleToRight < angleToForward)
                up = Vector3.Project(initPose.up, m_FocusObject.transform.right);
            else
                up = Vector3.Project(initPose.up, m_FocusObject.transform.forward);

            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        angle = Vector3.Angle(right, m_FocusObject.transform.right);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(initPose.up, m_FocusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        angle = Vector3.Angle(right, m_FocusObject.transform.forward);
        if (angle < 0.1f || 180.0f - angle < 0.1f)
        {
            up = Vector3.Project(initPose.up, m_FocusObject.transform.up);
            forward = Vector3.Cross(right.normalized, up.normalized);

            return Quaternion.LookRotation(forward, up);
        }

        if (m_ManipulationMode.mode == ManipulationModes.Mode.DIRECT)
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