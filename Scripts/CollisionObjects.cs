using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class CollisionObjects : MonoBehaviour
{
    [Header("Materials")]
    public Material m_AttachedMat = null;
    public Material m_CollidingMat = null;
    public Material m_FocusObjectMat = null;

    [HideInInspector] public GameObject m_FocusObject = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;

    private static readonly string[] m_FingerNames = {
        "HandColliderRight(Clone)/fingers/finger_index_2_r",
        "HandColliderLeft(Clone)/fingers/finger_index_2_r" };

    private int m_Id = 0;

    public bool isCreating = false;

    public struct ColObj
    {
        public Transform parent;
        public GameObject colObj;
    }

    public List<ColObj> m_CollisionObjects = new List<ColObj>();

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").GetComponent<Manipulator>();
    }

    private void Start()
    {
        Invoke("SetFingerColliderScript", 0.5f);   //Adds CollisionObjectCreator to fingers
    }

    private void OnDestroy()
    {
        foreach (var colObj in m_CollisionObjects)
        {
            Destroy(colObj.colObj);
        }
    }

    private void SetFingerColliderScript()
    {
        GameObject rightIndex = Player.instance.transform.Find(m_FingerNames[0]).gameObject;
        rightIndex.AddComponent<CollisionObjectCreator>();
        rightIndex.GetComponent<CollisionObjectCreator>().hand = Player.instance.rightHand;

        GameObject leftIndex = Player.instance.transform.Find(m_FingerNames[1]).gameObject;
        leftIndex.AddComponent<CollisionObjectCreator>();
        leftIndex.GetComponent<CollisionObjectCreator>().hand = Player.instance.leftHand;
    }

    /*private void MakeBox(Vector3 position, Vector3 scale)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().material = m_ColObjMat;

        box.AddComponent<Rigidbody>();
        box.GetComponent<Rigidbody>().isKinematic = true;
        
        box.AddComponent<CollisionHandling>();
        box.GetComponent<CollisionHandling>().m_EludingMaterial = m_ColObjMat;
        box.GetComponent<CollisionHandling>().m_CollidingMaterial = m_CollidingMat;

        box.AddComponent<CollisionBox>();
        box.GetComponent<CollisionBox>().AddCollisionBox(GetFreeID().ToString());
    }*/

    public void AddCollisionObject(GameObject colobj)
    {
        ColObj obj = new ColObj
        {
            parent = colobj.transform.parent,
            colObj = colobj
        };

        m_CollisionObjects.Add(obj);
        colobj.transform.SetParent(gameObject.transform);
    }

    public void RemoveCollisionObject(GameObject colObj)
    {
        foreach (var obj in m_CollisionObjects)
        {
            if (obj.colObj == colObj)
            {
                colObj.transform.SetParent(obj.parent);

                m_CollisionObjects.Remove(obj);
                break;
            }
        }
    }

    public void RemoveAllCollisionObjects()
    {
        foreach (var obj in m_CollisionObjects)
        {
            obj.colObj.transform.SetParent(obj.parent);

            Destroy(obj.colObj.GetComponent<CollisionHandling>());

            if (obj.colObj.GetComponent<CollisionObject>() != null)
                Destroy(obj.colObj.GetComponent<CollisionObject>());
        }
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }

    public void SetFocusObject(GameObject focusObject)
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