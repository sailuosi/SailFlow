using System;
using mooSQL.data;
using sailflow.engine.Ids;
using sailflow.runtime.Ids;
using Xunit;

namespace sailflow.engine.tests
{
    public class SailIdSourceTests
    {
        [Fact]
        public void GuidStringIdSource_Yields_D_Format()
        {
            var s = GuidStringIdSource.Instance;
            Assert.Equal(SailIdNames.AutoKey, s.AutoKeyName);
            Assert.True(s.IsGuidCompatible);
            Assert.True(Guid.TryParseExact(s.NextEntityRowId(), "D", out _));
            Assert.True(Guid.TryParseExact(s.NextProcessDesignId(), "D", out _));
        }

        [Fact]
        public void Moo_DualRegister_SailId_And_Guid_Same_Source()
        {
            var calls = 0;
            var source = new CountingIdSource(() =>
            {
                calls++;
                return Guid.NewGuid().ToString("D");
            });

            var moo = new MooClient();
            Func<object> gen = () => source.NextEntityRowId();
            moo.RegisterKeyGenerator(SailIdNames.AutoKey, gen);
            moo.RegisterKeyGenerator(SailIdNames.AutoKeyLegacyGuid, gen);

            var a = moo.ResolveAutoKey(SailIdNames.AutoKey, typeof(string));
            var b = moo.ResolveAutoKey(SailIdNames.AutoKeyLegacyGuid, typeof(string));
            Assert.IsType<string>(a);
            Assert.IsType<string>(b);
            Assert.Equal(2, calls);
        }

        [Fact]
        public void DefaultCodec_RoundTrips_Guid()
        {
            var ids = GuidStringIdSource.Instance;
            var codec = new DefaultSailBpmnIdCodec(ids);
            var id = ids.NextProcessDesignId();
            var bpmn = codec.ToBpmnElementId(id);
            var back = codec.FromBpmnElementId(bpmn, out var generated);
            Assert.False(generated);
            Assert.Equal(id, back);
        }

        [Fact]
        public void DefaultCodec_Legacy_Process_Random_Generates_New()
        {
            var codec = DefaultSailBpmnIdCodec.Instance;
            var id = codec.FromBpmnElementId("process_5639_9532", out var generatedNew);
            Assert.True(generatedNew);
            Assert.True(Guid.TryParseExact(id, "D", out _));
        }

        [Fact]
        public void DefaultCodec_RewriteProcessElementId_Updates_Plane()
        {
            const string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<bpmn:definitions xmlns:bpmn=""http://www.omg.org/spec/BPMN/20100524/MODEL"" xmlns:bpmndi=""http://www.omg.org/spec/BPMN/20100524/DI"">
  <bpmn:process id=""Process_6274_9275"" name=""t"" />
  <bpmndi:BPMNDiagram id=""BPMNDiagram_1"">
    <bpmndi:BPMNPlane id=""BPMNPlane_1"" bpmnElement=""Process_6274_9275"" />
  </bpmndi:BPMNDiagram>
</bpmn:definitions>";
            var next = "Process_550e8400e29b41d4a716446655440000";
            var outXml = DefaultSailBpmnIdCodec.Instance.RewriteProcessElementId(xml, "Process_6274_9275", next);
            Assert.Contains($"id=\"{next}\"", outXml);
            Assert.Contains($"bpmnElement=\"{next}\"", outXml);
            Assert.DoesNotContain("Process_6274_9275", outXml);
        }

        sealed class CountingIdSource : ISailIdSource
        {
            private readonly Func<string> _next;
            public CountingIdSource(Func<string> next) => _next = next;
            public bool IsGuidCompatible => true;
            public string AutoKeyName => SailIdNames.AutoKey;
            public string NextEntityRowId() => _next();
            public string NextProcessDesignId() => _next();
        }
    }
}
