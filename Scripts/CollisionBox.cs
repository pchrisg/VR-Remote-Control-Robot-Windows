using System;
using UnityEngine;
using RosMessageTypes.Moveit;
using RosMessageTypes.Shape;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class CollisionBox : MonoBehaviour
{
    private ROSPublisher m_ROSPublisher = null;
    private CollisionObjectMsg m_ColisionBox = null;

    void Awake()
    {
        m_ROSPublisher = GameObject.FindGameObjectWithTag("ROS").GetComponent<ROSPublisher>();
    }

    public void OnDestroy()
    {
        RemoveCollisionBox();
    }

    public String GetID()
    {
        return m_ColisionBox.id;
    }

    public void AddCollisionBox(string id)
    {
        SetParameters();
        m_ColisionBox.id = id;
        m_ColisionBox.header.frame_id = "base_link";

        m_ROSPublisher.PublishAddCollisionObject(m_ColisionBox);
    }

    private void SetParameters()
    {
        m_ColisionBox = new CollisionObjectMsg();

        var primitive = new SolidPrimitiveMsg();
        primitive.type = SolidPrimitiveMsg.BOX;
        Array.Resize(ref primitive.dimensions, 3);

        float scale_x = gameObject.transform.lossyScale.x * gameObject.GetComponent<BoxCollider>().size.x;
        float scale_y = gameObject.transform.lossyScale.y * gameObject.GetComponent<BoxCollider>().size.y;
        float scale_z = gameObject.transform.lossyScale.z * gameObject.GetComponent<BoxCollider>().size.z;

        primitive.dimensions[SolidPrimitiveMsg.BOX_X] = scale_z;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Y] = scale_x;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Z] = scale_y;

        var box_pose = new PoseMsg
        {
            position = gameObject.transform.position.To<FLU>(),
            orientation = gameObject.transform.rotation.To<FLU>()
        };

        Array.Resize(ref m_ColisionBox.primitives, 1);
        m_ColisionBox.primitives[0] = primitive;
        Array.Resize(ref m_ColisionBox.primitive_poses, 1);
        m_ColisionBox.primitive_poses[0] = box_pose;
        m_ColisionBox.operation = CollisionObjectMsg.ADD;
    }

    public void RemoveCollisionBox()
    {
        m_ROSPublisher.PublishRemoveCollisionObject(m_ColisionBox);
    }

    public void AttachCollisionBox()
    {
        m_ColisionBox.header.frame_id = "tool0";

        m_ROSPublisher.PublishAttachCollisionObject(m_ColisionBox);
    }

    public void DetachCollisionBox()
    {
        m_ROSPublisher.PublishDetachCollisionObject(m_ColisionBox);
    }
}