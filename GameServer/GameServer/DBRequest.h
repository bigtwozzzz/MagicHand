#pragma once
#include <hiredis/hiredis.h>
#include <string>
#include <google/protobuf/message.h>
#include <yaml-cpp/yaml.h>
#include "protos/character.pb.h"
#include "protos/enemy.pb.h"
#include "protos/skill.pb.h"
#include "protos/scene.pb.h"
#include "protos/base.pb.h"

class DBRequest
{
public:
    DBRequest();
    ~DBRequest();
    bool Init(std::string,int, std::string);
    bool Read(redisContext* context,std::string, google::protobuf::Message*);
    bool Write(redisContext* context,std::string, google::protobuf::Message&);
    bool Del(redisContext* context, std::string, google::protobuf::Message&);
    redisContext* GetRedisContext();
    // 通用函数：连接 Redis 并认证
    redisContext* Connect();
    
private:
    redisContext* m_redisContext;
    redisContext* ConnectToRedis(std::string host, int port, std::string password);
    // 通用函数：序列化 Protobuf 对象并写入 Redis
    bool WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message);
    // 通用函数：从 Redis 读取并反序列化 Protobuf 消息
    bool ReadFromRedis(redisContext* context, const std::string& key, google::protobuf::Message* message);
    // 通用函数：从 Redis 删除对象
    bool DelFromRedis(redisContext* context, const std::string& key, google::protobuf::Message& message);
    // 初始化技能定义
    bool InitSkillDefinitions(redisContext* context, const std::vector<skill::SkillDefinition>& skills);
    // 初始化角色基础信息
    bool InitCharacterBases(redisContext* context, const std::vector<character::CharacterBase>& characters);
    // 初始化怪物基础信息
    bool InitMonsterBases(redisContext * context, const std::vector<enemy::MonsterBase>&monsters);
    // 初始化场景基础信息
    bool InitSceneData(redisContext* context, const std::vector<scene::SceneData>& scenes);
    // 初始化用户信息
    bool InitUsername(redisContext* context, const std::vector<base::LoginRequest>& usernames);
    // 加载角色配置
    std::vector < character::CharacterBase > LoadCharacterBasesFromYAML(const std::string& config_path);
    // 加载怪物配置
    std::vector<enemy::MonsterBase> LoadMonsterBasesFromYAML(const std::string& config_path);
    // 加载技能配置
    std::vector<skill::SkillDefinition> LoadSkillDefinitionsFromYAML(const std::string& config_path);
    // 加载场景配置
    std::vector<scene::SceneData> LoadSceneDataFromYAML(const std::string& config_path);
    // 加载用户配置
    std::vector<base::LoginRequest> LoadUsernameFromYAML(const std::string& config_path);

};

