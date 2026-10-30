using System;

namespace saiflow.data
{
    /// <summary>
    /// Manager Abstract Class
    /// 管理抽象类
    /// </summary>
    public abstract class ManagerBase
    {
        private IRepository _repository;
        public IRepository Repository
        {
            get
            {
                if (_repository == null)
                {
                    _repository = RepositoryProvider.Create();
                }
                return _repository;
            }
        }
    }
}
