namespace UnitOfWork.Unity.Inteception
{
    public interface IUnitOfWork
    {
        void Begin();
        void Commit();
        void Rollback();
        void Flush();
    }
}
