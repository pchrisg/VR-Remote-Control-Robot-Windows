using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using static UnityEngine.ParticleSystem;
using static Rails;

public class CollisionObjectCreator : MonoBehaviour
{
    [Header("Scene Object")]
    [SerializeField] private ManipulationMode m_ManipulationMode;

    private CollisionObjects m_CollisionObjects = null;
    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;
    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;
    private GameObject m_NewBox = null;

    private void Awake()
    {
        m_CollisionObjects = gameObject.transform.parent.GetComponent<CollisionObjects>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        m_Trigger.onStateDown += SetCollisionBox;
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void OnDisable()
    {
        if (m_NewBox != null)
            Destroy(m_NewBox);
        m_NewBox = null;
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= SetCollisionBox;
    }

    private void Update()
    {
        if(m_ManipulationMode.mode == Mode.AABBCREATOR)
        {
            if (m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
            {
                MakeCollisionBox();
            }
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void MakeCollisionBox()
    {
        Transform rightIndex = m_RightHand.skeleton.indexTip;
        Transform leftIndex = m_LeftHand.skeleton.indexTip;
        Vector3 connectingVector = rightIndex.position - leftIndex.position;

        if (m_NewBox == null)
        {
            //m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            //m_NewBox.GetComponent<CapsuleCollider>().isTrigger = true;

            m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_EludingMaterial;
            m_NewBox.GetComponent<BoxCollider>().isTrigger = true;

            m_NewBox.AddComponent<PlayableArea>();
            m_NewBox.GetComponent<PlayableArea>().m_EludingMaterial = m_CollisionObjects.m_EludingMaterial;
            m_NewBox.GetComponent<PlayableArea>().m_CollidingMaterial = m_CollisionObjects.m_CollidingMaterial;
            m_NewBox.GetComponent<PlayableArea>().m_IsPlayableArea = false;

            //m_NewBox.transform.localScale = new Vector3(0.0025f, connectingVector.magnitude/2, 0.0025f);
            //m_NewBox.transform.localScale = connectingVector;
        }

        ////////
        float a = connectingVector.magnitude/2;

        m_NewBox.transform.localScale = new Vector3(a,a,0.02f);
        ////////
        /*float CosAngle = Vector3.Dot(Vector3.Normalize(connectingVector), Vector3.forward);
        Vector3 projectedPoint = rightIndex.position - (CosAngle * connectingVector.magnitude) * Vector3.forward;
        Vector3 projectedConnectingVector = projectedPoint - leftIndex.position;

        float x = Mathf.Abs(projectedConnectingVector.x);
        float y = Mathf.Abs(projectedConnectingVector.y);
        float z = Mathf.Sqrt(Mathf.Pow(connectingVector.magnitude,2) - Mathf.Pow(x, 2) - Mathf.Pow(y, 2));

        m_NewBox.transform.localScale = new Vector3(x, y, z);*/
        ////////
        //m_NewBox.transform.rotation = Quaternion.FromToRotation(Vector3.up, connectingVector);
        /*m_NewBox.transform.rotation = Quaternion.FromToRotation(Vector3.up, connectingVector);
        m_NewBox.transform.localScale = new Vector3(0.0025f, connectingVector.magnitude/2, 0.0025f);
        float dot = Vector3.Dot(Vector3.Normalize(connectingVector), Vector3.right);
        float arcos = Mathf.Acos(dot);
        float ang = Mathf.Rad2Deg * arcos;
        print(dot + " " + arcos + " " + ang);
        m_NewBox.transform.rotation = Quaternion.AngleAxis(ang, Vector3.up) * m_NewBox.transform.rotation;*/
        ////////
        ///
        Vector3 direction = Vector3.Cross(Vector3.Normalize(connectingVector), Vector3.up);

        m_NewBox.transform.position = leftIndex.position + connectingVector / 2.0f;
        m_NewBox.transform.rotation = m_NewBox.transform.rotation * Quaternion.FromToRotation(m_NewBox.transform.forward, direction);
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
        /*else if (m_Rails.GetLastChild() != m_Rails.GetComponent<Transform>())
        {
            print("inside");
            GameObject lastChild = m_Rails.GetLastChild().gameObject;
            Destroy(lastChild);
        }*/
    }
}