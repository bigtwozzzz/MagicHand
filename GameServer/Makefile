SRCS := $(wildcard *.cpp) $(wildcard protos/*.cpp)
OBJS := $(SRCS:.cpp=.o)

GameServer: $(OBJS)
	g++ -pthread -std=c++11 -g -Wall -fPIC $^ -o $@ -lzinx -lprotobuf -lpthread -lhiredis

%.o: %.cpp
	g++ -pthread -std=c++11 -g -Wall -fPIC -c $< -o $@