cmake_minimum_required(VERSION 3.22)
project(Geno)

set(CMAKE_CXX_STANDARD 20)

#file(GLOB_RECURSE SRC CONFIGURE_DEPENDS "./src/*.h" "./src/*.hpp" "./src/*.cpp")
file(GLOB_RECURSE SRC CONFIGURE_DEPENDS "./src/main.cpp")
add_executable(${PROJECT_NAME} ${SRC})

set(OS_NAME "x64-linux")
set(VCPKG_PATH "~/vcpkg-boot/vcpkg")
if (WIN32)
    set(OS_NAME "x64-windows")
    set(VCPKG_PATH "A:/SDKs/vcpkg")
endif ()

set(CMAKE_TOOLCHAIN_FILE "${VCPKG_PATH}/scripts/buildsystems/vcpkg.cmake")
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

include("${VCPKG_PATH}/scripts/buildsystems/vcpkg.cmake")

target_link_directories(${PROJECT_NAME} PRIVATE
        ${VCPKG_PATH}/installed/${OS_NAME}/lib
        ${VCPKG_PATH}/installed/${OS_NAME}/include
        ${VCPKG_PATH}/installed/${OS_NAME}-static/lib
        ${VCPKG_PATH}/installed/${OS_NAME}-static/include
        ${VCPKG_PATH}/installed/${OS_NAME}-static/lib/manual-link
)
target_include_directories(${PROJECT_NAME} PRIVATE
        ${VCPKG_PATH}/installed/${OS_NAME}/lib
        ${VCPKG_PATH}/installed/${OS_NAME}/include
        ${VCPKG_PATH}/installed/${OS_NAME}-static/lib
        ${VCPKG_PATH}/installed/${OS_NAME}-static/include
        ${VCPKG_PATH}/installed/${OS_NAME}-static/lib/manual-link
)

find_package(unofficial-sodium CONFIG REQUIRED)
find_package(dpp CONFIG REQUIRED)
find_package(ZLIB REQUIRED)
find_package(OpenSSL REQUIRED)
find_package(Opus CONFIG REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE Opus::opus unofficial-sodium::sodium dpp::dpp OpenSSL::SSL OpenSSL::Crypto)