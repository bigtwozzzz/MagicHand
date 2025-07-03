#pragma once
#include <vector>
#include <set>
#include <list>
#include <string>
#include <mutex>
#include <unordered_map>
#include "protos/common.pb.h"
class Player {
    public:
    virtual float getX() = 0;
    virtual float getY() = 0;
    virtual std::string getUserID() = 0;
};
class Grid {
public:
    std::set<Player*> m_oPlayers;
};
class GameWorld
{
public: 
    //std::vector<Grid> m_grids;
    Grid m_grid;
    GameWorld(float x_b, float y_b, float x_e, float y_e);
    virtual ~GameWorld();
    float x_begin = 0.0f;
    float y_begin = 0.0f;
    float x_end = 0.0f;
    float y_end = 0.0f;
    //获得其他玩家信息
    std::list<Player*> getPlayers(Player* _player);
    //添加玩家
    bool AddPlayer(Player* _player);
    //移除玩家
    void DePlayer(Player* _player);
    //std::mutex m_mutex; // 同步锁

    // 现有代码...
    std::unordered_map<std::string, common::StageSelectState> confirm_states;  // 玩家确认状态
    std::mutex m_mutex;  // 线程锁
    bool updateConfirmState(const std::string& player_id, common::StageSelectState state);
    bool checkAllConfirmed(const std::string& stage_id);

    // 新增方法
    void resetConfirmStates();  // 重置确认状态
};

