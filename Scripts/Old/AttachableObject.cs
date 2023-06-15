/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachableObject : MonoBehaviour
{
    private GameObject m_GhostObject = null;
    public bool m_isAttached = false;

    private void Start()
    {
        m_GhostObject = new GameObject("ObjectAttachPoint");
        DetachObject();
    }

    private void Update()
    {
        bool isAttached = gameObject.GetComponent<CollisionHandling>().m_isAttached;

        if(!isAttached)
            DetachObject();

        else
        {
            if (!m_isAttached)
                AttachObject();

            else
                FollowGripper();
        }
    }

    private void AttachObject()
    {
        CollisionObject colBox = gameObject.GetComponent<CollisionObject>();
        //colBox.AttachCollisionBox();

        m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
        m_GhostObject.transform.SetParent(GameObject.FindGameObjectWithTag("Robotiq").transform);
        m_isAttached = true;
    }

    private void FollowGripper()
    {
        gameObject.transform.SetPositionAndRotation(m_GhostObject.transform.position, m_GhostObject.transform.rotation);
    }

    private void DetachObject()
    {
        if(m_isAttached)
        {
            CollisionObject colBox = gameObject.GetComponent<CollisionObject>();
            //colBox.DetachCollisionBox();
            colBox.AddCollisionObject(colBox.GetID());

            m_GhostObject.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            m_GhostObject.transform.SetParent(gameObject.transform);

            m_isAttached = false;
        }
    }
}*/