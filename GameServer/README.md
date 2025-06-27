### 服务端使用说明
## 可以在main.cpp更改端口号（初始为8888）
## 运行 ./env.bash 直接装环境编译
## 目前运行只实现了登录和场景选择的收发，格式为 4字节正文长度+4字节标识+正文（每个字段长度+字段Ascii码）十六进制小端
## protos里面的bak是proto源文件，其他为生成文件
## GameRole.cpp中的ProcMsg是目前客户端和服务端通信内容，case选项语句是客户端发送，服务端解析，执行语句是服务端发送，客户端解析，客户端需要根据proto源文件对接收和发送格式进行定义，可以找对应语言的protobuf包来处理收发
## GameMsg.cpp中可以找到对应执行函数，和proto源文件一一对应
## 要在visual studio中编译，需要先连接服务器，然后右键项目选择属性，然后进入VC++ 包含目录添加/usr/include;$(LibraryPath)， 库目录添加/usr/lib;$(LibraryPath);/usr/lib/x86_64-linux-gnu/；进入链接器目录库依赖项添加zinx;protobuf;pthread;hiredis， 所有选项附加选项添加-pthread
## ./compile_proto.bash为proto的编译指令