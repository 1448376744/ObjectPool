using Microsoft.Extensions.ObjectPool;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NUnitTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestTimeout()
        {
            //最大两个对象，
            var pool = new ObjectPool<Connection>(new Policy<Connection>(), 2,10000);
            var con1 = pool.Get();
            var con2 = pool.Get();
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
            Assert.Pass();
        }
    }
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
}