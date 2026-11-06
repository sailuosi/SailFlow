using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace saiflow.data
{
    /// <summary>
    /// 读取实体映射：优先 mooSQL <c>[SooTable]</c>/<c>[SooColumn]</c>（属性 Name / IsPrimaryKey），
    /// 兼容过渡期自建 <c>[Table]</c>/<c>[Column]</c>（字段 TableName/ColumnName）。
    /// 供 <see cref="SailEntityKit"/> / <see cref="SailCmdRunner"/> 构建 SQL，不依赖 DapperExtensions。
    /// </summary>
    public static class SailEntityMapper
    {
        public static string ResolveTableName(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var attrs = Attribute.GetCustomAttributes(entityType);
            foreach (var attr in attrs)
            {
                // SooTable.Name（property）优先
                var name = TryReadStringProp(attr, "Name")
                    ?? TryReadStringField(attr, "TableName")
                    ?? TryReadStringField(attr, "tableName");
                if (!string.IsNullOrWhiteSpace(name)
                    && IsTableLikeAttribute(attr))
                    return name;
            }

            // 再扫一遍：仅有 Name 且类型名含 Table
            foreach (var attr in attrs)
            {
                if (!IsTableLikeAttribute(attr))
                    continue;
                var name = TryReadStringProp(attr, "Name")
                    ?? TryReadStringField(attr, "TableName");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            return entityType.Name;
        }

        public static string ResolveTableName<T>() where T : class => ResolveTableName(typeof(T));

        /// <summary>属性 → 数据库列名（含可写属性；跳过未映射标记由调用方决定）。</summary>
        public static IReadOnlyList<ColumnMap> GetColumnMaps(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var list = new List<ColumnMap>();
            foreach (var prop in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanRead)
                    continue;

                var col = ResolveColumnName(prop);
                var isKey = IsPrimaryKey(prop, col);
                list.Add(new ColumnMap(prop, col, isKey));
            }
            return list;
        }

        public static IReadOnlyList<ColumnMap> GetColumnMaps<T>() where T : class => GetColumnMaps(typeof(T));

        public static string ResolveColumnName(PropertyInfo prop)
        {
            foreach (var attr in prop.GetCustomAttributes(false))
            {
                if (!IsColumnLikeAttribute(attr))
                    continue;

                var name = TryReadStringProp(attr, "Name")
                    ?? TryReadStringField(attr, "ColumnName")
                    ?? TryReadStringField(attr, "columnName");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            // 与 Dapper PropertyMap 一致：无 Column 特性时用属性名
            return prop.Name;
        }

        public static bool IsPrimaryKey(PropertyInfo prop, string columnName)
        {
            foreach (var attr in prop.GetCustomAttributes(false))
            {
                if (!IsColumnLikeAttribute(attr))
                    continue;

                var pk = TryReadBoolProp(attr, "IsPrimaryKey");
                if (pk == true)
                    return true;
            }

            if (string.Equals(prop.Name, "Id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "id", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var attr in prop.GetCustomAttributes(false))
            {
                var typeName = attr.GetType().Name;
                if (typeName.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool IsTableLikeAttribute(object attr)
        {
            var n = attr.GetType().Name;
            return n.IndexOf("Table", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsColumnLikeAttribute(object attr)
        {
            var n = attr.GetType().Name;
            return n.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("SooColumn", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string TryReadStringProp(object attr, string propName)
        {
            var p = attr.GetType().GetProperty(propName);
            return p?.GetValue(attr) as string;
        }

        private static bool? TryReadBoolProp(object attr, string propName)
        {
            var p = attr.GetType().GetProperty(propName);
            if (p == null || p.PropertyType != typeof(bool))
                return null;
            return (bool)p.GetValue(attr);
        }

        private static string TryReadStringField(object attr, string fieldName)
        {
            var field = attr.GetType().GetField(fieldName);
            return field?.GetValue(attr) as string;
        }
    }

    public sealed class ColumnMap
    {
        public ColumnMap(PropertyInfo property, string columnName, bool isKey)
        {
            Property = property;
            ColumnName = columnName;
            IsKey = isKey;
        }

        public PropertyInfo Property { get; }
        public string ColumnName { get; }
        public bool IsKey { get; }

        public object GetValue(object entity) => Property.GetValue(entity);
    }
}
