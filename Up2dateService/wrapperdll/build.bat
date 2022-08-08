echo %1
cp .\up2date-cpp\vcpkg.json .
%VCPKG_ROOT%\vcpkg.exe install

rm -r build_x64
mkdir build_x64
cmake -B build_x64 -S . –A x64 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build .\build_x64\. --config "Release"

rm -r build_x86
mkdir build_x86
cmake -B build_x86 -S . –A WIN32 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x86-windows
cmake --build .\build_x86\. --config "Release"