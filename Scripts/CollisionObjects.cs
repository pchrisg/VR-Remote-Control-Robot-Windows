using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionObjects : MonoBehaviour
{
    [Header("Prefab")]
    public Material m_InBoundsMaterial = null;

    private int m_Id = 0;

    private void Awake()
    {
        m_Id = -1;
        MakeBase();
    }

    private void Start()
    {
        Invoke("PublishChildren", 1.0f);
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void MakeBase()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.transform.position = new Vector3(0.0f, -0.05f, 0);
        box.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
        box.transform.SetParent(gameObject.transform);

        box.GetComponent<BoxCollider>().isTrigger = true;
        box.GetComponent<Renderer>().material = m_InBoundsMaterial;

        box.AddComponent<CollisionBox>();
    }

    public int GetFreeID()
    {
        m_Id++;
        return m_Id;
    }

    private void PublishChildren()
    {
        foreach(Transform child in gameObject.GetComponentInChildren<Transform>())
        {
            if (child.GetComponent<CollisionBox>() != null)
            {
                m_Id++;
                child.GetComponent<CollisionBox>().PublishCollisionBox(m_Id.ToString());
            }
        }
    }
}