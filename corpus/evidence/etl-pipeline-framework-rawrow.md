---
title: ETL Pipeline Framework — RawRow Schema-Agnostic Row Representation
tags: [etl, pipeline-architecture, iasyncenumerable, csharp, generics, data-import, schema-design, performance]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/etl-pipeline-framework-architecture.md
  - evidence/etl-pipeline-framework-transformers.md
  - evidence/etl-pipeline-framework-pipeline-variations.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/etl-pipeline-framework.md
---

# ETL Pipeline Framework — RawRow Schema-Agnostic Row Representation

Rather than requiring each pipeline source to produce a known model type, the Madera ETL framework uses a `RawRow` class — an array-backed property bag with case-insensitive string keys and automatic type conversion. The array-backed storage (with a separate key-to-index mapping) provides O(1) value access; a static type conversion registry built from reflection at startup caches `MethodInfo` for all known conversions to avoid per-row reflection. This design bridges schema differences across four external data providers without requiring intermediate models per column layout.

---

## Evidence: RawRow — Schema-Agnostic Row Representation

Rather than requiring each source to produce a known model type, the pipeline uses a `RawRow` class — an array-backed property bag with case-insensitive string keys and automatic type conversion:

```csharp
public sealed class RawRow : IPropertyBag, IEquatable<RawRow>
{
    private readonly Dictionary<string, int> _keyMappings = new(StringComparer.OrdinalIgnoreCase);
    private object?[] _data = [];
    
    public TReturn? GetAs<TReturn>(string key, TReturn? defaultValue = default) { ... }
    public void SetValue<TValue>(string key, TValue? value) { ... }
    public void UpdateIf<TValue>(string key, Predicate<TValue?> condition, Func<RawRow, TValue?, TValue> selector) { ... }
    public void Rename(string oldName, string newName) { ... }
}
```

Key design decisions:
- **Array-backed storage** with a separate key-to-index mapping, not `Dictionary<string, object>` — enables O(1) value access by index after initial key resolution
- **Static type conversion registry** built from reflection at startup, caching `MethodInfo` for all known conversions between types — avoids repeated reflection on the hot path
- **`UpdateIf` combinator** — enables conditional in-place transformation without reading and writing separately, reducing the boilerplate in transformers
- **`Rename` operation** — remaps a key without copying the value, which is important when source column names don't match downstream expectations (e.g., Ringba's `"Campaign ID"` becomes `"Ringba ID"`)
- **Multiple factory methods:** `FromDynamic` for dictionary sources, `FromSepRow` for high-performance Sep/Sylvan CSV readers, `FromTemplate` for pre-allocated row structures

Sources produce `RawRow`, processors consume and mutate `RawRow`, then convert to typed DTOs at the end. This avoids defining intermediate models for every possible column layout across four different external data providers.
