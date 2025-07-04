import cv2
import numpy as np
import copy
import math
import win32api
import win32con
import socket
import time

#client 发送端
client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
PORT=1111
PORT1=1112
PORT2=1113
PORT3=1114
PORT4=1115
PORT5=1116
# 参数
cap_region_x_begin = 0.5  # 起点/总宽度
cap_region_y_end = 0.8
threshold = 60  # 二值化阈值
blurValue = 41  # 高斯模糊参数
bgSubThreshold = 50
learningRate = 0

# 变量
isBgCaptured = 0  # 布尔类型, 背景是否被捕获
triggerSwitch = False  # 如果正确，键盘模拟器将工作


def printThreshold(thr):
    print("! Changed threshold to " + str(thr))


def removeBG(frame): #移除背景
    fgmask = bgModel.apply(frame, learningRate=learningRate) #计算前景掩膜
    kernel = np.ones((3, 3), np.uint8)
    fgmask = cv2.erode(fgmask, kernel, iterations=1) #使用特定的结构元素来侵蚀图像。
    res = cv2.bitwise_and(frame, frame, mask=fgmask) #使用掩膜移除静态背景
    return res

# 相机/摄像头
camera = cv2.VideoCapture(0)   #rr打开电脑自带摄像头，如果参数是1会打开外接摄像头
camera.set(10, 200)   #设置视频属性
cv2.namedWindow('trackbar') #设置窗口名字
cv2.resizeWindow("trackbar", 640, 200)  #重新设置窗口尺寸
cv2.createTrackbar('threshold', 'trackbar', threshold, 100, printThreshold)
#createTrackbar是Opencv中的API，其可在显示图像的窗口中快速创建一个滑动控件，用于手动调节阈值，具有非常直观的效果。

while camera.isOpened():
    ret, frame = camera.read()
    threshold = cv2.getTrackbarPos('threshold', 'trackbar') #返回滑动条上的位置的值（即实时更新阈值）
    # frame = cv2.cvtColor(frame,cv2.COLOR_RGB2YCrCb)
    frame = cv2.bilateralFilter(frame, 5, 50, 100)  # 双边滤波
    frame = cv2.flip(frame, 1)  # 翻转  0:沿X轴翻转(垂直翻转)   大于0:沿Y轴翻转(水平翻转)   小于0:先沿X轴翻转，再沿Y轴翻转，等价于旋转180°
    cv2.rectangle(frame, (int(cap_region_x_begin * frame.shape[1]), 0),(frame.shape[1], int(cap_region_y_end * frame.shape[0])), (0, 0, 255), 2)
    #画矩形框  frame.shape[0]表示frame的高度    frame.shape[1]表示frame的宽度   注：opencv的像素是BGR顺序
    cv2.imshow('original', frame)   #经过双边滤波后的初始化窗口

    #主要操作
    if isBgCaptured == 1:  # isBgCaptured == 1 表示已经捕获背景
        img = removeBG(frame)  #移除背景
        img = img[0:int(cap_region_y_end * frame.shape[0]),int(cap_region_x_begin * frame.shape[1]):frame.shape[1]]  # 剪切右上角矩形框区域
        cv2.imshow('mask', img)

        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)  #将移除背景后的图像转换为灰度图
        blur = cv2.GaussianBlur(gray, (blurValue, blurValue), 0)  #加高斯模糊
        cv2.imshow('blur', blur)
        ret, thresh = cv2.threshold(blur, threshold, 255, cv2.THRESH_BINARY)  #二值化处理
        cv2.imshow('binary', thresh)

        # get the coutours
        thresh1 = copy.deepcopy(thresh)
        contours, hierarchy = cv2.findContours(thresh1, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
        #寻找轮廓   注：这里的'_'用作变量名称，_表示一个变量被指定了名称，但不打算使用。
        length = len(contours)
        maxArea = -1
        if length > 0:
            for i in range(length):  # 找到最大的轮廓（根据面积）
                temp = contours[i]
                area = cv2.contourArea(temp)  #计算轮廓区域面积
                if area > maxArea:
                    maxArea = area
                    ci = i

            res = contours[ci]  #得出最大的轮廓区域
            hull = cv2.convexHull(res)  #得出点集（组成轮廓的点）的凸包
            drawing = np.zeros(img.shape, np.uint8)
            cv2.drawContours(drawing, [res], 0, (0, 255, 0), 2)   #画出最大区域轮廓
            cv2.drawContours(drawing, [hull], 0, (0, 0, 255), 3)  #画出凸包轮廓

            moments = cv2.moments(res)  # 求最大区域轮廓的各阶矩
            center = (int(moments['m10'] / moments['m00']), int(moments['m01'] / moments['m00']))
            cv2.circle(drawing, center, 8, (0,0,255), -1)   #画出重心

            fingerRes = []   #寻找指尖
            max = 0; count = 0; notice = 0; cnt = 0
            for i in range(len(res)):
                temp = res[i]
                dist = (temp[0][0] -center[0])*(temp[0][0] -center[0]) + (temp[0][1] -center[1])*(temp[0][1] -center[1]) #计算重心到轮廓边缘的距离
                if dist > max:
                    max = dist
                    notice = i
                if dist != max:
                    count = count + 1
                    if count > 40:
                        count = 0
                        max = 0
                        flag = False   #布尔值
                        if center[1] < res[notice][0][1]:   #低于手心的点不算
                            continue
                        for j in range(len(fingerRes)):  #离得太近的不算
                            if abs(res[notice][0][0]-fingerRes[j][0]) < 20 :
                                flag = True
                                break
                        if flag :
                            continue
                        fingerRes.append(res[notice][0])
                        cv2.circle(drawing, tuple(res[notice][0]), 8 , (255, 0, 0), -1) #画出指尖
                        cv2.line(drawing, center, tuple(res[notice][0]), (255, 0, 0), 2)
                        cnt = cnt + 1

            cv2.imshow('output', drawing)
            print(cnt)
            server_address = ("127.0.0.1", PORT)  # 接收方 服务器的ip地址和端口号
            server_address1 = ("127.0.0.1", PORT1)
            server_address2 = ("127.0.0.1", PORT2)
            server_address3 = ("127.0.0.1", PORT3)
            server_address4 = ("127.0.0.1", PORT4)
            server_address5 = ("127.0.0.1", PORT5)
            client_socket.sendto(str(cnt).encode(), server_address) #将msg内容发送给指定接收方
            client_socket.sendto(str(cnt).encode(), server_address1)
            client_socket.sendto(str(cnt).encode(), server_address2)
            client_socket.sendto(str(cnt).encode(), server_address3)
            client_socket.sendto(str(cnt).encode(), server_address4)
            client_socket.sendto(str(cnt).encode(), server_address5)
            '''
            if triggerSwitch is True:
                if cnt >= 3:
                    print(cnt)
                    # app('System Events').keystroke(' ')  # simulate pressing blank space
                    win32api.keybd_event(32, 0, 0, 0)  # 空格键位码是32
                    win32api.keybd_event(32, 0, win32con.KEYEVENTF_KEYUP, 0)  # 释放空格键
            '''

    # 输入的键盘值
    k = cv2.waitKey(10)
    if k == 27:  # 按下ESC退出
        break
    elif k == ord('b'):  # 按下'b'会捕获背景
        bgModel = cv2.createBackgroundSubtractorMOG2(0, bgSubThreshold)
        #Opencv集成了BackgroundSubtractorMOG2用于动态目标检测，用到的是基于自适应混合高斯背景建模的背景减除法。
        isBgCaptured = 1
        print('!!!Background Captured!!!')
    elif k == ord('r'):  # 按下'r'会重置背景
        bgModel = None
        triggerSwitch = False
        isBgCaptured = 0
        print('!!!Reset BackGround!!!')
    '''
    elif k == ord('n'):
        triggerSwitch = True
        print('!!!Trigger On!!!')
    '''