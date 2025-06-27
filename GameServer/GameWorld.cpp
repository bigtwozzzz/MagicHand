#include "GameWorld.h"
GameWorld::GameWorld(float x_b, float y_b, float x_e, float y_e) : x_begin(x_b), y_begin(y_b), x_end(x_e), y_end(y_e){
}
GameWorld::~GameWorld() {

}

std::list<Player*> GameWorld::getPlayers(Player* _player) {
	std::list<Player*> players;
	for (auto player : m_grid.m_oPlayers) {
        players.push_back(player);
	}
	return players;
}
//Ìí¼ÓÍæ¼Ò
bool GameWorld::AddPlayer(Player* _player) {
	if(m_grid.m_oPlayers.find(_player) != m_grid.m_oPlayers.end())
		return false;
	m_grid.m_oPlayers.insert(_player);
	return true;
}
//ÒÆ³ıÍæ¼Ò
void GameWorld::DePlayer(Player* _player) {
	m_grid.m_oPlayers.erase(_player);
}