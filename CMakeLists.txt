cmake_minimum_required(VERSION 3.25)
project(TestBot)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_PATH}/scripts/buildsystems/vcpkg.cmake" CACHE STRING "")

file(GLOB_RECURSE SRC CONFIGURE_DEPENDS "./src/*.h" "./src/*.cpp")
add_executable(${PROJECT_NAME} ${SRC})

set(OS_NAME "x64-linux")
if(WIN32)
    set(OS_NAME "x64-windows")
endif()

find_package(dpp CONFIG REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE dpp)

target_include_directories(${PROJECT_NAME} PRIVATE $ENV{VCPKG_PATH}/installed/${OS_NAME}/include)
target_include_directories(${PROJECT_NAME} PRIVATE $ENV{VCPKG_PATH}/installed/${OS_NAME}/lib)
target_link_directories(${PROJECT_NAME} PRIVATE $ENV{VCPKG_PATH}/installed/${OS_NAME}/include)
target_link_directories(${PROJECT_NAME} PRIVATE $ENV{VCPKG_PATH}/installed/${OS_NAME}/lib)
