using RosMessageTypes.Moveit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planner : MonoBehaviour
{
    [SerializeField] private GameObject m_UR5;
    [SerializeField] private GameObject m_PlanningRobotPrefab;
    [SerializeField] private ManipulatorPublisher m_rosPublisher;
    private GameObject m_PlanningRobot;

    [HideInInspector] public bool isPlanning;
    [HideInInspector] public RobotTrajectoryMsg m_Trajectory;

    private void Awake()
    {
        isPlanning = false;
        m_Trajectory = null;
    }

    public void SetUpPlanningRobot()
    {
        if (m_PlanningRobot == null)
        {
            isPlanning = true;
            m_PlanningRobot = Instantiate(m_PlanningRobotPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            m_PlanningRobot.GetComponent<PlanningRobot>().SetRobotPose(m_UR5);
        }
    }

    public void DestroyPlanningRobot()
    {
        Destroy(m_PlanningRobot);
        isPlanning = false;
        m_Trajectory = null;
    }

    public void DisplayTrajectory(RobotTrajectoryMsg trajectory)
    {
        m_Trajectory = trajectory;
        m_PlanningRobot.GetComponent<PlanningRobot>().MoveTo(m_Trajectory, m_UR5);
    }

    public void ExecuteTrajectory()
    {
        if(m_Trajectory != null)
        {
            m_PlanningRobot.GetComponent<PlanningRobot>().StopPlanning();
            m_rosPublisher.PublishExecutePlan(m_Trajectory);
            m_Trajectory = null;
        }
    }
}