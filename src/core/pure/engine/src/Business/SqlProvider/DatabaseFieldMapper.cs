using saiflow.data;

namespace saiflow.engine.Business.SqlProvider
{
    /// <summary>
    /// Database Field Mapper（委托 <see cref="SailSqlDialect.MapField"/>）。
    /// </summary>
    public class DatabaseFieldMapper
    {
        public static string MapField(string entityField)
            => SailSqlDialect.MapField(entityField);
    }
}
