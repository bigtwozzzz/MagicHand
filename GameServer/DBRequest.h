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
    // ͨ�ú��������� Redis ����֤
    redisContext* ConnectToRedis(std::string host, int port, std::string password);
    // ͨ�ú��������л� Protobuf ����д�� Redis
    bool WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message);
    // ͨ�ú������� Redis ��ȡ�������л� Protobuf ��Ϣ
    bool ReadFromRedis(redisContext* context, const std::string& key, google::protobuf::Message* message);
    // ��ʼ�����ܶ���
    bool InitSkillDefinitions(redisContext* context, const std::vector<skill::SkillDefinition>& skills);
    // ��ʼ����ɫ������Ϣ
    bool InitCharacterBases(redisContext* context, const std::vector<character::CharacterBase>& characters);
    // ��ʼ�����������Ϣ
    bool InitMonsterBases(redisContext * context, const std::vector<enemy::MonsterBase>&monsters);

    // ���ؽ�ɫ����
    std::vector < character::CharacterBase > LoadCharacterBasesFromYAML(const std::string& config_path);
    // ���ع�������
    std::vector<enemy::MonsterBase> LoadMonsterBasesFromYAML(const std::string& config_path);
    // ���ؼ�������
    std::vector<skill::SkillDefinition> LoadSkillDefinitionsFromYAML(const std::string& config_path);

};

