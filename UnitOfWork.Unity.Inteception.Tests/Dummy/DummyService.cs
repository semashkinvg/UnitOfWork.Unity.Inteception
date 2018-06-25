using System;
using System.Threading.Tasks;

namespace UnitOfWork.Unity.Inteception.Tests.Dummy
{
    public interface IDummyService
    {
        Task<int> AsyncMethodWithResult();
        Task<int> AsyncMethodWithResultThrowsException();
        Task AsyncMethod();
        void Method();
        int MethodWithResult();
        int MethodWithResultThrownsException();

    }

    internal class DummyService : IDummyService
    {
        public async Task<int> AsyncMethodWithResult()
        {
            // calling database or something that is handled by your UnitOfWork
            return await Task.FromResult(1);
        }
        public async Task<int> AsyncMethodWithResultThrowsException()
        {
            // calling database or something that is handled by your UnitOfWork
            return await Task.FromException<int>(new Exception("unique key violation"));
        }

        public async Task AsyncMethod()
        {
            // calling database or something that is handled by your UnitOfWork
            await Task.CompletedTask;
        }

        public void Method()
        {
            // calling database or something that is handled by your UnitOfWork
        }

        public int MethodWithResult()
        {
            // calling database or something that is handled by your UnitOfWork
            return 1;
        }

        public int MethodWithResultThrownsException()
        {
            // calling database or something that is handled by your UnitOfWork
            throw new Exception("unique key violation");
        }

    }
}
