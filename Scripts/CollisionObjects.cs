using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionObjects : MonoBehaviour
{
    [Header("Materials")]
    public Material m_AttObjMat = null;
    public Material m_AttachedMat = null;
    public Material m_ColObjMat = null;
    public Material m_CollidingMat = null;
    public Material m_FocusObjectMat = null;

    [HideInInspector] public GameObject m_FocusObject = null;

    private int m_Id = 0;

    private void Start()
    {
        Invoke("MakeGloveBox", 0.5f);
    }

    private void MakeGloveBox()
    {
        MakeBox(new Vector3(0.0f, -0.025f, -0.4f), new Vector3(1.3f, 0.05f, 1.3f)); // left
        MakeBox(new Vector3(-0.64f, 0.5f, -0.4f), new Vector3(0.02f, 1.0f, 1.3f)); // left
        MakeBox(new Vector3(0.64f, 0.5f, -0.4f), new Vector3(0.02f, 1.0f, 1.3f)); // right
        MakeBox(new Vector3(0.0f, 0.5f, -1.04f), new Vector3(1.3f, 1.0f, 0.02f)); // front
        MakeBox(new Vector3(0.0f, 0.5f, 0.24f), new Vector3(1.3f, 1.0f, 0.02f)); // back
        MakeBox(new Vector3(0.0f, 1.01f, -0.4f), new Vector3(1.3f, 0.02f, 1.3f)); // top
    }

    private void MakeBox(Vector3 position, Vector3 scale)
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
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id - 1;
    }
}