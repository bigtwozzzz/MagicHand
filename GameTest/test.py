###############################单线程用户登录
# import socket
# import struct
# import base_pb2
#
# def parse_zinx_message(data):
#     if len(data) < 8:
#         return None  # 数据不完整
#
#     total_length = struct.unpack('<I', data[:4])[0] + 8
#     msg_id = struct.unpack('<I', data[4:8])[0]
#     msg_body = data[8:8 + (total_length - 8)]
#
#     print(f"[DEBUG] Raw Message Body: {msg_body.hex()}")
#     return {
#         'msg_id': msg_id,
#         'body': msg_body
#     }
#
# def send_login_request(host, port, username, password):
#     client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
#     client_socket.connect((host, port))
#
#     login_request = base_pb2.LoginRequest()
#     login_request.username = username
#     login_request.password = password
#     msg_body = login_request.SerializeToString()
#
#     msg_id = 1  # MSG_TYPE_LOGIN_REQUEST
#     total_length = len(msg_body)
#
#     full_message = struct.pack('<I', total_length) + struct.pack('<I', msg_id) + msg_body
#     print(f"[DEBUG] Sending login request for user: {username}")
#     client_socket.sendall(full_message)
#
#     received_data = client_socket.recv(4096)
#     print(f"[DEBUG] Received {len(received_data)} bytes of data")
#
#     parsed = parse_zinx_message(received_data)
#     if parsed and parsed['msg_id'] == 101:  # MSG_TYPE_LOGIN_RESPONSE
#         login_response = base_pb2.LoginResponse()
#         login_response.ParseFromString(parsed['body'])
#         print("[INFO] Login Response:")
#         print(f"  User ID: {login_response.user_id}")
#         print(f"  Status: {login_response.status}")  # ← 现在是字符串类型
#     else:
#         print(f"[WARNING] Unexpected message ID: {parsed['msg_id'] if parsed else 'None'}")
#         print(f"Raw Message Body: {parsed['body'] if parsed else 'None'}")
#
#     client_socket.close()
#
# if __name__ == "__main__":
#     send_login_request("192.168.13.129", 8888, "Noct", "qzx123456")
##########################################################################################################
# ################################多线程用户登录
# import socket
# import struct
# import time
#
# import base_pb2
# import broadcast_pb2
# import threading
#
#
# def parse_zinx_message(data):
#     if len(data) < 8:
#         return None  # 数据不完整
#
#     total_length = struct.unpack('<I', data[:4])[0] + 8
#     msg_id = struct.unpack('<I', data[4:8])[0]
#     msg_body = data[8:8 + (total_length - 8)]
#
#     print(f"[DEBUG] Raw Message Body: {msg_body.hex()}")
#     return {
#         'msg_id': msg_id,
#         'body': msg_body
#     }
#
#
# def send_login_request(username, host="192.168.13.129", port=8888, password="qzx123456"):
#     client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
#     client_socket.connect((host, port))
#
#     # 发送登录请求
#     login_request = base_pb2.LoginRequest()
#     login_request.username = username
#     login_request.password = password
#     msg_body = login_request.SerializeToString()
#
#     full_message = struct.pack('<I', len(msg_body)) + struct.pack('<I', 1) + msg_body
#     print(f"[DEBUG] [User: {username}] Sending login request, full_message hex: {full_message.hex()}")
#     client_socket.sendall(full_message)
#
#     # 持续接收广播消息（改进版）
#     buffer = b''
#     try:
#         while True:
#             data = client_socket.recv(4096)
#             if not data:
#                 break
#             buffer += data
#
#             while len(buffer) >= 8:
#                 total_length = struct.unpack('<I', buffer[:4])[0] + 8
#                 msg_id = struct.unpack('<I', buffer[4:8])[0]
#                 print(f"[DEBUG] [User: {username}] Parsing message: ID={msg_id}, total_length={total_length}")
#                 if len(buffer) < total_length:
#                     break
#                 msg_body = buffer[8:total_length]
#                 print(f"[DEBUG] [User: {username}] Message body hex: {msg_body.hex()}")
#                 buffer = buffer[total_length:]
#                 # 处理msg_body...
#
#                 print(f"[DEBUG] [User: {username}] Received broadcast data (ID: {msg_id}), length: {len(msg_body)}")
#                 parsed_broadcast = {
#                     'msg_id': msg_id,
#                     'body': msg_body
#                 }
#                 if parsed_broadcast['msg_id'] == 201:
#                     online_notify = broadcast_pb2.PlayerOnlineNotify()
#                     online_notify.ParseFromString(parsed_broadcast['body'])
#                     print(f"[INFO] [User: {username}] Received Broadcast (Player Online):")
#                     print(f"  Player ID: {online_notify.player_id}")
#                     print(f"  Player Name: {online_notify.player_name}")
#                     print(f"  Position X: {online_notify.pos_x}")
#                     print(f"  Position Y: {online_notify.pos_y}")
#                     print(f"  Status: {online_notify.status}")
#     except KeyboardInterrupt:
#         print("[INFO] [User: {username}] Client shutting down...")
#     finally:
#         client_socket.close()
# if __name__ == "__main__":
#     # 创建两个线程模拟并发登录
#     thread1 = threading.Thread(target=send_login_request, args=("Noct",))
#     thread2 = threading.Thread(target=send_login_request, args=("Stella",))  # 使用指定的用户名
#     thread3 = threading.Thread(target=send_login_request, args=("Luna",))
#
#     # 启动线程
#     thread1.start()
#     thread2.start()
#     thread3.start()
#
#     # 等待线程完成
#     thread1.join()
#     thread2.join()
#     thread3.join()
###########################################################################################
###########################登出
# import socket
# import struct
# import base_pb2
# import threading
# def parse_zinx_message(data):
#     if len(data) < 8:
#         return None  # 数据不完整
#
#     total_length = struct.unpack('<I', data[:4])[0] + 8
#     msg_id = struct.unpack('<I', data[4:8])[0]
#     msg_body = data[8:8 + (total_length - 8)]
#
#     print(f"[DEBUG] Raw Message Body: {msg_body.hex()}")
#     return {
#         'msg_id': msg_id,
#         'body': msg_body
#     }
#
# def send_logout_request(user_id, host="192.168.13.129", port=8888):
#     client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
#     client_socket.connect((host, port))
#
#     logout_request = base_pb2.LogoutRequest()
#     logout_request.user_id = str(user_id)  # 强制转为 string 类型
#     msg_body = logout_request.SerializeToString()
#
#     msg_id = 2  # MSG_TYPE_LOGOUT_REQUEST
#     total_length = len(msg_body)
#
#     full_message = struct.pack('<I', total_length) + struct.pack('<I', msg_id) + msg_body
#     print(f"[DEBUG] [User ID: {user_id}] Sending logout request, full_message hex: {full_message.hex()}")
#     client_socket.sendall(full_message)
#
#     received_data = client_socket.recv(4096)
#     print(f"[DEBUG] [User ID: {user_id}] Received {len(received_data)} bytes of data")
#
#     parsed = parse_zinx_message(received_data)
#     if parsed and parsed['msg_id'] == 102:  # 假设服务端返回 MSG_TYPE_LOGOUT_RESPONSE = 102
#         logout_response = base_pb2.LogoutResponse()
#         try:
#             logout_response.ParseFromString(parsed['body'])
#             print(f"[INFO] [User ID: {user_id}] Logout Response:")
#             print(f"  Result: {logout_response.result}")
#         except Exception as e:
#             print(f"[ERROR] Failed to parse logout response: {e}")
#     else:
#         print(f"[WARNING] [User ID: {user_id}] Unexpected message ID: {parsed['msg_id'] if parsed else 'None'}")
#         print(f"Raw Message Body: {parsed['body'].hex() if parsed else 'None'}")
#
#     client_socket.close()
# if __name__ == "__main__":
#     # 创建两个线程模拟并发登出
#     thread1 = threading.Thread(target=send_logout_request, args=("5",))
#     thread2 = threading.Thread(target=send_logout_request, args=("7",))
#
#     # 启动线程
#     thread1.start()
#     thread2.start()
#
#     # 等待线程完成
#     thread1.join()
#     thread2.join()
import socket
import struct
import select
import sys
import base_pb2

import socket
import struct
import threading
import sys
import base_pb2
logged_in_user_id = None
def receive_messages(client_socket):
    buffer = b''
    try:
        while True:
            data = client_socket.recv(4096)
            if not data:
                print("[INFO] Server closed the connection.")
                break
            buffer += data

            # 解析完整消息
            while len(buffer) >= 8:
                data_len = struct.unpack('<I', buffer[:4])[0]
                total_length = data_len + 8
                if len(buffer) < total_length:
                    break
                msg_id = struct.unpack('<I', buffer[4:8])[0]
                msg_body = buffer[8:total_length]

                if msg_id == 101:  # 登录响应
                    login_response = base_pb2.LoginResponse()
                    login_response.ParseFromString(msg_body)
                    print("[INFO] Login successful.")
                    print(f"  User ID: {login_response.user_id}")
                    print(f"  Status: {login_response.status}")
                    global logged_in_user_id
                    logged_in_user_id = login_response.user_id  # 存储用户ID
                    print("[INFO] Type 'logout' to log out or 'quit' to exit.")


                elif msg_id == 102:  # 登出响应
                    logout_response = base_pb2.LogoutResponse()
                    logout_response.ParseFromString(msg_body)
                    print(f"[INFO] Logout for user: {logout_response.user_id}")
                    print(f"[INFO] Status: {logout_response.status}")
                    print("[INFO] Disconnecting...")
                    client_socket.close()
                    sys.exit(0)

                elif msg_id == 201:  # 玩家上线广播
                    online_notify = base_pb2.PlayerOnlineNotify()
                    online_notify.ParseFromString(msg_body)
                    print(f"[NOTIFY] Player Online: {online_notify.player_name} (ID: {online_notify.player_id})")
                    print(f"  Position: ({online_notify.pos_x}, {online_notify.pos_y})")
                    print(f"  Status: {online_notify.status}")

                elif msg_id == 202:  # 玩家下线广播
                    offline_notify = base_pb2.PlayerOfflineNotify()
                    offline_notify.ParseFromString(msg_body)
                    print(f"[NOTIFY] Player Offline: {offline_notify.player_id}")

                buffer = buffer[total_length:]

    except Exception as e:
        print(f"[ERROR] Receiving messages failed: {e}")
    finally:
        client_socket.close()

def send_login_request(host, port, username, password):
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.connect((host, port))

    # 构造并发送登录请求
    login_request = base_pb2.LoginRequest()
    login_request.username = username
    login_request.password = password
    msg_body = login_request.SerializeToString()

    data_len = len(msg_body)
    full_message = struct.pack('<I', data_len) + struct.pack('<I', 1) +  msg_body
    print(f"[DEBUG] Sending login request for user: {username}")
    client_socket.sendall(full_message)

    # 启动后台线程接收消息
    receiver_thread = threading.Thread(target=receive_messages, args=(client_socket,))
    receiver_thread.start()

    # 主线程处理用户输入
    try:
        while True:
            try:
                cmd = input().strip().lower()
                if cmd == 'logout':
                    # 构造登出请求
                    logout_request = base_pb2.LogoutRequest()
                    logout_request.user_id = logged_in_user_id
                    msg_body = logout_request.SerializeToString()
                    data_len = len(msg_body)
                    full_message = struct.pack('<I', data_len) + struct.pack('<I', 2) +  msg_body
                    client_socket.sendall(full_message)
                    print("[DEBUG] Sent logout request.")
                elif cmd == 'quit':
                    print("[INFO] Exiting without logging out.")
                    client_socket.close()
                    sys.exit(0)
                else:
                    print("Unknown command. Use 'logout' or 'quit'.")
            except KeyboardInterrupt:
                print("\n[INFO] Exiting...")
                client_socket.close()
                sys.exit(0)
    except Exception as e:
        print(f"[ERROR] Main thread error: {e}")
    finally:
        client_socket.close()
if __name__ == "__main__":
    send_login_request("192.168.13.129", 8888, "Noct", "qzx123456")