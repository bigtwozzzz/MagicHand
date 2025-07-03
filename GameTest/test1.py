import socket
import struct
import base_pb2
import broadcast_pb2  # ← 新增导入 broadcast_pb2


import socket
import struct
import threading
import sys
import base_pb2
import broadcast_pb2
import character_pb2  # 新增导入角色信息的 proto 模块

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
                    online_notify = broadcast_pb2.PlayerOnlineNotify()
                    online_notify.ParseFromString(msg_body)
                    print(f"[NOTIFY] Player Online: {online_notify.player_name} (ID: {online_notify.player_id})")
                    print(f"  Position: ({online_notify.pos_x}, {online_notify.pos_y})")
                    print(f"  Status: {online_notify.status}")

                elif msg_id == 202:  # 玩家下线广播
                    offline_notify = broadcast_pb2.PlayerOfflineNotify()
                    offline_notify.ParseFromString(msg_body)
                    print(f"[NOTIFY] Player Offline: {offline_notify.player_id}")

                elif msg_id == 302:  # 角色信息广播（新增）
                    try:
                        character_info = character_pb2.CharacterBase()
                        character_info.ParseFromString(msg_body)
                        print(f"[INFO] Received Character Info for {character_info.role_id}:")
                        print(f"  Name: {character_info.role_name}")
                        print(f"  HP: {character_info.current_hp}/{character_info.max_hp}")
                        print(f"  Level: {character_info.level} | Exp: {character_info.exp}")
                        print(f"  Position: ({character_info.pos_x:.1f}, {character_info.pos_y:.1f})")
                        print(f"  Direction: {character_info.direction:.1f}°")
                        print(f"  Status: {character_info.status}")
                        print("  Skills:")
                        for slot in character_info.skills:
                            print(f"    - Skill ID: {slot.skill_id}")
                            print(f"      Cooldown: {slot.current_cooldown:.1f}s")
                            print(f"      Active: {slot.is_active}")
                    except Exception as e:
                        print(f"[ERROR] Failed to parse character info: {e}")

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
    full_message = struct.pack('<I', data_len) + struct.pack('<I', 1) + msg_body
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
                    full_message = struct.pack('<I', data_len) + struct.pack('<I', 2) + msg_body
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
    send_login_request("192.168.13.129", 8888, "Stella", "qzx123456")