using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionObjects : MonoBehaviour
{
    [Header("Materials")]
    public Material m_AttObjMaterial = null;
    public Material m_AttachedMaterial = null;
    public Material m_ColObjMaterial = null;
    public Material m_CollidingMaterial = null;
    public Material m_FocusObjectMaterial = null;

    [HideInInspector] public GameObject m_FocusObject = null;

    private int m_Id = 0;

    private void Start()
    {
        Invoke("MakeBase", 0.5f);
    }

    private void MakeBase()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.transform.position = new Vector3(0.0f, -0.05f, 0);
        box.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
        box.GetComponent<Renderer>().material = m_ColObjMaterial;

        box.AddComponent<Rigidbody>();
        box.GetComponent<Rigidbody>().isKinematic = true;
        
        box.AddComponent<CollisionHandling>();
        box.GetComponent<CollisionHandling>().m_EludingMaterial = m_ColObjMaterial;
        box.GetComponent<CollisionHandling>().m_CollidingMaterial = m_CollidingMaterial;

        box.AddComponent<CollisionBox>();
        box.GetComponent<CollisionBox>().AddCollisionBox(GetFreeID().ToString());
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }
}