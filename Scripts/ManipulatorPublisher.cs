using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.ChrisUr5Moveit;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using RosMessageTypes.Shape;
using System;
using RosMessageTypes.Std;

public class ManipulatorPublisher : MonoBehaviour
{
    // ROS Connector
    private ROSConnection m_Ros;

    // Variables required for ROS communication
    [SerializeField] private GameObject m_Manipulator = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;

    [SerializeField] private string m_PlanTrajTopic = "chris_plan_trajectory";
    [SerializeField] private string m_ExecPlanTopic = "chris_execute_plan";
    [SerializeField] private string m_MoveArmTopic = "chris_move_arm";
    [SerializeField] private string m_SdofTranslateTopic = "chris_sdof_translate";
    [SerializeField] private string m_AddCollisionObjectTopic = "chris_add_collision_object";

    void Start()
    {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();

        // Register ROS communication
        m_Ros.RegisterRosService<TrajectoryPlannerServiceRequest, TrajectoryPlannerServiceResponse>(m_PlanTrajTopic);
        m_Ros.RegisterPublisher<RobotTrajectoryMsg>(m_ExecPlanTopic);
        m_Ros.RegisterPublisher<PoseMsg>(m_MoveArmTopic);
        m_Ros.RegisterPublisher<SdofTranslationMsg>(m_SdofTranslateTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_AddCollisionObjectTopic);
    }

    public void PublishTrajectoryRequest()
    {
        var request = new TrajectoryPlannerServiceRequest();

        request.destination = new PoseMsg
        {
            position = m_Manipulator.transform.position.To<FLU>(),
            orientation = m_Manipulator.transform.rotation.To<FLU>()
        };
        m_Ros.SendServiceMessage<TrajectoryPlannerServiceResponse>(m_PlanTrajTopic, request, HandleTrajectoryResponse);
    }

    public void HandleTrajectoryResponse(TrajectoryPlannerServiceResponse response)
    {
        if (response.trajectory != null)
        {
            Debug.Log("Trajectory returned.");
            m_PlanningRobot.DisplayTrajectory(response.trajectory);
        }
        else
        {
            Debug.LogError("No trajectory returned from MoverService.");
        }
    }

    public void PublishExecutePlan(RobotTrajectoryMsg trajectory)
    {
        m_Ros.Publish(m_ExecPlanTopic, trajectory);
    }

    public void PublishMoveArm()
    {
        var destination = new PoseMsg
        {
            position = m_Manipulator.transform.position.To<FLU>(),
            orientation = m_Manipulator.transform.rotation.To<FLU>()
        };
        m_Ros.Publish(m_MoveArmTopic, destination);
    }

    public void PublishConstrainedMovement()
    {
        var dest = new PoseMsg
        {
            position = m_Manipulator.transform.position.To<FLU>(),
            orientation = m_Manipulator.transform.rotation.To<FLU>()
        };
        var ocm = new OrientationConstraintMsg
        {
            link_name = "tool0",
            orientation = m_Manipulator.transform.rotation.To<FLU>(),
            absolute_x_axis_tolerance = 0.1,
            absolute_y_axis_tolerance = 0.1,
            absolute_z_axis_tolerance = 0.1,
            weight = 1.0
        };
        ocm.header.frame_id = "base_link";

        var sdofTranslation = new SdofTranslationMsg()
        {
            orientation_constraint = ocm,
            destination = dest
        };

        m_Ros.Publish(m_SdofTranslateTopic, sdofTranslation);
    }

    public void PublishCollisionObject(GameObject box)
    {
        var collision_object = new CollisionObjectMsg();
        collision_object.header.frame_id = "base_link";
        collision_object.id = "base";

        var primitive = new SolidPrimitiveMsg();
        primitive.type = SolidPrimitiveMsg.BOX;
        Array.Resize(ref primitive.dimensions, 3);
        primitive.dimensions[SolidPrimitiveMsg.BOX_X] = box.transform.lossyScale.z;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Y] = box.transform.lossyScale.x;
        primitive.dimensions[SolidPrimitiveMsg.BOX_Z] = box.transform.lossyScale.y;

        var box_pose = new PoseMsg
        {
            position = box.transform.position.To<FLU>(),
            orientation = box.transform.rotation.To<FLU>()
        };

        Array.Resize(ref collision_object.primitives, 1);
        collision_object.primitives[0] = primitive;
        Array.Resize(ref collision_object.primitive_poses, 1);
        collision_object.primitive_poses[0] = box_pose;
        collision_object.operation = CollisionObjectMsg.ADD;

        m_Ros.Publish(m_AddCollisionObjectTopic, collision_object);
    }
}