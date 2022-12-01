using UnityEngine;
using System.Collections;

public class UR5Controller : MonoBehaviour {

    public GameObject ur5;
    public float shoulder_pan_joint, shoulder_lift_joint, elbow_joint, wrist_1_joint, wrist_2_joint, wrist_3_joint;

    private Transform[] joint = new Transform[6];

    // Use this for initialization
    void Start () {
        initializeJoints();
	}

	// Update is called once per frame
	void LateUpdate () {
        SetPose();
	}

    void SetPose()
    {
        joint[0].localEulerAngles = new Vector3(0.0f, shoulder_pan_joint, 0.0f);
        joint[1].localEulerAngles = new Vector3(0.0f, 0.0f, shoulder_lift_joint);
        joint[2].localEulerAngles = new Vector3(0.0f, 0.0f, elbow_joint);
        joint[3].localEulerAngles = new Vector3(0.0f, 0.0f, wrist_1_joint);
        joint[4].localEulerAngles = new Vector3(0.0f, wrist_2_joint, 0.0f);
        joint[5].localEulerAngles = new Vector3(0.0f, 0.0f, wrist_3_joint);
    }

    // Create the list of GameObjects that represent each joint of the robot
    void initializeJoints() {
        var RobotJoint = ur5.GetComponentsInChildren<Transform>();
        for (int i = 0; i < RobotJoint.Length; i++) {
            if (RobotJoint[i].name == "base") {
                joint[0] = RobotJoint[i];
            }
            else if (RobotJoint[i].name == "shoulder") {
                joint[1] = RobotJoint[i];
            }
            else if (RobotJoint[i].name == "elbow") {
                joint[2] = RobotJoint[i];
            }
            else if (RobotJoint[i].name == "wrist_1") {
                joint[3] = RobotJoint[i];
            }
            else if (RobotJoint[i].name == "wrist_2") {
                joint[4] = RobotJoint[i];
            }
            else if (RobotJoint[i].name == "wrist_3") {
                joint[5] = RobotJoint[i];
            }
        }

        shoulder_pan_joint = -92;
        shoulder_lift_joint = -99;
        elbow_joint = -126;
        wrist_1_joint = -46;
        wrist_2_joint = 91;
        wrist_3_joint = -2;
    }
}
