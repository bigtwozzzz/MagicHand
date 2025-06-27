#pragma once
#include "ZinxTCP.h"
#include "GameProtocol.h"
class GameChannel :
    public ZinxTcpData
{
    public:
        GameChannel(int _fd);
        virtual ~GameChannel();
        GameProtocol* m_pGameProtocol = NULL;
        //返回协议对象
        virtual AZinxHandler* GetInputNextStage(BytesMsg& _oInput) override;
};
class GameConnectFact : public IZinxTcpConnFact {
    public:
        virtual ZinxTcpData* CreateTcpDataChannel(int _fd) override;
};