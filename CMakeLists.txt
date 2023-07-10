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
target_link_libraries(${PROJECT_NAME} PRIVATE zlib opus sodium)
target_link_libraries(${PROJECT_NAME} PRIVATE ssl)

find_package(ZLIB REQUIRED)
if (ZLIB_FOUND)
    target_link_libraries(${PROJECT_NAME} PRIVATE dpp ZLIB::ZLIB)
else()
    message(FATAL_ERROR "ZLIB library not found")
endif()

target_include_directories(${PROJECT_NAME} PRIVATE /vcpkg-boot/vcpkg/installed/x64-linux/include)
