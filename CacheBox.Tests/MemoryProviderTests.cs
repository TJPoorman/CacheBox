using CacheBox.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace CacheBox.Tests;

[TestClass]
public class MemoryProviderTests
{
    internal static ICacheProvider provider;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        string jsonConfig = @"{
                ""Cache"": {
                    ""AppPrefix"": ""MemoryProviderTests"",
                    ""Timeout"": ""0:0:10""
                }
            }";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));

        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Configuration.AddJsonStream(stream);
        hostBuilder.AddCacheProvider<MemoryCacheProvider>();
        var host = hostBuilder.Build();
        provider = host.Services.GetRequiredService<ICacheProvider>();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        provider = null;
    }

    [TestMethod]
    public async Task Can_Save_Object_Value()
    {
        TestClass t = new()
        {
            Name = "This is a test",
            Note = "This is a note"
        };

        await provider.SetAsync("TestKey", t, "TestCase");
        var val = await provider.GetAsync<TestClass>("TestKey", "TestCase");

        Assert.IsNotNull(val);
        Assert.AreEqual(t.Name, val.Name);
        Assert.AreEqual(t.Note, val.Note);

        await provider.RemoveAsync("TestKey", "TestCase");
    }

    [TestMethod]
    public async Task Can_Save_String_Value()
    {
        await provider.SetAsync("TestKey", "TestValue", "TestCase");
        var val = await provider.GetAsync("TestKey", "TestCase");

        Assert.IsNotNull(val);
        Assert.AreEqual("TestValue", val);

        await provider.RemoveAsync("TestKey", "TestCase");
    }

    [TestMethod]
    public async Task Default_Expire_Item_Not_Retreived()
    {
        await provider.SetAsync("TestKey", "TestValue", "TestCase");
        var valBeforeExpire = await provider.GetAsync("TestKey", "TestCase");
        await Task.Delay(TimeSpan.FromSeconds(12));
        var valAfterExpire = await provider.GetAsync("TestKey", "TestCase");

        Assert.IsNotNull(valBeforeExpire);
        Assert.AreEqual("TestValue", valBeforeExpire);
        Assert.IsNull(valAfterExpire);
    }

    [TestMethod]
    public async Task Expire_Item_Not_Retrieved()
    {
        await provider.SetAsync("TestKey", "TestValue", TimeSpan.FromSeconds(2), "TestCase");
        var valBeforeExpire = await provider.GetAsync("TestKey", "TestCase");
        await Task.Delay(TimeSpan.FromSeconds(3));
        var valAfterExpire = await provider.GetAsync("TestKey", "TestCase");

        Assert.IsNotNull(valBeforeExpire);
        Assert.AreEqual("TestValue", valBeforeExpire);
        Assert.IsNull(valAfterExpire);
    }
}
