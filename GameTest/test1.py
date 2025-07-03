import socket
import struct
import threading
import sys
import base_pb2
import scene_pb2
import broadcast_pb2
import character_pb2
import common_pb2  # 枚举定义
logged_in_user_id = None
current_stage_id = None  # 可选：记录当前正在确认的关卡 ID


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
                    logged_in_user_id = login_response.user_id
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

                elif msg_id == 302:  # 角色信息广播
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

                elif msg_id == 203:  # 关卡选择请求通知
                    notify = broadcast_pb2.StageSelectRequestNotify()
                    notify.ParseFromString(msg_body)
                    print(f"[NOTIFY] Stage Select Request: Player {notify.player_id} wants to enter stage '{notify.stage_id}'")
                    if notify.stage_name:
                        print(f"  Stage Name: {notify.stage_name}")
                    print("Type 'confirm_stage <stage_id> CONFIRMED' to agree or 'confirm_stage <stage_id> REJECTED' to reject")
                    global current_stage_id
                    current_stage_id = notify.stage_id  # 记录当前待确认的关卡 ID

                elif msg_id == 204:  # 关卡选择结果通知
                    result = broadcast_pb2.StageSelectResultNotify()
                    result.ParseFromString(msg_body)
                    print(f"[NOTIFY] Stage Select Result: Stage '{result.stage_id}' {'confirmed by all players' if result.is_all_confirmed else 'not confirmed'}")
                elif msg_id == 301:  # 场景信息广播
                    try:
                        scene_data = scene_pb2.SceneData()
                        scene_data.ParseFromString(msg_body)
                        print(f"[NOTIFY] Scene Info for '{scene_data.scene_id}':")
                        for monster in scene_data.monsters:
                            print(f"  Monster: {monster.monster_id}")
                            print(f"    Type: {monster.type}")
                            print(f"    HP: {monster.current_hp}/{monster.max_hp}")
                            print(f"    Attack Power: {monster.attack_power}")
                            print(f"    Attack Speed: {monster.attack_speed:.2f}s")
                            print(f"    Move Speed: {monster.move_speed:.2f}")
                            print(f"    Position: ({monster.pos_x:.1f}, {monster.pos_y:.1f}, {monster.pos_z:.1f})")
                            print(f"    Direction: {monster.direction:.2f}°")
                            print(f"    Attack Range: {monster.attack_range:.1f}")
                            print(f"    State: {monster.state}")
                            print(f"    Exp Reward: {monster.exp_reward}")
                            print("-" * 40)
                    except Exception as e:
                        print(f"[ERROR] Failed to parse scene data: {e}")

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
                if cmd.startswith('select_stage '):
                    if logged_in_user_id is None:
                        print("[ERROR] You must be logged in to select a stage.")
                        continue
                    parts = cmd.split()
                    if len(parts) < 2:
                        print("Usage: select_stage <stage_id>")
                        continue
                    stage_id = parts[1]
                    req = broadcast_pb2.PlayerSelectStageRequest()
                    req.player_id = logged_in_user_id
                    req.stage_id = stage_id
                    msg_body = req.SerializeToString()
                    data_len = len(msg_body)
                    full_message = struct.pack('<I', data_len) + struct.pack('<I', 5) + msg_body
                    client_socket.sendall(full_message)
                    print(f"[DEBUG] Sent stage select request: {stage_id}")

                elif cmd.startswith('confirm_stage '):
                    if logged_in_user_id is None:
                        print("[ERROR] You must be logged in to confirm a stage.")
                        continue
                    parts = cmd.split()
                    if len(parts) < 3:
                        print("Usage: confirm_stage <stage_id> [CONFIRMED|REJECTED]")
                        continue
                    stage_id = parts[1]
                    state_str = parts[2].upper()
                    if state_str not in ['CONFIRMED', 'REJECTED']:
                        print("Invalid state. Use 'CONFIRMED' or 'REJECTED'.")
                        continue
                    state = common_pb2.StageSelectState.Value(state_str)
                    resp = broadcast_pb2.PlayerConfirmStageResponse()
                    resp.player_id = logged_in_user_id
                    resp.stage_id = stage_id
                    resp.state = state
                    msg_body = resp.SerializeToString()
                    data_len = len(msg_body)
                    full_message = struct.pack('<I', data_len) + struct.pack('<I', 6) + msg_body
                    client_socket.sendall(full_message)
                    print(f"[DEBUG] Sent stage confirm response: {stage_id} {state_str}")

                elif cmd == 'logout':
                    if logged_in_user_id is None:
                        print("[ERROR] You are not logged in.")
                        continue
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
                    print("Available commands:")
                    print("  select_stage <stage_id> - Request to select a stage")
                    print("  confirm_stage <stage_id> [CONFIRMED|REJECTED] - Respond to stage selection")
                    print("  logout - Log out")
                    print("  quit - Exit the client")
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