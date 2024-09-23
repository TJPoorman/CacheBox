# CacheBox

CacheBox is a lightweight and flexible caching solution designed for .NET applications. It provides an abstraction over multiple cache providers, allowing you to seamlessly switch between different caching providers while maintaining a consistent API.

## Features

- **Pluggable Cache Providers**: Use multiple cache backends like in-memory, Redis, or distributed caches with ease.
- **Dependency Injection Support**: Integrate CacheBox seamlessly into your .NET Core projects using dependency injection.
- **High Performance**: Optimized for minimal overhead.

## Installation

To install CacheBox in your project, you can use the following NuGet command for your provider:
```bash
dotnet add package CacheBox.LiteDb
dotnet add package CacheBox.Memory
dotnet add package CacheBox.Redis
dotnet add package CacheBox.Sqlite
```

## Usage

### Basic Setup

 1. **Configuration**
 
 	CacheBox uses a very simple configuration for all providers with 3 lines.
	```json
	"Cache": {
	  // Optional: Global prefix to prepend to all keys.  Useful for shared systems.
	  "AppPrefix": "MyApplication",
	  // Connection string to connect to the provider.  See examples and links below for individual providers.
	  "ConnectionString": "MyConnectionString",
	  // Optional: Default TTL of the stored items.  This is a string representation interpreted by TimeSpan.TryParse().
	  "Timeout": "0:10:0"
	},
	```
	See the [Microsoft Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.timespan.tryparse?#system-timespan-tryparse(system-string-system-timespan@)) for examples of time strings.

1. Register CacheBox in your DI Container
	```csharp
    using CacheBox;
	
	public class Startup
	{
	    public void ConfigureServices(IServiceCollection services)
	    {
	        services.AddCacheProvider<MemoryCacheProvider>(); // Example using in-memory cache
	    }
	}
    ```
    
1. Use CacheBox in your application
	```csharp
    public class MyService
	{
	    private readonly ICacheProvider _cache;
	
	    public MyService(ICacheProvider cache)
	    {
	        _cache = cache;
	    }
	
	    public async Task DoWorkAsync()
	    {
	        string data = await _cache.GetAsync("myKey", nameof(MyService));
            MyClass stronglyTyped = await _cache.GetAsync<MyClass>("myOtherKey", nameof(MyService));
	    }
	}
    ```
    
### Switching Providers

To switch cache providers, simply register the desired provider in `ConfigureServices`:
```csharp
services.AddCacheProvider<RedisCacheProvider>();
```
    
## Cache Providers

CacheBox supports multiple cache providers. Each provider can be installed as a separate NuGet package:
* **LiteDbCacheProvider:** Uses [LiteDb](https://www.litedb.org/) as the cache backing.
	
    Example connection string: `Filename=D:\\cache.db;Password=MyPassword;Connection=shared`
    
    See [here](https://www.litedb.org/docs/connection-string/) for documentation.
    
    *Note: You must use a shared connection for a distributed cache, otherwise the first service to connect will create a lock.*

* **MemoryCacheProvider:** A simple in-memory cache.
	
    A connection string is not needed for in-memory caching.

* **RedisCacheProvider:** Uses [Redis](https://redis.io/) as the cache backing.
	
    Example connection string: `myRedisServer:6379,password=MyPassword`
    
    See [here](https://stackexchange.github.io/StackExchange.Redis/Configuration.html) for documentation.

* **SqliteCacheProvider:** Uses [Sqlite](https://www.sqlite.org/) as the cache backing.

	Example connection string: `Data Source=Cache.db;Password=MyPassword`
    
    See [here](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) for documentation.

### Adding Custom Providers

You can also implement your own custom cache provider by creating a class that implements the `ICacheProvider` interface.
```csharp
public class MyCustomCacheProvider : ICacheProvider
{
    // Implementation goes here
}
```

## License
CacheBox is licensed under the GPL License. See the [LICENSE](https://spdx.org/licenses/GPL-3.0-or-later.html) for more information.

## Contributing
Contributions are always welcome! Feel free to open an issue or submit a pull request.
