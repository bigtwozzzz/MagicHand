#include "GameWorld.h"
#include <iostream>

GameWorld::GameWorld(float x_b, float y_b, float x_e, float y_e)
    : x_begin(x_b), y_begin(y_b), x_end(x_e), y_end(y_e) {
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
    std::cout << "AddPlayer: " << _player->getUserID() << std::endl;
    for (auto player : m_grid.m_oPlayers) {
        std::cout << "Players: " << player->getUserID() << std::endl;
    }
    if (m_grid.m_oPlayers.find(_player) != m_grid.m_oPlayers.end())
        return false;
    m_grid.m_oPlayers.insert(_player);
    return true;
}

void GameWorld::DePlayer(Player* _player) {
    if (_player) {
        m_grid.m_oPlayers.erase(_player);

        // 可选：从投票状态中移除该玩家
        std::lock_guard<std::recursive_mutex> lock(m_mutex);
        if (m_voteInProgress) {
            confirm_states.erase(_player->getUserID());
        }
    }
}

void GameWorld::resetConfirmStates() {
    std::lock_guard<std::recursive_mutex> lock(m_mutex);
    confirm_states.clear();
    m_voteInProgress = false;
    m_currentStageId.clear();
    m_expectedPlayers.clear();
    std::cerr << "[DEBUG] Reset all confirm states for new stage selection" << std::endl;
}

// 启动一次新的投票（必须在发起关卡选择时调用）
void GameWorld::StartStageVote(const std::string& stage_id) {
    std::lock_guard<std::recursive_mutex> lock(m_mutex);
    std::cerr << "[INFO] Start stage vote for: " << stage_id << std::endl;
    // 重置之前的状态
    resetConfirmStates();

    m_currentStageId = stage_id;
    m_voteInProgress = true;
    std::cerr << "[INFO] Started stage vote for: " << stage_id << std::endl;
    // 记录当前所有玩家ID
    for (auto player : m_grid.m_oPlayers) {
        m_expectedPlayers.insert(player->getUserID());
    }

    std::cerr << "[INFO] Started stage vote for: " << stage_id
        << ", Expected players count: " << m_expectedPlayers.size() << std::endl;
}

bool GameWorld::updateConfirmState(const std::string& player_id, common::StageSelectState state) {
    std::lock_guard<std::recursive_mutex> lock(m_mutex);

    // 如果没有进行中的投票，忽略
    if (!m_voteInProgress) {
        std::cerr << "[WARN] Vote not in progress, ignore vote from: " << player_id << std::endl;
        return false;
    }

    // 检查该玩家是否属于当前游戏
    if (m_expectedPlayers.find(player_id) == m_expectedPlayers.end()) {
        std::cerr << "[WARN] Player not in expected players: " << player_id << std::endl;
        return false;
    }

    confirm_states[player_id] = state;
    std::cerr << "[DEBUG] Player voted: " << player_id << ", State: " << state << std::endl;

    return true;
}

// 检查是否所有玩家都已投票（不判断结果，只看是否都投了）
bool GameWorld::areAllPlayersVoted() {
    std::lock_guard<std::recursive_mutex> lock(m_mutex);

    if (!m_voteInProgress) return false;

    for (const auto& pid : m_expectedPlayers) {
        if (confirm_states.find(pid) == confirm_states.end()) {
            return false;
        }
    }
    return true;
}

// 检查是否全部同意（仅在 all voted 后调用）
bool GameWorld::checkAllConfirmed(const std::string& stage_id) {
    std::lock_guard<std::recursive_mutex> lock(m_mutex);
    std::cerr << "[DEBUG] Checking all players confirmed for stage: " << stage_id << "current_id:  "<<m_currentStageId<< std::endl;
    if (!m_voteInProgress || m_currentStageId != stage_id) {
        return false;
    }
    std::cerr << "[DEBUG] All players confirmed for stage: " << stage_id << std::endl;
    for (const auto& pid : m_expectedPlayers) {
        auto it = confirm_states.find(pid);
        if (it == confirm_states.end() || it->second != common::StageSelectState::CONFIRMED) {
            return false;
        }
    }
    return true;
}
//#include "GameWorld.h"
//#include <iostream>
//
//GameWorld::GameWorld(float x_b, float y_b, float x_e, float y_e) : x_begin(x_b), y_begin(y_b), x_end(x_e), y_end(y_e){
//}
//GameWorld::~GameWorld() {
//	m_grid.m_oPlayers.clear();
//}
//
//std::list<Player*> GameWorld::getPlayers(Player* _player) {
//	std::list<Player*> players;
//	for (auto player : m_grid.m_oPlayers) {
//        players.push_back(player);
//	}
//	return players;
//}
////添加玩家
//bool GameWorld::AddPlayer(Player* _player) {
//	std::cout << "AddPlayer: " <<_player->getUserID()<<std::endl;
//	for (auto player : m_grid.m_oPlayers) {
//		std::cout << "Players: " << player->getUserID() << std::endl;
//	}
//	if(m_grid.m_oPlayers.find(_player) != m_grid.m_oPlayers.end())
//		return false;
//	m_grid.m_oPlayers.insert(_player);
//	return true;
//}
////移除玩家
//void GameWorld::DePlayer(Player* _player) {
//	m_grid.m_oPlayers.erase(_player);
//}
//
//void GameWorld::resetConfirmStates() {
//	std::lock_guard<std::recursive_mutex> lock(m_mutex);
//	confirm_states.clear();  // 清空所有确认状态
//	std::cerr << "[DEBUG] Reset all confirm states for new stage selection" << std::endl;
//}
//
//bool GameWorld::updateConfirmState(const std::string& player_id, common::StageSelectState state) {
//	std::lock_guard<std::recursive_mutex> lock(m_mutex);
//	confirm_states[player_id] = state;
//	return true;
//}
//
//bool GameWorld::checkAllConfirmed(const std::string& stage_id) {
//	std::lock_guard<std::recursive_mutex> lock(m_mutex);
//	for (auto player : getPlayers(nullptr)) {  // 获取所有在线玩家
//		std::string player_id = player->getUserID();
//		auto it = confirm_states.find(player_id);
//		if (it == confirm_states.end() || it->second != common::StageSelectState::CONFIRMED) {
//			return false;
//		}
//	}
//	return true;
//}