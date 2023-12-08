#include <algorithm>
#include <assert.h>
#include <cstdint>
#include <cstdlib>
#include <iostream>
#include <list>
#include <stdio.h>
#include <string.h>
#include <string>
#ifndef WIN32
#include <dlfcn.h>
#include <unistd.h>
#endif
//#include "unittest-cpp/UnitTest++/UnitTest++.h"
#define CATCH_CONFIG_RUNNER
#include "catch/catch.hpp"

#define UNITY_EDITOR 1
#define USE_CORECLR

// On macOS and Linux the compiler does not define _DEBUG, but CMake defines
// NDEBUG for release builds, so if NDEBUG is not defined, assume this
// is a debug build and define _DEBUG.
#if defined(__APPLE__) || defined(__linux__)
#ifndef NDEBUG
#define _DEBUG
#endif
#endif

typedef signed short SInt16;
typedef unsigned short UInt16;
typedef unsigned char UInt8;
typedef signed char SInt8;
typedef signed int SInt32;
typedef unsigned int UInt32;
typedef signed long long SInt64;
typedef unsigned long long UInt64;
typedef void* mono_register_object_callback;
typedef void* mono_liveness_world_state_callback;
const int mdtTypeDef = 0x02000000;

void* s_MonoLibrary = nullptr;
std::string g_monoDllPath;

enum Mode
{
    CoreCLR,
    Mono,
};

Mode g_Mode;

#ifdef WIN32
#include <Windows.h>
const int RTLD_LAZY = 0; // not used
void* dlopen(const char* path, int flags)
{
    return ::LoadLibraryA(path);
}
void dlclose(void* handle)
{
    ::FreeLibrary((HMODULE)handle);
}
void* dlsym(void* handle, const char* funcname)
{
    auto sym = ::GetProcAddress((HMODULE)handle, funcname);
    if (!sym)
    {
        printf("Failing to dlsym '%s'\n", funcname);
        //exit(1);
    }
    return sym;
}

static const std::string kNewLine = "\r\n";

#else
static const std::string kNewLine = "\n";
#endif

void* get_handle()
{
    if(s_MonoLibrary == nullptr)
    {
        printf("Loading Mono from '%s'...\n", g_monoDllPath.c_str());
        s_MonoLibrary = dlopen(g_monoDllPath.c_str(), RTLD_LAZY);

        if(s_MonoLibrary == nullptr)
        {
            assert(false && "Failed to load mono\n");
            printf("Failed to load mono from '%s'\n", g_monoDllPath.c_str());
            exit(1);
        }
    }
    return s_MonoLibrary;
}

typedef wchar_t mono_char; // used by CoreCLR

void* get_method(const char* functionName)
{
    void* func = dlsym(get_handle(), functionName);
    if(func == nullptr)
    {
        printf("Failed to load function '%s'\n", functionName);
        // Don't hard exit as some functions are not exported while still exposed by MonoFunctions.h
        // So we might get a null access exception if we are using a function
        // that was not found, but we can identify them when it is failing
        // exit(1);
        return nullptr;
    }
    return func;
}

#define DO_API(r,n,p) typedef r (*type_##n)p; type_##n n;

#include "../../src/coreclr/vm/mono/MonoCoreClr.h"

#undef DO_API

MonoDomain *g_domain;
MonoAssembly *g_assembly;

// shim to map UnitTest++ to Catch
#define TEST(x) TEST_CASE(#x)
#define CHECK_EQUAL(x, y) REQUIRE((x) == (y))


#define CHECK_EQUAL_STR(x, y) REQUIRE(strcmp((x), (y)) == 0)

TEST(Sanity)
{
   CHECK_EQUAL(1, 1);
}

#define kTestDLLNameSpace "TestDll"
#define kTestClassName "TestClass"
#define kInvalidName "DoesNotExist"

#define GET_AND_CHECK(what, code) \
    auto what = code; \
    CHECK(what != nullptr)

static void get_dirname(char* source)
{
    for (int i = strlen(source) - 1; i >= 0; i--)
    {
        if (source[i] == '/' || source[i] == '\\')
        {
            source[i] = '\0';
            return;
        }
    }
}

#if WIN32
char* realpath(const char *path, char *resolved_path)
{
    char* result = (char*)malloc(1024);
    if (GetFullPathNameA((LPCSTR)path, 1024, result, NULL) == 0)
    {
        fprintf(stderr, "Fontconfig warning: GetFullPathNameA failed.\n");
        return NULL;
    }
    return result;
}
#endif

static std::string abs_path_from_unity_root(const char* relative_to_this_file)
{
    char* base = getenv("UNITY_ROOT");
    if (base == nullptr)
    {
        printf("Please supply UNITY_ROOT environment variable, so we can find your mono installation.\n");
        exit(1);
    }
    char* concat = new char[strlen(base) + strlen(relative_to_this_file) + 2];
    strcpy(concat, base);
    strcat(concat, "/");
    strcat(concat, relative_to_this_file);
    char* resolved = realpath(concat, nullptr);
    delete[] concat;
    if (resolved == nullptr) {
        perror("Failed to get absolute path");
        return "";
    }
    std::string result(resolved);
    free(resolved);
    return result;
}

static std::string abs_path_from_file(const char* relative_to_this_file)
{
    char* base = strdup(__FILE__);
    get_dirname(base);
    char* concat = new char[strlen(base) + strlen(relative_to_this_file) + 2];
    strcpy(concat, base);
    strcat(concat, "/");
    strcat(concat, relative_to_this_file);
    char* resolved = realpath(concat, nullptr);
    free(base);
    delete[] concat;
    if (resolved == nullptr)
    {
        perror("Failed to get absolute path");
        return "";
    }
    std::string result(resolved);
    free(resolved);
    return result;
}

template <typename T>
T* ExtractManagedFromHandle(T* ptr)
{
    if(ptr && reinterpret_cast<intptr_t>(ptr) & 2)
        return *reinterpret_cast<T**>(reinterpret_cast<intptr_t>(ptr) & ~2);

    return ptr;
}

MonoClass* GetClassHelper(const char* namespaze, const char* classname)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(klass, mono_class_from_name(image, namespaze, classname));
    return klass;
}

MonoObject* CreateObjectHelper(const char* namespaze, const char* classname)
{
    GET_AND_CHECK(obj, mono_object_new(g_domain, GetClassHelper(namespaze, classname)));
    return obj;
}

void* scripting_array_element_ptr(MonoArray* array, int i, size_t element_size)
{
    GET_AND_CHECK(arrayClass, mono_object_get_class((MonoObject*)array));

    size_t SCRIPTING_ARRAY_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) * 2 * mono_class_get_rank(arrayClass): sizeof(void*) * 4;
    return SCRIPTING_ARRAY_HEADERSIZE + i * element_size + (char*)array;
}

TEST(mono_class_from_name_returns_class)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(klass, mono_class_from_name(image, kTestDLLNameSpace, kTestClassName));
    CHECK(strcmp(kTestDLLNameSpace, mono_class_get_namespace(klass)) == 0);
    CHECK(strcmp(kTestClassName, mono_class_get_name(klass)) == 0);
    CHECK_EQUAL(image, mono_class_get_image(klass));
}

TEST(mono_class_from_returns_null_if_class_does_not_exist)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    MonoClass* klass = mono_class_from_name(image, kTestDLLNameSpace, kInvalidName);
    CHECK(klass == NULL);
}

TEST(mono_class_get_property_from_name_returns_static_property)
{
    const char* propertyname = "StaticIntProperty";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_StaticIntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(klass, mono_method_get_class(method));
}

TEST(mono_class_get_property_from_name_returns_instance_property)
{
    const char* propertyname = "IntProperty";
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_IntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(klass, mono_method_get_class(method));
}

TEST(mono_class_get_property_from_name_returns_instance_property_of_base_class)
{
    const char* propertyname = "IntProperty";
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "DerivedClass");
    GET_AND_CHECK(property, mono_class_get_property_from_name (klass, propertyname));
    GET_AND_CHECK(method, mono_property_get_get_method(property));
    CHECK(strcmp("get_IntProperty", mono_method_get_name(method)) == 0);
    CHECK_EQUAL(base, mono_method_get_class(method));
}

TEST(mono_type_get_name_returns_name)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(type, mono_class_get_type(klass));
    GET_AND_CHECK(name , mono_type_get_name(type));
    CHECK(strcmp("TestDll.TestClass", name) == 0);
    mono_unity_g_free(name);
}

TEST(mono_type_get_name_full_returns_assembly_qualified_name)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(type, mono_class_get_type(klass));
    GET_AND_CHECK(name , mono_type_get_name_full(type, MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED));
    CHECK(strcmp("TestDll.TestClass, coreclr-test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", name) == 0);
    mono_unity_g_free(name);
}

TEST(mono_type_get_name_full_returns_il_name)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(type, mono_class_get_type(klass));
    GET_AND_CHECK(name , mono_type_get_name_full(type, MonoTypeNameFormat::MONO_TYPE_NAME_FORMAT_IL));
    CHECK(strcmp("TestDll.TestClass", name) == 0);
    mono_unity_g_free(name);
}

TEST(mono_object_isinst_works_with_same_class)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "ClassWithNestedClass");
    MonoObject* testobj = mono_object_new(g_domain, klass);
    CHECK(mono_object_isinst(testobj, klass) != NULL);

    MonoClass* unrelatedclass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    CHECK(mono_object_isinst(testobj, unrelatedclass) == NULL);

    unrelatedclass = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_object_isinst(testobj, unrelatedclass) == NULL);
}

TEST(mono_unity_class_is_abstract_works)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_unity_class_is_abstract(base));
    CHECK(!mono_unity_class_is_abstract(inherited));
}

TEST(mono_class_is_generic_works)
{
    MonoClass *nongeneric = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    MonoClass *generic = GetClassHelper(kTestDLLNameSpace, "GenericClass`1");
    CHECK(mono_class_is_generic(generic));
    CHECK(!mono_class_is_generic(nongeneric));
}

TEST(mono_class_is_subclass_of_works_with_base_class)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "InheritedClass");
    CHECK(mono_class_is_subclass_of(inherited, base, false));
    CHECK(mono_class_is_subclass_of(base, base, false));
    CHECK(!mono_class_is_subclass_of(base, inherited, false));
}

TEST(type_forwarder_lookup_results_in_identical_class)
{
    MonoClass *directLookup = GetClassHelper(kTestDLLNameSpace, kTestClassName);
#if defined(_DEBUG)
    std::string testDllPath = abs_path_from_file("../forwarder-test/bin/Debug/net8.0/forwarder-test.dll");
#else
    std::string testDllPath = abs_path_from_file("../forwarder-test/bin/Release/net8.0/forwarder-test.dll");
#endif
    MonoAssembly *forwarderAssembly = mono_domain_assembly_open (g_domain, testDllPath.c_str());
    GET_AND_CHECK(forwarderImage, mono_assembly_get_image(forwarderAssembly));
    GET_AND_CHECK(directImage, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(forwarderLookup, mono_class_from_name(forwarderImage, kTestDLLNameSpace, kTestClassName));
    CHECK_EQUAL(directLookup, forwarderLookup);
    CHECK_EQUAL(directImage, mono_class_get_image(forwarderLookup));
    CHECK(forwarderImage != directImage);
}

TEST(mono_assembly_get_object)
{
    MonoClass *directLookup = GetClassHelper(kTestDLLNameSpace, kTestClassName);
#if defined(_DEBUG)
    std::string testDllPath = abs_path_from_file("../forwarder-test/bin/Debug/net8.0/forwarder-test.dll");
#else
    std::string testDllPath = abs_path_from_file("../forwarder-test/bin/Release/net8.0/forwarder-test.dll");
#endif
    MonoAssembly *forwarderAssembly = mono_domain_assembly_open (g_domain, testDllPath.c_str());
    CHECK(forwarderAssembly != NULL);

    MonoObject *result = mono_assembly_get_object(NULL, forwarderAssembly);
    CHECK(result != NULL);
}

TEST(can_get_types_from_image_table)
{
    MonoClass *testclass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    int rows = mono_image_get_table_rows(image, mdtTypeDef);
    CHECK(rows > 0);
    bool found = false;
    for (int i=0; i<rows; i++)
    {
        GET_AND_CHECK(klass, mono_unity_class_get(image, mdtTypeDef | (i + 1)));
        if (klass == testclass)
            found = true;
    }
    CHECK(found);
}

TEST(mono_class_is_subclass_of_works_with_arrays)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, "BaseClass");
    MonoClass *arrayklass = mono_array_class_get(klass, 1);
    CHECK(mono_class_is_subclass_of(arrayklass, arrayklass, false));
}

TEST(mono_class_is_subclass_of_works_with_interfaces)
{
    MonoClass *base = GetClassHelper(kTestDLLNameSpace, "TestInterface");
    MonoClass *inherited = GetClassHelper(kTestDLLNameSpace, "ClassImplementingInterface");
    CHECK(mono_class_is_subclass_of(inherited, base, true));
    CHECK(!mono_class_is_subclass_of(inherited, base, false));
    CHECK(!mono_class_is_subclass_of(base, inherited, true));
}

TEST(mono_class_get_flags_works)
{
    CHECK_EQUAL(TYPE_ATTRIBUTE_PUBLIC | TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, kTestClassName)));
    CHECK_EQUAL(TYPE_ATTRIBUTE_PUBLIC | TYPE_ATTRIBUTE_ABSTRACT | TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, "BaseClass")));
    CHECK_EQUAL(TYPE_ATTRIBUTE_BEFORE_FIELD_INIT,
        mono_class_get_flags(GetClassHelper(kTestDLLNameSpace, "TestAttribute")));
}

TEST(mono_class_get_fields_retrieves_all_fields)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithFields");

    gpointer ptr = nullptr;
    int count = 0;
    std::string fieldnames;
    MonoClassField* field;
    while ((field = mono_class_get_fields(klass, &ptr)) != nullptr)
    {
        GET_AND_CHECK(fieldname, mono_field_get_name(field));
        CHECK(strcmp("System.Int32", mono_type_get_name(mono_field_get_type(field))) == 0);
        fieldnames += fieldname;
        count++;
    }
    CHECK_EQUAL(4, count);
    // CoreCLR reports static fields after non-static fields.
    CHECK(strcmp(g_Mode == CoreCLR ? "xywz" : "xyzw", fieldnames.c_str()) == 0);
}

TEST(can_get_type_of_generic_field)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "GenericClass`1");

    gpointer ptr = nullptr;
    GET_AND_CHECK(field, mono_class_get_fields(klass, &ptr));
    CHECK(strcmp("genericField", mono_field_get_name(field)) == 0);
    CHECK(strcmp("T", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field != NULL);
    CHECK(strcmp("genericArrayField", mono_field_get_name(field)) == 0);
    CHECK(strcmp("T[]", mono_type_get_name(mono_field_get_type(field))) == 0);
    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(mono_class_get_interfaces_retrieves_all_interfaces)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassImplementingInterface");
    MonoClass* testInterface = GetClassHelper(kTestDLLNameSpace, "TestInterface");

    gpointer ptr = nullptr;
    MonoClass* monoInterface = mono_class_get_interfaces(klass, &ptr);
    CHECK_EQUAL(testInterface, monoInterface);
    monoInterface = mono_class_get_interfaces(klass, &ptr);
    CHECK(monoInterface == NULL);
}

TEST(mono_class_get_interfaces_may_retrieve_parent_interfaces)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassDerivingFromClassImplementingInterface");

    gpointer ptr = nullptr;
    MonoClass* monoInterface = mono_class_get_interfaces(klass, &ptr);

    // Behavior here is different between mono and coreclr.
    // Mono will not report parent interfaces. CoreCLR will. It is not easy to make CoreCLR
    // match mono, as the information is not available to CoreCLR at that point.
    if (g_Mode == CoreCLR )
        CHECK(monoInterface != NULL);
    else
        CHECK(monoInterface == NULL);
}

TEST(sequential_layout_is_respected)
{
    // CoreCLR does not respect sequential layout for non-blittable types
    if (g_Mode == CoreCLR)
        return;

    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassWithSequentialLayout");
    gpointer ptr = nullptr;
    int count = 0;
    MonoClassField* field;
    size_t lastOffset = 0;
    while ((field = mono_class_get_fields(klass, &ptr)) != nullptr)
    {
        size_t offset = mono_field_get_offset(field);
        CHECK(offset > lastOffset);
        lastOffset = offset;
    }
    CHECK(lastOffset > 0);
}

TEST(explicit_layout_is_respected)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "ClassWithExplicitLayout");
    gpointer ptr = nullptr;
    MonoClassField* field;
    size_t offset = 0;
    size_t SCRIPTING_OBJECT_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) : sizeof(void*) * 2;

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 0, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 8, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 16, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 20, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + 24, offset);

    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(explicit_layout_is_correctly_calculated_for_derived_class)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "DerivedClassWithExplicitLayout");
    gpointer ptr = nullptr;
    MonoClassField* field;
    size_t offset = 0;
    size_t SCRIPTING_OBJECT_HEADERSIZE = g_Mode == CoreCLR ? sizeof(void*) : sizeof(void*) * 2;
    size_t parentSize = 32;

    // CoreCLR treats explicit layout for derived classes different than mono or il2cpp do.
    // It will add the base type size to the offset. This is a problem, because it causes a
    // different layout than with mono. So, in this case, where the FieldOffset attributes
    // already include the parent size, we need to add the parent size a second time for CoreCLR.
    // We need to figure out how we want to deal with this, but for now, we test the behavior we have.
    if (g_Mode == CoreCLR)
        parentSize *= 2;

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 0, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 8, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 16, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 20, offset);

    field = mono_class_get_fields(klass, &ptr);
    offset = mono_field_get_offset(field);
    CHECK_EQUAL(SCRIPTING_OBJECT_HEADERSIZE + parentSize + 24, offset);

    field = mono_class_get_fields(klass, &ptr);
    CHECK(field == NULL);
}

TEST(mono_class_get_methods_retrieves_all_methods)
{
    MonoClass* klass = GetClassHelper(kTestDLLNameSpace, "TestClassWithMethods");

    gpointer ptr = nullptr;
    int count = 0;
    std::string methodnames;
    MonoMethod* method;
    while((method = mono_class_get_methods(klass, &ptr)) != nullptr)
    {
        GET_AND_CHECK(methodname, mono_method_get_name(method));
        methodnames += methodname;
        count++;
    }

    CHECK_EQUAL(4, count);
    CHECK_EQUAL("ABC.ctor", methodnames);
}

TEST(mono_get_enum_class_returns_enum_class)
{
    GET_AND_CHECK(enumClass, mono_get_enum_class());
    CHECK_EQUAL_STR("Enum", mono_class_get_name(enumClass));
}

TEST(mono_get_corlib_can_get_corlib_type)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    CHECK_EQUAL_STR("Int32", mono_class_get_name(int32Class));
}

TEST(mono_array_class_get_creates_array_class)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    GET_AND_CHECK(int64Class, mono_class_from_name(mono_get_corlib(), "System", "Int64"));

    GET_AND_CHECK(arrayInt32Class, mono_array_class_get(int32Class, 1));
    GET_AND_CHECK(arrayInt64Class, mono_array_class_get(int64Class, 2));

    CHECK_EQUAL(4, mono_array_element_size(arrayInt32Class));
    CHECK_EQUAL(8, mono_array_element_size(arrayInt64Class));

    CHECK_EQUAL(1, mono_class_get_rank(arrayInt32Class));
    CHECK_EQUAL(2, mono_class_get_rank(arrayInt64Class));

    CHECK_EQUAL(int32Class, mono_class_get_element_class(arrayInt32Class));
    CHECK_EQUAL(int64Class, mono_class_get_element_class(arrayInt64Class));

    CHECK_EQUAL_STR("Int32[]", mono_class_get_name(arrayInt32Class));
    CHECK_EQUAL_STR("Int64[,]", mono_class_get_name(arrayInt64Class));
}

TEST(mono_array_new_creates_array_instance)
{
    GET_AND_CHECK(int32Class, mono_class_from_name(mono_get_corlib(), "System", "Int32"));
    GET_AND_CHECK(arrayInt32Class, mono_array_class_get(int32Class, 1));
    GET_AND_CHECK(arrayInt32Instance, mono_array_new(g_domain, int32Class, 5));
    CHECK_EQUAL(5, coreclr_array_length(arrayInt32Instance));
}

#if WIN32
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif

int GetCoreLibClassTypeHelper(const char* namespaze, const char* name)
{
    GET_AND_CHECK(klass, mono_class_from_name(mono_get_corlib(), namespaze, name));
    GET_AND_CHECK(type, mono_class_get_type(klass));
    return mono_type_get_type(type);
}

TEST(mono_type_get_type_returns_expected_values)
{
    CHECK_EQUAL(MONO_TYPE_OBJECT, GetCoreLibClassTypeHelper ("System", "Object"));
    CHECK_EQUAL(MONO_TYPE_STRING, GetCoreLibClassTypeHelper ("System", "String"));
    CHECK_EQUAL(MONO_TYPE_I4, GetCoreLibClassTypeHelper ("System", "Int32"));
}

TEST(mono_class_from_mono_type_returns_class)
{
    GET_AND_CHECK(objectClass, mono_class_from_name(mono_get_corlib(), "System", "Object"));
    GET_AND_CHECK(objectType, mono_class_get_type(objectClass));
    CHECK_EQUAL(objectClass, mono_class_from_mono_type(objectType));
}

TEST(mono_type_get_object_returns_type_object)
{
    GET_AND_CHECK(objectClass, mono_class_from_name(mono_get_corlib(), "System", "Object"));
    GET_AND_CHECK(objectType, mono_class_get_type(objectClass));
    GET_AND_CHECK(objectTypeObject, mono_type_get_object(g_domain, objectType));
}

TEST(mono_class_get_nesting_type_returns_nesting_class)
{
    MonoClass *containingClass = GetClassHelper(kTestDLLNameSpace, "ClassWithNestedClass");
    MonoClass *nestedClass = GetClassHelper(kTestDLLNameSpace, "ClassWithNestedClass/NestedClass");
    CHECK_EQUAL(containingClass, mono_class_get_nesting_type(nestedClass));
    CHECK(mono_class_get_nesting_type(containingClass) == nullptr);
}

TEST(mono_class_get_nesting_type_returns_generic_nesting_class)
{
    MonoClass *containingClass = GetClassHelper(kTestDLLNameSpace, "GenericClassWithNestedClass`1");
    MonoClass *nestedClass = GetClassHelper(kTestDLLNameSpace, "GenericClassWithNestedClass`1/NestedClass");
    CHECK_EQUAL(containingClass, mono_class_get_nesting_type(nestedClass));
    CHECK(mono_class_get_nesting_type(containingClass) == nullptr);
}

TEST(mono_object_get_class_returns_class)
{
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    GET_AND_CHECK(obj, mono_object_new(g_domain, klass));
    CHECK_EQUAL(klass, mono_object_get_class(obj));
}

TEST(mono_class_set_userdata_can_be_retrieved)
{
    int userData = 100;
    MonoClass *klass = GetClassHelper(kTestDLLNameSpace, kTestClassName);
    mono_class_set_userdata(klass, &userData);

    CHECK_EQUAL(&userData, (int*)mono_class_get_userdata(klass));
    CHECK_EQUAL(&userData, *(int**)(((char*)klass) + mono_class_get_userdata_offset()));
}

TEST(mono_custom_attrs_has_attr_can_check_class_attribute)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");
    MonoClass *klassAnotherTestAttribute = GetClassHelper(kTestDLLNameSpace, "AnotherTestAttribute");

    MonoObject* attrObj = mono_unity_class_get_attribute(klassClassWithAttribute, klassTestAttribute);

    CHECK(attrObj != NULL);
    CHECK(mono_unity_class_get_attribute(klassClassWithAttribute, klassTestWithParamsAttribute) != NULL);
    CHECK(mono_unity_class_get_attribute(klassClassWithAttribute, klassAnotherTestAttribute) == NULL);
}

TEST(mono_custom_attrs_get_attr_can_get_attribute_instance)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithAttribute");
    MonoClass *klassTestWithParamsAttribute = GetClassHelper(kTestDLLNameSpace, "TestWithParamsAttribute");

    GET_AND_CHECK(attributeInstance, mono_unity_class_get_attribute(klassClassWithAttribute, klassTestWithParamsAttribute));
    CHECK_EQUAL(klassTestWithParamsAttribute, mono_object_get_class(attributeInstance));
}

TEST(mono_custom_attrs_get_attr_can_get_attribute_instance_for_inherited_attribute_from_base)
{
    MonoClass *klassClassWithAttribute = GetClassHelper(kTestDLLNameSpace, "ClassWithInheritedAttribute");
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassInheritedTestAttribute = GetClassHelper(kTestDLLNameSpace, "InheritedTestAttribute");

    GET_AND_CHECK(attributeInstance, mono_unity_class_get_attribute(klassClassWithAttribute, klassTestAttribute));
    CHECK_EQUAL(klassInheritedTestAttribute, mono_object_get_class(attributeInstance));
}

TEST(mono_custom_attrs_has_attr_can_check_field_attribute)
{
    // TODO
}

TEST(mono_custom_attrs_has_attr_can_check_property_attribute)
{
    // TODO
}

TEST(mono_custom_attrs_has_attr_can_check_assembly_attribute)
{
    MonoClass *klassTestAttribute = GetClassHelper(kTestDLLNameSpace, "TestAttribute");
    MonoClass *klassAnotherTestAttribute = GetClassHelper(kTestDLLNameSpace, "AnotherTestAttribute");

    CHECK(mono_unity_assembly_get_attribute(g_assembly, klassTestAttribute) != NULL);
    CHECK(mono_unity_assembly_get_attribute(g_assembly, klassAnotherTestAttribute) == NULL);
}

#define kHelloString "Hello"
#define kHelloWorldString "Hello, World!"
#define kHelloWorldStringWithEmbeddedNull "Hello\0World"
#define kHelloWorldStringWithUnicode "Hello, 団結!"



TEST(mono_string_new_len_with_unicode_ascii_creates_string)
{
    if (g_Mode == CoreCLR)
    {
        MonoString* str = mono_string_new_len(nullptr, kHelloWorldStringWithUnicode, strlen(kHelloWorldStringWithUnicode));
        CHECK(ExtractManagedFromHandle(str)->length == 10);
    }
}

TEST(mono_string_new_wrapper_with_unicode_ascii_creates_string)
{
    if (g_Mode == CoreCLR)
    {
        MonoString* str = mono_string_new_wrapper(kHelloWorldStringWithUnicode);
        CHECK(ExtractManagedFromHandle(str)->length == 10);
    }
}

int InternalMethod()
{
   return 42;
}

static const char* find_plugin_callback(const char* name)
{
    printf("Load plugin %s\n", name);

    if (strcmp(name, "foo.lib") == 0)
        return abs_path_from_file("nativelib/nativelib.dylib").c_str();

    return NULL;
}

#if WIN32
#define sleep Sleep;
#endif

bool g_WaitForGC;
void InternalMethodWhichBlocks()
{
    MonoInternalCallFrameOpaque frame;
    // In mono, we don't have (or need) this function, so check for it's existance.
    if (mono_enter_internal_call)
        mono_enter_internal_call(&frame);

    g_WaitForGC = true;
    while (g_WaitForGC)
        sleep(1);

    if (mono_exit_internal_call)
        mono_exit_internal_call(&frame);
}

void InternalMethodWhichThrows()
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(ex, mono_exception_from_name_msg(image, kTestDLLNameSpace, "TestException", "Hello"));
    mono_raise_exception(ex);
}

void InternalMethodWhichReturnsExceptionInRefParam(MonoException **e)
{
    GET_AND_CHECK(image, mono_assembly_get_image(g_assembly));
    GET_AND_CHECK(ex, mono_exception_from_name_msg(image, kTestDLLNameSpace, "TestException", "Hello"));
    *e = ex;
}

#if ENABLE_FAILING_TESTS
#define REMAP_TEST_SRC_PATH_NAME "Foo.txt"
#define REMAP_TEST_SRC_ASSEMBLY_NAME "Foo.dll"

size_t RemapMonoPath(const char* path, char* buffer, size_t bufferLen)
{
    const char* remapped;
    std::string remappedString;
    if (strstr(path, REMAP_TEST_SRC_PATH_NAME) != NULL)
    {
        remappedString = abs_path_from_file("Hello.txt");
    }

    if (strstr(path, REMAP_TEST_SRC_ASSEMBLY_NAME) != NULL)
    {
        remappedString = abs_path_from_file("../unloadable-test-dll/bin/Debug/net461/unloadable-test-dll.dll");
    }

    if (remappedString.empty())
        return 0;

    remapped = remappedString.c_str();

    printf("Remap %s to %s\n", path, remapped);
    size_t lenNeeded = strlen(remapped);
    if (bufferLen >= lenNeeded)
        strcpy(buffer, remapped);
    return lenNeeded;
}

TEST(mono_unity_register_path_remapper_can_remap_assembly_load)
{
    MonoAssembly *forwarderAssembly = mono_domain_assembly_open (g_domain, "Foo.dll");
    CHECK(forwarderAssembly == NULL);

    mono_unity_register_path_remapper (RemapMonoPath);

    forwarderAssembly = mono_domain_assembly_open (g_domain, "Foo.dll");
    CHECK(forwarderAssembly != NULL);

    mono_unity_register_path_remapper (NULL);
}
#endif // ENABLE_FAILING_TESTS

void SetupMono(Mode mode)
{
    g_Mode = mode;
#if defined(_DEBUG)
    std::string testDllPath = abs_path_from_file("../coreclr-test/bin/Debug/net8.0/coreclr-test.dll");
#else
    std::string testDllPath = abs_path_from_file("../coreclr-test/bin/Release/net8.0/coreclr-test.dll");
#endif

    std::string monoLibFolder;
    std::string assembliesPaths;
    if (mode == CoreCLR)
    {
#if defined(__APPLE__)
#if defined(_DEBUG)
#ifdef __aarch64__
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Debug/runtimes/osx-arm64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Debug/runtimes/osx-arm64/native/libcoreclr.dylib");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Debug/runtimes/osx-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Debug/runtimes/osx-x64/native/libcoreclr.dylib");
#endif // __aarch64__
#else
#ifdef __aarch64__
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/native/libcoreclr.dylib");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/runtimes/osx-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.osx-x64/Release/runtimes/osx-x64/native/libcoreclr.dylib");
#endif // __aarch64__
#endif
#elif defined(__linux__)
#if defined(_DEBUG)
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Debug/runtimes/linux-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Debug/runtimes/linux-x64/native/libcoreclr.so");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/native/libcoreclr.so");
#endif
#elif defined(WIN32)
#if defined(_DEBUG)
#ifdef _M_AMD64
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Debug/runtimes/win-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Debug/runtimes/win-x64/native/coreclr.dll");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Debug/runtimes/win-x86/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Debug/runtimes/win-x86/native/coreclr.dll");
#endif
#else
#ifdef _M_AMD64
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Release/runtimes/win-x64/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x64/Release/runtimes/win-x64/native/coreclr.dll");
#else
        monoLibFolder = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Release/runtimes/win-x86/lib/net8.0");
        g_monoDllPath = abs_path_from_file("../../artifacts/bin/microsoft.netcore.app.runtime.win-x86/Release/runtimes/win-x86/native/coreclr.dll");
#endif
#endif
#else
        printf("Unsupported platform\n");
        g_monoDllPath = "";
#endif
    }
    else
    {
        monoLibFolder = abs_path_from_unity_root("External/MonoBleedingEdge/builds/monodistribution/lib");
#if defined(__APPLE__)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/osx/libmonobdwgc-2.0.dylib");
#elif defined(__linux__)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/linux64/libmonobdwgc-2.0.so");
#elif defined(WIN32)
        g_monoDllPath = abs_path_from_unity_root("External/MonoBleedingEdge/builds/embedruntimes/win64/mono-2.0-bdwgc.dll");
#endif
    }

    #define DO_API(r,n,p) typedef r (*type_##n)p; n = (type_##n)get_method(#n);
    #include "../../src/coreclr/vm/mono/MonoFunctionsClr.h"
    #undef DO_API

    printf("Setting up directories for Mono...\n");
    mono_set_dirs(monoLibFolder.c_str(), "");

    g_domain = mono_jit_init_version("myapp", "v4.0.30319");
    g_assembly = mono_domain_assembly_open(g_domain, testDllPath.c_str());
}

void ShutdownMono()
{
    printf("Cleaning up...\n");

#if JON
    // we cannot close the coreclr library
    dlclose(s_MonoLibrary);
#endif
    s_MonoLibrary = NULL;
}

int RunTests(Mode mode)
{
    SetupMono(mode);

    Catch::Session session;
    int result = session.run();

    ShutdownMono();

    return result;
}

int main(int argc, char * argv[])
{
    if (getenv("UNITY_ROOT") != NULL)
        return RunTests(Mono);

    return RunTests(CoreCLR);
}
