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
//����Э�����
AZinxHandler* GameChannel::GetInputNextStage(BytesMsg& _oInput) {
	return m_pGameProtocol;
}

ZinxTcpData* GameConnectFact::CreateTcpDataChannel(int _fd) {
	//����ͨ����Э��
	auto pChannel = new GameChannel(_fd);
	auto pProtocol = new GameProtocol();
	//�������
	auto pRole = new GameRole();
	////�������ݿ�����
	//auto pDBRequest = new DBRequest();
	//auto context = pDBRequest->ConnectToRedis("127.0.0.1", 6379, "qzx123456");
	//pDBRequest->setRedisContext(context);
	//��Э���ͨ����
	pChannel->m_pGameProtocol = pProtocol;
	pProtocol->m_pGameChannel = pChannel;
	//����Һ�Э��
    pProtocol->m_pGameRole = pRole;
    pRole->m_pGameProtocol = pProtocol;

	//����Һ����ݿ�����
   // pRole->SetDBRequest(pDBRequest);

    //pRole->Init();
	//ע��Э��
	ZinxKernel::Zinx_Add_Proto(*pProtocol);
	//ע�����
	ZinxKernel::Zinx_Add_Role(*pRole);

	return pChannel;
}