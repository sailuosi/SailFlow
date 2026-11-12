using sailflow.AI.Entity;
using System.Collections.Generic;

namespace sailflow.AI.Service
{
    /// <summary>
    /// Knowledge Base Document Service Interface
    /// 知识库文档服务接口
    /// </summary>
    public interface IKbDocumentService
    {
        List<KbDocumentEntity> GetAllDocuments();
        List<string> GetTableNames();
        List<KbDocumentEntity> TestReadBizDocuments(int limit = 5);
        KbDocumentEntity SaveDocument(KbDocumentEntity entity);
        int DeleteDocumentById(long id);
        List<KbDocumentEntity> SearchDocuments(string query, float threshold = 0.7f, int count = 5);
    }
}
