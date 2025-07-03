#include "GameProtocol.h"
#include "GameMsg.h"
#include "GameChannel.h"	
#include "GameRole.h"
#include "protos/base.pb.h"
#include "protos/scene.pb.h"
#include "protos/broadcast.pb.h"
#include "protos/character.pb.h"
#include "protos/enemy.pb.h"
#include "protos/combat.pb.h"
#include "protos/skill.pb.h"
GameProtocol::GameProtocol() {};
GameProtocol:: ~GameProtocol() {
	if (NULL != m_pGameRole) {
		ZinxKernel::Zinx_Del_Role(*m_pGameRole);
        delete m_pGameRole;
	}
}
// ������Ϣת��
UserData* GameProtocol::raw2request(std::string _szInput) {
	// ��Ϣ����4bytes, ��ϢId4bytes��
	MultiMsg* pRet = new MultiMsg();
	szLast.append(_szInput);
	//std::cout<<"input: " + _szInput << std::endl;
	while (1) {
		// �ж���Ϣ����, ����8�ֽڣ�����NULL
		if (szLast.size() < 8) {
			break;
		}
		//��ȡ����
		int iLength = 0;
		iLength |= (szLast[0] << 0) | (szLast[1] << 8) | (szLast[2] << 16) | (szLast[3] << 24);
		int id = 0;
        id |= (szLast[4] << 0) | (szLast[5] << 8) | (szLast[6] << 16) | (szLast[7] << 24);
		//��������û����
		if (szLast.size() - 8 < iLength) {
			break;
		}
		//������������
		//std::cout <<"length: " <<iLength << '\n';
		GameMsg* pGameMsg = new GameMsg((GameMsg::MSG_TYPE)id, szLast.substr(8, iLength));
		pRet->m_listMsg.push_back(pGameMsg);
		szLast.erase(0, iLength + 8);
	}
	//std::cout<<"size: "<<pRet->m_listMsg.size()<<std::endl;
	for (auto single : pRet->m_listMsg) {
		std::cout<<single->m_pMsg->Utf8DebugString()<<std::endl;
		std::cout<<single->enMsgType<<std::endl;
	}
	return pRet;
}
// ������Ϣת��
std::string* GameProtocol::response2raw(UserData& _oUserData) {
	int iLength = 0;
	int id = 0;
	std::string MsgContent;
	GET_REF2DATA(GameMsg, oOutput, _oUserData);
	id = oOutput.enMsgType;
	//std::cout<<"id is "<<id<<std::endl;
	MsgContent = oOutput.serialize();
	iLength = MsgContent.size();
	auto pret = new std::string();

	pret->push_back( (iLength >> 0) & 0xff);
	pret->push_back((iLength >> 8) & 0xff);
	pret->push_back((iLength >> 16) & 0xff);
	pret->push_back((iLength >> 24) & 0xff);
	pret->push_back((id >> 0) & 0xff);
	pret->push_back((id >> 8) & 0xff);
	pret->push_back((id >> 16) & 0xff);
	pret->push_back((id >> 24) & 0xff);
	pret->append(MsgContent);
	//pb::Talk *pmsg = new Pb::Talk();
	//pmsg->set_content("hello")'
	//GameMsg *pGameMsg = nnew GameMsg(GameMsg::MSG_TYPE_TALK, pmsg);
	
	return pret;
}
// ������Ϣͨ����ȡ
Irole* GameProtocol::GetMsgProcessor(UserDataMsg& _oUserDataMsg) {
	return m_pGameRole;
}
Ichannel* GameProtocol::GetMsgSender(BytesMsg& _oBytes) {
	return m_pGameChannel;
}