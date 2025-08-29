#pragma once
#include <zinx.h>
#include <uuid/uuid.h>
//#include <bcrypt.h>
#include "GameMsg.h"
#include "GameWorld.h"
#include "protos/combat.pb.h"
#include "protos/globalrandom.pb.h" 
#include "DBRequest.h"
#include <chrono>
#include <random>
#include <cstdint>
extern  globalrandom::GlobalRandomNum g_global_random_num;
extern  globalrandom::GlobalRandomNum GenerateGlobalRandomNum();
extern  const globalrandom::GlobalRandomNum& GetGlobalRandomNum();

class GameProtocol;
class GameRole :
    public Irole, public Player
{
    float x = 0;
    float z = 0;
    float v = 0;
    std::string scene_id = "";
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
    void broadcastLogout(std::string id, std::string name);
    void SendFirstScene();
    void SendNextScene();
    void broadcastNextScene();
    void broadcastScene(std::string);
    void broadcastEnemyMove(std::string enemy_id, float target_x, float target_y);
    void broadcastPlayerMove(std::string id, float x, float z);
    bool updateCharacterPositionInDB(std::string role_id, float pos_x, float pos_y);
    bool updateMonsterPositionInDB(std::string role_id, float pos_x, float pos_y);
    bool UpdateCharacterInfoInDB(std::string player_id, std::string player_name, std::string role_id);
    void broadcastSkillInfo(std::string player_id, std::string skill_id);
    void broadcastPlayerAttack(std::string entity_id, combat::EntityType entity_type, std::string target_id, float attack_angle, std::string skill_id, float cast_time);
    void broadcastSelectRequestNotify(std::string stage_id, std::string player_id);
    void broadcastSelectResultNotify(std::string stage_id, bool isSuccess);
    void broadcastEnemyHit(std::string entity_id, combat::EntityType entity_type, std::string attacker_id);
    const globalrandom::GlobalRandomNum& GenerateRandomSeed();
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
    void sendRandomNum();
    void sendInfo(bool);
    base::User GetUserByID(std::string user_id);
    virtual UserData* ProcMsg(UserData& _poUserData) override;
    bool IsPlayerRole(const std::string& role_id);
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

