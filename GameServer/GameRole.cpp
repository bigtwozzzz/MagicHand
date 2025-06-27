#include "GameRole.h"
#include "GameMsg.h"
#include "GameWorld.h"
#include "GameProtocol.h"
#include "GameChannel.h"
#include "protos//base.pb.h"
#include "protos/broadcast.pb.h"
#include "protos/character.pb.h"
#include "protos/combat.pb.h"
static GameWorld gameWorld(0,400,0,400);

GameRole::GameRole() {
	status = "online";
	x = 100;
	z = 100;
}
GameRole::~GameRole() {}


GameMsg* GameRole::handleLogin()
{
	base::LoginResponse *pmsg = new base::LoginResponse();
    pmsg->set_user_id(m_iID);
    pmsg->set_status(status);
	GameMsg *pRet = new GameMsg(GameMsg::MSG_TYPE_LOGIN_RESPONSE, pmsg);
	return pRet;
}
void GameRole::broadcastLogin(std::string username) {
	broadcast::PlayerOnlineNotify *pmsg = new broadcast::PlayerOnlineNotify();
	pmsg->set_player_id(m_iID);
	pmsg->set_player_name(username);
	pmsg->set_pos_x(x);
	pmsg->set_pos_y(z);
	pmsg->set_status(common::IDLE);
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		if (player != this) {
			GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_PLAYER_ONLINE_NOTIFY, pmsg);
			auto pRole = dynamic_cast<GameRole*>(player);
			ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
		}
	}
}
void GameRole::broadcastLogout(std::string id) { 
	broadcast::PlayerOfflineNotify *pmsg = new broadcast::PlayerOfflineNotify();
    pmsg->set_player_id(id);
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
        if (player != this) {
            GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_PLAYER_OFFLINE_NOTIFY, pmsg);
            auto pRole = dynamic_cast<GameRole*>(player);
            ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
        }
	}
}
void GameRole::broadcastPlayerMove(std::string role_id, float target_x, float target_y) {
	broadcast::CharacterMoveNotify *pmsg = new broadcast::CharacterMoveNotify();
	pmsg->set_entity_id(m_iID);
	pmsg->set_pos_x(x);
	pmsg->set_pos_y(z);
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_CHARACTER_MOVE_NOTIFY, pmsg);
		auto pRole = dynamic_cast<GameRole*>(player);
        ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
	}
}

void GameRole::broadcastPlayerAttack(std::string entity_id, combat::EntityType entity_type, std::string target_id, float attack_angle, std::string skill_id, float cast_time) {
	broadcast::EntityAttackNotify *pmsg = new broadcast::EntityAttackNotify();
	pmsg->set_entity_id(entity_id);
	pmsg->set_entity_type(entity_type);
	pmsg->set_target_id(target_id);
	pmsg->set_attack_angle(attack_angle);
	pmsg->set_skill_id(skill_id);
	pmsg->set_cast_time(cast_time);
	auto player_list = gameWorld.getPlayers(this);
    for (auto player : player_list) {
          GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_CHARACTER_ATTACK_NOTIFY, pmsg);
          auto pRole = dynamic_cast<GameRole*>(player);
          ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
    }
}
void GameRole::broadcastSelectRequestNotify(std::string stage_id, std::string player_id) {
    broadcast::StageSelectRequestNotify*pmsg = new broadcast::StageSelectRequestNotify();
    pmsg->set_stage_id(stage_id);
    pmsg->set_player_id(player_id);

	auto player_list = gameWorld.getPlayers(this);
    for (auto player : player_list) {
          GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_STAGE_SELECT_REQUEST_NOTIFY, pmsg);
		  
          auto pRole = dynamic_cast<GameRole*>(player);
          ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
    }
}
void GameRole::broadcastSelectResultNotify(std::string stage_id, bool isSuccess) {
    broadcast::StageSelectResultNotify*pmsg = new broadcast::StageSelectResultNotify();
    pmsg->set_stage_id(stage_id);
    pmsg->set_is_all_confirmed(isSuccess);
	auto player_list = gameWorld.getPlayers(this);
    for (auto player : player_list) {
          GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_STAGE_SELECT_RESULT_NOTIFY, pmsg);
          auto pRole = dynamic_cast<GameRole*>(player);
          ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
    }
}
void GameRole::broadcastEnemyHit(std::string entity_id, combat::EntityType entity_type, std::string attacker_id)
{
    broadcast::EntityHitNotify*pmsg = new broadcast::EntityHitNotify();
    pmsg->set_entity_id(entity_id);
    pmsg->set_entity_type(entity_type);
    pmsg->set_attacker_id(attacker_id);
	int blood = countBlood();
	int damage = countDamage();
	pmsg->set_damage(damage);
	if (blood - damage <= 0) {
		pmsg->set_new_monster_state(common::M_DEAD);
	}
	auto player_list = gameWorld.getPlayers(this);
    for (auto player : player_list) {
          GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_ENEMY_HIT_BROADCAST, pmsg);
          auto pRole = dynamic_cast<GameRole*>(player);
          ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
    }
}
bool GameRole::Init() { 
	bool bRet = false;
	m_iID = m_pGameProtocol->m_pGameChannel->GetFd();
	
	bRet = gameWorld.AddPlayer(this);
	if (true == bRet) {
		auto pMsg = handleLogin();
		ZinxKernel::Zinx_SendOut(*pMsg, *m_pGameProtocol);
	}
	return bRet; 
}
//处理用户请求
UserData* GameRole::ProcMsg(UserData& _poUserData) {
	//test
	GET_REF2DATA(MultiMsg, input, _poUserData);
	for (auto single : input.m_listMsg) {
		/*std::cout << "type is " << single->enMsgType << std::endl;
		std::cout << single->m_pMsg->Utf8DebugString() << std::endl;*/
		switch (single->enMsgType)
		{
		case GameMsg::MSG_TYPE_LOGIN_REQUEST: //接收登录请求 no: 1
		{
			if (Init()) {	//进行登录回应 no: 101
				broadcastLogin(dynamic_cast<base::LoginRequest*>(single->m_pMsg)->username());
			}
			else {
				///登录失败逻辑暂时没写
				std::cout << "login failed" << std::endl;
				///ZinxKernel::Zinx_SendOut(*single, *m_pGameProtocol);
			}
			break;
		}
		case GameMsg::MSG_TYPE_LOGOUT_REQUEST: //接收登出请求 no: 2
		{
			broadcastLogout(m_iID); // 登出广播 no: 202
			Fini(); //登出回应 no: 102
			break;
		}
		case GameMsg::MSG_TYPE_MOVE_REQUEST: //接收角色移动请求 no: 3
		{
			float target_x = dynamic_cast<character::MoveRequest*>(single->m_pMsg)->target_x();
			float target_y = dynamic_cast<character::MoveRequest*>(single->m_pMsg)->target_y();
			broadcastPlayerMove((dynamic_cast<character::MoveRequest*>(single->m_pMsg)->role_id()), target_x, target_y); //角色移动广播 no: 205
			break;
		}
		case GameMsg::MSG_TYPE_ATTACK_REQUEST: //接收角色攻击请求 no: 4
		{
			broadcastPlayerAttack(dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->entity_id(),
				dynamic_cast<combat::AttackRequest*> (single->m_pMsg)->entity_type(),
				dynamic_cast<combat::AttackRequest*> (single->m_pMsg)->target_id(),
				dynamic_cast<combat::AttackRequest*> (single->m_pMsg)->attack_angle(),
				dynamic_cast<combat::AttackRequest*> (single->m_pMsg)->skill_id(),
				dynamic_cast<combat::AttackRequest*> (single->m_pMsg)->cast_time()
			); //角色攻击广播 no: 213
			broadcastEnemyHit(
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->target_id(),
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->entity_type(),
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->entity_id()
			);
			break;
		}
		case GameMsg::MSG_TYPE_PLAYER_SELECT_STAGE_REQUEST: //接收选择关卡请求 no: 5
		{
			std::cout << "测试: " << dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->stage_id()<<" "<< dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->Utf8DebugString() << std::endl;
			broadcastSelectRequestNotify(
				dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->stage_id(),
				dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->player_id()
			);// 选择关卡广播 no: 203
			break;
		}
		//暂时放着没写完
		case GameMsg::MSG_TYPE_PLAYER_CONFIRM_STAGE_RESPONSE: //接收确认关卡请求 no: 6
		{
			bool isSuccess = countIfAll();
			broadcastSelectResultNotify(
				dynamic_cast<broadcast::StageSelectResultNotify*>(single->m_pMsg)->stage_id(),
				isSuccess
			);// 确认关卡广播 no: 204
			break;
		}
		default:
			break;
		}
	}
	return nullptr;
}
/// <summary>
/// 待完善
/// </summary>
/// <returns></returns>
bool GameRole::countIfAll() {
	return true;
}
int GameRole::countBlood() {
	return 100;
}
int GameRole::countDamage() {
	return 10;
}

void GameRole::Fini() {
	base::LogoutResponse *pmsg = new base::LogoutResponse();
    pmsg->set_user_id(m_iID);
	status = "offline";
    pmsg->set_status(status);
    GameMsg *pRet = new GameMsg(GameMsg::MSG_TYPE_LOGOUT_RESPONSE, pmsg);
    ZinxKernel::Zinx_SendOut(*pRet, *m_pGameProtocol);
	gameWorld.DePlayer(this);
}

float GameRole::getX()
{
	return x;
}

float GameRole::getY()
{
	return z;
}
