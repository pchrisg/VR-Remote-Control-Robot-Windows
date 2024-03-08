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
    private CollisionObjectMsg m_ColisionObject = null;

    private InteractableObjects m_InteractableObjects = null;

    private readonly float modifier = 0.005f;

    void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_InteractableObjects = GameObject.FindGameObjectWithTag("InteractableObjects").GetComponent<InteractableObjects>();
    }

    public String GetID()
    {
        return m_ColisionObject.id;
    }

    public void AddInteractableObject()
    {
        if (m_ColisionObject != null)
        {
            m_ColisionObject.header.stamp = new TimeMsg((uint)Time.time, 0);

            Collider col = null;
            foreach (var collider in gameObject.GetComponents<Collider>())
            {
                if ((collider is BoxCollider || collider is CapsuleCollider) && collider.isTrigger)
                {
                    col = collider;
                    break;
                }
            }

            if (col != null)
            {
                var pose = new PoseMsg
                {
                    position = col.bounds.center.To<FLU>(),
                    orientation = gameObject.transform.rotation.To<FLU>()
                };

                m_ColisionObject.primitive_poses[0] = pose;

                m_InteractableObjects.AddInteractableObject(gameObject);
                m_ROSPublisher.PublishAddCollisionObject(m_ColisionObject);
            }
        }
    }

    public void AddInteractableObject(bool isAttachable, Collider collider)
    {
        string id = string.Empty;
        if (isAttachable)
            id += "-(Attachable)";
        else
            id += "-(Collision)";

        id = m_InteractableObjects.GetFreeID().ToString() + id;

        var pose = new PoseMsg
        {
            position = collider.bounds.center.To<FLU>(),
            orientation = gameObject.transform.rotation.To<FLU>()
        };

        AddInteractableObject(id, pose, GetPrimitive(collider));
    }

    private void AddInteractableObject(string id, PoseMsg pose, SolidPrimitiveMsg primitive)
    {
        m_ColisionObject = new CollisionObjectMsg();

        m_ColisionObject.header.stamp = new TimeMsg((uint)Time.time, 0);
        m_ColisionObject.header.frame_id = "base_link";
        m_ColisionObject.id = id;
        m_ColisionObject.operation = CollisionObjectMsg.ADD;

        //Add pose
        Array.Resize(ref m_ColisionObject.primitive_poses, 1);
        m_ColisionObject.primitive_poses[0] = pose;

        //Add scale
        Array.Resize(ref m_ColisionObject.primitives, 1);
        m_ColisionObject.primitives[0] = primitive;

        m_InteractableObjects.AddInteractableObject(gameObject);
        m_ROSPublisher.PublishAddCollisionObject(m_ColisionObject);
    }

    private SolidPrimitiveMsg GetPrimitive(Collider collider)
    {
        var primitive = new SolidPrimitiveMsg();

        if (collider is BoxCollider boxCollider)
        {
            primitive.type = SolidPrimitiveMsg.BOX;
            Array.Resize(ref primitive.dimensions, 3);

            float width = gameObject.transform.lossyScale.z * boxCollider.size.z + modifier;
            float depth = gameObject.transform.lossyScale.x * boxCollider.size.x + modifier;
            float height = gameObject.transform.lossyScale.y * boxCollider.size.y + modifier;

            primitive.dimensions[SolidPrimitiveMsg.BOX_X] = width;
            primitive.dimensions[SolidPrimitiveMsg.BOX_Y] = depth;
            primitive.dimensions[SolidPrimitiveMsg.BOX_Z] = height;
        }

        if (collider is CapsuleCollider capsuleCollider)
        {
            primitive.type = SolidPrimitiveMsg.CYLINDER;
            Array.Resize(ref primitive.dimensions, 2);

            float height = gameObject.transform.lossyScale.y * capsuleCollider.height + modifier;
            float radius = gameObject.transform.lossyScale.x * capsuleCollider.radius + modifier;

            primitive.dimensions[SolidPrimitiveMsg.CYLINDER_HEIGHT] = height;
            primitive.dimensions[SolidPrimitiveMsg.CYLINDER_RADIUS] = radius;
        }

        return primitive;
    }

    public void RemoveInteractableObject()
    {
        m_InteractableObjects.RemoveInteractableObject(gameObject);
        m_ROSPublisher.PublishRemoveCollisionObject(m_ColisionObject);
    }
}