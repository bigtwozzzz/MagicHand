import socket
# 更改ip,端口
# 创建并连接 TCP 套接字
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect(('192.168.13.129', 8888))

# 发送消息（示例）
message = bytes([
    0x0f, 0x00, 0x00, 0x00,  # 总长度 15
    0x05, 0x00, 0x00, 0x00,  # 消息类型 5
    0x0a, 0x07, 0x50, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,  # player_id: "P123456"
    0x12, 0x04, 0x53, 0x30, 0x30, 0x31  # stage_id: "S001"
])
s.send(message)

# 接收并直接打印服务器返回的原始字节码
while True:
    data = s.recv(4096)  # 每次接收最大 4096 字节
    if not data:
        print("Server closed connection.")
        break
    print("Received raw bytes:", data)
    print("Hex view:", ' '.join(f"{b:02x}" for b in data))
    print("String view:", data.decode('utf-8', errors='replace'))