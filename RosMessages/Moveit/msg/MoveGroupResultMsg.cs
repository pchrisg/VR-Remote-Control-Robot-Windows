//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Moveit
{
    [Serializable]
    public class MoveGroupResultMsg : Message
    {
        public const string k_RosMessageName = "moveit_msgs/MoveGroupResult";
        public override string RosMessageName => k_RosMessageName;

        //  ====== DO NOT MODIFY! AUTOGENERATED FROM AN ACTION DEFINITION ======
        //  An error code reflecting what went wrong
        public MoveItErrorCodesMsg error_code;
        //  The full starting state of the robot at the start of the trajectory
        public RobotStateMsg trajectory_start;
        //  The trajectory that moved group produced for execution
        public RobotTrajectoryMsg planned_trajectory;
        //  The trace of the trajectory recorded during execution
        public RobotTrajectoryMsg executed_trajectory;
        //  The amount of time it took to complete the motion plan
        public double planning_time;

        public MoveGroupResultMsg()
        {
            this.error_code = new MoveItErrorCodesMsg();
            this.trajectory_start = new RobotStateMsg();
            this.planned_trajectory = new RobotTrajectoryMsg();
            this.executed_trajectory = new RobotTrajectoryMsg();
            this.planning_time = 0.0;
        }

        public MoveGroupResultMsg(MoveItErrorCodesMsg error_code, RobotStateMsg trajectory_start, RobotTrajectoryMsg planned_trajectory, RobotTrajectoryMsg executed_trajectory, double planning_time)
        {
            this.error_code = error_code;
            this.trajectory_start = trajectory_start;
            this.planned_trajectory = planned_trajectory;
            this.executed_trajectory = executed_trajectory;
            this.planning_time = planning_time;
        }

        public static MoveGroupResultMsg Deserialize(MessageDeserializer deserializer) => new MoveGroupResultMsg(deserializer);

        private MoveGroupResultMsg(MessageDeserializer deserializer)
        {
            this.error_code = MoveItErrorCodesMsg.Deserialize(deserializer);
            this.trajectory_start = RobotStateMsg.Deserialize(deserializer);
            this.planned_trajectory = RobotTrajectoryMsg.Deserialize(deserializer);
            this.executed_trajectory = RobotTrajectoryMsg.Deserialize(deserializer);
            deserializer.Read(out this.planning_time);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.error_code);
            serializer.Write(this.trajectory_start);
            serializer.Write(this.planned_trajectory);
            serializer.Write(this.executed_trajectory);
            serializer.Write(this.planning_time);
        }

        public override string ToString()
        {
            return "MoveGroupResultMsg: " +
            "\nerror_code: " + error_code.ToString() +
            "\ntrajectory_start: " + trajectory_start.ToString() +
            "\nplanned_trajectory: " + planned_trajectory.ToString() +
            "\nexecuted_trajectory: " + executed_trajectory.ToString() +
            "\nplanning_time: " + planning_time.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
