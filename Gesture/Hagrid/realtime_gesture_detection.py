import cv2
import torch
import torch.nn.functional as F
import torchvision.transforms as transforms
import numpy as np
import time
import os
import argparse
import mediapipe as mp
import socket
import json
from PIL import Image
from models import classifiers_list

# UDP配置
UDP_IP = "127.0.0.1"  # Unity运行的IP地址
UDP_PORT = 12345      # Unity监听的端口

# 手势编号映射 - 将34种手势映射到Unity可识别的编号
GESTURE_MAPPING = {
    'grabbing': 1, 'grip': 2, 'holy': 3, 'point': 4, 'call': 12, 'three3': 15,
    'timeout': 17, 'xsign': 18, 'hand_heart': 4, 'hand_heart2': 4, 'little_finger': 19,
    'middle_finger': 8, 'take_picture': 20, 'dislike': 6, 'fist': 10, 'four': 14,
    'like': 5, 'mute': 21, 'ok': 13, 'one': 9, 'palm': 22, 'peace': 11,
    'peace_inverted': 23, 'rock': 24, 'stop': 2, 'stop_inverted': 25, 'three': 15,
    'three2': 16, 'two_up': 26, 'two_up_inverted': 27, 'three_gun': 28,
    'thumb_index': 29, 'thumb_index2': 30, 'no_gesture': 0
}

def create_gesture_classifier(model_name="ResNet18", num_classes=34):
    """创建手势分类器"""
    if model_name in classifiers_list:
        model_class = classifiers_list[model_name]
        return model_class(num_classes=num_classes)
    else:
        raise ValueError(f"不支持的模型: {model_name}。支持的模型: {list(classifiers_list.keys())}")

class RealTimeGestureDetector:
    def __init__(self, model_path, model_name="ResNet18", device='cuda' if torch.cuda.is_available() else 'cpu', 
                 enable_signal=False, enable_window=True):
        self.device = device
        self.model_name = model_name
        self.model = self.load_model(model_path)
        self.transform = self.get_transform()
        self.enable_signal = enable_signal
        self.enable_window = enable_window
        
        self.gesture_names = [
            'grabbing', 'grip', 'holy', 'point', 'call', 'three3', 'timeout', 'xsign',
            'hand_heart', 'hand_heart2', 'little_finger', 'middle_finger', 'take_picture',
            'dislike', 'fist', 'four', 'like', 'mute', 'ok', 'one', 'palm', 'peace',
            'peace_inverted', 'rock', 'stop', 'stop_inverted', 'three', 'three2',
            'two_up', 'two_up_inverted', 'three_gun', 'thumb_index', 'thumb_index2', 'no_gesture'
        ]
        
        # 双手手势列表
        self.dual_hand_gestures = {'xsign', 'hand_heart', 'hand_heart2', 'holy', 'take_picture', 'timeout'}
        
        # 初始化MediaPipe手部检测
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=2,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5
        )
        self.mp_draw = mp.solutions.drawing_utils
        
        # 初始化UDP socket（如果启用信号发送）
        if self.enable_signal:
            try:
                self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                print(f"✅ UDP通信已启用: {UDP_IP}:{UDP_PORT}")
            except Exception as e:
                print(f"❌ UDP初始化失败: {e}")
                self.enable_signal = False
        
        # 性能统计
        self.fps_counter = 0
        self.fps_start_time = time.time()
        self.current_fps = 0
        
        # 截图功能已移除
        
    def load_model(self, model_path):
        """加载训练好的模型"""
        model = create_gesture_classifier(self.model_name, num_classes=34)
        
        try:
            # 尝试加载模型权重
            checkpoint = torch.load(model_path, map_location=self.device)
            if 'MODEL_STATE' in checkpoint:
                model.hagrid_model.load_state_dict(checkpoint['MODEL_STATE'])
                print(f"✅ 成功加载模型权重: {model_path}")
            else:
                # 如果直接是state_dict
                model.hagrid_model.load_state_dict(checkpoint)
                print(f"✅ 成功加载模型权重: {model_path}")
        except Exception as e:
            print(f"❌ 模型加载失败: {e}")
            print("使用未训练的模型进行演示...")
            
        model.to(self.device)
        model.eval()
        return model
    
    def get_transform(self):
        """获取图像预处理变换"""
        return transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize(
                mean=[0.54, 0.499, 0.474],
                std=[0.234, 0.235, 0.231]
            )
        ])
    
    def preprocess_frame(self, frame):
        """预处理摄像头帧"""
        # 转换为PIL图像
        pil_image = Image.fromarray(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
        
        # 应用变换
        tensor_image = self.transform(pil_image).unsqueeze(0)
        return tensor_image.to(self.device)
    
    def detect_hands(self, frame):
        """检测手部关键点"""
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = self.hands.process(rgb_frame)
        
        hands_info = []
        if results.multi_hand_landmarks and results.multi_handedness:
            for hand_landmarks, handedness in zip(results.multi_hand_landmarks, results.multi_handedness):
                # 获取手部边界框
                h, w, _ = frame.shape
                x_coords = [landmark.x * w for landmark in hand_landmarks.landmark]
                y_coords = [landmark.y * h for landmark in hand_landmarks.landmark]
                
                hands_info.append({
                    'landmarks': hand_landmarks,
                    'handedness': handedness.classification[0].label,  # 'Left' or 'Right'
                    'confidence': handedness.classification[0].score,
                    'x_coords': x_coords,
                    'y_coords': y_coords,
                    'bbox': (int(min(x_coords)), int(min(y_coords)), 
                            int(max(x_coords)), int(max(y_coords)))
                })
        
        return hands_info
    
    def split_frame_by_hands(self, frame, hands_info):
        """根据手部位置分割图像"""
        if len(hands_info) != 2:
            return None, None, None
        
        # 找到左手和右手
        left_hand = None
        right_hand = None
        for hand in hands_info:
            if hand['handedness'] == 'Left':
                left_hand = hand
            else:
                right_hand = hand
        
        if not left_hand or not right_hand:
            return None, None, None
        
        # 获取关键边界点
        left_hand_rightmost = max(left_hand['x_coords'])
        right_hand_leftmost = min(right_hand['x_coords'])
        
        # 计算分割线位置（用于显示）
        split_x = int((left_hand_rightmost + right_hand_leftmost) / 2)
        
        # 优化分割策略：最大化保留图像信息
        h, w = frame.shape[:2]
        
        # 左手图像：从最左边到右手的最左关键点
        left_frame = frame[:, :int(right_hand_leftmost)]
        
        # 右手图像：从左手最右边关键点到最右边
        right_frame = frame[:, int(left_hand_rightmost):]
        
        return left_frame, right_frame, split_x
    
    def predict(self, frame):
        """预测手势"""
        with torch.no_grad():
            # 预处理
            input_tensor = self.preprocess_frame(frame)
            
            # 模型推理 - 使用ClassifierModel的调用方式
            model_output = self.model([input_tensor.squeeze(0)])
            outputs = model_output['labels']
            probabilities = F.softmax(outputs, dim=1)
            
            # 获取预测结果
            confidence, predicted_idx = torch.max(probabilities, 1)
            predicted_class = self.gesture_names[predicted_idx.item()]
            confidence_score = confidence.item()
            
            return predicted_class, confidence_score, probabilities[0]
    
    def predict_dual_hand(self, frame):
        """智能双手手势预测"""
        # 第一步：对整个画面进行分类
        gesture, confidence, probabilities = self.predict(frame)
        
        # 第二步：判断是否是双手手势
        if gesture in self.dual_hand_gestures:
            # 发送双手手势到Unity
            self.send_gesture_to_unity(gesture, confidence, "dual")
            
            return {
                'type': 'dual_gesture',
                'gesture': gesture,
                'confidence': confidence,
                'probabilities': probabilities
            }
        
        # 第三步：使用MediaPipe检测手部
        hands_info = self.detect_hands(frame)
        
        if len(hands_info) == 0:
            return {
                'type': 'no_hand',
                'gesture': 'no_gesture',
                'confidence': 0.0
            }
        elif len(hands_info) == 1:
            # 单手情况
            hand_info = hands_info[0]
            
            # 发送单手手势到Unity（包含关键点数据）
            self.send_gesture_to_unity(gesture, confidence, "single", hand_info['landmarks'])
            
            return {
                'type': 'single_hand',
                'gesture': gesture,
                'confidence': confidence,
                'probabilities': probabilities,
                'hand_type': hand_info['handedness'],
                'hand_confidence': hand_info['confidence']
            }
        else:
            # 双手情况，需要分割图像分别分类
            left_frame, right_frame, split_x = self.split_frame_by_hands(frame, hands_info)
            
            if left_frame is None or right_frame is None:
                # 分割失败，使用整体分类结果
                return {
                    'type': 'dual_hand_failed',
                    'gesture': gesture,
                    'confidence': confidence,
                    'probabilities': probabilities
                }
            
            # 分别对左右手区域进行分类
            left_gesture, left_confidence, left_probabilities = self.predict(left_frame)
            right_gesture, right_confidence, right_probabilities = self.predict(right_frame)
            
            # 发送左右手手势到Unity（包含关键点数据）
            left_landmarks = None
            right_landmarks = None
            for hand in hands_info:
                if hand['handedness'] == 'Left':
                    left_landmarks = hand['landmarks']
                elif hand['handedness'] == 'Right':
                    right_landmarks = hand['landmarks']
            
            # 同时发送左右手数据到Unity
            self.send_dual_hands_to_unity(left_gesture, left_confidence, left_landmarks, 
                                        right_gesture, right_confidence, right_landmarks)
            
            return {
                'type': 'dual_hand_split',
                'left_hand': {
                    'gesture': left_gesture,
                    'confidence': left_confidence,
                    'probabilities': left_probabilities
                },
                'right_hand': {
                    'gesture': right_gesture,
                    'confidence': right_confidence,
                    'probabilities': right_probabilities
                },
                'hands_info': hands_info,
                'split_x': split_x
            }
    
    def update_fps(self):
        """更新FPS计算"""
        self.fps_counter += 1
        current_time = time.time()
        if current_time - self.fps_start_time >= 1.0:
            self.current_fps = self.fps_counter
            self.fps_counter = 0
            self.fps_start_time = current_time
    
    def draw_info(self, frame, gesture, confidence, fps):
        """在帧上绘制信息（保留原方法用于兼容性）"""
        height, width = frame.shape[:2]
        
        # 绘制背景矩形
        cv2.rectangle(frame, (10, 10), (width-10, 120), (0, 0, 0), -1)
        cv2.rectangle(frame, (10, 10), (width-10, 120), (255, 255, 255), 2)
        
        # 绘制手势信息
        cv2.putText(frame, f"Gesture: {gesture}", (20, 40), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
        
        # 绘制置信度
        confidence_color = (0, 255, 0) if confidence > 0.7 else (0, 255, 255) if confidence > 0.5 else (0, 0, 255)
        cv2.putText(frame, f"Confidence: {confidence:.3f}", (20, 70), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.6, confidence_color, 2)
        
        # 绘制FPS
        cv2.putText(frame, f"FPS: {fps}", (20, 100), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
        
        # 绘制置信度条
        bar_width = int((width - 40) * confidence)
        cv2.rectangle(frame, (20, height-40), (20 + bar_width, height-20), confidence_color, -1)
        cv2.rectangle(frame, (20, height-40), (width-20, height-20), (255, 255, 255), 2)
        
        return frame
    
    def draw_camera_info(self, frame, last_prediction, last_confidence):
        """在摄像头画面上绘制界面信息"""
        height, width = frame.shape[:2]
        
        # 绘制主要信息背景
        cv2.rectangle(frame, (10, 10), (width-10, 180), (0, 0, 0), -1)
        cv2.rectangle(frame, (10, 10), (width-10, 180), (255, 255, 255), 2)
        
        # 绘制标题
        cv2.putText(frame, "Smart Dual-Hand Gesture Classifier", (20, 40), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
        
        # 绘制操作提示
        cv2.putText(frame, "Press SPACE to classify gesture", (20, 70), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
        
        # 绘制FPS
        cv2.putText(frame, f"FPS: {self.current_fps}", (20, 100), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
        
        # 绘制检测模式说明
        cv2.putText(frame, "Auto dual-hand detection enabled", (20, 125), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 0), 1)
        
        # 如果有最后一次预测结果，显示它
        if last_prediction is not None:
            confidence_color = (0, 255, 0) if last_confidence > 0.7 else (0, 255, 255) if last_confidence > 0.5 else (0, 0, 255)
            # 处理长文本显示
            prediction_text = f"Last: {last_prediction} ({last_confidence:.3f})"
            if len(prediction_text) > 25:
                # 如果文本太长，分两行显示
                cv2.putText(frame, f"Last: {last_prediction[:25]}", (20, 150), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.5, confidence_color, 2)
                cv2.putText(frame, f"      ({last_confidence:.3f})", (20, 170), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.5, confidence_color, 2)
            else:
                cv2.putText(frame, prediction_text, (20, 150), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.5, confidence_color, 2)
            
            # 绘制置信度条
            bar_width = int((width - 40) * last_confidence)
            cv2.rectangle(frame, (20, height-40), (20 + bar_width, height-20), confidence_color, -1)
            cv2.rectangle(frame, (20, height-40), (width-20, height-20), (255, 255, 255), 2)
        
        # 绘制按键提示
        cv2.putText(frame, "Q: Quit | SPACE: Classify | S: Screenshot", (20, height-10), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.5, (200, 200, 200), 1)
        
        return frame
    
    def send_gesture_to_unity(self, gesture_name, confidence, hand_type="single", landmarks_data=None):
        """通过UDP发送手势数据到Unity"""
        if not self.enable_signal:
            return
            
        try:
            # 获取手势编号
            gesture_id = GESTURE_MAPPING.get(gesture_name, 0)
            
            # 根据hand_type确定hand_side
            if hand_type == "left":
                hand_side = "Left"
            elif hand_type == "right":
                hand_side = "Right"
            else:
                hand_side = "Right"  # 默认为右手
            
            # 处理关键点数据
            landmarks_list = []
            if landmarks_data and hasattr(landmarks_data, 'landmark'):
                # 提取MediaPipe手部关键点数据（21个关键点）
                for landmark in landmarks_data.landmark:
                    landmarks_list.append([landmark.x, landmark.y, landmark.z])
            
            # 构建手部数据（与mediapipe格式兼容）
            hand_data = {
                "hand_side": hand_side,
                "gesture_name": gesture_name,
                "gesture_id": gesture_id,
                "landmarks": landmarks_list
            }
            
            # 构建发送数据（与Unity期望的格式匹配）
            data = {
                "hands": [hand_data],
                "timestamp": time.time()
            }
            
            # 转换为JSON字符串并发送
            json_data = json.dumps(data)
            self.sock.sendto(json_data.encode('utf-8'), (UDP_IP, UDP_PORT))
            
        except Exception as e:
            print(f"UDP发送错误: {e}")
    
    def send_dual_hands_to_unity(self, left_gesture, left_confidence, left_landmarks, 
                                right_gesture, right_confidence, right_landmarks):
        """同时发送左右手手势数据到Unity"""
        if not self.enable_signal:
            return
            
        try:
            hands_data = []
            
            # 处理左手数据
            if left_gesture and left_gesture != "no_gesture":
                left_gesture_id = GESTURE_MAPPING.get(left_gesture, 0)
                left_landmarks_list = []
                if left_landmarks and hasattr(left_landmarks, 'landmark'):
                    for landmark in left_landmarks.landmark:
                        left_landmarks_list.append([landmark.x, landmark.y, landmark.z])
                
                left_hand_data = {
                    "hand_side": "Left",
                    "gesture_name": left_gesture,
                    "gesture_id": left_gesture_id,
                    "landmarks": left_landmarks_list
                }
                hands_data.append(left_hand_data)
            
            # 处理右手数据
            if right_gesture and right_gesture != "no_gesture":
                right_gesture_id = GESTURE_MAPPING.get(right_gesture, 0)
                right_landmarks_list = []
                if right_landmarks and hasattr(right_landmarks, 'landmark'):
                    for landmark in right_landmarks.landmark:
                        right_landmarks_list.append([landmark.x, landmark.y, landmark.z])
                
                right_hand_data = {
                    "hand_side": "Right",
                    "gesture_name": right_gesture,
                    "gesture_id": right_gesture_id,
                    "landmarks": right_landmarks_list
                }
                hands_data.append(right_hand_data)
            
            # 构建发送数据（与Unity期望的格式匹配）
            if hands_data:  # 只有当有手势数据时才发送
                data = {
                    "hands": hands_data,
                    "timestamp": time.time()
                }
                
                # 转换为JSON字符串并发送
                json_data = json.dumps(data)
                self.sock.sendto(json_data.encode('utf-8'), (UDP_IP, UDP_PORT))
                
        except Exception as e:
            print(f"UDP双手发送错误: {e}")
    
    def draw_split_line(self, frame, split_x):
        """在双手分割时绘制分割线"""
        h, w = frame.shape[:2]
        
        # 绘制垂直分割线
        cv2.line(frame, (split_x, 0), (split_x, h), (0, 255, 255), 3)  # 黄色线条
        
        # 在分割线上方添加标签
        cv2.putText(frame, "Split Line", (split_x - 40, 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 255), 2)
        
        # 在分割线两侧添加左右手标识
        cv2.putText(frame, "Left Hand", (max(10, split_x - 120), h - 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 0, 0), 2)
        cv2.putText(frame, "Right Hand", (min(w - 120, split_x + 20), h - 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
        
        return frame
    
    def run(self, camera_id=0, confidence_threshold=0.3, auto_interval=0):
        """运行实时手势检测
        
        Args:
            camera_id: 摄像头ID
            confidence_threshold: 置信度阈值
            auto_interval: 自动模式间隔(毫秒)，0表示手动模式
        """
        print("🚀 启动手势分类器...")
        print(f"📱 使用设备: {self.device}")
        print(f"🎯 置信度阈值: {confidence_threshold}")
        if auto_interval > 0:
            print(f"🤖 自动模式: 每 {auto_interval} 毫秒自动分类一次")
            print("📹 按 'q' 键退出")
        else:
            print("👆 手动模式: 按 '空格' 键进行手势分类")
            print("📹 按 'q' 键退出，按 '空格' 键进行手势分类")
        
        # 初始化摄像头
        cap = cv2.VideoCapture(camera_id)
        if not cap.isOpened():
            print("❌ 无法打开摄像头")
            return
        
        # 设置摄像头参数
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
        cap.set(cv2.CAP_PROP_FPS, 30)
        
        # screenshot_counter变量已移除
        last_prediction = None
        last_confidence = 0.0
        
        # 自动模式时间控制
        last_auto_time = 0
        
        try:
            while True:
                ret, frame = cap.read()
                if not ret:
                    print("❌ 无法读取摄像头帧")
                    break
                
                # 水平翻转（镜像效果）
                frame = cv2.flip(frame, 1)
                
                # 更新FPS
                self.update_fps()
                
                # 绘制界面信息
                self.draw_camera_info(frame, last_prediction, last_confidence)
                
                # 显示帧（如果启用窗口）
                if self.enable_window:
                    window_title = f'Gesture Classifier - {"Auto Mode" if auto_interval > 0 else "Manual Mode"}'
                    cv2.imshow(window_title, frame)
                
                # 检查是否需要自动分类
                current_time = time.time() * 1000  # 转换为毫秒
                should_classify = False
                
                if auto_interval > 0:
                    # 自动模式
                    if current_time - last_auto_time >= auto_interval:
                        should_classify = True
                        last_auto_time = current_time
                
                # 处理按键
                if self.enable_window:
                    key = cv2.waitKey(1) & 0xFF
                    if key == ord('q'):
                        print("👋 退出程序")
                        break
                    elif key == ord(' ') and auto_interval == 0:  # 手动模式下的空格键触发分类
                        should_classify = True
                else:
                    # 不显示窗口时，仍需要waitKey来处理OpenCV内部事件
                    key = cv2.waitKey(1) & 0xFF
                    if key == ord('q'):
                        print("👋 退出程序")
                        break
                    elif key == ord(' ') and auto_interval == 0:  # 手动模式下的空格键触发分类
                        should_classify = True
                
                # 执行分类
                if should_classify:
                    print("\n🔍 正在进行智能手势分类...")
                    try:
                        result = self.predict_dual_hand(frame)
                        
                        if result['type'] == 'dual_gesture':
                            # 双手手势
                            gesture = result['gesture']
                            confidence = result['confidence']
                            probabilities = result['probabilities']
                            
                            last_prediction = gesture
                            last_confidence = confidence
                            
                            print(f"🤲 双手手势: {gesture}")
                            print(f"📊 置信度: {confidence:.4f}")
                            
                            # 显示前5个最可能的类别
                            top5_probs, top5_indices = torch.topk(probabilities, 5)
                            print("\n📈 Top 5 预测:")
                            for i in range(5):
                                class_name = self.gesture_names[top5_indices[i]]
                                prob = top5_probs[i].item()
                                print(f"  {i+1}. {class_name}: {prob:.4f}")
                            
                            # 截图保存功能已移除
                            
                        elif result['type'] == 'single_hand':
                            # 单手手势
                            gesture = result['gesture']
                            confidence = result['confidence']
                            hand_type = result['hand_type']
                            hand_confidence = result['hand_confidence']
                            probabilities = result['probabilities']
                            
                            last_prediction = f"{hand_type} {gesture}"
                            last_confidence = confidence
                            
                            print(f"👋 {hand_type}手手势: {gesture}")
                            print(f"📊 分类置信度: {confidence:.4f}")
                            print(f"🤚 手部检测置信度: {hand_confidence:.4f}")
                            
                            # 显示前5个最可能的类别
                            top5_probs, top5_indices = torch.topk(probabilities, 5)
                            print("\n📈 Top 5 预测:")
                            for i in range(5):
                                class_name = self.gesture_names[top5_indices[i]]
                                prob = top5_probs[i].item()
                                print(f"  {i+1}. {class_name}: {prob:.4f}")
                            
                            # 截图保存功能已移除
                            
                        elif result['type'] == 'dual_hand_split':
                            # 双手分别分类
                            left_gesture = result['left_hand']['gesture']
                            left_confidence = result['left_hand']['confidence']
                            right_gesture = result['right_hand']['gesture']
                            right_confidence = result['right_hand']['confidence']
                            split_x = result['split_x']
                            
                            last_prediction = f"L:{left_gesture} R:{right_gesture}"
                            last_confidence = (left_confidence + right_confidence) / 2
                            
                            print(f"👈 左手: {left_gesture} (置信度: {left_confidence:.4f})")
                            print(f"👉 右手: {right_gesture} (置信度: {right_confidence:.4f})")
                            print(f"✂️ 分割线位置: x={split_x}")
                            
                            # 显示左手Top 3预测
                            left_probs = result['left_hand']['probabilities']
                            top3_left_probs, top3_left_indices = torch.topk(left_probs, 3)
                            print("\n📈 左手 Top 3:")
                            for i in range(3):
                                class_name = self.gesture_names[top3_left_indices[i]]
                                prob = top3_left_probs[i].item()
                                print(f"  {i+1}. {class_name}: {prob:.4f}")
                            
                            # 显示右手Top 3预测
                            right_probs = result['right_hand']['probabilities']
                            top3_right_probs, top3_right_indices = torch.topk(right_probs, 3)
                            print("\n📈 右手 Top 3:")
                            for i in range(3):
                                class_name = self.gesture_names[top3_right_indices[i]]
                                prob = top3_right_probs[i].item()
                                print(f"  {i+1}. {class_name}: {prob:.4f}")
                            
                            # 在帧上绘制分割线
                            frame = self.draw_split_line(frame, split_x)
                            
                            # 截图保存功能已移除
                            
                        elif result['type'] == 'no_hand':
                            # 未检测到手部
                            last_prediction = "No Hand"
                            last_confidence = 0.0
                            print("🚫 未检测到手部")
                            
                        else:
                            # 其他情况（如dual_hand_failed）
                            gesture = result['gesture']
                            confidence = result['confidence']
                            last_prediction = gesture
                            last_confidence = confidence
                            print(f"🤲 整体分类: {gesture} (置信度: {confidence:.4f})")
                        
                        print("-" * 50)
                        
                    except Exception as e:
                        print(f"⚠️ 分类错误: {e}")
                        last_prediction = "Error"
                        last_confidence = 0.0
                
                # 手动截图功能已移除
                    
        except KeyboardInterrupt:
            print("\n👋 程序被用户中断")
        
        finally:
            cap.release()
            cv2.destroyAllWindows()
            
            # 关闭UDP socket
            if self.enable_signal and hasattr(self, 'sock'):
                self.sock.close()
                print("🔌 UDP连接已关闭")
            print("🔚 程序结束")

def main():
    # 解析命令行参数
    parser = argparse.ArgumentParser(description='实时手势分类器')
    parser.add_argument('--auto', type=int, default=0, 
                       help='自动模式间隔(毫秒)，0表示手动模式(默认: 0)')
    parser.add_argument('--model_path', type=str, 
                       default="work_dir/ResNet18/ResNet18_epoch_4_F1Score_0.94_loss_0.12.pth",
                       help='模型文件路径')
    parser.add_argument('--camera_id', type=int, default=0,
                       help='摄像头ID (默认: 0)')
    parser.add_argument('--confidence_threshold', type=float, default=0.5,
                       help='置信度阈值 (默认: 0.5)')
    parser.add_argument('--signal', action='store_true', help='启用UDP信号发送到Unity')
    parser.add_argument('--window', action='store_true', help='启用实时检测窗口显示')
    
    args = parser.parse_args()
    
    # 创建检测器
    detector = RealTimeGestureDetector(args.model_path, model_name="ResNet18", 
                                     enable_signal=args.signal, enable_window=args.window)
    
    # 显示启动信息
    print(f"🚀 启动实时手势识别系统")
    print(f"📡 UDP信号发送: {'✅ 启用' if args.signal else '❌ 关闭'}")
    print(f"🖥️  窗口显示: {'✅ 启用' if args.window else '❌ 关闭'}")
    print(f"📹 摄像头ID: {args.camera_id}")
    print(f"🎯 置信度阈值: {args.confidence_threshold}")
    print(f"⏱️  模式: {'自动模式 (' + str(args.auto) + 'ms)' if args.auto > 0 else '手动模式'}")
    print("按 'q' 退出程序")
    if args.window and args.auto == 0:
        print("按 '空格' 进行手势识别")
    print("-" * 50)
    
    # 运行检测
    detector.run(camera_id=args.camera_id, 
                confidence_threshold=args.confidence_threshold,
                auto_interval=args.auto)

if __name__ == "__main__":
    main()