cmake_minimum_required(VERSION 3.25)
project(TestBot)

set(CMAKE_CXX_STANDARD 17)

file(GLOB_RECURSE SRC CONFIGURE_DEPENDS "./src/*.h" "./src/*.cpp")
add_executable(${PROJECT_NAME} ${SRC})

set(OS_NAME "x64-linux")
if(WIN32)
    set(OS_NAME "x64-windows")
endif()

find_package(dpp CONFIG REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE dpp)

target_link_directories(${PROJECT_NAME} PRIVATE /vcpkg-boot/vcpkg/installed/x64-linux/lib)
target_include_directories(${PROJECT_NAME} PRIVATE /vcpkg-boot/vcpkg/installed/x64-linux/include)
