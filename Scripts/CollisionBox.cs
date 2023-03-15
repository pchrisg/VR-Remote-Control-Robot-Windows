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

    public void PublishCollisionBox(string id)
    {
        m_ColisionBox = new CollisionObjectMsg();
        m_ColisionBox.header.frame_id = "base_link";
        m_ColisionBox.id = id;

        var primitive = new SolidPrimitiveMsg();
        primitive.type = SolidPrimitiveMsg.BOX;
        Array.Resize(ref primitive.dimensions, 3);
        primitive.dimensions[SolidPrimitiveMsg.BOX_X] = gameObject.transform.lossyScale.z;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Y] = gameObject.transform.lossyScale.x;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Z] = gameObject.transform.lossyScale.y;

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

        m_ROSPublisher.PublishCreateCollisionObject(m_ColisionBox);
    }

    public void OnDestroy()
    {
        m_ROSPublisher.PublishRemoveCollisionObject(m_ColisionBox);
    }
}
