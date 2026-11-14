using System;
using sailflow.engine.Ids;

namespace sailflow.engine.Hosting
{
    /// <summary>配置 <see cref="ISailIdSource"/> / <see cref="ISailBpmnIdCodec"/>（Builder 期）。内置 Guid 实现见 runtime <c>UseGuidString</c>。</summary>
    public sealed class SailIdBuilder
    {
        internal ISailIdSource Source { get; private set; }
        internal ISailBpmnIdCodec Codec { get; private set; }
        internal bool? GuidCompatibleColumns { get; private set; }

        public SailIdBuilder UseSource(ISailIdSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            return this;
        }

        /// <summary>行键与设计键分流。</summary>
        public SailIdBuilder UseSource(ISailIdSource entityRow, ISailIdSource processDesign)
        {
            Source = new CompositeSailIdSource(entityRow, processDesign);
            return this;
        }

        public SailIdBuilder UseBpmnCodec(ISailBpmnIdCodec codec)
        {
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            return this;
        }

        /// <summary>供 InitDb 读取；默认跟 Source.IsGuidCompatible。</summary>
        public SailIdBuilder SetGuidCompatibleColumns(bool enabled)
        {
            GuidCompatibleColumns = enabled;
            return this;
        }
    }
}
