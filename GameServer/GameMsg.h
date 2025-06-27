#pragma once
#include <zinx.h>
#include <google/protobuf/message.h>
#include<list>
class GameMsg :
    public UserData
{
    public:
        //��Ϣ����
        google::protobuf::Message* m_pMsg = NULL;
        //��Ϣ����
        enum MSG_TYPE
        {
            MSG_TYPE_LOGIN_REQUEST = 1,
            MSG_TYPE_LOGOUT_REQUEST = 2,
            MSG_TYPE_MOVE_REQUEST = 3,
            MSG_TYPE_ATTACK_REQUEST = 4,
            MSG_TYPE_PLAYER_SELECT_STAGE_REQUEST = 5,
            MSG_TYPE_PLAYER_CONFIRM_STAGE_RESPONSE = 6,

            MSG_TYPE_LOGIN_RESPONSE = 101,
            MSG_TYPE_LOGOUT_RESPONSE = 102,

            MSG_TYPE_PLAYER_ONLINE_NOTIFY = 201,
            MSG_TYPE_PLAYER_OFFLINE_NOTIFY = 202,
            MSG_TYPE_STAGE_SELECT_REQUEST_NOTIFY = 203,
            MSG_TYPE_STAGE_SELECT_RESULT_NOTIFY = 204,
            MSG_TYPE_CHARACTER_MOVE_NOTIFY = 205,
            MSG_TYPE_MONSTER_MOVE_NOTIFY = 206, //û����
            MSG_TYPE_CHARACTER_STATUS_UPDATE = 207, //
            MSG_TYPE_MONSTER_STATUS_UPDATE = 208,//
            MSG_TYPE_SKILL_CAST_NOTIFY = 209,//
            MSG_TYPE_ENTITY_ATTACK_NOTIFY = 210, //
            MSG_TYPE_HIT_BROADCAST = 211, //
            MSG_TYPE_ENEMY_HIT_BROADCAST = 212,
            MSG_TYPE_CHARACTER_ATTACK_NOTIFY = 213,

            MSG_TYPE_SCENE_UPDATE = 301, //
            
        } enMsgType;
        //������֪��Ϣ��������
        GameMsg(MSG_TYPE _type, google::protobuf::Message* _pMsg);
        //�����ֽ�����������
        GameMsg(MSG_TYPE _type, std::string _pstream);

        //���л�
        std::string serialize();
        virtual ~GameMsg();
};

class MultiMsg :
    public UserData
{ 
public:
    std::list <GameMsg*> m_listMsg;
};