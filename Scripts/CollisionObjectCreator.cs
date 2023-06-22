using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ManipulationOptions;
using Unity.VisualScripting;

public class CollisionObjectCreator : MonoBehaviour
{
    private ManipulationMode m_ManipulationMode = null;
    private CollisionObjects m_CollisionObjects = null;

    private SteamVR_Action_Boolean m_Grip = null;

    private Collider[] m_ColManipulator = null;
    private Collider[] m_ColUR5 = null;

    [HideInInspector]public Hand hand = null;

    private void Awake()
    {
        m_ManipulationMode = GameObject.FindGameObjectWithTag("ManipulationMode").GetComponent<ManipulationMode>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
        m_Grip = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");

        m_ColManipulator = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("palm").GetComponentsInChildren<Collider>();
        m_ColUR5 = GameObject.FindGameObjectWithTag("robot").GetComponentsInChildren<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(m_Grip.GetState(hand.handType))
        {
            foreach (var collider in m_ColManipulator)
            {
                if (other == collider)
                    return;
            }
            foreach (var collider in m_ColUR5)
            {
                if (other == collider)
                    return;
            }

            if (other.tag == "PlanningScene")
                return;

            if (m_ManipulationMode.mode == Mode.COLOBJCREATOR || m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
            {
                if (other.GetComponent<CollisionHandling>() == null)
                    AddCollisionObject(other);
                else
                    RemoveCollisionObject(other);
            }

            else if (other.GetComponent<CollisionHandling>().m_isAttachable)
                SetFocusObject(other);
        }
    }

    private void AddCollisionObject(Collider other)
    {
        other.AddComponent<CollisionHandling>();
        other.GetComponent<CollisionHandling>().m_OriginalMat = other.GetComponent<Renderer>().material;
        other.GetComponent<CollisionHandling>().m_CollidingMat = m_CollisionObjects.m_CollidingMat;

        string id = "";
        if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            id += "-(Attachable)";
            other.GetComponent<CollisionHandling>().m_isAttachable = true;
            other.GetComponent<CollisionHandling>().m_AttachedMat = m_CollisionObjects.m_AttachedMat;
            other.GetComponent<CollisionHandling>().m_FocusObjectMat = m_CollisionObjects.m_FocusObjectMat;
        }
        else
            id += "-(Collision)";

        if(other.GetComponentInChildren<Collider>() != null)
        {
            id = m_CollisionObjects.GetFreeID().ToString() + id;
            other.AddComponent<CollisionObject>();
            other.GetComponent<CollisionObject>().AddCollisionObject(id);
        }
    }

    private void RemoveCollisionObject(Collider other)
    {
        Destroy(other.GetComponent<CollisionHandling>());

        if (other.GetComponent<CollisionObject>() != null)
            Destroy(other.GetComponent<CollisionObject>());
    }

    private void SetFocusObject(Collider other)
    {
        if (m_CollisionObjects.m_FocusObject == null)
        {
            m_CollisionObjects.m_FocusObject = other.gameObject;

            other.GetComponent<CollisionHandling>().SetFocusObject(true);
            GameObject.FindGameObjectWithTag("Manipulator").GetComponent<DirectManipulation>().FocusObjectSelected();
        }

        else if (m_CollisionObjects.m_FocusObject == other.gameObject)
        {
            m_CollisionObjects.m_FocusObject = null;

            other.GetComponent<CollisionHandling>().SetFocusObject(false);
        }
    }

    /*private void MakeCollisionBox()
    {
        m_CollisionHandlings = gameObject.transform.parent.GetComponentsInChildren<CollisionHandling>();
        foreach (var colhand in m_CollisionHandlings)
            colhand.m_isDeleteAble = false;

        m_NewBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

        m_NewBox.AddComponent<Rigidbody>();
        m_NewBox.GetComponent<Rigidbody>().isKinematic = true;
        m_NewBox.AddComponent<CollisionHandling>();
        m_NewBox.GetComponent<CollisionHandling>().m_CollidingMaterial = m_CollisionObjects.m_CollidingMat;

        if (m_ManipulationMode.mode == Mode.COLOBJCREATOR)
        {
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_ColObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_EludingMaterial = m_CollisionObjects.m_ColObjMat;
        }
        else if (m_ManipulationMode.mode == Mode.ATTOBJCREATOR)
        {
            m_NewBox.GetComponent<CollisionHandling>().m_isAttachable = true;
            m_NewBox.GetComponent<Renderer>().material = m_CollisionObjects.m_AttObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_EludingMaterial = m_CollisionObjects.m_AttObjMat;
            m_NewBox.GetComponent<CollisionHandling>().m_AttachedMaterial = m_CollisionObjects.m_AttachedMat;
            m_NewBox.GetComponent<CollisionHandling>().m_FocusObjectMaterial = m_CollisionObjects.m_FocusObjectMat;
            m_NewBox.AddComponent<AttachableObject>();
        }

        ScaleCollisionBox();
    }*/

    /*private void SetCollisionBox(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (m_NewBox != null)
        {
            foreach (var colhand in m_CollisionHandlings)
                colhand.m_isDeleteAble = true;

            m_NewBox.transform.SetParent(gameObject.transform.parent);
            m_NewBox.GetComponent<CollisionHandling>().m_isDeleteAble = true;
            m_NewBox.AddComponent<CollisionBox>();
            m_NewBox.GetComponent<CollisionBox>().AddCollisionBox(m_CollisionObjects.GetFreeID().ToString());
            m_NewBox = null;
        }
    }*/
}