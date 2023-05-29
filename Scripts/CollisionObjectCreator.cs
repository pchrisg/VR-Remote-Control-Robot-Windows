using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using System;
using Unity.VisualScripting;

public class CollisionObjectCreator : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material m_ColObjMat = null;

    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;
    private SteamVR_Action_Boolean m_Trigger = null;

    private Hand m_RightHand = null;
    private Hand m_LeftHand = null;

    private GameObject m_NewBox = null;
    private CollisionHandling[] m_CollisionHandlings = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = gameObject.transform.parent.GetComponent<CollisionObjects>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        m_Trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabTrigger");
        m_Trigger.onStateDown += SetCollisionBox;
        m_LeftHand = Player.instance.leftHand;
        m_RightHand = Player.instance.rightHand;
    }

    private void OnDisable()
    {
        if (m_NewBox != null)
        {
            Destroy(m_NewBox);
            m_NewBox = null;
        }
    }

    private void OnDestroy()
    {
        m_Trigger.onStateDown -= SetCollisionBox;
    }

    private void Update()
    {
        if(m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            if (m_NewBox == null )
            {
                if (m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
                    MakeCollisionBox();
            }
            else
            {
                if (m_Grip.GetState(m_RightHand.handType) && m_Grip.GetState(m_LeftHand.handType))
                    ScaleCollisionBox();

                else if (m_Grip.GetState(m_RightHand.handType) || m_Grip.GetState(m_LeftHand.handType))
                    RotateCollisionBox();
            }
        }
    }

    public void Show(bool value)
    {
        Color color = m_ColObjMat.color;
        if (value)
            m_ColObjMat.color = new Color(color.r, color.g, color.b, 1.0f);
        else
            m_ColObjMat.color = new Color(color.r, color.g, color.b, 0.0f);

        gameObject.SetActive(value);
    }

    private void MakeCollisionBox()
    {
        m_CollisionHandlings = gameObject.transform.parent.GetComponentsInChildren<CollisionHandling>();
        foreach (var colhand in m_CollisionHandlings)
            colhand.m_isDeleteAble = false;

        m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

        m_NewBox.AddComponent<Rigidbody>();
        m_NewBox.GetComponent<Rigidbody>().isKinematic = true;
        m_NewBox.AddComponent<CollisionHandling>();
        m_NewBox.GetComponent<CollisionHandling>().m_CollidingMaterial = m_CollisionObjects.m_CollidingMat;

        if(m_ManipulationMode.mode == Mode.COLOBJCREATOR)
        {
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_ColObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_EludingMaterial = m_CollisionObjects.m_ColObjMat;
        }
        else if(m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_NewBox.GetComponent<CollisionHandling>().m_isAttachable = true;
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_AttObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_EludingMaterial = m_CollisionObjects.m_AttObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_AttachedMaterial = m_CollisionObjects.m_AttachedMat;
            m_NewBox.GetComponent<CollisionHandling>().m_FocusObjectMaterial = m_CollisionObjects.m_FocusObjectMat;
            m_NewBox.AddComponent<AttachableObject>();
        }

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
        m_NewBox.transform.localScale = new Vector3(Mathf.Abs(connectingVector.x), Mathf.Abs(connectingVector.y), Mathf.Abs(connectingVector.z));
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
            foreach (var colhand in m_CollisionHandlings)
                colhand.m_isDeleteAble = true;
            
            m_NewBox.transform.SetParent(gameObject.transform.parent);
            m_NewBox.GetComponent<CollisionHandling>().m_isDeleteAble = true;
            m_NewBox.AddComponent<CollisionBox>();
            m_NewBox.GetComponent<CollisionBox>().AddCollisionBox(m_CollisionObjects.GetFreeID().ToString());
            m_NewBox = null;
        }
    }
}