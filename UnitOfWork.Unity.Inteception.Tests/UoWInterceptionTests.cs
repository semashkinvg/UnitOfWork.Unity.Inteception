using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading.Tasks;
using UnitOfWork.Unity.Inteception.Tests.Dummy;
using Unity;
using Unity.Interception.ContainerIntegration;
using Unity.Interception.Interceptors.InstanceInterceptors.InterfaceInterception;

namespace UnitOfWork.Unity.Inteception.Tests
{
    [TestClass]
    public class UoWInterceptionTests
    {
        private IUnitOfWork _unitOfWork;
        private IUnityContainer _uc;

        [TestInitialize]
        public void SetUp()
        {
            _uc = new UnityContainer();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _uc.AddNewExtension<Interception>();
            _uc.RegisterInstance<IUnitOfWork>(_unitOfWork);
            _uc.RegisterType<IDummyService, DummyService>(
                new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<UoWInterceptionBehavior<IUnitOfWork>>());

        }

        [TestCleanup]
        public void TearDown()
        {
            _uc.Dispose();
        }

        [TestMethod]
        public void should_commit_async_method_call()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            CallAsync(service.AsyncMethod).Wait();

            // Assert
            _unitOfWork.Received(1).Commit();
        }

        [TestMethod]
        public void should_commit_async_method_call_with_result()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            CallAsync(service.AsyncMethodWithResult).Wait();

            // Assert
            _unitOfWork.Received(1).Commit();
        }

        [TestMethod]
        public void should_rollback_async_method_call_if_exception_is_thrown()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            try
            {
                CallAsync(service.AsyncMethodWithResultThrowsException).Wait();
            }
            catch (AggregateException e)
            {
                // Assert
                e.InnerException.Should().NotBeNull();
                e.InnerException.Message.Should().Be("unique key violation");
                _unitOfWork.Received(1).Rollback();
                _unitOfWork.DidNotReceive().Commit();
                return;
            }

            Assert.Fail("The exception wasn't rethrown");

        }

        [TestMethod]
        public void should_commit_method_call_with_result()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            service.MethodWithResult();

            // Assert
            _unitOfWork.Received(1).Commit();
        }

        [TestMethod]
        public void should_commit_method_call()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            service.Method();

            // Assert
            _unitOfWork.Received(1).Commit();
        }

        [TestMethod]
        public void should_rollback_method_call_if_exception_is_thrown()
        {
            // Arrange 
            var service = _uc.Resolve<IDummyService>();

            // Act
            try
            {
                service.MethodWithResultThrownsException();
            }
            catch (Exception e)
            {
                // Assert
                e.Message.Should().Be("unique key violation");
                _unitOfWork.Received(1).Rollback();
                _unitOfWork.DidNotReceive().Commit();
                return;
            }

            Assert.Fail("The exception wasn't rethrown");
        }

        private async Task CallAsync(Func<Task> action)
        {
            await action();
        }
    }
}
