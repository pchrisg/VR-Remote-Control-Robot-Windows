using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.ChrisUr5Moveit;
using RosMessageTypes.Robotiq3fGripperArticulated;
using RosMessageTypes.Std;
using System.Collections;
using System;

public class ROSPublisher : MonoBehaviour
{
    //Change to false if using real robotiq gripper
    public bool m_Gazebo = true;

    [Header("ROS Topics")]
    [SerializeField] private readonly string m_ResetPoseTopic = "chris_reset_pose";
    [SerializeField] private readonly string m_PlanTrajTopic = "chris_plan_trajectory";
    [SerializeField] private readonly string m_ExecPlanTopic = "chris_execute_plan";
    [SerializeField] private readonly string m_MoveArmTopic = "chris_move_arm";
    [SerializeField] private readonly string m_EmergencyStopTopic = "chris_emergency_stop";
    [SerializeField] private readonly string m_SdofTranslateTopic = "chris_sdof_translate";
    [SerializeField] private readonly string m_AddCollisionObjectTopic = "chris_add_collision_object";
    [SerializeField] private readonly string m_RemoveCollisionObjectTopic = "chris_remove_collision_object";
    [SerializeField] private readonly string m_AttachCollisionObjectTopic = "chris_attach_collision_object";
    [SerializeField] private readonly string m_DetachCollisionObjectTopic = "chris_detach_collision_object";
    [SerializeField] private string m_RoboticSqueezeTopic = string.Empty;

    private Transform m_Manipulator = null;
    private PlanningRobot m_PlanningRobot = null;

    private ROSConnection m_Ros = null;
    public bool locked = false;

    private void Awake()
    {
        if (m_Gazebo)
            m_RoboticSqueezeTopic = "left_hand/command";
        else
            m_RoboticSqueezeTopic = "Robotiq3FGripperRobotOutput";

        m_Manipulator = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("Pose").transform;
        m_PlanningRobot = GameObject.FindGameObjectWithTag("PlanningRobot").GetComponent<PlanningRobot>();

        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();

        // Register ROS communication
        m_Ros.RegisterPublisher<EmptyMsg>(m_ResetPoseTopic);
        m_Ros.RegisterRosService<TrajectoryPlannerServiceRequest, TrajectoryPlannerServiceResponse>(m_PlanTrajTopic);
        m_Ros.RegisterPublisher<RobotTrajectoryMsg>(m_ExecPlanTopic);
        m_Ros.RegisterPublisher<PoseMsg>(m_MoveArmTopic);
        m_Ros.RegisterPublisher<EmptyMsg>(m_EmergencyStopTopic);
        m_Ros.RegisterPublisher<SdofTranslationMsg>(m_SdofTranslateTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_AddCollisionObjectTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_RemoveCollisionObjectTopic);
        m_Ros.RegisterPublisher<AttachedCollisionObjectMsg>(m_AttachCollisionObjectTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_DetachCollisionObjectTopic);

        m_Ros.RegisterPublisher<Robotiq3FGripperRobotOutputMsg>(m_RoboticSqueezeTopic);
    }

    private void Start()
    {
        Invoke("RemoveAnyCollisionObjects", 0.5f);
    }

    private void RemoveAnyCollisionObjects()
    {
        CollisionObjectMsg colobjmsg = new CollisionObjectMsg();
        colobjmsg.id = "-1";
        PublishAddCollisionObject(colobjmsg);
    }

    private void OnDestroy()
    {
        m_Ros.Disconnect();
    }

    public void PublishResetPose()
    {
        EmptyMsg msg = new EmptyMsg();

        m_Ros.Publish(m_ResetPoseTopic, msg);
    }

    public void PublishTrajectoryRequest()
    {
        var request = new TrajectoryPlannerServiceRequest();

        request.destination = new PoseMsg
        {
            position = m_Manipulator.position.To<FLU>(),
            orientation = m_Manipulator.rotation.To<FLU>()
        };
        m_Ros.SendServiceMessage<TrajectoryPlannerServiceResponse>(m_PlanTrajTopic, request, HandleTrajectoryResponse);
    }

    public void HandleTrajectoryResponse(TrajectoryPlannerServiceResponse response)
    {
        if (response.trajectory != null)
            m_PlanningRobot.DisplayTrajectory(response.trajectory);
    }

    public void PublishExecutePlan(RobotTrajectoryMsg trajectory)
    {
        m_Ros.Publish(m_ExecPlanTopic, trajectory);
    }

    public void PublishMoveArm()
    {
        if (!locked)
        {
            var destination = new PoseMsg
            {
                position = m_Manipulator.position.To<FLU>(),
                orientation = m_Manipulator.rotation.To<FLU>()
            };
            m_Ros.Publish(m_MoveArmTopic, destination);
        }
    }

    public void PublishEmergencyStop()
    {
        StartCoroutine(Lock());


    }

    IEnumerator Lock()
    {
        locked = true;

        EmptyMsg msg = new EmptyMsg();
        m_Ros.Publish(m_EmergencyStopTopic, msg);
        m_Ros.Publish(m_EmergencyStopTopic, msg);
        yield return new WaitForSeconds(4.0f);

        locked = false;
        yield return null;
    }

    public void PublishConstrainedMovement()
    {
        var dest = new PoseMsg
        {
            position = m_Manipulator.position.To<FLU>(),
            orientation = m_Manipulator.rotation.To<FLU>()
        };
        var ocm = new OrientationConstraintMsg
        {
            link_name = "tool0",
            orientation = m_Manipulator.rotation.To<FLU>(),
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

    public void PublishAddCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_AddCollisionObjectTopic, collisionObject);
    }

    public void PublishRemoveCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_RemoveCollisionObjectTopic, collisionObject);
    }

    public void PublishAttachCollisionObject(AttachedCollisionObjectMsg attColObj)
    {
        m_Ros.Publish(m_AttachCollisionObjectTopic, attColObj);
    }

    public void PublishDetachCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_DetachCollisionObjectTopic, collisionObject);
    }

    public void PublishRobotiqSqueeze(Robotiq3FGripperRobotOutputMsg outputMessage)
    {
        m_Ros.Publish(m_RoboticSqueezeTopic, outputMessage);
    }
}