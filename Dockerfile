FROM ubuntu:lunar

RUN apt-get update
RUN apt-get install build-essential git tar curl zip unzip cmake ninja-build

WORKDIR /vcpkg-boot

RUN git clone https://github.com/microsoft/vcpkg
RUN ./vcpkg/bootstrap-vcpkg.sh
RUN ./vcpkg/vcpkg install dpp

WORKDIR /app

ADD . /app

RUN cmake -B /out -S . -DCMAKE_TOOLCHAIN_FILE=/vcpkg-boot/vcpkg/scripts/buildsystems/vcpkg.cmake
RUN cmake --build /out

ENTRYPOINT ["/out/TestBot"]