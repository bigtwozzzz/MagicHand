#pragma once
#include <zinx.h>
#include "GameMsg.h"
#include "GameWorld.h"
#include "protos/combat.pb.h"
class GameProtocol;
class GameRole :
    public Irole, public Player
{
    float x = 0;
    float z = 0;
    float v = 0;
    std::string m_iID = "";
    std::string status;
    GameMsg* handleLogin();
    void broadcastLogin(std::string);
    void broadcastLogout(std::string id);
    void broadcastPlayerMove(std::string id, float x, float z);
    void broadcastPlayerAttack(std::string entity_id, combat::EntityType entity_type, std::string target_id, float attack_angle, std::string skill_id, float cast_time);
    void broadcastSelectRequestNotify(std::string stage_id, std::string player_id);
    void broadcastSelectResultNotify(std::string stage_id, bool isSuccess);
    void broadcastEnemyHit(std::string entity_id, combat::EntityType entity_type, std::string attacker_id);
    bool countIfAll();
    int countBlood();
    int countDamage();
public:
    GameRole();
    virtual ~GameRole();
    // 通过 Irole 继承
    virtual bool Init() override;
    virtual UserData* ProcMsg(UserData& _poUserData) override;
    virtual void Fini() override;
    GameProtocol* m_pGameProtocol = NULL;

    // 通过 Player 继承
    virtual float getX() override;
    virtual float getY() override;
};

