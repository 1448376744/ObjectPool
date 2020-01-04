# ObjectPool
## 项目说明

1. 该项目改编自微软同名项目：Microsoft.Extension.ObjectPool，微软编写的Pool思路是这样的，如果池的容量为8，当池中对象为空时
直接创建新的对象，实际可以超过8个，但是它容器只管理8对象的引用。 如果该对象是实现了IDisposable接口并且pool是安照下面的方式创建的
则溢出的对象会自动释放，也无法从容器中获取到这个已被释放的。全程无锁，线程安全可共享。
``` C#
 var poolProvider = new DefaultObjectPoolProvider();
 poolProvider.MaximumRetained = 2;
 var pool = poolProvider.Create(new Policy<Connection>());
 var c1 = pool.Get();
 var c2 = pool.Get();
 var c3 = pool.Get();
 pool.Return(c1);
 pool.Return(c2);
 //已超出，会自动释放
 pool.Return(c3);
```

2. 而被我改编之后的逻辑是，创建之前会判断容器已创建的对象总数。当超过容器的最大容量，将发起线程等待直至超时或者，有新的对象放回容器
ObjectPool全程无锁，线程安全可共享。

## 应用场景

为重量、非线程安全的对象提供统一管理，对对象进行复用从而提升性能。从池中返回的对象同一时刻只能被一个线程使用（核心）。底层类似队列，先进后出。
可以用于管理Redis,Grpc,RabbitMQ等中间件TCP连接对象管理。

## 使用实例

``` C#
 /// <summary>
/// 定义对象
/// </summary>
public class Connection
{
    public int Id { get; set; } = 0;
    public System.Guid Guid { get; set; } = System.Guid.NewGuid();
}
/// <summary>
/// 定义策略
/// </summary>
/// <typeparam name="T"></typeparam>
public class Policy<T> : IPooledObjectPolicy<T> where T : class, new()
{
    public T Create()
    {
        return new T();
    }

    public bool Return(T obj)
    {
        (obj as Connection).Id++;
        return true;
    }
}
```

``` C#
[Test]
public void TestTimeout()
{
    //最大两个对象，
    var pool = new ObjectPool<Connection>(new Policy<Connection>(), 2,10000);
    //获取对象
    var con1 = pool.Get();
    var con2 = pool.Get();
    //换给容器
    pool.Return(con1);
    pool.Return(con2);
    var con3 = pool.Get();
    var con4 = pool.Get();
    try
    {
        //已达到最大,超时等待10秒，1秒之后放回
        //如果下面这行代码注释将超时
        Task.Run(() => { Task.Delay(1000); pool.Return(con2); });
        var con5 = pool.Get();
    }
    catch (System.Exception)
    {
        //重新放回再次获取
        pool.Return(con2);
        var con5 = pool.Get();

    }

    //重新放回再次获取
    pool.Return(con2);
    var con6 = pool.Get();
    pool.Disposable();
    Assert.Pass();
}

```
