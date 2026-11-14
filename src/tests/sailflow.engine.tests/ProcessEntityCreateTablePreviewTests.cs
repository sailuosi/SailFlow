using System;
using mooSQL.data;
using mooSQL.data.context;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using sailflow.engine.Entity;
using Xunit;

namespace sailflow.engine.tests
{
    /// <summary>ProcessEntity Tag / Length 在 MSSQL createTable preview 中的落地抽查。</summary>
    public class ProcessEntityCreateTablePreviewTests
    {
        static DBInstance CreateMssqlDialectDb()
        {
            var client = new MooClient();
            client.dialectFactory = new DialectFactory();
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new SooEntityAnalyser());
            client.entityAnalyseFactory = factory;

            var dbConfig = new DataBase
            {
                dbType = DataBaseType.MSSQL,
                dialectKey = DataBaseType.MSSQL.ToString(),
                DBConnectStr = string.Empty
            };

            var db = new DBInstance
            {
                config = dbConfig,
                client = client
            };
            db.dialect = client.dialectFactory.getDialect(dbConfig);
            db.dialect.dbInstance = db;
            db.dialect.db = dbConfig;
            db.cmd = new CmdExecutor(db);
            return db;
        }

        [Fact]
        public void Preview_ProcessEntity_Tag_And_Length()
        {
            var db = CreateMssqlDialectDb();
            var sql = db.useSQL()
                .useDBInit()
                .preview()
                .columnTypeByTag("sail.id", DataFam.Guid)
                .columnTypeByTag("sail.xml", DataFam.Json)
                .createTable<ProcessEntity>()
                .Result;

            Assert.False(string.IsNullOrWhiteSpace(sql));
            Assert.Contains("uniqueidentifier", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_id] uniqueidentifier", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_process_id] uniqueidentifier", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_xml_content] nvarchar(max)", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_description] nvarchar(500)", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_name] nvarchar(255)", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[p_start_expression] nvarchar(2000)", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
