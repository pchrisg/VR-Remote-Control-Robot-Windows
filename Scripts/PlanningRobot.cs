using RosMessageTypes.ChrisUr5Moveit;
using System.Collections;
using UnityEngine;
using System.Linq;
using RosMessageTypes.Moveit;
using System;

public class PlanningRobot : MonoBehaviour
{
    private const int k_NumJoints = 6;
    private ArticulationBody[] m_Joints;
    private bool m_StillPlanning;

    private void Start()
    {
        m_StillPlanning = true;
    }

    public void SetRobotPose(GameObject ur5)
    {
        m_Joints = new ArticulationBody[k_NumJoints];
        var linkName = string.Empty;
        for (var joint = 0; joint < k_NumJoints; joint++)
        {
            linkName += JointStateSubscriber.m_LinkNames[joint];
            m_Joints[joint] = this.transform.Find(linkName).GetComponent<ArticulationBody>();

            var joint1XDrive = m_Joints[joint].xDrive;
            joint1XDrive.target = ur5.transform.Find(linkName).GetComponent<ArticulationBody>().xDrive.target;
            m_Joints[joint].xDrive = joint1XDrive;
        }
    }

    public void MoveTo(RobotTrajectoryMsg trajectory, GameObject ur5)
    {
        StopAllCoroutines();
        m_StillPlanning = false;
        StartCoroutine(ExecuteTrajectories(trajectory, ur5));
    }

    public void StopPlanning()
    {
        m_StillPlanning = false;
    }

    IEnumerator ExecuteTrajectories(RobotTrajectoryMsg trajectory, GameObject ur5)
    {
        if (trajectory != null)
        {
            m_StillPlanning = true;
            while(m_StillPlanning)
            {
                var linkName = string.Empty;
                for (var joint = 0; joint < k_NumJoints; joint++)
                {
                    linkName += JointStateSubscriber.m_LinkNames[joint];
                    var joint1XDrive = m_Joints[joint].xDrive;
                    joint1XDrive.target = ur5.transform.Find(linkName).GetComponent<ArticulationBody>().xDrive.target;
                    m_Joints[joint].xDrive = joint1XDrive;
                }
                yield return new WaitForSeconds(0.1f);

                // For every robot pose in trajectory plan
                foreach (var t in trajectory.joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    // Set the joint values for every joint
                    for (var joint = 0; joint < k_NumJoints; joint++)
                    {
                        var joint1XDrive = m_Joints[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_Joints[joint].xDrive = joint1XDrive;
                    }
                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(0.1f);
                }
                // Wait for the robot to achieve the final pose from joint assignment
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}