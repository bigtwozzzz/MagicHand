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
//    // ͨ�ú��������� Redis ����֤
//    redisContext* ConnectToRedis(const std::string& host, int port, const std::string& password);
//    // ͨ�ú��������л� Protobuf ����д�� Redis
//    bool WriteToRedis(redisContext* context, const std::string& key, const google::protobuf::Message& message);// ��ʼ�����ܶ���
//    bool InitSkillDefinitions(redisContext* context, const std::vector<SkillDefinition>& skills);
//    // ��ʼ����ɫ������Ϣ
//    bool InitCharacterBases(redisContext* context, const std::vector<CharacterBase>& characters);
//    // ��ʼ�����������Ϣ
//    bool InitMonsterBases(redisContext * context, const std::vector<MonsterBase>&monsters);
//};
//
