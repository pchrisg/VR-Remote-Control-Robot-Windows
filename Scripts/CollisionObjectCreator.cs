using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using System;

public class CollisionObjectCreator : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private GameObject m_NewBox = null;

    [HideInInspector] public string m_KillFinger = String.Empty;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
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
        if(m_ManipulationMode.mode == Mode.COLOBJCREATOR)
        {
            if (m_NewBox != null )
            {
                if (m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
                    ScaleCollisionBox();

                else if (m_Grip.GetState(m_RightHand.handType) || m_Grip.GetState(m_LeftHand.handType))
                    RotateCollisionBox();
            }
            else
            {
                if (m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
                    MakeCollisionBox();

                else if (m_Grip.GetState(m_RightHand.handType) || m_Grip.GetState(m_LeftHand.handType))
                    DeleteCollisionBox();

                else
                    m_KillFinger = String.Empty;
            }
        }
    }

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    private void MakeCollisionBox()
    {
        m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_EludingMaterial;

        m_NewBox.AddComponent<Rigidbody>();
        m_NewBox.GetComponent<Rigidbody>().isKinematic = true;
        m_NewBox.AddComponent<PlayableArea>();
        m_NewBox.GetComponent<PlayableArea>().m_EludingMaterial = m_CollisionObjects.m_EludingMaterial;
        m_NewBox.GetComponent<PlayableArea>().m_CollidingMaterial = m_CollisionObjects.m_CollidingMaterial;
        m_NewBox.GetComponent<PlayableArea>().m_IsPlayableArea = false;
        m_NewBox.GetComponent<PlayableArea>().m_isDeleteAble = true;
        m_NewBox.GetComponent<PlayableArea>().m_ColObjCreator = gameObject.GetComponent<CollisionObjectCreator>();

        ScaleCollisionBox();
    }

    private void ScaleCollisionBox()
    {
        Vector3 rightIndex = m_RightHand.skeleton.indexTip.position;
        Vector3 leftIndex = m_LeftHand.skeleton.indexTip.position;
        Vector3 connectingVector = rightIndex - leftIndex;
        Vector3 midPoint = leftIndex + connectingVector * 0.5f;

        connectingVector = Quaternion.FromToRotation(m_NewBox.transform.right, Vector3.right) * connectingVector;
        m_NewBox.transform.position = midPoint;
        m_NewBox.transform.localScale = new Vector3(connectingVector.x, connectingVector.y, connectingVector.z);
    }

    private void RotateCollisionBox()
    {
        if(m_Grip.GetState(m_RightHand.handType))
        {
            Transform rightIndex = m_RightHand.skeleton.indexTip.transform;
            Vector3 connectingVector = rightIndex.position - m_NewBox.transform.position;
            Vector3 rightBackEdge = m_NewBox.transform.position + m_NewBox.transform.localScale * 0.5f - new Vector3(0, m_NewBox.transform.localScale.y * 0.5f, 0);
            Vector3 centerToRightBackEdge = rightBackEdge - m_NewBox.transform.position;

            m_NewBox.transform.rotation = Quaternion.FromToRotation(centerToRightBackEdge, Vector3.ProjectOnPlane(connectingVector, Vector3.up));
        }
        else
        {
            Transform leftIndex = m_LeftHand.skeleton.indexTip.transform;
            Vector3 connectingVector = leftIndex.position - m_NewBox.transform.position;
            Vector3 leftfrontEdge = m_NewBox.transform.position - m_NewBox.transform.localScale * 0.5f + new Vector3(0, m_NewBox.transform.localScale.y * 0.5f, 0);

            Vector3 centerToLeftFrontEdge = leftfrontEdge - m_NewBox.transform.position;

            m_NewBox.transform.rotation = Quaternion.FromToRotation(centerToLeftFrontEdge, Vector3.ProjectOnPlane(connectingVector, Vector3.up));
        }
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

    private void DeleteCollisionBox()
    {
        if(m_Grip.GetState(m_RightHand.handType))
            m_KillFinger = "right";
        else
            m_KillFinger = "left";
    }
}