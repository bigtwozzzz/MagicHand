#include "GameChannel.h"
#include"GameRole.h"
GameChannel::GameChannel(int _fd) : ZinxTcpData(_fd){

}
GameChannel:: ~GameChannel() {
	if (NULL != m_pGameProtocol) {
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
	//��Э���ͨ����
	pChannel->m_pGameProtocol = pProtocol;
	pProtocol->m_pGameChannel = pChannel;
	//����Һ�Э��
    pProtocol->m_pGameRole = pRole;
    pRole->m_pGameProtocol = pProtocol;
	//ע��Э��
	ZinxKernel::Zinx_Add_Proto(*pProtocol);
	//ע�����
	ZinxKernel::Zinx_Add_Role(*pRole);
	return pChannel;
}