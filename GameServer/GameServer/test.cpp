#include "GameChannel.h"
#include "GameMsg.h"
#include "hiredis/hiredis.h"
#include "DBRequest.h"

static void PrintCharacterBase(const character::CharacterBase& character) {
    std::cout << "=== Character Info ===\n";
    std::cout << "Role ID: " << character.role_id() << "\n";
    std::cout << "Name: " << character.role_name() << "\n";
    std::cout << "HP: " << character.current_hp() << "/" << character.max_hp() << "\n";
    std::cout << "Level: " << character.level() << "\n";
    std::cout << "Exp: " << character.exp() << "\n";
    std::cout << "Position: (" << character.pos_x() << ", " << character.pos_y() << ")\n";
    std::cout << "Direction: " << character.direction() << " radians\n";

    // 处理状态枚举（假设 common::Status 是一个 enum）
    switch (character.status()) {
    case common::Status::IDLE:
        std::cout << "Status: IDLE\n";
        break;
    case common::Status::CASTING:
        std::cout << "Status: CASTING\n";
        break;
    default:
        std::cout << "Status: UNKNOWN (" << character.status() << ")\n";
    }

    // 打印技能栏位
    std::cout << "Skills:\n";
    for (int i = 0; i < character.skills_size(); ++i) {
        const auto& skill = character.skills(i);
        std::cout << "  - Skill ID: " << skill.skill_id() << "\n";
        std::cout << "    Cooldown: " << skill.current_cooldown() << "s\n";
        std::cout << "    Active: " << (skill.is_active() ? "Yes" : "No") << "\n";
    }
}
static void test_character(DBRequest& dbRequest, redisContext* pRedis)
{ 
    character::CharacterBase character;
    dbRequest.Read(pRedis, "character:player_001", &character);
    PrintCharacterBase(character);
}