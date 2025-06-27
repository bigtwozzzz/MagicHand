#!/bin/bash

# 定义源文件目录和目标输出目录
PROTO_PATH="./protos"
GENERATED_PATH="./generates"

# 创建输出目录（如果不存在）
mkdir -p $GENERATED_PATH

# 查找所有 .proto 文件并编译为 C++
find $PROTO_PATH -name "*.proto" | while read proto_file; do
    protoc --proto_path=$PROTO_PATH --cpp_out=$GENERATED_PATH $proto_file
done