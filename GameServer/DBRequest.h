#pragma once
#include <hiredis/hiredis.h>
#include <string>
#include <google/protobuf/message.h>
//class DBRequest
//{
//public:
//    DBRequest();
//    ~DBRequest();
//    bool Init(std::string,int, std::string);
//private:
//    // 通用函数：连接 Redis 并认证
//    redisContext* ConnectToRedis(const std::string& host, int port, const std::string& password);
//    // 通用函数：序列化 Protobuf 对象并写入 Redis
//    bool WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message);// 初始化技能定义
//    bool InitSkillDefinitions(redisContext* context, const std::vector<SkillDefinition>& skills);
//    // 初始化角色基础信息
//    bool InitCharacterBases(redisContext* context, const std::vector<CharacterBase>& characters);
//    // 初始化怪物基础信息
//    bool InitMonsterBases(redisContext * context, const std::vector<MonsterBase>&monsters);
//};
//
