using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using StringThing.UnsafeSql;

namespace StringThing.Core;

public abstract class SqlStatement<TNamer, TParameter>
    where TNamer : IParameterNamer
    where TParameter : class
{
    private static readonly ConcurrentDictionary<(string File, int Line), IBuilderTemplate> _cache = new();

    private readonly IStatementBuilder _builder;
    private readonly IReadOnlyList<TParameter> _parameters;
    private string? _resolvedSql;

    public SqlStatement(
        int literalLength,
        int formattedCount,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var cacheKey = (filePath, lineNumber);

        if (formattedCount == 0)
        {
            _parameters = Array.Empty<TParameter>();
            if (_cache.TryGetValue(cacheKey, out var template))
                _builder = template.CreateBuilder(null!);
            else
                _builder = new ZeroParamFreshBuilder(cacheKey);
            return;
        }

        var parameterList = new List<TParameter>(formattedCount);
        _parameters = parameterList;

        if (_cache.TryGetValue(cacheKey, out var nonZeroTemplate))
        {
            _builder = nonZeroTemplate.CreateBuilder(parameterList);
        }
        else
        {
            _builder = new FreshBuilder(cacheKey, parameterList);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _builder.AppendLiteral(literalText);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(UnsafeSqlFragment rawSqlFragment)
    {
        _builder.AppendLiteral(rawSqlFragment.RawText);
    }

    public void AppendFormatted(SqlFragment<TParameter> fragment,
        [CallerArgumentExpression(nameof(fragment))] string? expression = null)
    {
        _builder.AppendFragment(fragment, expression);
    }

    protected void AppendParameter(TParameter parameter, string? expression)
    {
        _builder.AppendParameter(parameter, expression);
    }

    private void EnsureResolved()
    {
        _resolvedSql ??= _builder.GetSql();
    }

    internal string Sql
    {
        get
        {
            EnsureResolved();
            return _resolvedSql!;
        }
    }

    internal IReadOnlyList<TParameter> Parameters
    {
        get
        {
            EnsureResolved();
            return _parameters;
        }
    }

    internal IReadOnlyList<string?> ParameterNames
    {
        get
        {
            EnsureResolved();
            return _builder.GetParameterNames();
        }
    }

    internal IReadOnlyList<string>? CachedPlaceholders
    {
        get
        {
            EnsureResolved();
            return _builder.GetCachedPlaceholders();
        }
    }

    private static int FindDuplicate(List<string?> names, string? expression)
    {
        if (expression is null || expression.Contains('('))
            return -1;
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], expression, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    private static void WriteFragmentSql(
        StringBuilder sql,
        List<TParameter> parameters,
        List<string?> resolvedNames,
        SqlFragment<TParameter> fragment,
        string? expression)
    {
        var prefix = SqlFragment<TParameter>.ResolveNamePrefix(expression);
        var elements = fragment.Elements;
        for (var j = 0; j < elements.Count; j++)
        {
            var element = elements[j];
            if (element.TryGetLiteral(out var literalText))
            {
                sql.Append(literalText);
            }
            else if (element.TryGetParameter(out var param, out var nestedExpr))
            {
                var combinedName = SqlFragment<TParameter>.CombineNames(prefix, nestedExpr);
                var existingIndex = FindDuplicate(resolvedNames, combinedName);
                if (existingIndex >= 0)
                {
                    sql.Append(TNamer.WritePlaceholder(existingIndex, combinedName));
                }
                else
                {
                    var paramIndex = parameters.Count;
                    parameters.Add(param);
                    resolvedNames.Add(combinedName);
                    sql.Append(TNamer.WritePlaceholder(paramIndex, combinedName));
                }
            }
        }
    }

    private interface IStatementBuilder
    {
        void AppendLiteral(string text);
        void AppendParameter(TParameter parameter, string? expression);
        void AppendFragment(SqlFragment<TParameter> fragment, string? expression);
        string GetSql();
        IReadOnlyList<string?> GetParameterNames();
        IReadOnlyList<string>? GetCachedPlaceholders();
    }

    private interface IBuilderTemplate
    {
        IStatementBuilder CreateBuilder(List<TParameter> parameters);
    }

    // --- FreshBuilder: first call at a call site, records everything ---

    private sealed class FreshBuilder : IStatementBuilder
    {
        private readonly (string File, int Line) _cacheKey;
        private readonly List<TParameter> _parameters;

        private enum SegmentKind : byte { Literal, Parameter, Fragment }
        private readonly List<(SegmentKind Kind, int Index)> _segments = [];
        private readonly List<string> _literals = [];
        private readonly List<(TParameter Parameter, string? Expression)> _staticParams = [];
        private readonly List<(SqlFragment<TParameter> Fragment, string? Expression)> _deferredFragments = [];
        private List<string?>? _resolvedNames;

        public FreshBuilder((string File, int Line) cacheKey, List<TParameter> parameters)
        {
            _cacheKey = cacheKey;
            _parameters = parameters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string text)
        {
            _segments.Add((SegmentKind.Literal, _literals.Count));
            _literals.Add(text);
        }

        public void AppendParameter(TParameter parameter, string? expression)
        {
            _segments.Add((SegmentKind.Parameter, _staticParams.Count));
            _staticParams.Add((parameter, expression));
        }

        public void AppendFragment(SqlFragment<TParameter> fragment, string? expression)
        {
            _segments.Add((SegmentKind.Fragment, _deferredFragments.Count));
            _deferredFragments.Add((fragment, expression));
        }

        public string GetSql()
        {
            // Phase 1: Resolve static parameters with deduplication
            var staticNames = new List<string?>(_staticParams.Count);
            var slotIndices = new int[_staticParams.Count];
            var slotAdded = new bool[_staticParams.Count];

            for (var i = 0; i < _staticParams.Count; i++)
            {
                var (param, expr) = _staticParams[i];
                var existing = FindDuplicate(staticNames, expr);
                if (existing >= 0)
                {
                    slotIndices[i] = existing;
                    slotAdded[i] = false;
                }
                else
                {
                    slotIndices[i] = _parameters.Count;
                    slotAdded[i] = true;
                    _parameters.Add(param);
                    staticNames.Add(expr);
                }
            }

            _resolvedNames = new List<string?>(staticNames);

            var sql = new StringBuilder();
            StringBuilder? segmentBuilder = _deferredFragments.Count > 0 ? new StringBuilder() : null;
            List<string>? sqlSegments = _deferredFragments.Count > 0 ? new(_deferredFragments.Count + 1) : null;
            var placeholders = new string[_parameters.Count];
            var staticCursor = 0;

            for (var i = 0; i < _segments.Count; i++)
            {
                var (kind, index) = _segments[i];
                switch (kind)
                {
                    case SegmentKind.Literal:
                        sql.Append(_literals[index]);
                        segmentBuilder?.Append(_literals[index]);
                        break;

                    case SegmentKind.Parameter:
                    {
                        var paramIndex = slotIndices[staticCursor];
                        var placeholder = TNamer.WritePlaceholder(
                            paramIndex,
                            _staticParams[staticCursor].Expression);
                        sql.Append(placeholder);
                        segmentBuilder?.Append(placeholder);
                        if (slotAdded[staticCursor])
                            placeholders[paramIndex] = placeholder;
                        staticCursor++;
                        break;
                    }

                    case SegmentKind.Fragment:
                    {
                        if (segmentBuilder is not null)
                        {
                            sqlSegments!.Add(segmentBuilder.ToString());
                            segmentBuilder.Clear();
                        }

                        var (fragment, expr) = _deferredFragments[index];
                        WriteFragmentSql(sql, _parameters, _resolvedNames!, fragment, expr);
                        break;
                    }
                }
            }

            if (_deferredFragments.Count == 0)
            {
                var sqlText = sql.ToString();
                _cache.TryAdd(_cacheKey, new StaticTemplate(sqlText, slotAdded, staticNames.ToArray(), placeholders));
                return sqlText;
            }
            else
            {
                sqlSegments!.Add(segmentBuilder!.ToString());
                _cache.TryAdd(_cacheKey, new DynamicTemplate(
                    sqlSegments.ToArray(),
                    slotAdded,
                    staticNames.ToArray()));
                return sql.ToString();
            }
        }

        public IReadOnlyList<string?> GetParameterNames() => _resolvedNames ?? [];

        public IReadOnlyList<string>? GetCachedPlaceholders() => null;
    }

    // --- StaticTemplate: cached template for statements with no fragments ---

    private sealed class StaticTemplate(
        string sqlText,
        bool[] slotAdded,
        string?[] parameterNames,
        string[] placeholders) : IBuilderTemplate
    {
        public IStatementBuilder CreateBuilder(List<TParameter> parameters) =>
            new StaticReplayBuilder(sqlText, slotAdded, parameterNames, placeholders, parameters);
    }

    private sealed class StaticReplayBuilder(
        string sqlText,
        bool[] slotAdded,
        string?[] parameterNames,
        string[] placeholders,
        List<TParameter> parameters) : IStatementBuilder
    {
        private int _cursor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string text) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendParameter(TParameter parameter, string? expression)
        {
            if (slotAdded[_cursor++])
                parameters.Add(parameter);
        }

        public void AppendFragment(SqlFragment<TParameter> fragment, string? expression) { }

        public string GetSql() => sqlText;

        public IReadOnlyList<string?> GetParameterNames() => parameterNames;

        public IReadOnlyList<string> GetCachedPlaceholders() => placeholders;
    }

    // --- DynamicTemplate: cached template for statements with fragment holes ---

    private sealed class DynamicTemplate(
        string[] sqlSegments,
        bool[] staticSlotAdded,
        string?[] staticParameterNames) : IBuilderTemplate
    {
        public IStatementBuilder CreateBuilder(List<TParameter> parameters) =>
            new DynamicReplayBuilder(
                sqlSegments,
                staticSlotAdded,
                staticParameterNames,
                parameters);
    }

    private sealed class DynamicReplayBuilder : IStatementBuilder
    {
        private readonly string[] _sqlSegments;
        private readonly bool[] _staticSlotAdded;
        private readonly string?[] _staticParameterNames;
        private readonly List<TParameter> _parameters;
        private int _staticCursor;
        private readonly List<(SqlFragment<TParameter> Fragment, string? Expression)> _deferredFragments = [];
        private List<string?>? _resolvedNames;

        public DynamicReplayBuilder(
            string[] sqlSegments,
            bool[] staticSlotAdded,
            string?[] staticParameterNames,
            List<TParameter> parameters)
        {
            _sqlSegments = sqlSegments;
            _staticSlotAdded = staticSlotAdded;
            _staticParameterNames = staticParameterNames;
            _parameters = parameters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string text) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendParameter(TParameter parameter, string? expression)
        {
            if (_staticSlotAdded[_staticCursor++])
                _parameters.Add(parameter);
        }

        public void AppendFragment(SqlFragment<TParameter> fragment, string? expression)
        {
            _deferredFragments.Add((fragment, expression));
        }

        public string GetSql()
        {
            _resolvedNames = new List<string?>(_staticParameterNames);
            var sql = new StringBuilder();

            for (var i = 0; i < _sqlSegments.Length; i++)
            {
                sql.Append(_sqlSegments[i]);
                if (i < _deferredFragments.Count)
                {
                    var (fragment, expr) = _deferredFragments[i];
                    WriteFragmentSql(sql, _parameters, _resolvedNames!, fragment, expr);
                }
            }

            return sql.ToString();
        }

        public IReadOnlyList<string?> GetParameterNames() =>
            (IReadOnlyList<string?>?)_resolvedNames ?? _staticParameterNames;

        public IReadOnlyList<string>? GetCachedPlaceholders() => null;
    }

    // --- Zero-parameter fast path ---

    private sealed class ZeroParamFreshBuilder : IStatementBuilder
    {
        private readonly (string File, int Line) _cacheKey;
        private string? _sql;

        public ZeroParamFreshBuilder((string File, int Line) cacheKey)
        {
            _cacheKey = cacheKey;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string text) => _sql = text;

        public void AppendParameter(TParameter parameter, string? expression) { }

        public void AppendFragment(SqlFragment<TParameter> fragment, string? expression) { }

        public string GetSql()
        {
            var sqlText = _sql ?? string.Empty;
            _cache.TryAdd(_cacheKey, new ZeroParamTemplate(sqlText));
            return sqlText;
        }

        public IReadOnlyList<string?> GetParameterNames() => [];

        public IReadOnlyList<string>? GetCachedPlaceholders() => null;
    }

    private sealed class ZeroParamTemplate : IBuilderTemplate
    {
        private readonly IStatementBuilder _sharedBuilder;

        public ZeroParamTemplate(string sql)
        {
            _sharedBuilder = new ZeroParamBuilder(sql);
        }

        public IStatementBuilder CreateBuilder(List<TParameter> parameters) => _sharedBuilder;
    }

    private sealed class ZeroParamBuilder(string sql) : IStatementBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral(string text) { }

        public void AppendParameter(TParameter parameter, string? expression) { }

        public void AppendFragment(SqlFragment<TParameter> fragment, string? expression) { }

        public string GetSql() => sql;

        public IReadOnlyList<string?> GetParameterNames() => [];

        public IReadOnlyList<string>? GetCachedPlaceholders() => null;
    }
}
