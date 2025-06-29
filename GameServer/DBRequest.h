#pragma once
#include <hiredis/hiredis.h>
#include <string>
#include <google/protobuf/message.h>
#include <yaml-cpp/yaml.h>
#include "protos/character.pb.h"
#include "protos/enemy.pb.h"
#include "protos/skill.pb.h"
class DBRequest
{
public:
    DBRequest();
    ~DBRequest();
    bool Init(std::string,int, std::string);
    bool Read(redisContext* context,std::string, google::protobuf::Message*);
    bool Write(redisContext* context,std::string, google::protobuf::Message&);
    redisContext* GetRedisContext();
private:
    redisContext* m_redisContext = nullptr;
    // 通用函数：连接 Redis 并认证
    redisContext* ConnectToRedis(std::string host, int port, std::string password);
    // 通用函数：序列化 Protobuf 对象并写入 Redis
    bool WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message);
    // 通用函数：从 Redis 读取并反序列化 Protobuf 消息
    bool ReadFromRedis(redisContext* context, const std::string& key, google::protobuf::Message* message);
    // 初始化技能定义
    bool InitSkillDefinitions(redisContext* context, const std::vector<skill::SkillDefinition>& skills);
    // 初始化角色基础信息
    bool InitCharacterBases(redisContext* context, const std::vector<character::CharacterBase>& characters);
    // 初始化怪物基础信息
    bool InitMonsterBases(redisContext * context, const std::vector<enemy::MonsterBase>&monsters);

    // 加载角色配置
    std::vector < character::CharacterBase > LoadCharacterBasesFromYAML(const std::string& config_path);
    // 加载怪物配置
    std::vector<enemy::MonsterBase> LoadMonsterBasesFromYAML(const std::string& config_path);
    // 加载技能配置
    std::vector<skill::SkillDefinition> LoadSkillDefinitionsFromYAML(const std::string& config_path);

};

