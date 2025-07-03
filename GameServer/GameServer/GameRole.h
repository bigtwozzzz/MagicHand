#pragma once
#include <zinx.h>
#include <uuid/uuid.h>
//#include <bcrypt.h>
#include "GameMsg.h"
#include "GameWorld.h"
#include "protos/combat.pb.h"
#include "DBRequest.h"
class GameProtocol;
class GameRole :
    public Irole, public Player
{
    float x = 0;
    float z = 0;
    float v = 0;
    std::string m_iID = "";
    std::string status = "";
    std::atomic<bool> is_fini_called_; // 使用原子变量保证线程安全
    std::string GenerateUserID();
    std::string GenerateRoleID(redisContext* context);
    bool TryDeallocateRoleID(redisContext* context, const std::string& role_id);
    bool TryAllocateRoleID(redisContext* context, const std::string& role_id);
    //std::string HashPasswordWithBcrypt(const std::string& password);
    //bool VerifyBcryptPassword(const std::string& password, const std::string& hash);
    GameMsg* handleLogin();
    GameMsg* handleLogout();
    void broadcastLogin(std::string);
    void broadcastLogout(std::string id);
    void broadcastPlayerMove(std::string id, float x, float z);
    void broadcastPlayerAttack(std::string entity_id, combat::EntityType entity_type, std::string target_id, float attack_angle, std::string skill_id, float cast_time);
    void broadcastSelectRequestNotify(std::string stage_id, std::string player_id);
    void broadcastSelectResultNotify(std::string stage_id, bool isSuccess);
    void broadcastEnemyHit(std::string entity_id, combat::EntityType entity_type, std::string attacker_id);
    character::CharacterBase GetCharacterBase(std::string);
    std::string GenerateUUID();
    std::string verifyLogin(std::string, std::string);
    bool countIfAll();
    int countBlood();
    int countDamage();
public:
    GameRole();
    virtual ~GameRole();
    // 通过 Irole 继承
    virtual bool Init() override;
    void sendInfo(bool);
    virtual UserData* ProcMsg(UserData& _poUserData) override;
    virtual void Fini() override;
    GameProtocol* m_pGameProtocol = NULL;
    DBRequest* db_request = NULL; // 保存 DBRequest 实例
    // 通过 Player 继承
    virtual float getX() override;
    virtual float getY() override;
    virtual std::string getUserID() override;
    // 构造函数或设置方法
    void SetDBRequest(DBRequest* db);
};

