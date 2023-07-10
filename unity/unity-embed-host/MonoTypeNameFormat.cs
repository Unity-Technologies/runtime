// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Unity.CoreCLRHelpers;

// Needs to match native MonoTypeNameFormat
public enum MonoTypeNameFormat
{
    MONO_TYPE_NAME_FORMAT_IL,
    MONO_TYPE_NAME_FORMAT_REFLECTION,
    MONO_TYPE_NAME_FORMAT_FULL_NAME,
    MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED,
    MONO_TYPE_NAME_FORMAT_REFLECTION_QUALIFIED
}
