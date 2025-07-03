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
GameRole::~GameRole() { }
void GameRole::SetDBRequest(DBRequest* db) {
	db_request = db;
}
std::string GameRole::GenerateUserID() {
	/*static int id_counter = 0;
	return "user_" + std::to_string(++id_counter);*/
	return m_iID;
}
// Lua 脚本：尝试分配角色 ID
std::string allocate_role_lua = R"(
    local role_id = ARGV[1]
    local allocated_key = KEYS[1]
    local character_key = "character:" .. role_id

    -- 1. 检查角色是否已分配
    if redis.call("SISMEMBER", allocated_key, role_id) == 1 then
        return 0
    end

    -- 2. 标记角色为已分配
    redis.call("SADD", allocated_key, role_id)

    -- 3. 返回成功
    return 1
)";
std::string deallocate_role_lua = R"(
    local role_id = ARGV[1]
    local allocated_key = KEYS[1]

    -- 移除角色ID
    redis.call("SREM", allocated_key, role_id)

    return 1
)";

bool GameRole::TryDeallocateRoleID(redisContext* context, const std::string& role_id) {
	redisReply* reply = (redisReply*)redisCommand(context,
		"EVAL %s 1 %s %s",
		deallocate_role_lua.c_str(),
		"allocated_roles",  // KEYS[1]
		role_id.c_str()     // ARGV[1]
	);
	if (!reply) {
		return false;
	}

	bool success = (reply->integer == 1);
	freeReplyObject(reply);
	return success;
}
bool GameRole::TryAllocateRoleID(redisContext* context, const std::string& role_id) {
	redisReply* reply = (redisReply*)redisCommand(context,
		"EVAL %s 1 %s %s",
		allocate_role_lua.c_str(),
		"allocated_roles",  // KEYS[1]
		role_id.c_str()     // ARGV[1]
	);
	if (!reply) {
		return false;
	}

	bool success = (reply->integer == 1);
	freeReplyObject(reply);
	return success;
}
std::string GameRole::GenerateRoleID(redisContext* context) {
	// 获取所有角色 ID（假设已知所有角色 ID 列表）
	std::vector<std::string> role_ids = { "player_001", "player_002", "player_003" }; // 从配置或 Redis 中获取

	for (const auto& role_id : role_ids) {
		if (TryAllocateRoleID(context, role_id)) {
			return role_id;
		}
	}

	throw std::runtime_error("All roles are allocated.");
}

GameMsg* GameRole::handleLogin()
{
	base::LoginResponse* pmsg = new base::LoginResponse();
	pmsg->set_user_id(m_iID);
	pmsg->set_status(status);
	std::cout << "[DEBUG] user_id: " << m_iID << " status: " << status << std::endl;

	GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_LOGIN_RESPONSE, pmsg);

	// 发送当前用户的登录响应
	auto pRole = this;
	ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
	return pRet;
}
GameMsg* GameRole::handleLogout()
{
	std::cout << "error use: m_iID: " << m_iID << std::endl;
	base::LogoutResponse* pmsg = new base::LogoutResponse();
	pmsg->set_user_id(m_iID);
	status = "offline";
	pmsg->set_status(status);
	GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_LOGOUT_RESPONSE, pmsg);
	ZinxKernel::Zinx_SendOut(*pRet, *m_pGameProtocol);
	gameWorld.DePlayer(this);
	//// 构建用户 key
	std::string user_key = "user:" + m_iID;

	// 从 Redis 获取用户数据
	base::User user;
	auto context = db_request->Connect();
	if (!db_request->Read(context, user_key, &user)) {
		std::cerr << "[ERROR] Failed to read user data from Redis for key: " << user_key << std::endl;
	}
	else {
		std::string role_id = user.role_id();

		// 取消分配角色ID
		if (!TryDeallocateRoleID(context, role_id)) {
			std::cerr << "[ERROR] Failed to deallocate role ID: " << role_id << std::endl;
		}

		// 删除用户数据
		if (!db_request->Del(context, user_key, user)) {
			std::cerr << "[ERROR] Failed to delete user from Redis: " << user_key << std::endl;
		}
	}
	redisFree(context);
	return pRet;
}
void GameRole::broadcastLogin(std::string username) {
	broadcast::PlayerOnlineNotify *pmsg = new broadcast::PlayerOnlineNotify();
	pmsg->set_player_id(m_iID);
	pmsg->set_player_name(username);
	pmsg->set_pos_x(x);
	pmsg->set_pos_y(z);
	pmsg->set_status(common::IDLE);
	std::cout << "[Server] Broadcasting Login - Player ID: " << m_iID << ", Username: " << username << std::endl;
	auto player_list = gameWorld.getPlayers(this);
	std::cout<< "[Server] Broadcasting Login - Player List: " << player_list.size()<<std::endl;
	for (auto player : player_list) {
		if (player != this) {
			std::cout<<"player: "<<player->getUserID()<<std::endl;
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
character::CharacterBase GameRole::GetCharacterBase(std::string user_id)
{
	// 获取 Redis 连接
	auto context = db_request->Connect();
	if (!context) {
		throw std::runtime_error("Failed to connect to Redis.");
	}

	// 构造用户键
	std::string user_key = "user:" + user_id;
	base::User user;

	// 读取用户数据
	if (!db_request->Read(context, user_key, &user)) {
		redisFree(context);
		throw std::runtime_error("User not found for ID: " + user_id);
	}

	// 获取角色 ID
	std::string role_id = user.role_id();

	// 构造角色键
	std::string character_key = "character:" + role_id;
	character::CharacterBase character_base;

	// 读取角色数据
	if (!db_request->Read(context, character_key, &character_base)) {
		redisFree(context);
		throw std::runtime_error("Character not found for ID: " + role_id);
	}

	// 释放 Redis 连接
	redisFree(context);

	// 返回角色信息
	return character_base;
}
std::string GameRole::GenerateUUID() {
	uuid_t uuid;
	uuid_generate(uuid);
	char uuid_str[37];
	uuid_unparse_lower(uuid, uuid_str);
	return std::string(uuid_str);
}
std::string GameRole::verifyLogin(std::string username, std::string password) {
	//std::cout << "测试" << '\n';
	std::string user_id = "";
	std::string stored_password;

	// 检查用户是否存在
	base::LoginRequest login_request;
	std::cout<<username<<std::endl;
	auto context = db_request->Connect();
	std::cout<< db_request->Read(context, "username:" + username, &login_request)<<std::endl;
	if(db_request->Read(context, "username:" + username, &login_request)){
		// 获取用户密码
		stored_password = login_request.password();
		if (password != stored_password) {
			return "";
		}
		m_iID = GenerateUUID();
		//m_iID = std::to_string(m_pGameProtocol->m_pGameChannel->GetFd());
		std::cout << "[Server] New connection - User ID: " << m_iID << std::endl;
		// 新用户，生成用户ID和角色
		user_id = GenerateUserID();
		std::string role_id = GenerateRoleID(context);
		// 保存用户信息到 Redis
		base::User user;
		user.set_user_id(user_id);
		user.set_username(username);
        user.set_password(password);
        user.set_role_id(role_id);
		std::cout << "[INFO] login success - User ID: " << user_id << ", character ID: " << role_id << std::endl;
		db_request->Write(context, "user:" + user_id, user);
	}
	else {
		std::cout << "user is not exist" << std::endl;
		// 哈希密码
		//std::string hashed_password = HashPasswordWithBcrypt(password);
	}
	redisFree(context);
	return user_id;
}

bool GameRole::Init() {
	DBRequest* db = new DBRequest();
	db_request = db;
	return true;
}
void GameRole::sendInfo(bool includeSelf) {
	// 获取在线玩家列表
	auto player_list = gameWorld.getPlayers(this);
	std::cout << "[Server] Broadcasting Login - Player List Size: " << player_list.size() << std::endl;
	if (includeSelf) {
		for (auto player : player_list) {

			try {
				// 获取目标玩家的角色信息
				character::CharacterBase char_info = GetCharacterBase(player->getUserID());

				// 调试输出角色信息
				std::cout << "[DEBUG] Character Info: "
					<< "role_id=" << char_info.role_id()
					<< ", name=" << char_info.role_name()
					<< ", hp=" << char_info.current_hp()
					<< "/" << char_info.max_hp()
					<< ", level=" << char_info.level()
					<< ", pos=(" << char_info.pos_x() << ", " << char_info.pos_y() << ")"
					<< ", status=" << static_cast<int>(char_info.status())
					<< std::endl;

				// 构造并发送角色信息消息
				GameMsg* msg = new GameMsg(GameMsg::MSG_TYPE_PLAYER_INFO, &char_info);
				ZinxKernel::Zinx_SendOut(*msg, *(this->m_pGameProtocol));
			}
			catch (const std::exception& e) {
				std::cerr << "[ERROR] Failed to get character info for user: " << player->getUserID()
					<< " - " << e.what() << std::endl;
			}
		}
	}
	else {
		character::CharacterBase char_info = GetCharacterBase(this->getUserID());
		// 调试输出角色信息
		std::cout << "[DEBUG] Character Info: "
			<< "role_id=" << char_info.role_id()
			<< ", name=" << char_info.role_name()
			<< ", hp=" << char_info.current_hp()
			<< "/" << char_info.max_hp()
			<< ", level=" << char_info.level()
			<< ", pos=(" << char_info.pos_x() << ", " << char_info.pos_y() << ")"
			<< ", status=" << static_cast<int>(char_info.status())
			<< std::endl;
		for(auto player : player_list) {
			if (player != this) {
				try {
					// 构造并发送角色信息消息
					GameMsg* msg = new GameMsg(GameMsg::MSG_TYPE_PLAYER_INFO, &char_info);
					auto pRole = dynamic_cast<GameRole*>(player);
					ZinxKernel::Zinx_SendOut(*msg, *(pRole->m_pGameProtocol));
				}
				catch (const std::exception& e) {
					std::cerr << "[ERROR] Failed to get character info for user: " << player->getUserID()
						<< " - " << e.what() << std::endl;
				}
			}
		}
	}
}
//处理用户请求
UserData* GameRole::ProcMsg(UserData& _poUserData) {
	GET_REF2DATA(MultiMsg, input, _poUserData);
	for (auto single : input.m_listMsg) {
		switch (single->enMsgType)
		{
		case GameMsg::MSG_TYPE_LOGIN_REQUEST: //接收登录请求 no: 1
		{
			base::LoginRequest* pLoginReq = dynamic_cast<base::LoginRequest*>(single->m_pMsg);
			if (!pLoginReq) {
				std::cerr << "[ERROR] Failed to parse LoginRequest" << std::endl;
				break;
			}

			std::string username = pLoginReq->username();
			std::string password = pLoginReq->password();

			// 验证用户名和密码
			if (verifyLogin(username, password) != "") {
				if (handleLogin()) {
					std::cout << "success" << '\n';
				}
				broadcastLogin(username);
				// 添加玩家到 gameWorld
				if (gameWorld.AddPlayer(this)) {
					sendInfo(true);
					sendInfo(false);
					std::cout << "[INFO] User " << username << " logged in successfully" << std::endl;
				}
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
			m_iID = dynamic_cast<base::LogoutRequest*>(single->m_pMsg)->user_id();
			broadcastLogout(m_iID); // 登出广播 no: 202
			handleLogout();
			std::cout << "logout" << '\n';
			//Fini(); //登出回应 no: 102
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
			//std::cout << "测试: " << dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->stage_id()<<" "<< dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->Utf8DebugString() << std::endl;
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
	//if (is_fini_called_.exchange(true)) return; // 原子操作，确保只执行一次
	
}

float GameRole::getX()
{
	return x;
}

float GameRole::getY()
{
	return z;
}

std::string GameRole::getUserID()
{ 
    return m_iID;
}