#include "GameWorld.h"
#include <iostream>

GameWorld::GameWorld(float x_b, float y_b, float x_e, float y_e) : x_begin(x_b), y_begin(y_b), x_end(x_e), y_end(y_e){
}
GameWorld::~GameWorld() {
	m_grid.m_oPlayers.clear();
}

std::list<Player*> GameWorld::getPlayers(Player* _player) {
	std::list<Player*> players;
	for (auto player : m_grid.m_oPlayers) {
        players.push_back(player);
	}
	return players;
}
//添加玩家
bool GameWorld::AddPlayer(Player* _player) {
	std::cout << "AddPlayer: " <<_player->getUserID()<<std::endl;
	for (auto player : m_grid.m_oPlayers) {
		std::cout << "Players: " << player->getUserID() << std::endl;
	}
	if(m_grid.m_oPlayers.find(_player) != m_grid.m_oPlayers.end())
		return false;
	m_grid.m_oPlayers.insert(_player);
	return true;
	//std::lock_guard<std::mutex> lock(m_mutex); // 加锁

	//std::cout << "AddPlayer: " << _player->getUserID() << std::endl;
	//for (auto player : m_grid.m_oPlayers) {
	//	std::cout << "Players: " << player->getUserID() << std::endl;
	//}

	//if (m_grid.m_oPlayers.find(_player) != m_grid.m_oPlayers.end()) {
	//	std::cout << "Player already exists!" << std::endl;
	//	return false;
	//}

	//m_grid.m_oPlayers.insert(_player);
	//return true;
}
//移除玩家
void GameWorld::DePlayer(Player* _player) {
	m_grid.m_oPlayers.erase(_player);
}