using saiflow.AI.Entity;
using saiflow.AI.Manager;
using saiflow.AI.Configuration;
using System.Collections.Generic;

namespace saiflow.AI.Service
{
    /// <summary>
    /// Knowledge Base Document Service
    /// 知识库文档服务实现
    /// </summary>
    public class KbDocumentService : IKbDocumentService
    {
        private readonly AiAppConfigProviderOptions _configOptions;

        public KbDocumentService(AiAppConfigProviderOptions configOptions = null)
        {
            _configOptions = configOptions;
        }

        public List<KbDocumentEntity> GetAllDocuments()
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.GetAll();
        }

        public List<string> GetTableNames()
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.GetTableNames();
        }

        public List<KbDocumentEntity> TestReadBizDocuments(int limit = 5)
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.TestReadBizDocuments(limit);
        }

        public KbDocumentEntity SaveDocument(KbDocumentEntity entity)
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.SaveDocument(entity);
        }

        public int DeleteDocumentById(long id)
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.DeleteDocumentById(id);
        }

        public List<KbDocumentEntity> SearchDocuments(string query, float threshold = 0.7f, int count = 5)
        {
            var documentManager = new KbDocumentManager(_configOptions);
            return documentManager.SearchDocuments(query, threshold, count);
        }
    }
}
