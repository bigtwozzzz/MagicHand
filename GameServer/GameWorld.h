#pragma once
#include <vector>
#include <set>
#include <list>
class Player {
    public:
    virtual float getX() = 0;
    virtual float getY() = 0;
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
};

