﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnitOfWork.Unity.Inteception.Logging;
using Unity;
using Unity.Interception.InterceptionBehaviors;
using Unity.Interception.PolicyInjection.Pipeline;

namespace UnitOfWork.Unity.Inteception
{
    public class UoWInterceptionBehavior<TUoW> : IInterceptionBehavior
        where TUoW : IUnitOfWork
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private readonly IUnityContainer _unityContainer;

        private readonly ConcurrentDictionary<Type, Func<Task, IMethodInvocation, Task>>
            _wrapperCreators = new ConcurrentDictionary<Type, Func<Task,
                IMethodInvocation, Task>>();

        public UoWInterceptionBehavior(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public IMethodReturn Invoke(IMethodInvocation input,
            GetNextInterceptionBehaviorDelegate getNext)
        {
            var result = getNext()(input, getNext);
            var method = input.MethodBase as MethodInfo;
            if (result.ReturnValue != null
                && method != null
                && typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                // If this method returns a Task, override the original return value
                var task = (Task)result.ReturnValue;
                return input.CreateMethodReturn(
                    this.GetWrapperCreator(method.ReturnType)(task, input), result.Outputs);
            }
            else
            {
                ProcessResult(input, result.Exception);
            }

            return result;
        }

        private Task CreateGenericWrapperTask<T>(Task task, IMethodInvocation input)
        {
            return this.DoCreateGenericWrapperTask<T>((Task<T>)task, input);
        }

        private async Task<T> DoCreateGenericWrapperTask<T>(Task<T> task,
            IMethodInvocation input)
        {
            return await task.ContinueWith(a =>
            {
                ProcessResult(input, a.Exception);
                return a.Result;
            }).ConfigureAwait(false);
        }

        private async Task CreateWrapperTask(Task task,
            IMethodInvocation input)
        {
            await task.ContinueWith(a =>
            {
                ProcessResult(input, a.Exception);
            }).ConfigureAwait(false);
            Log.Info("Successfully finished async operation {0}",
                input.MethodBase.Name);
        }

        private Func<Task, IMethodInvocation, Task> GetWrapperCreator(Type taskType)
        {
            return this._wrapperCreators.GetOrAdd(
                taskType,
                (Type t) =>
                {
                    if (t == typeof(Task))
                    {
                        return this.CreateWrapperTask;
                    }
                    else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        return (Func<Task, IMethodInvocation, Task>)this.GetType()
                            .GetMethod("CreateGenericWrapperTask",
                                BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(new Type[] { t.GenericTypeArguments[0] })
                            .CreateDelegate(typeof(Func<Task, IMethodInvocation, Task>), this);
                    }
                    else
                    {
                        // Other cases are not supported
                        return (task, _) => task;
                    }
                });
        }

        private void ProcessResult(IMethodInvocation input, Exception ex)
        {
            var uow = ResolveUoW();
            if (ex != null)
            {
                Log.Debug($"{input.MethodBase.Name} Rollback transaction");
                uow.Rollback();
            }
            else
            {
                Log.Debug($"{input.MethodBase.Name} Commit transaction");
                uow.Commit();
            }
        }

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        private TUoW ResolveUoW()
        {
            var uow = _unityContainer.Resolve<TUoW>();
            return uow;
        }

        public bool WillExecute
        {
            get { return true; }
        }
    }
}
