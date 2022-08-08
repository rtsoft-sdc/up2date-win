echo %1
echo %2
cp .\up2date-cpp\vcpkg.json .
%VCPKG_ROOT%\vcpkg.exe install
mkdir build_%2
if %2==x64 (
    cmake -B build_%2 -S . –A x64 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x64-windows
    cmake --build .\build_%2\. --config %1
) else (
    cmake -B build_%2 -S . –A WIN32 -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x86-windows
    cmake --build .\build_%2\. --config %1
)