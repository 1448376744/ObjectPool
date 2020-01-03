using Microsoft.Extensions.ObjectPool;
using NUnit.Framework;

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
            //�����������
            var pool = new ObjectPool<Connection>(new Policy<Connection>(), 2,100);
            var con1 = pool.Get();
            var con2 = pool.Get();
            pool.Return(con1);
            pool.Return(con2);
            var con3 = pool.Get();
            var con4 = pool.Get();
            try
            {
                //�Ѵﵽ���
                var con5 = pool.Get();
            }
            catch (System.Exception)
            {
                //���·Ż��ٴλ�ȡ
                pool.Return(con2);
                var con5 = pool.Get();

            }
            Assert.Pass();
        }
    }
    /// <summary>
    /// �������
    /// </summary>
    public class Connection
    {
        public int Id { get; set; } = 0;
        public System.Guid Guid { get; set; } = System.Guid.NewGuid();
    }
    /// <summary>
    /// �������
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