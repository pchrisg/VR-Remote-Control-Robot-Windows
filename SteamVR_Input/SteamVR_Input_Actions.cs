//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Valve.VR
{
    using System;
    using UnityEngine;
    
    
    public partial class SteamVR_Actions
    {
        
        private static SteamVR_Action_Boolean p_default_GrabGrip;
        
        private static SteamVR_Action_Boolean p_default_GrabTrigger;
        
        private static SteamVR_Action_Single p_default_SqueezeTrigger;
        
        private static SteamVR_Action_Boolean p_default_TouchTrackpad;
        
        private static SteamVR_Action_Vector2 p_default_TouchPosition;
        
        private static SteamVR_Action_Boolean p_default_TouchRight;
        
        private static SteamVR_Action_Boolean p_default_TouchLeft;
        
        private static SteamVR_Action_Pose p_default_Pose;
        
        private static SteamVR_Action_Skeleton p_default_SkeletonRightHand;
        
        private static SteamVR_Action_Skeleton p_default_SkeletonLeftHand;
        
        private static SteamVR_Action_Boolean p_default_HeadsetOnHead;
        
        private static SteamVR_Action_Vibration p_default_Haptic;
        
        public static SteamVR_Action_Boolean default_GrabGrip
        {
            get
            {
                return SteamVR_Actions.p_default_GrabGrip.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Boolean default_GrabTrigger
        {
            get
            {
                return SteamVR_Actions.p_default_GrabTrigger.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Single default_SqueezeTrigger
        {
            get
            {
                return SteamVR_Actions.p_default_SqueezeTrigger.GetCopy<SteamVR_Action_Single>();
            }
        }
        
        public static SteamVR_Action_Boolean default_TouchTrackpad
        {
            get
            {
                return SteamVR_Actions.p_default_TouchTrackpad.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Vector2 default_TouchPosition
        {
            get
            {
                return SteamVR_Actions.p_default_TouchPosition.GetCopy<SteamVR_Action_Vector2>();
            }
        }
        
        public static SteamVR_Action_Boolean default_TouchRight
        {
            get
            {
                return SteamVR_Actions.p_default_TouchRight.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Boolean default_TouchLeft
        {
            get
            {
                return SteamVR_Actions.p_default_TouchLeft.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Pose default_Pose
        {
            get
            {
                return SteamVR_Actions.p_default_Pose.GetCopy<SteamVR_Action_Pose>();
            }
        }
        
        public static SteamVR_Action_Skeleton default_SkeletonRightHand
        {
            get
            {
                return SteamVR_Actions.p_default_SkeletonRightHand.GetCopy<SteamVR_Action_Skeleton>();
            }
        }
        
        public static SteamVR_Action_Skeleton default_SkeletonLeftHand
        {
            get
            {
                return SteamVR_Actions.p_default_SkeletonLeftHand.GetCopy<SteamVR_Action_Skeleton>();
            }
        }
        
        public static SteamVR_Action_Boolean default_HeadsetOnHead
        {
            get
            {
                return SteamVR_Actions.p_default_HeadsetOnHead.GetCopy<SteamVR_Action_Boolean>();
            }
        }
        
        public static SteamVR_Action_Vibration default_Haptic
        {
            get
            {
                return SteamVR_Actions.p_default_Haptic.GetCopy<SteamVR_Action_Vibration>();
            }
        }
        
        private static void InitializeActionArrays()
        {
            Valve.VR.SteamVR_Input.actions = new Valve.VR.SteamVR_Action[] {
                    SteamVR_Actions.default_GrabGrip,
                    SteamVR_Actions.default_GrabTrigger,
                    SteamVR_Actions.default_SqueezeTrigger,
                    SteamVR_Actions.default_TouchTrackpad,
                    SteamVR_Actions.default_TouchPosition,
                    SteamVR_Actions.default_TouchRight,
                    SteamVR_Actions.default_TouchLeft,
                    SteamVR_Actions.default_Pose,
                    SteamVR_Actions.default_SkeletonRightHand,
                    SteamVR_Actions.default_SkeletonLeftHand,
                    SteamVR_Actions.default_HeadsetOnHead,
                    SteamVR_Actions.default_Haptic};
            Valve.VR.SteamVR_Input.actionsIn = new Valve.VR.ISteamVR_Action_In[] {
                    SteamVR_Actions.default_GrabGrip,
                    SteamVR_Actions.default_GrabTrigger,
                    SteamVR_Actions.default_SqueezeTrigger,
                    SteamVR_Actions.default_TouchTrackpad,
                    SteamVR_Actions.default_TouchPosition,
                    SteamVR_Actions.default_TouchRight,
                    SteamVR_Actions.default_TouchLeft,
                    SteamVR_Actions.default_Pose,
                    SteamVR_Actions.default_SkeletonRightHand,
                    SteamVR_Actions.default_SkeletonLeftHand,
                    SteamVR_Actions.default_HeadsetOnHead};
            Valve.VR.SteamVR_Input.actionsOut = new Valve.VR.ISteamVR_Action_Out[] {
                    SteamVR_Actions.default_Haptic};
            Valve.VR.SteamVR_Input.actionsVibration = new Valve.VR.SteamVR_Action_Vibration[] {
                    SteamVR_Actions.default_Haptic};
            Valve.VR.SteamVR_Input.actionsPose = new Valve.VR.SteamVR_Action_Pose[] {
                    SteamVR_Actions.default_Pose};
            Valve.VR.SteamVR_Input.actionsBoolean = new Valve.VR.SteamVR_Action_Boolean[] {
                    SteamVR_Actions.default_GrabGrip,
                    SteamVR_Actions.default_GrabTrigger,
                    SteamVR_Actions.default_TouchTrackpad,
                    SteamVR_Actions.default_TouchRight,
                    SteamVR_Actions.default_TouchLeft,
                    SteamVR_Actions.default_HeadsetOnHead};
            Valve.VR.SteamVR_Input.actionsSingle = new Valve.VR.SteamVR_Action_Single[] {
                    SteamVR_Actions.default_SqueezeTrigger};
            Valve.VR.SteamVR_Input.actionsVector2 = new Valve.VR.SteamVR_Action_Vector2[] {
                    SteamVR_Actions.default_TouchPosition};
            Valve.VR.SteamVR_Input.actionsVector3 = new Valve.VR.SteamVR_Action_Vector3[0];
            Valve.VR.SteamVR_Input.actionsSkeleton = new Valve.VR.SteamVR_Action_Skeleton[] {
                    SteamVR_Actions.default_SkeletonRightHand,
                    SteamVR_Actions.default_SkeletonLeftHand};
            Valve.VR.SteamVR_Input.actionsNonPoseNonSkeletonIn = new Valve.VR.ISteamVR_Action_In[] {
                    SteamVR_Actions.default_GrabGrip,
                    SteamVR_Actions.default_GrabTrigger,
                    SteamVR_Actions.default_SqueezeTrigger,
                    SteamVR_Actions.default_TouchTrackpad,
                    SteamVR_Actions.default_TouchPosition,
                    SteamVR_Actions.default_TouchRight,
                    SteamVR_Actions.default_TouchLeft,
                    SteamVR_Actions.default_HeadsetOnHead};
        }
        
        private static void PreInitActions()
        {
            SteamVR_Actions.p_default_GrabGrip = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/GrabGrip")));
            SteamVR_Actions.p_default_GrabTrigger = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/GrabTrigger")));
            SteamVR_Actions.p_default_SqueezeTrigger = ((SteamVR_Action_Single)(SteamVR_Action.Create<SteamVR_Action_Single>("/actions/default/in/SqueezeTrigger")));
            SteamVR_Actions.p_default_TouchTrackpad = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/TouchTrackpad")));
            SteamVR_Actions.p_default_TouchPosition = ((SteamVR_Action_Vector2)(SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/default/in/TouchPosition")));
            SteamVR_Actions.p_default_TouchRight = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/TouchRight")));
            SteamVR_Actions.p_default_TouchLeft = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/TouchLeft")));
            SteamVR_Actions.p_default_Pose = ((SteamVR_Action_Pose)(SteamVR_Action.Create<SteamVR_Action_Pose>("/actions/default/in/Pose")));
            SteamVR_Actions.p_default_SkeletonRightHand = ((SteamVR_Action_Skeleton)(SteamVR_Action.Create<SteamVR_Action_Skeleton>("/actions/default/in/SkeletonRightHand")));
            SteamVR_Actions.p_default_SkeletonLeftHand = ((SteamVR_Action_Skeleton)(SteamVR_Action.Create<SteamVR_Action_Skeleton>("/actions/default/in/SkeletonLeftHand")));
            SteamVR_Actions.p_default_HeadsetOnHead = ((SteamVR_Action_Boolean)(SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/default/in/HeadsetOnHead")));
            SteamVR_Actions.p_default_Haptic = ((SteamVR_Action_Vibration)(SteamVR_Action.Create<SteamVR_Action_Vibration>("/actions/default/out/Haptic")));
        }
    }
}
