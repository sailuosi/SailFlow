using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Client;

namespace sailflow.biz.query
{
    /// <summary>流程抄送产品查询/写入（sf_process_instance_copy）。</summary>
    public class SailCopyService
    {
        public SailCopyService(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        public int CreateMany(IEnumerable<SailProcessCopyEntity> rows)
        {
            var n = 0;
            var kit = Sail.UseRunSQL();
            var repo = kit.useRepo<SailProcessCopyEntity>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.UserId)) continue;
                // Id 留 null，由 AutoKey=Guid 在 Insert 时发号写回
                if (string.IsNullOrWhiteSpace(row.ProcessCopyId))
                    row.ProcessCopyId = null;
                row.CreateTime ??= DateTime.Now;
                repo.Insert(row);
                n++;
            }
            return n;
        }

        public (IList<SailProcessCopyEntity> Items, int Total) QueryMyPage(string userId, int pageIndex, int pageSize)
        {
            pageSize = pageSize > 0 ? pageSize : 20;
            pageIndex = pageIndex > 0 ? pageIndex : 1;
            var skip = (pageIndex - 1) * pageSize;

            var clip = Sail.UseRunSQL().useClip();
            clip.from<SailProcessCopyEntity>(out var c)
                .whereIf(!string.IsNullOrEmpty(userId), () => c.UserId, userId)
                .orderByDesc(() => c.CreateTime);
            var all = clip.select(c).queryList().ToList();
            return (all.Skip(skip).Take(pageSize).ToList(), all.Count);
        }
    }
}
