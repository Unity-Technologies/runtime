using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

[TestFixture]
public class EmbeddingApiTests : BaseEmbeddingApiTests
{
    internal override ICoreCLRHostWrapper ClrHost { get; } = new CoreCLRHostWrappers();

    [Test]
    public unsafe void ClassFromNameNullImageThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.class_from_name(IntPtr.Zero, null, null, false));
    }

    [Test]
    public unsafe void ClassFromNameNullNamespaceThrows()
    {
        var type = typeof(Animal);
        Assert.Throws<ArgumentNullException>(() => ClrHost.class_from_name(ClrHost.class_get_image(type), null, null, false));
    }


    [Test]
    public unsafe void ClassFromNameNullNameThrows()
    {
        var type = typeof(Animal);
        byte[] @namespace = Encoding.UTF8.GetBytes(type.Namespace!);
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixed (byte* ns = @namespace)
            {
                ClrHost.class_from_name(ClrHost.class_get_image(type), (sbyte*)ns, null, false);
            }
        });
    }

    [Test]
    public unsafe void ClassGetImageNullImageThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.class_get_image(null));
    }

    [Test]
    public unsafe void ArrayLengthNullArrayThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.array_length(null));
    }

    [Test]
    public unsafe void ArrayNewNullKlassThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.array_new(null, 0));
    }

    [Test]
    public unsafe void AssemblyGetObjectNullAssemblyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.assembly_get_object(IntPtr.Zero));
    }

    [Test]
    public unsafe void AssemblyLoadedNullAssemblyNameThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.assembly_loaded(null));
    }

    [Test]
    public unsafe void ExceptionFromNameMsgNullImageThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.exception_from_name_msg(IntPtr.Zero, null, null, null));
    }

    [Test]
    public unsafe void ExceptionFromNameMsgNullNamespaceThrows()
    {
        var type = typeof(Animal);
        Assert.Throws<ArgumentNullException>(() => ClrHost.exception_from_name_msg(ClrHost.class_get_image(type), null, null, null));
    }

    [Test]
    public unsafe void ExceptionFromNameMsgNullNameThrows()
    {
        var type = typeof(Animal);
        byte[] @namespace = Encoding.UTF8.GetBytes(type.Namespace!);
        Assert.Throws<ArgumentNullException>(() =>
        {
            fixed (byte* ns = @namespace)
            {
                ClrHost.exception_from_name_msg(ClrHost.class_get_image(type), (sbyte*)ns, null, null);
            }
        });
    }

    [Test]
    public void ObjectNewNullTypeThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.object_new(null));
    }

    [Test]
    public void ValueBoxNullTypeThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClrHost.value_box(null, IntPtr.Zero));
    }
}
