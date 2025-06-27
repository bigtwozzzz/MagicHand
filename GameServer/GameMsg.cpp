#include "GameMsg.h"
#include "protos/base.pb.h"
#include "protos/scene.pb.h"
#include "protos/broadcast.pb.h"
#include "protos/character.pb.h"
#include "protos/enemy.pb.h"
#include "protos/combat.pb.h"
#include "protos/skill.pb.h"
#include <iomanip>
GameMsg::GameMsg(MSG_TYPE _type, google::protobuf::Message* _pMsg): enMsgType(_type), m_pMsg(_pMsg)
{
}

GameMsg::GameMsg(MSG_TYPE _type, std::string _pstream)
{
	enMsgType = _type;
    switch (_type)
    {
    case MSG_TYPE_LOGIN_REQUEST:
        m_pMsg = new base::LoginRequest();
        break;

    case MSG_TYPE_LOGOUT_REQUEST:
        m_pMsg = new base::LogoutRequest();
        break;

    case MSG_TYPE_MOVE_REQUEST:
        m_pMsg = new character::MoveRequest();
        break;

    case MSG_TYPE_ATTACK_REQUEST:
        m_pMsg = new combat::AttackRequest();
        break;

    case MSG_TYPE_PLAYER_SELECT_STAGE_REQUEST:
        m_pMsg = new broadcast::PlayerSelectStageRequest();
        break;

    case MSG_TYPE_PLAYER_CONFIRM_STAGE_RESPONSE:
        m_pMsg = new broadcast::PlayerConfirmStageResponse();
        break;

    case MSG_TYPE_LOGIN_RESPONSE:
        m_pMsg = new base::LoginResponse();
        break;

    case MSG_TYPE_LOGOUT_RESPONSE:
        m_pMsg = new base::LogoutResponse();
        break;

    case MSG_TYPE_PLAYER_ONLINE_NOTIFY:
        m_pMsg = new broadcast::PlayerOnlineNotify();
        break;

    case MSG_TYPE_PLAYER_OFFLINE_NOTIFY:
        m_pMsg = new broadcast::PlayerOfflineNotify();
        break;

    case MSG_TYPE_STAGE_SELECT_REQUEST_NOTIFY:
        m_pMsg = new broadcast::StageSelectRequestNotify();
        break;

    case MSG_TYPE_STAGE_SELECT_RESULT_NOTIFY:
        m_pMsg = new broadcast::StageSelectResultNotify();
        break;

    case MSG_TYPE_CHARACTER_MOVE_NOTIFY:
        m_pMsg = new broadcast::CharacterMoveNotify();
        break;

    case MSG_TYPE_MONSTER_MOVE_NOTIFY:
        m_pMsg = new broadcast::MonsterMoveNotify();
        break;

    case MSG_TYPE_CHARACTER_STATUS_UPDATE:
        m_pMsg = new broadcast::CharacterStatusUpdate();
        break;

    case MSG_TYPE_MONSTER_STATUS_UPDATE:
        m_pMsg = new broadcast::MonsterStatusUpdate();
        break;

    case MSG_TYPE_SKILL_CAST_NOTIFY:
        m_pMsg = new broadcast::SkillCastNotify();
        break;

    case MSG_TYPE_ENTITY_ATTACK_NOTIFY:
        m_pMsg = new broadcast::EntityAttackNotify();
        break;

    case MSG_TYPE_HIT_BROADCAST:
        m_pMsg = new broadcast::EntityAttackNotify();
        break;

    case MSG_TYPE_SCENE_UPDATE:
        m_pMsg = new scene::SceneUpdate();
        break;
    case MSG_TYPE_CHARACTER_ATTACK_NOTIFY:
        m_pMsg = new broadcast::EntityAttackNotify();
        break;
    default:
        // 处理未知消息类型
        
        m_pMsg = nullptr;
        break;
    }
	m_pMsg->ParseFromString(_pstream);
}

std::string GameMsg::serialize()
{
	std::string ret;
	m_pMsg->SerializeToString(&ret);
	return ret;
}
GameMsg::~GameMsg()
{
}