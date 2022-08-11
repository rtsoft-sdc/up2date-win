::echo %1
copy .\up2date-cpp\vcpkg.json .
:%VCPKG_ROOT%\vcpkg.exe install

RMDIR /S /Q build_x86
mkdir build_x86
cmake -B build_x86\ -S .\ -A "WIN32" --toolchain="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x86-windows
cmake --build .\build_x86\. --config %1

copy build_x86\%1\*.* ..\Up2dateClient\cppclient\bin-x86\


RMDIR /S /Q build_x64
mkdir build_x64
cmake -B build_x64\ -S .\ –A "x64" --toolchain="%VCPKG_ROOT%\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build .\build_x64\. --config %1

copy build_x64\%1\*.* ..\Up2dateClient\cppclient\bin-x64\