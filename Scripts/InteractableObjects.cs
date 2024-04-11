using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.InteractionSystem;
using ManipulationModes;
using System.Collections;

public class InteractableObjects : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material m_CollidingMat = null;
    [SerializeField] private Material m_AttachedMat = null;
    [SerializeField] private Material m_FocusObjectMat = null;

    private ROSPublisher m_ROSPublisher = null;
    private ManipulationMode m_ManipulationMode = null;
    private Manipulator m_Manipulator = null;
    private ExperimentManager m_ExperimentManager = null;

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
        m_ExperimentManager = GameObject.FindGameObjectWithTag("Experiment").GetComponent<ExperimentManager>();
    }

    private void Start()
    {
        Invoke(nameof(SetFingerColliderScript), 0.5f);   //Adds CollisionObjectCreator to fingers
    }

    private void SetFingerColliderScript()
    {
        GameObject rightIndex = Player.instance.transform.Find(m_FingerNames[0]).gameObject;
        rightIndex.AddComponent<InteractableObjectCreator>();
        rightIndex.GetComponent<InteractableObjectCreator>().Setup("right");

        GameObject leftIndex = Player.instance.transform.Find(m_FingerNames[1]).gameObject;
        leftIndex.AddComponent<InteractableObjectCreator>();
        leftIndex.GetComponent<InteractableObjectCreator>().Setup("left");
    }

    private int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }

    public void RemoveAllInteractableObjects()
    {
        m_FocusObject = null;

        StartCoroutine(RemoveAllInteractableObjectsRoutine());
    }

    private IEnumerator RemoveAllInteractableObjectsRoutine()
    {
        while (m_InteractableObjects.Count > 0)
        {
            RemoveInteractableObject(m_InteractableObjects[^1]);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void RemoveInteractableObject(IObject iObj)
    {
        Destroy(iObj.gameObj.GetComponent<CollisionHandling>());
        iObj.gameObj.GetComponent<InteractableObject>().RemoveInteractableObject();
        Destroy(iObj.gameObj.GetComponent<InteractableObject>());

        iObj.gameObj.transform.SetParent(iObj.parent);
        m_InteractableObjects.Remove(iObj);
    }

    public void IsCreating(bool value)
    {
        foreach (var iObj in m_InteractableObjects)
            iObj.gameObj.GetComponent<CollisionHandling>().IsCreating(value);
    }

    public void AddInteractableObject(Collider collider)
    {
        print("yea");
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

    public void SetFocusObject(Collider collider)
    {
        if (m_FocusObject == null)
        {
            m_FocusObject = collider.gameObject;
            m_FocusObject.GetComponent<CollisionHandling>().IsFocusObject(true);
            m_FocusObject.GetComponent<InteractableObject>().RemoveInteractableObject();
            m_ExperimentManager.RecordFocusObject(m_FocusObject.name, true);
        }

        else if (m_FocusObject == collider.gameObject)
        {
            m_FocusObject.GetComponent<CollisionHandling>().IsFocusObject(false);
            m_FocusObject.GetComponent<InteractableObject>().AddInteractableObject();
            m_ExperimentManager.RecordFocusObject(m_FocusObject.name, false);
            m_FocusObject = null;
        }
    }
}