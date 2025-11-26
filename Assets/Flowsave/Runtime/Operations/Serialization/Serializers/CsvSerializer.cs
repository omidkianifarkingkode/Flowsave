using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Very simple CSV serializer.
    /// 
    /// Supported:
    /// - T is primitive / string → single cell
    /// - T is a POCO → header + 1 row
    /// - T is IEnumerable<POCO> / List<POCO> → header + N rows
    /// 
    /// Limitations:
    /// - Only public readable/writable properties are serialized.
    /// - No nested objects (only ToString() of property values).
    /// </summary>
    public sealed class CsvSerializer : ISerializer
    {
        public SerializationType Format => SerializationType.Csv;

        private static readonly char[] NewLineChars = new[] { '\r', '\n' };

        // ─────────────────────────────────────────────────────────
        //  Serialize
        // ─────────────────────────────────────────────────────────

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                if (data == null)
                    return Result<byte[]>.Failure("CSV: data is null.");

                var type = typeof(T);

                // Primitive / string → single-cell CSV
                if (IsPrimitiveOrString(type))
                {
                    var value = data?.ToString() ?? string.Empty;
                    var line = EscapeCell(value) + Environment.NewLine;
                    var bytes = Encoding.UTF8.GetBytes(line);
                    return Result<byte[]>.Success(bytes);
                }

                // IEnumerable<TItem> (but not string)
                if (IsEnumerableButNotString(type, out var itemType))
                {
                    return SerializeSequence(data as IEnumerable, itemType);
                }

                // POCO → header + single row
                return SerializeSingleObject(data, type);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"CSV serialize failed: {ex.Message}");
            }
        }

        private Result<byte[]> SerializeSequence(IEnumerable sequence, Type itemType)
        {
            if (sequence == null)
                return Result<byte[]>.Failure("CSV: sequence is null.");

            var props = GetSerializableProperties(itemType);
            if (props.Length == 0)
                return Result<byte[]>.Failure($"CSV: type '{itemType.Name}' has no serializable properties.");

            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",", props.Select(p => EscapeCell(p.Name))));

            // Rows
            foreach (var item in sequence)
            {
                var cells = props.Select(p =>
                {
                    var value = p.GetValue(item, null);
                    return EscapeCell(value?.ToString() ?? string.Empty);
                });

                sb.AppendLine(string.Join(",", cells));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Result<byte[]>.Success(bytes);
        }

        private Result<byte[]> SerializeSingleObject(object obj, Type type)
        {
            var props = GetSerializableProperties(type);
            if (props.Length == 0)
                return Result<byte[]>.Failure($"CSV: type '{type.Name}' has no serializable properties.");

            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",", props.Select(p => EscapeCell(p.Name))));

            // Single row
            var cells = props.Select(p =>
            {
                var value = p.GetValue(obj, null);
                return EscapeCell(value?.ToString() ?? string.Empty);
            });

            sb.AppendLine(string.Join(",", cells));

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Result<byte[]>.Success(bytes);
        }

        // ─────────────────────────────────────────────────────────
        //  Deserialize
        // ─────────────────────────────────────────────────────────

        public Result<T> Deserialize<T>(byte[] data)
        {
            if (data == null)
                return Result<T>.Failure("CSV: data is null.");

            try
            {
                var text = Encoding.UTF8.GetString(data);
                var type = typeof(T);

                // Primitive / string: take first cell of first row
                if (IsPrimitiveOrString(type))
                {
                    var value = ParsePrimitiveOrString<T>(text, type, out var result, out string error);
                    if (!value)
                        return Result<T>.Failure(error);
                    return Result<T>.Success(result);
                }

                // IEnumerable<TItem> (e.g., List<TItem>)
                if (IsGenericEnumerable(type, out var itemType))
                {
                    var listResult = DeserializeSequence(text, itemType, out var listObj, out string error);
                    if (!listResult)
                        return Result<T>.Failure(error);

                    return Result<T>.Success((T)listObj);
                }

                // POCO: single object
                var singleResult = DeserializeSingleObject<T>(text, type, out var obj, out var singleError);
                if (!singleResult)
                    return Result<T>.Failure(singleError);

                return Result<T>.Success(obj);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"CSV deserialize failed: {ex.Message}");
            }
        }

        private bool DeserializeSequence(string text, Type itemType, out object listObj, out string error)
        {
            listObj = null;
            error = null;

            var lines = SplitLines(text).ToList();
            if (lines.Count == 0)
            {
                listObj = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                return true;
            }

            // Parse header
            var headerCells = ParseCsvLine(lines[0]);
            var props = GetSerializableProperties(itemType);
            var propMap = MapHeaderToProperties(headerCells, props);

            if (propMap == null)
            {
                error = $"CSV: header does not match type '{itemType.Name}' properties.";
                return false;
            }

            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType);

            // Rows
            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var cells = ParseCsvLine(lines[i]);
                var instance = Activator.CreateInstance(itemType);

                for (int c = 0; c < propMap.Length && c < cells.Count; c++)
                {
                    var p = propMap[c];
                    if (p == null) continue;

                    var converted = ConvertString(cells[c], p.PropertyType);
                    p.SetValue(instance, converted);
                }

                list.Add(instance);
            }

            listObj = list;
            return true;
        }

        private bool DeserializeSingleObject<T>(string text, Type type, out T obj, out string error)
        {
            obj = default;
            error = null;

            var lines = SplitLines(text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count < 2)
            {
                error = "CSV: not enough rows to build a single object (need header + one row).";
                return false;
            }

            var headerCells = ParseCsvLine(lines[0]);
            var rowCells = ParseCsvLine(lines[1]);

            var props = GetSerializableProperties(type);
            var propMap = MapHeaderToProperties(headerCells, props);

            if (propMap == null)
            {
                error = $"CSV: header does not match type '{type.Name}' properties.";
                return false;
            }

            var instance = Activator.CreateInstance(type);

            for (int i = 0; i < propMap.Length && i < rowCells.Count; i++)
            {
                var p = propMap[i];
                if (p == null) continue;

                var converted = ConvertString(rowCells[i], p.PropertyType);
                p.SetValue(instance, converted);
            }

            obj = (T)instance;
            return true;
        }

        private bool ParsePrimitiveOrString<T>(string text, Type type, out T value, out string error)
        {
            value = default;
            error = null;

            var lines = SplitLines(text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0)
            {
                error = "CSV: no data to parse.";
                return false;
            }

            var cells = ParseCsvLine(lines[0]);
            if (cells.Count == 0)
            {
                error = "CSV: no cells in first row.";
                return false;
            }

            string cell = cells[0];

            try
            {
                if (type == typeof(string))
                {
                    value = (T)(object)cell;
                    return true;
                }

                object converted = Convert.ChangeType(cell, type);
                value = (T)converted;
                return true;
            }
            catch (Exception ex)
            {
                error = $"CSV: cannot convert '{cell}' to {type.Name}: {ex.Message}";
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Reflection helpers
        // ─────────────────────────────────────────────────────────

        private static bool IsPrimitiveOrString(Type t)
        {
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal);
        }

        private static bool IsEnumerableButNotString(Type t, out Type itemType)
        {
            itemType = null;

            if (t == typeof(string))
                return false;

            if (!typeof(IEnumerable).IsAssignableFrom(t))
                return false;

            if (t.IsArray)
            {
                itemType = t.GetElementType();
                return true;
            }

            if (IsGenericEnumerable(t, out itemType))
                return true;

            return false;
        }

        private static bool IsGenericEnumerable(Type t, out Type itemType)
        {
            itemType = null;

            if (t == typeof(string))
                return false;

            if (t.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition()))
            {
                itemType = t.GetGenericArguments()[0];
                return true;
            }

            var iface = t.GetInterfaces()
                         .FirstOrDefault(i => i.IsGenericType &&
                                              i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (iface != null)
            {
                itemType = iface.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static PropertyInfo[] GetSerializableProperties(Type t)
        {
            return t
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();
        }

        private static PropertyInfo[] MapHeaderToProperties(IList<string> headerCells, PropertyInfo[] props)
        {
            if (headerCells == null || headerCells.Count == 0)
                return null;

            var result = new PropertyInfo[headerCells.Count];

            for (int i = 0; i < headerCells.Count; i++)
            {
                var name = headerCells[i];
                var prop = props.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                result[i] = prop; // may be null if no match
            }

            return result;
        }

        private static object ConvertString(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return Activator.CreateInstance(targetType); // default value
                return null;
            }

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return Convert.ChangeType(value, underlying);
        }

        // ─────────────────────────────────────────────────────────
        //  CSV parsing/escaping helpers
        // ─────────────────────────────────────────────────────────

        private static string EscapeCell(string value)
        {
            if (value == null)
                return string.Empty;

            bool mustQuote =
                value.Contains(',') ||
                value.Contains('"') ||
                value.IndexOfAny(NewLineChars) >= 0;

            if (!mustQuote)
                return value;

            var escaped = value.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            using var reader = new System.IO.StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            if (line == null)
            {
                cells.Add(string.Empty);
                return cells;
            }

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escaped quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        cells.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            cells.Add(sb.ToString());
            return cells;
        }
    }
}
