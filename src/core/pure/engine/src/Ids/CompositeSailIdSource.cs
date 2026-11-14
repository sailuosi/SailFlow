using System;

namespace sailflow.engine.Ids
{
    /// <summary>行键与设计键分流到两个 Source（仍对外一个 ISailIdSource）。</summary>
    public sealed class CompositeSailIdSource : ISailIdSource
    {
        private readonly ISailIdSource _entityRow;
        private readonly ISailIdSource _processDesign;

        public CompositeSailIdSource(ISailIdSource entityRow, ISailIdSource processDesign)
        {
            _entityRow = entityRow ?? throw new ArgumentNullException(nameof(entityRow));
            _processDesign = processDesign ?? throw new ArgumentNullException(nameof(processDesign));
        }

        public bool IsGuidCompatible =>
            _entityRow.IsGuidCompatible && _processDesign.IsGuidCompatible;

        public string AutoKeyName => _entityRow.AutoKeyName;

        public string NextEntityRowId() => _entityRow.NextEntityRowId();

        public string NextProcessDesignId() => _processDesign.NextProcessDesignId();
    }
}
