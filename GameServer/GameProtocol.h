#pragma once
#include <zinx.h>
class GameChannel;
class GameRole;
class GameProtocol :
    public Iprotocol
{
    //未处理报文
    std::string szLast;
public:
    GameChannel *m_pGameChannel = NULL;
    GameRole *m_pGameRole = NULL;
    GameProtocol();
    virtual ~GameProtocol();
    virtual UserData* raw2request(std::string _szInput) override;
    virtual std::string* response2raw(UserData& _oUserData) override;
    virtual Irole* GetMsgProcessor(UserDataMsg& _oUserDataMsg) override;
    virtual Ichannel* GetMsgSender(BytesMsg & _oBytes) override;
};

