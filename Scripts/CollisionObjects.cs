using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class CollisionObjects : MonoBehaviour
{
    [Header("Materials")]
    public Material m_AttachedMat = null;
    public Material m_CollidingMat = null;
    public Material m_FocusObjectMat = null;

    [HideInInspector] public GameObject m_FocusObject = null;
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

    private void Start()
    {
        Invoke("SetFingerColliderScript", 0.5f);   //Adds CollisionObjectCreator to fingers
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
            if(obj.colObj == colObj)
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
}