cmake_minimum_required(VERSION 3.6.2)

# Set the project name
project(unity-profiler)

# CoreCLR expect this to be passed from outside
set(CLR_CMAKE_HOST_ARCH ${CMAKE_GENERATOR_PLATFORM})
if(HOST_WIN32)
    if(HOST_X86)
        set(CLR_CMAKE_HOST_ARCH x86)
    elseif(HOST_ARM64)
        set(CLR_CMAKE_HOST_ARCH arm64)
    elseif(HOST_ARM)
        set(CLR_CMAKE_HOST_ARCH arm)
    elseif(HOST_AMD64)
        set(CLR_CMAKE_HOST_ARCH x64)
    endif()
endif()

# Fix MC find issue w/o vcvars
LIST(APPEND CMAKE_PROGRAM_PATH "$ENV{ProgramFiles\(x86\)}/Windows Kits/10/bin/${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}/${CLR_CMAKE_HOST_ARCH}")

# Basic CoreCLR setup: paths, platform, compiler
include(../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

set(CLR_DIR ${CLR_REPO_ROOT_DIR}/src/coreclr)

# Initial setup
set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(FEATURE_EVENT_TRACE 0)
#add_definitions(-DPAL_STDCPP_COMPAT=1)

# Include PAL and native headers
include_directories(
    BEFORE
    ${CLR_DIR}/inc
#    ${CLR_DIR}/pal/inc
#    ${CLR_DIR}/pal/inc/rt
#    ${CLR_DIR}/pal/inc/rt/cpp
    ${CLR_DIR}/prebuilt/inc
    ${CLR_DIR}/nativeresources)

# Extra PAL stuff for non-Windows platforms
if (CLR_CMAKE_HOST_UNIX)
    include_directories(${CLR_DIR}/pal/inc)
    include_directories(${CLR_DIR}/pal/inc/rt)
    include_directories(${CLR_DIR}/pal/src/safecrt)

    append("-Wno-missing-prototypes -Wno-pointer-arith -Wno-macro-redefined" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

    include_directories(${CLR_DIR}/pal/inc/rt/cpp)
endif (CLR_CMAKE_HOST_UNIX)

# Profiler DLL sources
set(PROFILER_SOURCES
    unity-profiler.cpp
    CoreProfilerFactory.cpp
    CoreProfiler.cpp)

# Setup libraries
if(HOST_DARWIN)
    set(OS_LIBS "-framework CoreFoundation" "-framework Foundation")
elseif(HOST_LINUX)
    set(OS_LIBS pthread m dl)
elseif(HOST_WIN32)
    set(OS_LIBS bcrypt.lib Mswsock.lib ws2_32.lib psapi.lib version.lib advapi32.lib winmm.lib kernel32.lib)
endif()

if(CLR_CMAKE_HOST_UNIX)
    list(APPEND PROFILER_LINK_LIBRARIES
        coreclrpal
        palrt
        nativeresourcestring
    )
endif()

# Build target configuration
add_library(unity-profiler SHARED ${PROFILER_SOURCES})
target_link_libraries(unity-profiler ${PROFILER_LINK_LIBRARIES})
