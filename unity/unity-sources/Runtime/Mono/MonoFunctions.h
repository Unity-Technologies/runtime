#ifndef DO_API_NO_RETURN
#define DO_API_NO_RETURN(a, b, c) DO_API(a,b,c)
#endif

#ifndef DO_API_OPTIONAL
#define DO_API_OPTIONAL(a, b, c) DO_API(a,b,c)
#endif

typedef UNUSED_SYMBOL void(*MonoUnityExceptionFunc) (MonoObject* exc);

// If you add functions to this file you also need to expose them in MonoBundle.exp
// Otherwise they wont be exported in the web plugin!
DO_API(gboolean, mono_unity_class_has_failure, (MonoClass * klass))

DO_API(void, mono_add_internal_call, (const char *name, gconstpointer method))

DO_API(MonoObject*, mono_runtime_invoke, (MonoMethod * method, void *obj, void **params, MonoException **exc))
DO_API(int, mono_field_get_offset, (MonoClassField * field))
DO_API(int, mono_class_get_userdata_offset, ())
DO_API(void, mono_class_set_userdata, (MonoClass * klass, void* userdata))

#if USE_MONO_AOT
DO_API(void*, mono_aot_get_method, (MonoDomain * domain, MonoMethod * method))
#endif

#if UNITY_EDITOR
DO_API(MonoMethodDesc*, mono_method_desc_new, (const char *name, gboolean include_namespace))
DO_API(MonoMethod*, mono_method_desc_search_in_class, (MonoMethodDesc * desc, MonoClass * klass))
DO_API(void, mono_method_desc_free, (MonoMethodDesc * desc))
DO_API(gboolean, mono_type_generic_inst_is_valuetype, (MonoType*))
#endif

DO_API(const char*, mono_field_get_name, (MonoClassField * field))
DO_API(int, mono_type_get_type, (MonoType * type))
DO_API(const char*, mono_method_get_name, (MonoMethod * method))
DO_API(MonoClass*, mono_method_get_class, (MonoMethod * method))
DO_API(const char*, mono_class_get_namespace, (MonoClass * klass))
DO_API(const char*, mono_class_get_name, (MonoClass * klass))

DO_API(void*, unity_coreclr_create_delegate, (const char* assemblyName, const char* typeName, const char* methodName))

DO_API(MonoThread *, mono_thread_attach, (MonoDomain * domain))

DO_API(void, mono_thread_detach, (MonoThread * thread))

DO_API(MonoThread *, mono_thread_current, (void))

DO_API(MonoMethodSignature*, mono_method_signature, (MonoMethod * method))
DO_API(MonoType*, mono_class_get_type, (MonoClass * klass))

DO_API(int, mono_assembly_name_parse, (const char* name, MonoAssemblyName * assembly))
DO_API(int, mono_image_get_table_rows, (MonoImage * image, int table_id))

DO_API(MonoMethod*, mono_method_get_last_managed, ())

DO_API(gint32, mono_class_instance_size, (MonoClass * klass))
DO_API(guint32, mono_class_get_type_token, (MonoClass * klass))
DO_API(MonoClass*, mono_class_from_mono_type, (MonoType * image))

#ifdef WIN32
typedef int (__cdecl *vprintf_func)(const char* msg, va_list args);
#else
typedef int (*vprintf_func)(const char* msg, va_list args);
#endif

#if UNITY_EDITOR
typedef UNUSED_SYMBOL size_t(*RemapPathFunction)(const char* path, char* buffer, size_t buffer_len);
DO_API(void, mono_unity_register_path_remapper, (RemapPathFunction func))
#endif

#if LOAD_MONO_DYNAMICALLY
DO_API_OPTIONAL(unsigned short, mono_error_get_error_code, (MonoError * error))
DO_API_OPTIONAL(const char*, mono_error_get_message, (MonoError * error))
#endif

#if UNITY_EDITOR
DO_API_OPTIONAL(gboolean, mono_debugger_get_generate_debug_info, ())
DO_API_OPTIONAL(void, mono_debugger_disconnect, ())
typedef void (*MonoDebuggerAttachFunc)(gboolean attached);
typedef UNUSED_SYMBOL void (*UnityLogErrorCallback) (const char* message);
DO_API(void, mono_unity_set_editor_logging_callback, (UnityLogErrorCallback callback))
#endif

#undef DO_API
#undef DO_API_NO_RETURN
#undef DO_API_OPTIONAL
