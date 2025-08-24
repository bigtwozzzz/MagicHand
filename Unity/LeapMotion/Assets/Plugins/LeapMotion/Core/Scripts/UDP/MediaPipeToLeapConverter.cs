using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity.CameraHands;

namespace Leap.Unity.CameraHands
{
    public static class MediaPipeToLeapConverter
    {
        // MediaPipe手部关键点索引定义
        private static readonly int[] FINGER_TIP_INDICES = { 4, 8, 12, 16, 20 }; // 拇指到小指指尖
        private static readonly int[] FINGER_DIP_INDICES = { 3, 7, 11, 15, 19 }; // 远端指间关节
        private static readonly int[] FINGER_PIP_INDICES = { 2, 6, 10, 14, 18 }; // 近端指间关节
        private static readonly int[] FINGER_MCP_INDICES = { 1, 5, 9, 13, 17 }; // 掌指关节
        private static readonly int WRIST_INDEX = 0;

        // 坐标转换参数
        private const float SCALE_FACTOR = 0.1f; // 缩放因子，将归一化坐标转换为米
        private const float HAND_SIZE_MULTIPLIER = 0.08f; // 手部大小倍数

        public static Frame ConvertToFrame(HandData[] handsData, Transform cameraTransform = null)
        {
            var frame = new Frame();
            frame.Hands = new List<Hand>();
            frame.Timestamp = (long)(Time.time * 1000000); // 微秒时间戳

            foreach (var handData in handsData)
            {
                var leapHand = ConvertToLeapHand(handData, cameraTransform);
                if (leapHand != null)
                {
                    frame.Hands.Add(leapHand);
                }
            }

            return frame;
        }

        public static Hand ConvertToLeapHand(HandData handData, Transform cameraTransform = null)
        {
            if (handData.landmarks == null || handData.landmarks.Length != 21)
            {
                Debug.LogWarning("Invalid hand landmarks data");
                return null;
            }

            // 确定手的左右
            bool isLeft = handData.hand_side.ToLower() == "left";
            
            // 转换关键点坐标
            Vector3[] landmarks3D = ConvertLandmarksToWorld(handData.landmarks, cameraTransform);
            
            // 创建Leap Hand对象
            var hand = new Hand();
            
            // 设置基本属性
            hand.Id = isLeft ? 1 : 2; // 简单的ID分配
            hand.IsLeft = isLeft;
            hand.IsRight = !isLeft;
            hand.Confidence = 0.8f; // 固定置信度
            
            // 计算手掌信息
            SetPalmProperties(hand, landmarks3D);
            
            // 创建手指
            CreateFingers(hand, landmarks3D, isLeft);
            
            // 设置手臂（简化处理）
            CreateArm(hand, landmarks3D[WRIST_INDEX]);
            
            return hand;
        }

        private static Vector3[] ConvertLandmarksToWorld(float[][] landmarks, Transform cameraTransform)
        {
            Vector3[] worldLandmarks = new Vector3[21];
            
            for (int i = 0; i < landmarks.Length; i++)
            {
                // MediaPipe坐标：x向右，y向下，z向前（相对于图像）
                // Unity坐标：x向右，y向上，z向前
                Vector3 localPos = new Vector3(
                    (landmarks[i][0] - 0.5f) * SCALE_FACTOR,
                    -(landmarks[i][1] - 0.5f) * SCALE_FACTOR, // 翻转Y轴
                    -landmarks[i][2] * SCALE_FACTOR * HAND_SIZE_MULTIPLIER // Z轴深度
                );
                
                // 如果有摄像头变换，应用到世界坐标
                if (cameraTransform != null)
                {
                    worldLandmarks[i] = cameraTransform.TransformPoint(localPos);
                }
                else
                {
                    worldLandmarks[i] = localPos;
                }
            }
            
            return worldLandmarks;
        }

        private static void SetPalmProperties(Hand hand, Vector3[] landmarks)
        {
            // 计算手掌中心（使用腕部和中指根部的中点）
            Vector3 wrist = landmarks[WRIST_INDEX];
            Vector3 middleBase = landmarks[FINGER_MCP_INDICES[2]];
            hand.PalmPosition = (wrist + middleBase) * 0.5f;
            
            // 计算手掌宽度（拇指根部到小指根部的距离）
            Vector3 thumbBase = landmarks[FINGER_MCP_INDICES[0]];
            Vector3 pinkyBase = landmarks[FINGER_MCP_INDICES[4]];
            hand.PalmWidth = Vector3.Distance(thumbBase, pinkyBase);
            
            // 计算手掌法向量
            Vector3 palmToMiddle = middleBase - wrist;
            Vector3 palmToThumb = thumbBase - wrist;
            hand.PalmNormal = Vector3.Cross(palmToMiddle, palmToThumb).normalized;
            
            // 计算手掌方向
            hand.Direction = palmToMiddle.normalized;
            
            // 设置手掌速度（简化为零）
            hand.PalmVelocity = Vector3.zero;
        }

        private static void CreateFingers(Hand hand, Vector3[] landmarks, bool isLeft)
        {
            hand.Fingers = new List<Finger>();
            
            for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++)
            {
                var finger = CreateFinger(fingerIndex, landmarks, isLeft);
                hand.Fingers.Add(finger);
            }
        }

        private static Finger CreateFinger(int fingerIndex, Vector3[] landmarks, bool isLeft)
        {
            var finger = new Finger();
            
            // 设置手指类型
            finger.Type = (Finger.FingerType)fingerIndex;
            finger.Id = fingerIndex;
            
            // 创建骨骼
            finger.bones = new Bone[4];
            
            if (fingerIndex == 0) // 拇指只有3个骨骼
            {
                finger.bones[0] = CreateBone(Bone.BoneType.TYPE_METACARPAL, landmarks[WRIST_INDEX], landmarks[FINGER_MCP_INDICES[0]]);
                finger.bones[1] = CreateBone(Bone.BoneType.TYPE_PROXIMAL, landmarks[FINGER_MCP_INDICES[0]], landmarks[FINGER_PIP_INDICES[0]]);
                finger.bones[2] = CreateBone(Bone.BoneType.TYPE_INTERMEDIATE, landmarks[FINGER_PIP_INDICES[0]], landmarks[FINGER_DIP_INDICES[0]]);
                finger.bones[3] = CreateBone(Bone.BoneType.TYPE_DISTAL, landmarks[FINGER_DIP_INDICES[0]], landmarks[FINGER_TIP_INDICES[0]]);
            }
            else // 其他手指有4个骨骼
            {
                finger.bones[0] = CreateBone(Bone.BoneType.TYPE_METACARPAL, landmarks[WRIST_INDEX], landmarks[FINGER_MCP_INDICES[fingerIndex]]);
                finger.bones[1] = CreateBone(Bone.BoneType.TYPE_PROXIMAL, landmarks[FINGER_MCP_INDICES[fingerIndex]], landmarks[FINGER_PIP_INDICES[fingerIndex]]);
                finger.bones[2] = CreateBone(Bone.BoneType.TYPE_INTERMEDIATE, landmarks[FINGER_PIP_INDICES[fingerIndex]], landmarks[FINGER_DIP_INDICES[fingerIndex]]);
                finger.bones[3] = CreateBone(Bone.BoneType.TYPE_DISTAL, landmarks[FINGER_DIP_INDICES[fingerIndex]], landmarks[FINGER_TIP_INDICES[fingerIndex]]);
            }
            
            // 计算手指长度
            finger.Length = 0f;
            foreach (var bone in finger.bones)
            {
                finger.Length += bone.Length;
            }
            
            // 设置指尖位置
            finger.TipPosition = landmarks[FINGER_TIP_INDICES[fingerIndex]];
            
            // 计算手指方向
            finger.Direction = (finger.TipPosition - landmarks[FINGER_MCP_INDICES[fingerIndex]]).normalized;
            
            // 设置速度（简化为零）
            finger.TipVelocity = Vector3.zero;
            
            // 计算是否伸展（简化判断）
            finger.IsExtended = CalculateFingerExtension(fingerIndex, landmarks);
            
            return finger;
        }

        private static Bone CreateBone(Bone.BoneType type, Vector3 startPos, Vector3 endPos)
        {
            var bone = new Bone();
            bone.Type = type;
            bone.PrevJoint = startPos;
            bone.NextJoint = endPos;
            bone.Center = (startPos + endPos) * 0.5f;
            bone.Direction = (endPos - startPos).normalized;
            bone.Length = Vector3.Distance(startPos, endPos);
            bone.Width = bone.Length * 0.3f; // 简化的骨骼宽度
            
            // 计算旋转（简化处理）
            if (bone.Direction != Vector3.zero)
            {
                bone.Rotation = Quaternion.LookRotation(bone.Direction);
            }
            else
            {
                bone.Rotation = Quaternion.identity;
            }
            
            return bone;
        }

        private static bool CalculateFingerExtension(int fingerIndex, Vector3[] landmarks)
        {
            // 简化的手指伸展判断：比较指尖到手腕的距离与掌指关节到手腕的距离
            Vector3 wrist = landmarks[WRIST_INDEX];
            Vector3 mcp = landmarks[FINGER_MCP_INDICES[fingerIndex]];
            Vector3 tip = landmarks[FINGER_TIP_INDICES[fingerIndex]];
            
            float mcpDistance = Vector3.Distance(wrist, mcp);
            float tipDistance = Vector3.Distance(wrist, tip);
            
            // 如果指尖距离明显大于掌指关节距离，认为手指伸展
            return tipDistance > mcpDistance * 1.2f;
        }

        private static void CreateArm(Hand hand, Vector3 wristPosition)
        {
            // 简化的手臂创建
            hand.Arm = new Arm();
            hand.Arm.ElbowPosition = wristPosition + Vector3.back * 0.25f; // 简化的肘部位置
            hand.Arm.WristPosition = wristPosition;
            hand.Arm.Center = (hand.Arm.ElbowPosition + hand.Arm.WristPosition) * 0.5f;
            hand.Arm.Direction = (hand.Arm.WristPosition - hand.Arm.ElbowPosition).normalized;
            hand.Arm.Length = Vector3.Distance(hand.Arm.ElbowPosition, hand.Arm.WristPosition);
            hand.Arm.Width = 0.05f; // 固定手臂宽度
            
            // 设置旋转
            if (hand.Arm.Direction != Vector3.zero)
            {
                hand.Arm.Rotation = Quaternion.LookRotation(hand.Arm.Direction);
            }
            else
            {
                hand.Arm.Rotation = Quaternion.identity;
            }
        }
    }
}