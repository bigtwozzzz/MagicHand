#include "GameChannel.h"
#include"GameRole.h"
GameChannel::GameChannel(int _fd) : ZinxTcpData(_fd){

}
GameChannel:: ~GameChannel() {
	if (NULL != m_pGameProtocol) {
		std::cout<<"delete m_pGameProtocol"<<std::endl;
		ZinxKernel::Zinx_Del_Proto(*m_pGameProtocol);
		delete m_pGameProtocol;
	}
}
//返回协议对象
AZinxHandler* GameChannel::GetInputNextStage(BytesMsg& _oInput) {
	return m_pGameProtocol;
}

ZinxTcpData* GameConnectFact::CreateTcpDataChannel(int _fd) {
	//创建通道和协议
	auto pChannel = new GameChannel(_fd);
	auto pProtocol = new GameProtocol();
	//创建玩家
	auto pRole = new GameRole();
	////创建数据库请求
	//auto pDBRequest = new DBRequest();
	//auto context = pDBRequest->ConnectToRedis("127.0.0.1", 6379, "qzx123456");
	//pDBRequest->setRedisContext(context);
	//将协议和通道绑定
	pChannel->m_pGameProtocol = pProtocol;
	pProtocol->m_pGameChannel = pChannel;
	//绑定玩家和协议
    pProtocol->m_pGameRole = pRole;
    pRole->m_pGameProtocol = pProtocol;

	//绑定玩家和数据库请求
   // pRole->SetDBRequest(pDBRequest);

    //pRole->Init();
	//注册协议
	ZinxKernel::Zinx_Add_Proto(*pProtocol);
	//注册玩家
	ZinxKernel::Zinx_Add_Role(*pRole);

	return pChannel;
}