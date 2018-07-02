# UnitOfWork.Unity.Inteception
The implementation of Unit-Of-Work pattern behaviour using unity interception 

## Example
```
var uc = new UnityContainer();
uc.AddNewExtension<Interception>();

// the unit of work has been registered as per scope.
uc.RegisterType<IUnitOfWork, YourUoWImplementation>(new SomePerScopeLifeTimeManager());
// by the specifying interceptor to a specific implementation, you can group some operations into one transaction.
uc.RegisterType<IDummyService, DummyService>(
    new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<UoWInterceptionBehavior<IUnitOfWork>>());
```
