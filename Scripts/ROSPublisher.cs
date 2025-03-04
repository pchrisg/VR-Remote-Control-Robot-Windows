using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Moveit;
using RosMessageTypes.ChrisUr5Moveit;
using RosMessageTypes.Robotiq3fGripperArticulated;
using RosMessageTypes.Std;
using System.Collections;
using TMPro;

public class ROSPublisher : MonoBehaviour
{
    //Change to false if using real robotiq gripper
    public bool m_Gazebo = true;

    [Header("Lock Timer")]
    [SerializeField] private GameObject m_HUD = null;
    TextMeshPro m_Text = null;

    private readonly string m_ResetPoseTopic = "chris_reset_pose";
    private readonly string m_PlanTrajTopic = "chris_plan_trajectory";
    private readonly string m_ExecPlanTopic = "chris_execute_plan";
    private readonly string m_StopArmTopic = "chris_stop_arm";
    private readonly string m_MoveArmTopic = "chris_move_arm";
    private readonly string m_EmergencyStopTopic = "chris_emergency_stop";
    private readonly string m_AddCollisionObjectTopic = "chris_add_collision_object";
    private readonly string m_RemoveCollisionObjectTopic = "chris_remove_collision_object";

    [SerializeField] private string m_RoboticSqueezeTopic = string.Empty;

    private Transform m_ManipulatorPose = null;

    private ROSConnection m_Ros = null;

    public float m_TimePenalty = 2.0f;
    private bool m_isLocked = false;
    private Coroutine m_ActiveCoroutine = null;

    private void Awake()
    {
        if (m_Gazebo)
            m_RoboticSqueezeTopic = "left_hand/command";
        else
            m_RoboticSqueezeTopic = "Robotiq3FGripperRobotOutput";

        m_Text = m_HUD.GetComponentInChildren<TextMeshPro>();
        m_Text.GetComponent<Renderer>().material.color = new(255.0f, 0.0f, 0.0f);

        m_ManipulatorPose = GameObject.FindGameObjectWithTag("Manipulator").transform.Find("Pose");

        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();

        // Register ROS communication
        m_Ros.RegisterRosService<TrajectoryPlannerServiceRequest, TrajectoryPlannerServiceResponse>(m_PlanTrajTopic);

        m_Ros.RegisterPublisher<EmptyMsg>(m_ResetPoseTopic);
        m_Ros.RegisterPublisher<RobotTrajectoryMsg>(m_ExecPlanTopic);
        m_Ros.RegisterPublisher<EmptyMsg>(m_StopArmTopic);
        m_Ros.RegisterPublisher<PoseMsg>(m_MoveArmTopic);
        m_Ros.RegisterPublisher<BoolMsg>(m_EmergencyStopTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_AddCollisionObjectTopic);
        m_Ros.RegisterPublisher<CollisionObjectMsg>(m_RemoveCollisionObjectTopic);
        m_Ros.RegisterPublisher<Robotiq3FGripperRobotOutputMsg>(m_RoboticSqueezeTopic);
    }

    private void Start()
    {
        Invoke(nameof(RemoveAnyCollisionObjects), 0.5f);
    }

    private void OnDestroy()
    {
        m_Ros.Disconnect();
    }

    private void RemoveAnyCollisionObjects()
    {
        CollisionObjectMsg colobjmsg = new()
        {
            id = "-1"
        };
        PublishAddCollisionObject(colobjmsg);
    }

    public void PublishResetPose()
    {
        Robotiq3FGripperRobotOutputMsg outputMessage = new()
        {
            rACT = 1,
            rPRA = (byte)(0.0f)
        };

        PublishRobotiqSqueeze(outputMessage);
        PublishRobotiqSqueeze(outputMessage);

        EmptyMsg msg = new();

        m_Ros.Publish(m_ResetPoseTopic, msg);
    }

    public void PublishTrajectoryRequest(Vector3 startPos, Quaternion startRot, Vector3 destPos, Quaternion destRot)
    {
        TrajectoryPlannerServiceRequest request = new()
        {
            start = new PoseMsg
            {
                position = startPos.To<FLU>(),
                orientation = startRot.To<FLU>()
            },

            destination = new PoseMsg
            {
                position = destPos.To<FLU>(),
                orientation = destRot.To<FLU>()
            }
        };
        m_Ros.SendServiceMessage<TrajectoryPlannerServiceResponse>(m_PlanTrajTopic, request, HandleTrajectoryResponse);
    }

    public void HandleTrajectoryResponse(TrajectoryPlannerServiceResponse response)
    {
        //if (response.trajectory != null)
        //    m_PlanningRobot.DisplayTrajectory(response.trajectory);
    }

    public void PublishExecutePlan(RobotTrajectoryMsg trajectory)
    {
        m_Ros.Publish(m_ExecPlanTopic, trajectory);
    }

    public void PublishStopArm()
    {
        EmptyMsg msg = new();
        m_Ros.Publish(m_StopArmTopic, msg);
    }

    public void PublishMoveArm()
    {
        if (!m_isLocked)
        {
            var destination = new PoseMsg
            {
                position = m_ManipulatorPose.position.To<FLU>(),
                orientation = m_ManipulatorPose.rotation.To<FLU>()
            };
            m_Ros.Publish(m_MoveArmTopic, destination);
        }
    }

    public void PublishEmergencyStop()
    {
        m_ActiveCoroutine ??= StartCoroutine(PublishEmergencyStopCoroutine());
    }

    IEnumerator PublishEmergencyStopCoroutine()
    {
        m_isLocked = true;
        print("locked");

        BoolMsg msg = new()
        {
            data = true
        };
        m_Ros.Publish(m_EmergencyStopTopic, msg);

        yield return StartCoroutine(UpdateTimeCoroutine());

        m_Text.text = "";

        msg.data = false;
        m_Ros.Publish(m_EmergencyStopTopic, msg);

        m_isLocked = false;
        print("unlocked");

        m_ActiveCoroutine = null;
    }

    IEnumerator UpdateTimeCoroutine()
    {
        float timer = m_TimePenalty;
        while (timer >= 0)
        {
            timer -= Time.deltaTime;

            float seconds = Mathf.FloorToInt(timer % 60);
            float milliseconds = (timer - Mathf.FloorToInt(timer)) * 1000;

            // Update the UI text
            m_Text.text = string.Format("{0:00}:{1:000}", seconds, milliseconds);

            yield return null;
        }
    }

    public bool IsLocked()
    {
        return m_isLocked;
    }

    public void PublishAddCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_AddCollisionObjectTopic, collisionObject);
    }

    public void PublishRemoveCollisionObject(CollisionObjectMsg collisionObject)
    {
        m_Ros.Publish(m_RemoveCollisionObjectTopic, collisionObject);
    }

    public void PublishRobotiqSqueeze(Robotiq3FGripperRobotOutputMsg outputMessage)
    {
        if(!m_isLocked)
        {
            m_Ros.Publish(m_RoboticSqueezeTopic, outputMessage);
        }
            
    }
}