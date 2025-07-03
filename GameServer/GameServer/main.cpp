#include "GameChannel.h"
#include "GameMsg.h"
#include "hiredis/hiredis.h"
#include "DBRequest.h"
#include "test.cpp"

int main() {
	
	//�������ݿ�����
	DBRequest* dbRequest = new DBRequest();
	if (dbRequest->Init("127.0.0.1", 6379, "qzx123456")) {
		redisContext* pRedis = dbRequest->GetRedisContext();
		//test_character(dbRequest, pRedis);
	}
	else {
		printf("db init failed\n");
	}
	ZinxKernel::ZinxKernelInit();
	//��Ӽ���ͨ��
	ZinxKernel::Zinx_Add_Channel( *new ZinxTCPListen(8888, new GameConnectFact()));
	ZinxKernel::Zinx_Run();
	ZinxKernel::ZinxKernelFini();
}