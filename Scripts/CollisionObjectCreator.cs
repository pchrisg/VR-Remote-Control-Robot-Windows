using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class CollisionObjectCreator : MonoBehaviour
{
    private CollisionObjects m_CollisionObjects = null;
    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private GameObject m_NewBox = null;
    private Vector3 m_PrevConVec = Vector3.zero;

    private void Awake()
    {
        m_CollisionObjects = gameObject.transform.parent.GetComponent<CollisionObjects>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        m_Trigger.onStateDown += SetCollisionBox;
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= SetCollisionBox;
    }

    private void Update()
    {
        if(m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
        {
            MakeCollisionBox();
        }
    }

    private void MakeCollisionBox()
    {
        Transform rightIndex = m_RightHand.skeleton.indexTip;
        Transform leftIndex = m_LeftHand.skeleton.indexTip;
        Vector3 connectingVector = rightIndex.position - leftIndex.position;

        if (m_NewBox == null)
        {
            m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_InBoundsMaterial;
            m_NewBox.GetComponent<BoxCollider>().isTrigger = true;
            m_PrevConVec = Vector3.Normalize(connectingVector);
        }

        //float angleDiagonal = Mathf.Acos(Vector3.Dot(Vector3.up, Vector3.Normalize(connectingVector))) * Mathf.Rad2Deg;
        //print(Mathf.Rad2Deg * angleDiagonal);
        //Vector3 diagonalOnFace = Mathf.Sin(angleDiagonal) * Vector3.Normalize(connectingVector);
        //Vector3 tester = Quaternion.AngleAxis(45.0f, Vector3.up) * Vector3.Normalize(diagonalOnFace);
        //Vector3 debug = rightIndex.position + diagonalOnFace;
        //Debug.DrawLine(rightIndex.position, rightIndex.position + diagonalOnFace, Color.blue);
        
        m_NewBox.transform.position = leftIndex.position + connectingVector / 2.0f;
        m_NewBox.transform.localScale = connectingVector;
        m_NewBox.transform.rotation *= Quaternion.FromToRotation(m_PrevConVec, Vector3.Normalize(connectingVector));

        m_PrevConVec = Vector3.Normalize(connectingVector);

        //m_NewBox.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(tester));
    }

    private void SetCollisionBox(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(m_NewBox != null)
        {
            m_NewBox.transform.SetParent(gameObject.transform.parent);
            m_NewBox.AddComponent<CollisionBox>();
            m_NewBox.GetComponent<CollisionBox>().PublishCollisionBox(m_CollisionObjects.GetFreeID().ToString());
            m_NewBox = null;
        }
    }
}