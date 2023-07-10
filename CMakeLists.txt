cmake_minimum_required(VERSION 3.25)
project(TestBot)

set(CMAKE_CXX_STANDARD 17)

add_executable(${PROJECT_NAME} src/main.cpp)

set(OS_NAME "x64-linux")
if(WIN32)
    set(OS_NAME "x64-windows")
endif()

find_package(dpp CONFIG REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE dpp::dpp)

target_include_directories(${PROJECT_NAME} PRIVATE $ENV{VCPKG_PATH}/installed/${OS_NAME}/include)
