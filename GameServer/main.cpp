#include "GameChannel.h"
#include "GameMsg.h"
#include "hiredis/hiredis.h"
int main() {
	//test
	//pb::SynicPid * pmsg = new SyncPid();
	//pmsg->set_pid(1);...
	/*base::LoginRequest *pmsg = new base::LoginRequest();
	pmsg->set_username("qzx");
	pmsg->set_password("123456");
	GameMsg gm(GameMsg::MSG_TYPE_LOGIN_ID_NAME, pmsg);
	auto output = gm.serialize();
	for (auto byte : output) {
		printf("%02x ", byte);
	}
	puts("");
	char buf[] = { 0x0a, 0x03, 0x71, 0x7a, 0x78, 0x12, 0x06, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
	std::string input(buf, sizeof(buf));
	auto ingm = GameMsg(GameMsg::MSG_TYPE_LOGIN_ID_NAME, input);*/
	//std::cout << dynamic_cast<base::LoginRequest*> (ingm.m_pMsg)->username()<<std::endl;
	// 创建 Redis 连接
	//redisContext* context = redisConnect("127.0.0.1", 6379);
	//if (context == nullptr || context->err) {
	//	if (context) {
	//		std::cerr << "Redis connection error: " << context->errstr << std::endl;
	//		redisFree(context);
	//	}
	//	else {
	//		std::cerr << "Failed to allocate Redis context" << std::endl;
	//	}
	//	return 1;
	//}
	//const std::string password = "qzx123456";  // 替换为你的实际密码
	//redisReply* verify_reply = (redisReply*)redisCommand(context, "AUTH %s", password.c_str());
	//if (verify_reply == nullptr) {
	//	std::cerr << "Redis AUTH command failed." << std::endl;
	//	redisFree(context);
	//	return 1;
	//}

	//std::string test = "test";
	//redisReply* reply = (redisReply*)redisCommand(context, "SET name %s", test);
	//std::cout << reply->str << '\n';
	//std::cout << "Connected to Redis!" << std::endl;

	ZinxKernel::ZinxKernelInit();
	//添加监听通道
	ZinxKernel::Zinx_Add_Channel( *new ZinxTCPListen(8888, new GameConnectFact()));
	ZinxKernel::Zinx_Run();
	ZinxKernel::ZinxKernelFini();
}