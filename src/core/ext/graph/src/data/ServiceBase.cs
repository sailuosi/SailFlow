using System;

namespace saiflow.data
{
    /// <summary>
    /// Service Base
    /// 服务基类
    /// </summary>
    public class ServiceBase
    {
        private IRepository _quickRepository;
        public IRepository QuickRepository
        {
            get
            {
                if (_quickRepository == null)
                {
                    _quickRepository = RepositoryProvider.Create();
                }
                return _quickRepository;
            }
        }
    }
}
