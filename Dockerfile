FROM ubuntu:lunar

RUN apt-get update
RUN apt-get install -y pkg-config build-essential git tar curl zip unzip cmake ninja-build zlib1g-dev libopus-dev libsodium-dev libssl-dev

WORKDIR /vcpkg-boot

RUN git clone https://github.com/microsoft/vcpkg
RUN ./vcpkg/bootstrap-vcpkg.sh
RUN ./vcpkg/vcpkg install --triplet=x64-linux dpp

WORKDIR /app

ADD . /app

RUN cmake -B /out -S . -DCMAKE_TOOLCHAIN_FILE=/vcpkg-boot/vcpkg/scripts/buildsystems/vcpkg.cmake
RUN cmake --build /out

ENTRYPOINT ["/out/TestBot"]
