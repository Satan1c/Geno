cmake_minimum_required(VERSION 3.25)
project(TestBot)

set(CMAKE_CXX_STANDARD 17)

file(GLOB_RECURSE SRC CONFIGURE_DEPENDS "./src/*.h" "./src/*.cpp")
add_executable(${PROJECT_NAME} ${SRC})

set(OS_NAME "x64-linux")
set(VCPKG_PATH "/vcpkg-boot/vcpkg")
if (WIN32)
	set(OS_NAME "x64-windows")
	set(VCPKG_PATH "C:/vcpkg")
endif ()

find_package(dpp CONFIG REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE dpp::dpp)

target_include_directories(${PROJECT_NAME} PRIVATE ${VCPKG_PATH}/installed/${OS_NAME}/include)
target_link_directories(${PROJECT_NAME} PRIVATE ${VCPKG_PATH}/installed/${OS_NAME}/lib)