using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.ChrisUr5Moveit;
using RosMessageTypes.Shape;

public class ROSPublisher : MonoBehaviour
{
    // ROS Connector
    private ROSConnection m_Ros = null;

    [Header("Scene Objects")]
    // Variables required for ROS communication
    [SerializeField] private GameObject m_Manipulator = null;
    [SerializeField] private PlanningRobot m_PlanningRobot = null;

    [Header("ROS Topics")]
    [SerializeField] private string m_PlanTrajTopic = "chris_plan_trajectory";
    [SerializeField] private string m_ExecPlanTopic = "chris_execute_plan";
    [SerializeField] private string m_MoveArmTopic = "chris_move_arm";
    [SerializeField] private string m_SdofTranslateTopic = "chris_sdof_translate";
    [SerializeField] private string m_AddCollisionObjectTopic = "chris_add_collision_object";
    [SerializeField] private string m_RemoveCollisionObjectTopic = "chris_remove_collision_object";
    [SerializeField] private string m_GameExitTopic = "chris_game_exit";

    private void Awake()
    {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();

        // Register ROS communication
        m_Ros.RegisterRosService<TrajectoryPlannerServiceRequest, TrajectoryPlannerServiceResponse>(m_PlanTrajTopic);
        m_Ros.RegisterPublisher<RobotTrajectoryMsg>(m_ExecPlanTopic);
        m_Ros.RegisterPublisher<PoseMsg>(m_MoveArmTopic);
        m_Ros.RegisterPublisher<SdofTranslationMsg>(m_SdofTranslateTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_AddCollisionObjectTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_RemoveCollisionObjectTopic);
        m_Ros.RegisterPublisher<EmptyMsg>(m_GameExitTopic);
    }

    public void OnDestroy()
    {
        PublishGameExit();
        m_Ros.Disconnect();
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
            m_PlanningRobot.DisplayTrajectory(response.trajectory);
        else
            Debug.LogError("No trajectory returned from MoverService.");
    }

    public void PublishExecutePlan(RobotTrajectoryMsg trajectory)
    {
        m_Ros.Publish(m_ExecPlanTopic, trajectory);
    }

    public void PublishMoveArm()
    {
        if(m_PlanningRobot.isPlanning)
        {
            PublishTrajectoryRequest();
        }
        else
        {
            var destination = new PoseMsg
            {
                position = m_Manipulator.transform.position.To<FLU>(),
                orientation = m_Manipulator.transform.rotation.To<FLU>()
            };
            m_Ros.Publish(m_MoveArmTopic, destination);
        }
    }

    public void PublishConstrainedMovement()
    {
        if(m_PlanningRobot.isPlanning)
        {
            PublishTrajectoryRequest();
        }
        else
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
                absolute_x_axis_tolerance = 0.01f,
                absolute_y_axis_tolerance = 0.01f,
                absolute_z_axis_tolerance = 0.01f,
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
    }

    public void PublishCreateCollisionObject(CollisionObjectMsg collisionObject)
    {
        /*var collision_object = new CollisionObjectMsg();
        collision_object.header.frame_id = "base_link";
        collision_object.id = id;

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
        */

        m_Ros.Publish(m_AddCollisionObjectTopic, collisionObject);
    }

    public void PublishRemoveCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_RemoveCollisionObjectTopic, collisionObject);
    }

    private void PublishGameExit()
    {
        m_Ros.Publish(m_GameExitTopic, new EmptyMsg());
    }
}