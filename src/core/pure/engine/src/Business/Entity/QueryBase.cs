using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 分页查询基类（非落库）。
    /// </summary>
    public abstract class QueryBase
    {
        /// <summary>页码（从实现约定起始，通常 1 或 0）。</summary>
        public int PageIndex { get; set; }

        /// <summary>每页条数。</summary>
        public int PageSize { get; set; }

        /// <summary>符合条件的总行数（查询后回填）。</summary>
        public int TotalRowsCount { get; set; }
    }

    /// <summary>
    /// 带实体条件的泛型查询（非落库）。
    /// </summary>
    /// <typeparam name="T">条件实体类型</typeparam>
    public class Query<T> : QueryBase
        where T:class
    {
        /// <summary>查询条件实体（字段作过滤条件）。</summary>
        public T Entity { get; set; }
    }
}
