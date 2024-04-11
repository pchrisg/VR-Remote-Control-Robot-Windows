using System;
using UnityEngine;
using RosMessageTypes.Moveit;
using RosMessageTypes.Shape;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;

public class InteractableObject : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private CollisionObjectMsg m_ColisionObjectMsg = null;

    private Vector3 m_PreviousPosition = new();

    private bool m_isMoving = false;

    private readonly float modifier = 0.005f; //5mm

    private void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();

        m_PreviousPosition = gameObject.transform.position;
    }

    private void Update()
    {
        if (m_isMoving)
        {
            if (!gameObject.GetComponent<CollisionHandling>().m_isAttached && Vector3.Distance(gameObject.transform.position, m_PreviousPosition) < 0.001f)
            {
                m_isMoving = false;
                AddInteractableObject();
            }
            else
                m_PreviousPosition = gameObject.transform.position;
        }
    }

    public void AddInteractableObject()
    {
        if (m_ColisionObjectMsg != null)
        {
            m_ColisionObjectMsg.header.stamp = new TimeMsg((uint)Time.time, 0);

            Collider collider = null;
            foreach (var col in gameObject.GetComponents<Collider>())
            {
                if ((col is BoxCollider || col is CapsuleCollider) && col.isTrigger)
                {
                    collider = col;
                    break;
                }
            }

            if (collider != null)
            {
                var poseMsg = new PoseMsg
                {
                    position = collider.bounds.center.To<FLU>(),
                    orientation = gameObject.transform.rotation.To<FLU>()
                };

                m_ColisionObjectMsg.primitive_poses[0] = poseMsg;
                m_ROSPublisher.PublishAddCollisionObject(m_ColisionObjectMsg);
            }
        }
    }

    public void AddInteractableObject(bool isAttachable, string idnum, Collider collider)
    {
        string id = string.Empty;
        if (isAttachable)
            id += "-(Attachable)";
        else
            id += "-(Collision)";

        id = idnum + id;

        var poseMsg = new PoseMsg
        {
            position = collider.bounds.center.To<FLU>(),
            orientation = gameObject.transform.rotation.To<FLU>()
        };

        AddInteractableObject(id, poseMsg, GetSolidPrimitiveMsg(collider));
    }

    private void AddInteractableObject(string id, PoseMsg poseMsg, SolidPrimitiveMsg solidPrimitiveMsg)
    {
        m_ColisionObjectMsg = new CollisionObjectMsg();

        m_ColisionObjectMsg.header.stamp = new TimeMsg((uint)Time.time, 0);
        m_ColisionObjectMsg.header.frame_id = "base_link";
        m_ColisionObjectMsg.id = id;
        m_ColisionObjectMsg.operation = CollisionObjectMsg.ADD;

        //Add pose
        Array.Resize(ref m_ColisionObjectMsg.primitive_poses, 1);
        m_ColisionObjectMsg.primitive_poses[0] = poseMsg;

        //Add scale
        Array.Resize(ref m_ColisionObjectMsg.primitives, 1);
        m_ColisionObjectMsg.primitives[0] = solidPrimitiveMsg;

        //m_InteractableObjects.AddInteractableObject(gameObject);
        m_ROSPublisher.PublishAddCollisionObject(m_ColisionObjectMsg);
    }

    private SolidPrimitiveMsg GetSolidPrimitiveMsg(Collider collider)
    {
        var solidPrimitiveMsg = new SolidPrimitiveMsg();

        if (collider is BoxCollider boxCollider)
        {
            solidPrimitiveMsg.type = SolidPrimitiveMsg.BOX;
            Array.Resize(ref solidPrimitiveMsg.dimensions, 3);

            float width = gameObject.transform.lossyScale.z * boxCollider.size.z + modifier;
            float depth = gameObject.transform.lossyScale.x * boxCollider.size.x + modifier;
            float height = gameObject.transform.lossyScale.y * boxCollider.size.y + modifier;

            solidPrimitiveMsg.dimensions[SolidPrimitiveMsg.BOX_X] = width;
            solidPrimitiveMsg.dimensions[SolidPrimitiveMsg.BOX_Y] = depth;
            solidPrimitiveMsg.dimensions[SolidPrimitiveMsg.BOX_Z] = height;
        }

        if (collider is CapsuleCollider capsuleCollider)
        {
            solidPrimitiveMsg.type = SolidPrimitiveMsg.CYLINDER;
            Array.Resize(ref solidPrimitiveMsg.dimensions, 2);

            float height = gameObject.transform.lossyScale.y * capsuleCollider.height + modifier;
            float radius = gameObject.transform.lossyScale.x * capsuleCollider.radius + modifier;

            solidPrimitiveMsg.dimensions[SolidPrimitiveMsg.CYLINDER_HEIGHT] = height;
            solidPrimitiveMsg.dimensions[SolidPrimitiveMsg.CYLINDER_RADIUS] = radius;
        }

        return solidPrimitiveMsg;
    }

    public void RemoveInteractableObject()
    {
        m_ROSPublisher.PublishRemoveCollisionObject(m_ColisionObjectMsg);
    }

    public void IsMoving()
    {
        if(!m_isMoving)
        {
            m_isMoving = true;
            RemoveInteractableObject();
        }
    }
}