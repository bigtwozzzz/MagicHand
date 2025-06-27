#include "GameChannel.h"
#include "GameMsg.h"
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
	ZinxKernel::ZinxKernelInit();
	//Ìí¼Ó¼àÌýÍ¨µÀ
	ZinxKernel::Zinx_Add_Channel( *new ZinxTCPListen(8888, new GameConnectFact()));
	ZinxKernel::Zinx_Run();
	ZinxKernel::ZinxKernelFini();
}