#!/bin/sh
set -e

echo "*****************************"
echo "Unity: Starting CoreCLR tests"
echo "*****************************"

echo
echo "******************************"
echo "Unity: Building embedding host"
echo "******************************"
echo
./dotnet.sh build unity/managed.sln -c Release

echo
echo "**********************************"
echo "Unity: Running embedding API tests"
echo "**********************************"
echo
cd unity/embed_api_tests
cmake -DCMAKE_BUILD_TYPE=Release .
cmake --build .
./mono_test_app
cd ../../

echo
echo "**********************************"
echo "Unity: Running class library tests"
echo "**********************************"
echo
LD_LIBRARY_PATH=/usr/local/opt/openssl/lib ./build.sh -subset libs.tests -test /p:RunSmokeTestsOnly=true -a arm64 -c release -ci -ninja

echo
echo "****************************"
echo "Unity: Running runtime tests"
echo "****************************"
echo
./src/tests/build.sh arm64 release ci -tree:GC -tree:JIT -tree:baseservices -tree:interop -tree:reflection
./src/tests/run.sh arm64 release

echo
echo "************************"
echo "Unity: Running PAL tests"
echo "************************"
echo
./build.sh clr.paltests
./artifacts/bin/coreclr/OSX.arm64.Debug/paltests/runpaltests.sh $(pwd)/artifacts/bin/coreclr/OSX.arm64.Debug/paltests

echo
echo "**********************************"
echo "Unity: Tested CoreCLR successfully"
echo "**********************************"
