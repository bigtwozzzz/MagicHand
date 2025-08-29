#include "GameRole.h"
#include "GameMsg.h"
#include "GameWorld.h"
#include "GameProtocol.h"
#include "GameChannel.h"
#include "protos//base.pb.h"
#include "protos/broadcast.pb.h"
#include "protos/character.pb.h"
#include "protos/combat.pb.h"
#include "protos/globalrandom.pb.h"


static GameWorld gameWorld(0,400,0,400);
globalrandom::GlobalRandomNum g_global_random_num;
globalrandom::GlobalRandomNum GenerateGlobalRandomNum()
{
	/// TODO
	globalrandom::GlobalRandomNum random_num;

	// 1. 高精度时间戳
	auto now = std::chrono::high_resolution_clock::now();
	auto ns = now.time_since_epoch();
	uint64_t timestamp = static_cast<uint64_t>(
		std::chrono::duration_cast<std::chrono::nanoseconds>(ns).count()
		);

	// 2. 使用 std::random_device（ 注意平台兼容性）
	std::random_device rd;
	uint32_t random_salt = rd() ^ rd();

	// 3. 混合生成 32 位值
	uint32_t combined_uint = static_cast<uint32_t>(
		(timestamp ^ (timestamp >> 32) ^ random_salt) & 0xFFFFFFFF
		);

	// 4. 安全转换为 int32_t
	int32_t combined_int = static_cast<int32_t>(combined_uint);

	// 5. 设置种子
	random_num.set_seed(combined_int);

	g_global_random_num = random_num;
	std::cout << "After set_seed: " << g_global_random_num.seed() << std::endl;
	std::cout << "Address: " << &g_global_random_num << std::endl;
	return random_num;
}
const globalrandom::GlobalRandomNum& GetGlobalRandomNum()
{
	return g_global_random_num;
}
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

	auto context = db_request->Connect();
	if (!context) {
		throw std::runtime_error("Failed to connect to Redis.");
	}

	// 构造用户键
	std::string user_key = "user:" + m_iID;
	base::User user;

	// 读取用户数据
	if (!db_request->Read(context, user_key, &user)) {
		redisFree(context);
		throw std::runtime_error("User not found for ID: " + m_iID);
	}

	// 获取角色 ID
	std::string role_id = user.role_id();

	// 构造角色键
	std::string character_key = "character:" + role_id;

	pmsg->set_player_id(m_iID);
	pmsg->set_player_name(username);
	pmsg->set_pos_x(x);
	pmsg->set_pos_y(z);
	pmsg->set_status(common::IDLE);
	pmsg->set_role_id(role_id);
	std::cout << "[Server] Broadcasting Login - Player ID: " << m_iID << ", Username: " << username << "Role ID: "<<role_id<<std::endl;
	auto player_list = gameWorld.getPlayers(this);
	std::cout<< "[Server] Broadcasting Login - Player List: " << player_list.size()<<std::endl;
	for (auto player : player_list) {
		if (player != this) {
			//std::cout<<"player: "<<player->getUserID()<<std::endl;
			GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_PLAYER_ONLINE_NOTIFY, pmsg);
			auto pRole = dynamic_cast<GameRole*>(player);
			ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
		}
	}

}
void GameRole::broadcastLogout(std::string id, std::string name) { 
	
	broadcast::PlayerOfflineNotify *pmsg = new broadcast::PlayerOfflineNotify();
    pmsg->set_player_id(id);
	pmsg->set_player_name(name);
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
        if (player != this) {
            GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_PLAYER_OFFLINE_NOTIFY, pmsg);
            auto pRole = dynamic_cast<GameRole*>(player);
            ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
        }
	}
}
void GameRole::SendFirstScene() {
	std::string first_id = db_request->GetFirstSceneId();
	if (first_id.empty()) return;

	scene::SceneData* pmsg = new scene::SceneData();
	if (db_request->Read(db_request->Connect(), "scene:" + first_id, pmsg)) {
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_SCENE_DATA, pmsg);
		ZinxKernel::Zinx_SendOut(*pRet, *(this->m_pGameProtocol));
		this->scene_id = first_id;  // 更新当前场景
	}
	else {
		delete pmsg;
	}
}

void GameRole::SendNextScene() {
	std::string next_id = db_request->GetNextSceneId(this->scene_id);
	if (next_id.empty()) {
		std::cout << "[INFO] No more scenes to load." << std::endl;
		return;
	}

	scene::SceneData* pmsg = new scene::SceneData();
	if (db_request->Read(db_request->Connect(), "scene:" + next_id, pmsg)) {
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_SCENE_DATA, pmsg);
		ZinxKernel::Zinx_SendOut(*pRet, *(this->m_pGameProtocol));
		this->scene_id = next_id;  // 更新为下一关
	}
	else {
		delete pmsg;
	}
}
void GameRole::broadcastNextScene() {
	// 1. 获取下一关 ID
	std::string next_scene_id = db_request->GetNextSceneId(this->scene_id);
	if (next_scene_id.empty()) {
		std::cout << "[INFO] No more scenes to broadcast after: " << this->scene_id << std::endl;
		return;
	}

	// 2. 从 Redis 加载下一关场景数据
	scene::SceneData* pmsg = new scene::SceneData();
	auto context = db_request->Connect();

	if (!db_request->Read(context, "scene:" + next_scene_id, pmsg)) {
		std::cerr << "[ERROR] Failed to read next scene data from Redis: " << next_scene_id << std::endl;
		delete pmsg;
		redisFree(context);
		return;
	}

	// 3. 调试输出
	std::cerr << "[DEBUG] Broadcasting next scene: " << next_scene_id << std::endl;
	std::cerr << "[DEBUG] Scene Data Content:\n" << pmsg->DebugString() << std::endl;

	// 4. 获取当前场景中的所有玩家（包括自己）
	auto player_list = gameWorld.getPlayers(this);
	if (player_list.empty()) {
		std::cout << "[INFO] No players in current scene to broadcast." << std::endl;
		delete pmsg;
		redisFree(context);
		return;
	}

	// 5. 向每个玩家发送“下一关”场景数据
	for (auto player : player_list) {
		GameRole* pRole = dynamic_cast<GameRole*>(player);
		if (!pRole) continue;

		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_SCENE_DATA, pmsg);
		ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));

		std::cout << "[Server] Broadcasted NEXT Scene - Scene ID: " << next_scene_id
			<< " to User ID: " << player->getUserID() << std::endl;
	}

	// 6. 清理资源（注意：pmsg 被多个 GameMsg 共享，不能在这里 delete！）
	//    → 必须由 ZinxKernel 或接收方负责释放（假设框架会 deep copy 或延迟 delete）
	//    如果框架不自动管理内存，你需要为每个玩家 deep copy pmsg

	redisFree(context);
}
void GameRole::broadcastScene(std::string scene_id)
{
	scene::SceneData* pmsg = new scene::SceneData();
	pmsg->set_scene_id(scene_id);
	auto context = db_request->Connect();

	if (!db_request->Read(context, "scene:" + scene_id, pmsg)) {
		std::cerr << "[ERROR] Failed to read scene data from Redis for scene_id: " << scene_id << std::endl;
	}
	else {
		// 添加调试信息 - 打印场景数据
		std::cerr << "[DEBUG] Successfully loaded scene data for " << scene_id << std::endl;
		std::cerr << "[DEBUG] Scene Data Content:\n" << pmsg->DebugString() << std::endl;

		// 逐个字段调试（可选）
		std::cerr << "[DEBUG] Scene ID: " << pmsg->scene_id() << std::endl;

		// 打印怪物列表详细信息
		for (const auto& monster : pmsg->monsters()) {
			std::cerr << "[DEBUG] Monster ID: " << monster.monster_id()
				<< " | Type: " << monster.type()
				<< " | HP: " << monster.current_hp() << "/" << monster.max_hp()
				<< " | Position: (" << monster.pos_x() << ", " << monster.pos_y() << ", " << monster.pos_z() << ")"
				<< std::endl;
		}
	}

	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_SCENE_DATA, pmsg);
		auto pRole = dynamic_cast<GameRole*>(player);
		std::cout << "[Server] Broadcasting Scene - Scene ID: " << scene_id << " to "<< player->getUserID() << std::endl;
		ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
	}
	redisFree(context);
}

void GameRole::broadcastEnemyMove(std::string enemy_id, float target_x, float target_y) {
	broadcast::MonsterMoveNotify* pmsg = new broadcast::MonsterMoveNotify();
    pmsg->set_entity_id(enemy_id);
    pmsg->set_pos_x(target_x);
    pmsg->set_pos_y(target_y);

	auto player = gameWorld.getPlayers(this);
    for (auto player : player) {
		auto pRet = new GameMsg(GameMsg::MSG_TYPE_MONSTER_MOVE_NOTIFY, pmsg);
		auto pRole = dynamic_cast<GameRole*>(player);
		ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
	}

	if (!updateMonsterPositionInDB(enemy_id, target_x, target_y)) {
		std::cerr<<"[ERROR] Failed to update monster position in database"<<std::endl;
	}
}
void GameRole::broadcastPlayerMove(std::string role_id, float target_x, float target_y) {
	// 创建 CharacterMoveNotify 消息
	broadcast::CharacterMoveNotify* pmsg = new broadcast::CharacterMoveNotify();
	pmsg->set_entity_id(role_id);
	pmsg->set_pos_x(target_x);
	pmsg->set_pos_y(target_y);

	// 获取在线玩家列表并广播移动通知
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_CHARACTER_MOVE_NOTIFY, pmsg);
		auto pRole = dynamic_cast<GameRole*>(player);
		ZinxKernel::Zinx_SendOut(*pRet, *(pRole->m_pGameProtocol));
	}

	// 更新角色的位置
	x = target_x;
	z = target_y;

	// 将新的位置信息写入数据库
	if (!updateCharacterPositionInDB(role_id, target_x, target_y)) {
		std::cerr << "[ERROR] Failed to update character position in database for role_id: " << role_id << std::endl;
	}
}

bool GameRole::updateCharacterPositionInDB(std::string role_id, float pos_x, float pos_y) {
	// 获取 Redis 连接
	auto context = db_request->Connect();
	if (!context) {
		std::cerr << "[ERROR] Failed to connect to Redis." << std::endl;
		return false;
	}
	// 构造角色键
	std::string character_key = "character:" + role_id;
	character::CharacterBase character_base;

	// 读取角色数据
	if (!db_request->Read(context, character_key, &character_base)) {
		redisFree(context);
		std::cerr << "[ERROR] Character not found for ID: " << role_id << std::endl;
		return false;
	}

	// 更新角色的位置信息
	character_base.set_pos_x(pos_x);
	character_base.set_pos_y(pos_y);

	// 将更新后的角色数据写回 Redis
	if (!db_request->Write(context, character_key, character_base)) {
		redisFree(context);
		std::cerr << "[ERROR] Failed to write updated character data back to Redis for role_id: " << role_id << std::endl;
		return false;
	}
	sendInfo(false);
	// 释放 Redis 连接
	redisFree(context);

	return true;
}
bool GameRole::updateMonsterPositionInDB(std::string role_id, float pos_x, float pos_y) {
	// 获取 Redis 连接
	auto context = db_request->Connect();
	if (!context) {
		std::cerr << "[ERROR] Failed to connect to Redis." << std::endl;
		return false;
	}
	std::string monster_key = "monster:" + role_id;
	enemy::MonsterBase enemy_base;
	if (!db_request->Read(context, monster_key, &enemy_base)) {
		redisFree(context);
		std::cerr << "[ERROR] Monster not found for ID: " << role_id << std::endl;
		return false;
	}
	enemy_base.set_pos_x(pos_x);
	enemy_base.set_pos_y(pos_y);
	if (!db_request->Write(context, monster_key, enemy_base)) {
        redisFree(context);
        std::cerr << "[ERROR] Failed to update monster position in Redis." << std::endl;
        return false;
	}
    redisFree(context);
    return true;
}
bool GameRole::UpdateCharacterInfoInDB(std::string player_id, std::string player_name, std::string role_id) {
	auto context = db_request->Connect();
	if (!context) {
		std::cerr << "[ERROR] Failed to connect to Redis." << std::endl;
		return false;
	}
	// 构造角色键
	std::string character_key = "character:" + role_id;
	character::CharacterBase character_base;

	// 读取角色数据
	if (!db_request->Read(context, character_key, &character_base)) {
		redisFree(context);
		std::cerr << "[ERROR] Character not found for ID: " << role_id << std::endl;
		return false;
	}
	character_base.set_player_id(player_id);
	character_base.set_player_name(player_name);

	// 将更新后的角色数据写回 Redis
	if (!db_request->Write(context, character_key, character_base)) {
		redisFree(context);
		std::cerr << "[ERROR] Failed to write updated character data back to Redis for role_id: " << role_id << std::endl;
		return false;
	}
	sendInfo(false);
	// 释放 Redis 连接
	redisFree(context);

	return true;
}
void GameRole::broadcastSkillInfo(std::string player_id, std::string skill_id) {
	skill::SkillDefinition *pmsg = new skill::SkillDefinition();
	// 获取 Redis 连接
	auto context = db_request->Connect();
	if (!context) {
		throw std::runtime_error("Failed to connect to Redis.");
	}
	std::string skill_key = "skill:" + skill_id;
    if (!db_request->Read(context, skill_key, pmsg)) {
		redisFree(context);
		std::cerr << "[ERROR] Skill not found for ID: " << skill_id << std::endl;
		return;
	}
	std::cout << "[DEBUG] SkillDefinition loaded:" << std::endl;
	std::cout << "  skill_id: " << pmsg->skill_id() << std::endl;
	std::cout << "  skill_name: " << pmsg->skill_name() << std::endl;
	std::cout << "  skill_type: " << pmsg->skill_type() << std::endl;
	std::cout << "  base_damage: " << pmsg->base_damage() << std::endl;
	std::cout << "  cast_time: " << pmsg->cast_time() << "s" << std::endl;
	std::cout << "  cool_down: " << pmsg->cool_down() << "s" << std::endl;
	std::cout << "  duration: " << pmsg->duration() << "s" << std::endl;
	std::cout << "  mana_cost: " << pmsg->mana_cost() << std::endl;

	// 打印 effects 列表
	std::cout << "  effects: [";
	for (int i = 0; i < pmsg->effects_size(); ++i) {
		std::cout << pmsg->effects(i);
		if (i != pmsg->effects_size() - 1) std::cout << ", ";
	}
	std::cout << "]" << std::endl;

	std::cout << "  element_type: " << pmsg->element_type() << std::endl;

	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		GameMsg* pRet = new GameMsg(GameMsg::MSG_TYPE_SKILL_INFO_DATA, pmsg);
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
	std::cout<<"[INFO] broadcastSelectRequestNotify"<<std::endl;
	auto player_list = gameWorld.getPlayers(this);
    for (auto player : player_list) {
        std::cout<<"[INFO] player"<<std::endl;
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

const globalrandom::GlobalRandomNum& GameRole::GenerateRandomSeed()
{
	return GetGlobalRandomNum();
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
		UpdateCharacterInfoInDB(m_iID, username, role_id);
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
void GameRole::sendRandomNum() {
	auto player_list = gameWorld.getPlayers(this);
	for (auto player : player_list) {
		try {
			const auto& random_num = GenerateRandomSeed();
			std::cout << "In sendRandomNum: " << random_num.seed() << std::endl;
			std::cout << "Address: " << &random_num << std::endl;
			auto* seed = const_cast<globalrandom::GlobalRandomNum*> (&random_num);
			std::cout<<"send random num: "<<random_num.seed()<<" to "<<player->getUserID()<<std::endl;
			GameMsg* pmsg = new GameMsg(GameMsg::MSG_TYPE_RANDOM_NUMBER, seed);
			ZinxKernel::Zinx_SendOut(*pmsg, *(this->m_pGameProtocol));

		} catch (const std::exception& e) {
			std::cerr << "Error: " << e.what() << std::endl;
		}
	}
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
					<< "player_id=" << char_info.player_id()
					<< "player_name=" << char_info.player_name()
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
			<< "player_name=" << char_info.player_name()
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

base::User GameRole::GetUserByID(std::string user_id) {
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
	// 释放 Redis 连接
	redisFree(context);

	// 返回角色信息
	return user;
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
					sendRandomNum();
					SendFirstScene();
					std::cout << "[INFO] User " << username << " logged in successfully" << std::endl;
				} else {
					std::cout << "[ERROR] Failed to add player to gameWorld" << std::endl;
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
			base::User user = GetUserByID(m_iID);
			broadcastLogout(m_iID, user.username()); // 登出广播 no: 202
			handleLogout();
			std::cout << "logout" << '\n';
			//Fini(); //登出回应 no: 102
			break;
		}
		case GameMsg::MSG_TYPE_MOVE_REQUEST: //接收角色移动请求 no: 3
		{
			float target_x = dynamic_cast<character::MoveRequest*>(single->m_pMsg)->target_x();
			float target_y = dynamic_cast<character::MoveRequest*>(single->m_pMsg)->target_y();
			if (IsPlayerRole(dynamic_cast<character::MoveRequest*>(single->m_pMsg)->role_id())) {
				broadcastPlayerMove((dynamic_cast<character::MoveRequest*>(single->m_pMsg)->role_id()), target_x, target_y); //角色移动广播 no: 205
			}
			else {
				std::cout<<"move need player role_id"<<std::endl;
				//broadcastEnemyMove((dynamic_cast<character::MoveRequest*>(single->m_pMsg)->role_id()), target_x, target_y);
			}
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
			/*broadcastEnemyHit(
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->target_id(),
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->entity_type(),
				dynamic_cast<combat::AttackRequest*>(single->m_pMsg)->entity_id()
			);*/
			break;
		}
		case GameMsg::MSG_TYPE_PLAYER_SELECT_STAGE_REQUEST: //接收选择关卡请求 no: 5
		{
			auto selectMsg = dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg);
			if (!selectMsg) {
				std::cerr << "[ERROR] Failed to parse PlayerSelectStageRequest" << std::endl;
				break;
			}

			std::string stage_id = selectMsg->stage_id();
			//gameWorld.resetConfirmStates();
			gameWorld.StartStageVote(stage_id);
			std::cout << "测试: " << dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->stage_id()<<" "<< dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->Utf8DebugString() << std::endl;
			broadcastSelectRequestNotify(
				dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->stage_id(),
				dynamic_cast<broadcast::PlayerSelectStageRequest*>(single->m_pMsg)->player_id()
			);// 选择关卡广播 no: 203
			break;
		}
		case GameMsg::MSG_TYPE_PLAYER_CONFIRM_STAGE_RESPONSE: //接收确认关卡请求 no: 6
		{
			// 1. 解析确认消息
			auto confirmMsg = dynamic_cast<broadcast::PlayerConfirmStageResponse*>(single->m_pMsg);
			if (!confirmMsg) {
				std::cerr << "[ERROR] Failed to cast message to PlayerConfirmStageResponse" << std::endl;
				break;
			}

			// 2. 提取关键信息
			std::string player_id = confirmMsg->player_id();
			std::string stage_id = confirmMsg->stage_id();
			common::StageSelectState state = confirmMsg->state();
			std::cout<<"player: " + player_id<<std::endl;
			// 1. 更新投票状态
			if (!gameWorld.updateConfirmState(player_id, state)) {
				break; // 更新失败（如无投票进行中）
			}

			// 2. 检查是否所有人都已投票
			if (gameWorld.areAllPlayersVoted()) {
				// 3. 此时才判断结果
				bool isSuccess = gameWorld.checkAllConfirmed(stage_id);
				std::cout<<"isSuccess: "<<isSuccess<<std::endl;
				// 4. 广播结果（只广播一次）
				broadcastSelectResultNotify(stage_id, isSuccess);

				if (isSuccess) {
					broadcastScene(stage_id);
					broadcastNextScene();
				}

				// 5. 结束投票
				gameWorld.resetConfirmStates();
			}


			break;
		}

		case GameMsg::MSG_TYPE_SKILL_INFO_REQUEST: //接收技能信息请求 no: 7
		{
			auto skillInfoMsg = dynamic_cast<skill::SkillInfoRequest*>(single->m_pMsg);
			if (!skillInfoMsg) {
                std::cerr << "[ERROR] Failed to parse SkillInfoRequest" << std::endl;
				break;
			}
			broadcastSkillInfo(dynamic_cast<skill::SkillInfoRequest*>(single->m_pMsg)->player_id(),
				dynamic_cast<skill::SkillInfoRequest*>(single->m_pMsg)->skill_id());
			break;
		}
		default:
			break;
		}
	}
	return nullptr;
}
bool GameRole::IsPlayerRole(const std::string& role_id) {
	return !role_id.empty() && role_id.find("player_") == 0;
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
	std::cout << "[GameRole] Connection closed, triggering logout for user." << std::endl;

	// 如果 m_iID 已设置（已登录），则广播登出
	//if (m_iID != -1)  // 假设 m_iID 初始化为 -1
	//{
		//m_iID = dynamic_cast<base::LogoutRequest*>(single->m_pMsg)->user_id();
	try {
		base::User user = GetUserByID(m_iID);
		broadcastLogout(m_iID, user.username()); // 登出广播 no: 202
		handleLogout();
	}catch (...) {
		std::cout << "error" << std::endl;
	}
		     // 清理当前角色状态
		//gameWorld.RemovePlayer(this);  // 从世界中移除
	//}
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