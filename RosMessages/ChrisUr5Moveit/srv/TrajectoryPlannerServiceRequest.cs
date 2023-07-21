//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.ChrisUr5Moveit
{
    [Serializable]
    public class TrajectoryPlannerServiceRequest : Message
    {
        public const string k_RosMessageName = "chris_ur5_moveit/TrajectoryPlannerService";
        public override string RosMessageName => k_RosMessageName;

        public Geometry.PoseMsg start;
        public Geometry.PoseMsg destination;

        public TrajectoryPlannerServiceRequest()
        {
            this.start = new Geometry.PoseMsg();
            this.destination = new Geometry.PoseMsg();
        }

        public TrajectoryPlannerServiceRequest(Geometry.PoseMsg start, Geometry.PoseMsg destination)
        {
            this.start = start;
            this.destination = destination;
        }

        public static TrajectoryPlannerServiceRequest Deserialize(MessageDeserializer deserializer) => new TrajectoryPlannerServiceRequest(deserializer);

        private TrajectoryPlannerServiceRequest(MessageDeserializer deserializer)
        {
            this.start = Geometry.PoseMsg.Deserialize(deserializer);
            this.destination = Geometry.PoseMsg.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.start);
            serializer.Write(this.destination);
        }

        public override string ToString()
        {
            return "TrajectoryPlannerServiceRequest: " +
            "\nstart: " + start.ToString() +
            "\ndestination: " + destination.ToString();
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
