using System;
using UnityEngine;
using RosMessageTypes.Moveit;
using RosMessageTypes.Shape;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using Unity.VisualScripting;

public class CollisionObject : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private CollisionObjectMsg m_ColisionObject = null;
    private CollisionObjects m_CollisionObjects = null;

    void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
        m_CollisionObjects = GameObject.FindGameObjectWithTag("CollisionObjects").GetComponent<CollisionObjects>();
    }

    public void OnDestroy()
    {
        RemoveCollisionObject();
    }

    public String GetID()
    {
        return m_ColisionObject.id;
    }

    public void AddCollisionObject()
    {
        if(m_ColisionObject == null)
        {
            print("Cannot add empty Object");
            return;
        }

        var pose = new PoseMsg
        {
            position = gameObject.transform.position.To<FLU>(),
            orientation = gameObject.transform.rotation.To<FLU>()
        };

        m_ColisionObject.primitive_poses[0] = pose;

        m_CollisionObjects.AddCollisionObject(gameObject);
        m_ROSPublisher.PublishAddCollisionObject(m_ColisionObject);
    }

    public void AddCollisionObject(string id)
    {
        var pose = new PoseMsg
        {
            position = gameObject.transform.position.To<FLU>(),
            orientation = gameObject.transform.rotation.To<FLU>()
        };

        AddCollisionObject(id, pose, GetPrimitive());
    }

    private void AddCollisionObject(string id, PoseMsg pose, SolidPrimitiveMsg primitive)
    {
        m_ColisionObject = new CollisionObjectMsg();

        m_ColisionObject.header.stamp = new TimeMsg((uint)Time.time, 0);
        m_ColisionObject.header.frame_id = "base_link";
        m_ColisionObject.id = id;
        m_ColisionObject.operation = CollisionObjectMsg.ADD;

        //Add box pose
        Array.Resize(ref m_ColisionObject.primitive_poses, 1);
        m_ColisionObject.primitive_poses[0] = pose;

        //Add box scale
        Array.Resize(ref m_ColisionObject.primitives, 1);
        m_ColisionObject.primitives[0] = primitive;

        m_CollisionObjects.AddCollisionObject(gameObject);
        m_ROSPublisher.PublishAddCollisionObject(m_ColisionObject);
    }

    private SolidPrimitiveMsg GetPrimitive()
    {
        var primitive = new SolidPrimitiveMsg();

        if (gameObject.GetComponentInChildren<BoxCollider>() != null)
        {
            primitive.type = SolidPrimitiveMsg.BOX;
            Array.Resize(ref primitive.dimensions, 3);

            float width = gameObject.transform.lossyScale.z * gameObject.GetComponentInChildren<BoxCollider>().size.z;
            float depth = gameObject.transform.lossyScale.x * gameObject.GetComponentInChildren<BoxCollider>().size.x;
            float height = gameObject.transform.lossyScale.y * gameObject.GetComponentInChildren<BoxCollider>().size.y;

            primitive.dimensions[SolidPrimitiveMsg.BOX_X] = width;
            primitive.dimensions[SolidPrimitiveMsg.BOX_Y] = depth;
            primitive.dimensions[SolidPrimitiveMsg.BOX_Z] = height;
        }

        if (gameObject.GetComponentInChildren<CapsuleCollider>() != null)
        {
            primitive.type = SolidPrimitiveMsg.CYLINDER;
            Array.Resize(ref primitive.dimensions, 2);

            float height = gameObject.transform.lossyScale.y * gameObject.GetComponentInChildren<CapsuleCollider>().height;
            float radius = gameObject.transform.lossyScale.x * gameObject.GetComponentInChildren<CapsuleCollider>().radius;

            primitive.dimensions[SolidPrimitiveMsg.CYLINDER_HEIGHT] = height;
            primitive.dimensions[SolidPrimitiveMsg.CYLINDER_RADIUS] = radius;
        }
        
        return primitive;
    }

    public void RemoveCollisionObject()
    {
        m_CollisionObjects.RemoveCollisionObject(gameObject);
        m_ROSPublisher.PublishRemoveCollisionObject(m_ColisionObject);
    }

    /*public void AttachCollisionObject()
    {
        AttachedCollisionObjectMsg attColObj = new AttachedCollisionObjectMsg();
        attColObj.@object.header.stamp = new TimeMsg((uint)Time.time, 0);
        attColObj.@object.header.frame_id = "base_link";
        attColObj.@object.id = m_ColisionObject.id;
        attColObj.@object.operation = CollisionObjectMsg.ADD;
        attColObj.link_name = "tool0";

        m_ROSPublisher.PublishAttachCollisionObject(attColObj);
    }

    public void DetachCollisionObject()
    {
        m_ROSPublisher.PublishDetachCollisionObject(m_ColisionObject);
    }*/
}