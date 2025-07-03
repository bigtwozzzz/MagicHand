#include "DBRequest.h"
#include <cstdlib>
#include <unistd.h>
DBRequest::DBRequest()
{
}
DBRequest::~DBRequest()
{
    if (m_redisContext) {
        redisFree(m_redisContext);
        m_redisContext = nullptr;
    }
}
static std::string get_current_directory() {
    char buffer[1024];
    if (getcwd(buffer, sizeof(buffer)) != nullptr) {
        return std::string(buffer);
    }
    else {
        std::cerr << "Failed to get current directory!" << std::endl;
        return "";
    }
}
bool DBRequest::Init(std::string ip,int port, std::string password)
{

	m_redisContext =  DBRequest::ConnectToRedis(ip, port, password);
	if (!m_redisContext) {
        return false;
	}
    // 验证 Redis 连接是否正常
    redisReply* reply = (redisReply*)redisCommand(m_redisContext, "PING");
    if (!reply || reply->type != REDIS_REPLY_STATUS || std::string(reply->str) != "PONG") {
        std::cerr << "Redis connection validation failed." << std::endl;
        freeReplyObject(reply);
        redisFree(m_redisContext);
        m_redisContext = nullptr;
        return false;
    }
    freeReplyObject(reply);

    // 3. 设置 Redis 超时时间（例如 5 秒）
    struct timeval timeout = { 5, 0 }; // 5 秒
    redisSetTimeout(m_redisContext, timeout);

    // 2. 加载配置文件
    std::string current_dir = get_current_directory();
    if (current_dir.empty()) return false;
    std::string character_config = current_dir + "/../../../config/characters.yaml";
    std::string monster_config = current_dir + "/../../../config/enemies.yaml";
    std::string skill_config = current_dir + "/../../../config/skills.yaml";
    std::string scene_config = current_dir + "/../../../config/scenes.yaml";
    std::string username_config = current_dir + "/../../../config/usernames.yaml";

    std::vector<character::CharacterBase> characters = LoadCharacterBasesFromYAML(character_config);
    std::vector<enemy::MonsterBase> monsters = LoadMonsterBasesFromYAML(monster_config);
    std::vector<skill::SkillDefinition> skills = LoadSkillDefinitionsFromYAML(skill_config);
    std::vector<scene::SceneData> scenes = LoadSceneDataFromYAML(scene_config);
    std::vector<base::LoginRequest> usernames = LoadUsernameFromYAML(username_config);
    // 3. 初始化 Redis 数据
    if (!InitCharacterBases(m_redisContext, characters)) {
        return false;
    }
    if (!InitMonsterBases(m_redisContext, monsters)) {
        return false;
    }
    if (!InitSkillDefinitions(m_redisContext, skills)) {
        return false;
    }
    if (!InitSceneData(m_redisContext, scenes)) {
        return false;
    }
    if (!InitUsername(m_redisContext, usernames)) {
        return false;
    }
    return true;
}
bool DBRequest::Read(redisContext* context, std::string key, google::protobuf::Message* message) {
    return ReadFromRedis(context, key, message);
}
bool DBRequest::Write(redisContext* context, std::string key, google::protobuf::Message& message) {
    return WriteToRedis(context, key, message);
}
bool DBRequest::Del(redisContext* context, std::string key, google::protobuf::Message& message)
{
    return DelFromRedis(context, key, message);
}
//bool Write(redisContext* context, std::string, google::protobuf::Message*) {
//
//}
redisContext* DBRequest::GetRedisContext() {
    return m_redisContext;
}

redisContext* DBRequest::Connect()
{
    return ConnectToRedis("127.0.0.1", 6379, "qzx123456");
}

redisContext* DBRequest::ConnectToRedis(std::string ip, int port, std::string password)
{
    redisContext* context = redisConnect(ip.c_str(), port);
    if (!context || context->err) {
        if (context) {
            std::cerr << "Redis connection error: " << context->errstr << std::endl;
            redisFree(context);
        }
        else {
            std::cerr << "Failed to allocate Redis context" << std::endl;
        }
        return nullptr;
    }

    // 认证
    if (!password.empty()) {
        redisReply* reply = (redisReply*)redisCommand(context, "AUTH %s", password.c_str());
        if (!reply || reply->type == REDIS_REPLY_ERROR) {
            std::cerr << "Redis authentication failed: " << (reply ? reply->str : "Unknown error") << std::endl;
            redisFree(context);
            freeReplyObject(reply);
            return nullptr;
        }
        freeReplyObject(reply);
    }
    return context;
}

bool DBRequest::WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message)
{
    std::string buffer;
    if (!message.SerializeToString(&buffer)) {
        std::cerr << "Failed to serialize message for key: " << key << std::endl;
        return false;
    }

    redisReply* reply = (redisReply*)redisCommand(context, "SET %s %b", key.c_str(), buffer.data(), buffer.size());
    if (!reply || reply->type == REDIS_REPLY_ERROR) {
        std::cerr << "Failed to write to Redis for key: " << key << ". Error: " << (reply ? reply->str : "Unknown error") << std::endl;
        freeReplyObject(reply);
        return false;
    }

    freeReplyObject(reply);
    return true;
}

bool DBRequest::ReadFromRedis(redisContext* context, const std::string& key, google::protobuf::Message* message) {
    if (!context || key.empty() || !message) {
        return false;
    }
    std::cout<<"Reading from Redis for key: "<<key<<std::endl;
    // 发送 GET 命令
    redisReply* reply = (redisReply*)redisCommand(context, "GET %s", key.c_str());
    if (!reply) {
        std::cout<<"error1"<<std::endl;
        return false;
    }

    if (reply->type != REDIS_REPLY_STRING) {
        std::cout<<"error2"<<std::endl;
        freeReplyObject(reply);
        return false;
    }

    // 反序列化 Protobuf 消息
    if (!message->ParseFromArray(reply->str, reply->len)) {
        std::cout<<"error3"<<std::endl;
        freeReplyObject(reply);
        return false;
    }

    freeReplyObject(reply);
    return true;
}

bool DBRequest::DelFromRedis(redisContext* context, const std::string& key, google::protobuf::Message& message)
{
    // 检查上下文和键是否有效
    if (!context || key.empty()) {
        return false;
    }

    // 发送 DEL 命令
    redisReply* reply = (redisReply*)redisCommand(context, "DEL %s", key.c_str());
    if (!reply) {
        std::cerr << "Failed to delete key: " << key << ". Error: Unknown error" << std::endl;
        return false;
    }

    // 如果回复类型不是 REDIS_REPLY_INTEGER，则认为是错误
    bool success = reply->type == REDIS_REPLY_INTEGER && reply->integer > 0;

    if (!success) {
        std::cerr << "Failed to delete key: " << key << ". Error: Not found or other error" << std::endl;
    }
    else {
        std::cout << "[INFO] Successfully deleted key: " << key << std::endl;
    }

    freeReplyObject(reply);
    return success;
}

bool DBRequest::InitCharacterBases(redisContext* context, const std::vector<character::CharacterBase>& characters) {
    for (const auto& character : characters) {
        std::string key = "character:" + character.role_id();
        if (!WriteToRedis(context, key, character)) {
            return false;
        }
    }
    // 初始化已分配角色集合（空）
    redisReply* reply = (redisReply*)redisCommand(context, "DEL allocated_roles");
    if (reply) freeReplyObject(reply);
    return true;
}

bool DBRequest::InitMonsterBases(redisContext* context, const std::vector<enemy::MonsterBase>& monsters) {
    for (const auto& monster : monsters) {
        std::string key = "monster:" + monster.monster_id();
        if (!WriteToRedis(context, key, monster)) {
            return false;
        }
    }
    return true;
}

bool DBRequest::InitSkillDefinitions(redisContext* context, const std::vector<skill::SkillDefinition>& skills) {
    for (const auto& skill : skills) {
        std::string key = "skill:" + skill.skill_id();
        if (!WriteToRedis(context, key, skill)) {
            return false;
        }
    }
    return true;
}
#

bool DBRequest::InitSceneData(redisContext* context, const std::vector<scene::SceneData>& scenes) {
    for (const auto& scene: scenes) {
        std::string key = "scene:" + scene.scene_id();
        if (!WriteToRedis(context, key, scene)) {
            return false;
        }
    }
    return true;
}

bool DBRequest::InitUsername(redisContext* context, const std::vector<base::LoginRequest>& usernames) {
    for (const auto& username: usernames) {
        std::string key = "username:" + username.username();
        if (!WriteToRedis(context, key, username)) {
            std::cerr << "[ERROR] Failed to write user: " << username.username() << std::endl; // 添加调试日志
            return false;
        }
    }
    return true;
}

// 加载角色配置
std::vector < character::CharacterBase >  DBRequest::LoadCharacterBasesFromYAML(const std::string& config_path) {
    std::vector<character::CharacterBase> characters;
    YAML::Node config = YAML::LoadFile(config_path);
    for (const auto& node : config) {
        character::CharacterBase base;
        base.set_role_id(node["role_id"].as<std::string>());
        base.set_role_name(node["role_name"].as<std::string>());
        base.set_current_hp(node["current_hp"].as<int32_t>());
        base.set_max_hp(node["max_hp"].as<int32_t>());
        base.set_level(node["level"].as<int32_t>());
        base.set_exp(node["exp"].as<int32_t>());
        base.set_pos_x(node["pos_x"].as<float>());
        base.set_pos_y(node["pos_y"].as<float>());
        base.set_direction(node["direction"].as<float>());
        // 处理status枚举，可能需要转换字符串到枚举值
        base.set_status(static_cast<common::Status>(node["status"].as<int32_t>()));
        // 处理skills字段，可能需要遍历子节点
        const YAML::Node& skills_node = node["skills"];
        for (const auto& skill : skills_node) {
            character::SkillSlot* slot = base.add_skills();
            slot->set_skill_id(skill["skill_id"].as<std::string>());
            slot->set_current_cooldown(skill["current_cooldown"].as<float>());
            slot->set_is_active(skill["is_active"].as<bool>());
        }
        characters.push_back(base);
    }
    return characters;
}
// 加载怪物配置
std::vector<enemy::MonsterBase> DBRequest::LoadMonsterBasesFromYAML(const std::string& config_path) {
    std::vector<enemy::MonsterBase> monsters;

    try {
        YAML::Node config = YAML::LoadFile(config_path);

        for (const auto& node : config) {
            enemy::MonsterBase monster;

            monster.set_monster_id(node["monster_id"].as<std::string>());
            monster.set_type(static_cast<common::MonsterType>(node["type"].as<int>()));
            monster.set_current_hp(node["current_hp"].as<int32_t>());
            monster.set_max_hp(node["max_hp"].as<int32_t>());
            monster.set_attack_power(node["attack_power"].as<int32_t>());
            monster.set_attack_speed(node["attack_speed"].as<float>());
            monster.set_move_speed(node["move_speed"].as<float>());
            monster.set_pos_x(node["pos_x"].as<float>());
            monster.set_pos_y(node["pos_y"].as<float>());
            monster.set_pos_z(node["pos_z"].as<float>());
            monster.set_direction(node["direction"].as<float>());
            monster.set_attack_range(node["attack_range"].as<float>());
            monster.set_state(static_cast<common::MonsterState>(node["state"].as<int>()));
            monster.set_exp_reward(node["exp_reward"].as<int32_t>());

            monsters.push_back(monster);
        }
    }
    catch (const YAML::Exception& e) {
        std::cerr << "YAML Error: " << e.what() << std::endl;
    }

    return monsters;
}
// 加载技能配置
std::vector<skill::SkillDefinition> DBRequest::LoadSkillDefinitionsFromYAML(const std::string& config_path) {
    std::vector<skill::SkillDefinition> skills;

    try {
        YAML::Node config = YAML::LoadFile(config_path);

        for (const auto& node : config) {
            skill::SkillDefinition def;

            def.set_skill_id(node["skill_id"].as<std::string>());
            def.set_skill_name(node["skill_name"].as<std::string>());
            def.set_skill_type(static_cast<skill::SkillType>(node["skill_type"].as<int>()));
            def.set_base_damage(node["base_damage"].as<int32_t>());
            def.set_cast_time(node["cast_time"].as<float>());
            def.set_cool_down(node["cool_down"].as<float>());
            def.set_duration(node["duration"].as<float>());
            def.set_mana_cost(node["mana_cost"].as<int32_t>());

            // 处理 EffectType 列表
            const YAML::Node& effects_node = node["effects"];
            for (const auto& effect : effects_node) {
                def.add_effects(static_cast<skill::EffectType>(effect.as<int>()));
            }

            // 处理元素类型
            def.set_element_type(static_cast<common::ElementType>(node["element_type"].as<int>()));

            skills.push_back(def);
        }
    }
    catch (const YAML::Exception& e) {
        std::cerr << "YAML Error: " << e.what() << std::endl;
    }
    return skills;
}
// 加载场景配置
std::vector<scene::SceneData> DBRequest::LoadSceneDataFromYAML(const std::string& config_path) {
    std::vector<scene::SceneData> scenes;

    try {
        YAML::Node config = YAML::LoadFile(config_path);

        for (const auto& scene_node : config) {
            scene::SceneData scene_data;
            if (!scene_node["scene_id"]) {
                std::cerr << "Missing scene_id in a scene node" << std::endl;
                continue;
            }
            scene_data.set_scene_id(scene_node["scene_id"].as<std::string>());

            const YAML::Node& monsters_node = scene_node["monsters"];
            if (!monsters_node.IsSequence()) {
                std::cerr << "Invalid monsters node (not a sequence) in scene: " << scene_data.scene_id() << std::endl;
                continue;
            }

            for (const auto& monster_node : monsters_node) {
                enemy::MonsterBase* monster = scene_data.add_monsters();
                monster->set_monster_id(monster_node["monster_id"].as<std::string>());
                monster->set_type(static_cast<common::MonsterType>(monster_node["type"].as<int>()));
                monster->set_current_hp(monster_node["current_hp"].as<int32_t>());
                monster->set_max_hp(monster_node["max_hp"].as<int32_t>());
                monster->set_attack_power(monster_node["attack_power"].as<int32_t>());
                monster->set_attack_speed(monster_node["attack_speed"].as<float>());
                monster->set_move_speed(monster_node["move_speed"].as<float>());
                monster->set_pos_x(monster_node["pos_x"].as<float>());
                monster->set_pos_y(monster_node["pos_y"].as<float>());
                monster->set_pos_z(monster_node["pos_z"].as<float>());
                monster->set_direction(monster_node["direction"].as<float>());
                monster->set_attack_range(monster_node["attack_range"].as<float>());
                monster->set_state(static_cast<common::MonsterState>(monster_node["state"].as<int>()));
                monster->set_exp_reward(monster_node["exp_reward"].as<int32_t>());
            }

            scenes.push_back(scene_data);
        }
    }
    catch (const YAML::Exception& e) {
        std::cerr << "YAML Error: " << e.what() << std::endl;
    }

    return scenes;
}

std::vector<base::LoginRequest> DBRequest::LoadUsernameFromYAML(const std::string& config_path) {
    std::vector<base::LoginRequest> usernames;
    try {
        YAML::Node config = YAML::LoadFile(config_path);

        for (const auto& node : config) {
            base::LoginRequest username;
            username.set_username(node["username"].as<std::string>());
            username.set_password(node["password"].as<std::string>());
            std::cout << "[DEBUG] Loaded user: " << username.username() << std::endl; // 添加调试日志
            usernames.push_back(username);
        }
    }
    catch (const YAML::Exception& e) {
        std::cerr << "YAML Error: " << e.what() << std::endl;
    }
    return usernames;
}