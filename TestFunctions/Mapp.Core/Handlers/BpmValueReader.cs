using TestFunctions.Mapp.Core.Models.BPMData;
using TestFunctions.Mapp.Core.Models.MappConfig;

namespace TestFunctions.Mapp.Core.Handlers
{
    /// <summary>
    /// Чтение значений из BPM структур
    /// </summary>
    public static class BpmValueReader
    {
        public static object? GetValueStruct(this object? data, List<Field> path, Field? iterableField = null!)
        {
            if (data == null)
                return null;
            if (!path.Any())
                return null;

            iterableField = path.NextElement(iterableField);

            if (iterableField == null) throw new InvalidDataException("Проверьте \"MappConfig.Dependencies[n].InputPath\" или \"MappConfig.Dependencies[n].OutPath\"!");

            switch (data)
            {
                case BPMField field:
                    if (field.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return field;
                    }
                    if (field.Key.GetCurrentKey() == iterableField.Key)
                    {
                        return field.Value.AccessToUnknownChild(iterableField).GetValueStruct(path, iterableField);
                    }
                    break;
                case BPMObject @object:
                    if (@object.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return @object;
                    }
                    if (@object.Key.GetCurrentKey() == iterableField.Key)
                    {
                        return @object.Fields.GetValueStruct(path, iterableField);
                    }
                    break;
                case List<BPMField> listFields:
                    if (path.IsLastElement(iterableField))
                    {
                        return listFields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key).AccessToUnknownChild(iterableField);
                    }
                    return listFields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key)?.Value.AccessToUnknownChild(iterableField).GetValueStruct(path, iterableField);
                case BPMList<BPMField> listFields:
                    if (listFields.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return listFields;
                    }
                    return listFields.IterateArrayStruct(path, iterableField).ToList();
                case BPMList<BPMObject> listObjects:
                    if (listObjects.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return listObjects;
                    }
                    return listObjects.IterateArrayStruct(path, iterableField).ToList();
            }
            return data;
        }

        public static IEnumerable<object?> GetValue(this object? data)
        {
            return Traverse(data);

            static IEnumerable<object?> Traverse(object? current)
            {
                if (current is null)
                {
                    yield return null;
                    yield break;
                }

                switch (current)
                {
                    case BPMField field:
                        foreach (var item in Traverse(field.Value))
                            yield return item;
                        break;

                    case BPMObject obj:
                        foreach (var f in obj.Fields)
                            foreach (var item in Traverse(f.Value))
                                yield return item;
                        break;
                    case BPMList<BPMField> listFields:
                        foreach (var f in listFields.Elements)
                            foreach (var item in Traverse(f.Value))
                                yield return item;
                        break;
                    case IEnumerable<object> enumerable:
                        foreach (var item in enumerable)
                            foreach (var sub in Traverse(item))
                                yield return sub;
                        break;
                    default:
                        yield return current;
                        break;
                }
            }
        }

        /// <summary>
        /// Получает значения с индексами из входных данных (поддержка вложенных массивов)
        /// </summary>
        public static List<(object? Value, List<int> Indices)> GetValuesWithIndices(this object? data, List<Field> path)
        {
            var result = new List<(object? Value, List<int> Indices)>();
            CollectValuesWithIndices(data, path, null, new List<int>(), result);
            return result;
        }

        private static void CollectValuesWithIndices(object? data, List<Field> path, Field? iterableField, List<int> currentIndices, List<(object? Value, List<int> Indices)> results)
        {
            if (data == null || !path.Any()) return;

            iterableField = path.NextElement(iterableField);
            if (iterableField == null) return;

            switch (data)
            {
                case BPMField field:
                    if (field.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            var values = field.GetValue()?.ToList();
                            if (values != null)
                            {
                                foreach (var v in values)
                                    results.Add((v, new List<int>(currentIndices)));
                            }
                        }
                        else
                        {
                            CollectValuesWithIndices(field.Value, path, iterableField, currentIndices, results);
                        }
                    }
                    break;

                case BPMObject obj:
                    if (obj.Key == "" || obj.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            results.Add((obj, new List<int>(currentIndices)));
                        }
                        else
                        {
                            CollectValuesWithIndices(obj.Fields, path, iterableField, currentIndices, results);
                        }
                    }
                    break;

                case List<BPMField> listFields:
                    var targetField = listFields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key);
                    if (targetField != null)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            var values = targetField.GetValue()?.ToList();
                            if (values != null)
                            {
                                if (targetField.Value is BPMList<BPMField> primitiveList && primitiveList.Elements != null)
                                {
                                    int idx = 0;
                                    foreach (var elem in primitiveList.Elements)
                                    {
                                        var newIndices = new List<int>(currentIndices) { idx };
                                        results.Add((elem.Value, newIndices));
                                        idx++;
                                    }
                                }
                                else
                                {
                                    foreach (var v in values)
                                        results.Add((v, new List<int>(currentIndices)));
                                }
                            }
                        }
                        else
                        {
                            if (targetField.Value is BPMObject nestedObj)
                            {
                                CollectValuesWithIndices(nestedObj.Fields, path, iterableField, currentIndices, results);
                            }
                            else if (targetField.Value is BPMList<BPMObject> nestedListObj)
                            {
                                if (nestedListObj.Elements != null)
                                {
                                    int idx = 0;
                                    foreach (var elem in nestedListObj.Elements)
                                    {
                                        var newIndices = new List<int>(currentIndices) { idx };
                                        CollectValuesWithIndices(elem.Fields, path, iterableField, newIndices, results);
                                        idx++;
                                    }
                                }
                            }
                            else if (targetField.Value is BPMList<BPMField> nestedListField)
                            {
                                if (nestedListField.Elements != null)
                                {
                                    int idx = 0;
                                    foreach (var elem in nestedListField.Elements)
                                    {
                                        var newIndices = new List<int>(currentIndices) { idx };
                                        results.Add((elem.Value, newIndices));
                                        idx++;
                                    }
                                }
                            }
                            else
                            {
                                CollectValuesWithIndices(targetField.Value, path, iterableField, currentIndices, results);
                            }
                        }
                    }
                    break;

                case BPMList<BPMField> bpmListFields:
                    if (bpmListFields.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            if (bpmListFields.Elements != null)
                            {
                                int idx = 0;
                                foreach (var elem in bpmListFields.Elements)
                                {
                                    var newIndices = new List<int>(currentIndices) { idx };
                                    results.Add((elem.Value, newIndices));
                                    idx++;
                                }
                            }
                        }
                        else if (bpmListFields.Elements != null)
                        {
                            int idx = 0;
                            foreach (var elem in bpmListFields.Elements)
                            {
                                var newIndices = new List<int>(currentIndices) { idx };
                                CollectValuesWithIndices(elem.Value, path, iterableField, newIndices, results);
                                idx++;
                            }
                        }
                    }
                    break;

                case BPMList<BPMObject> bpmListObjects:
                    if (bpmListObjects.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            if (bpmListObjects.Elements != null)
                            {
                                int idx = 0;
                                foreach (var elem in bpmListObjects.Elements)
                                {
                                    var newIndices = new List<int>(currentIndices) { idx };
                                    results.Add((elem, newIndices));
                                    idx++;
                                }
                            }
                        }
                        else if (bpmListObjects.Elements != null)
                        {
                            int idx = 0;
                            foreach (var elem in bpmListObjects.Elements)
                            {
                                var newIndices = new List<int>(currentIndices) { idx };
                                CollectValuesWithIndices(elem.Fields, path, iterableField, newIndices, results);
                                idx++;
                            }
                        }
                    }
                    break;
            }
        }

        private static object? AccessToUnknownChild(this object? data, Field iterableField)
        {
            switch (data)
            {
                case BPMField field:
                    if (field.Key.GetCurrentKey() == iterableField.Key)
                        return field.Value;
                    return data;
                case BPMObject @object:
                    if (@object.Key.GetCurrentKey() == iterableField.Key)
                        return @object.Fields;
                    return data;
                case BPMList<BPMField> listFields:
                    return data;
                case BPMList<BPMObject> listObjects:
                    return data;
                default:
                    return data;
            }
        }

        private static IEnumerable<object?> IterateArrayStruct(this object? data, List<Field> path, Field iterableField)
        {
            if (!path.Any()) yield return null;

            switch (data)
            {
                case BPMList<BPMField> listFields:
                    if (listFields.Elements == null) yield return null;

                    foreach (BPMField bpmlField in listFields.Elements)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            yield return bpmlField;
                            continue;
                        }
                        yield return bpmlField.Value.GetValueStruct(path, iterableField);
                    }
                    break;
                case BPMList<BPMObject> listObjects:
                    if (listObjects.Elements == null) yield return null;

                    foreach (BPMObject bpmlField in listObjects.Elements)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            yield return bpmlField?.Fields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key)?.Value;
                            continue;
                        }
                        yield return bpmlField?.Fields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key)?.Value.AccessToUnknownChild(iterableField).GetValueStruct(path, iterableField);
                    }
                    break;
                default:
                    yield return data;
                    break;
            }
        }

        internal static bool IsFirstElement<T>(this IEnumerable<T> source, T item)
        {
            return source.ToList().IndexOf(item) == 0;
        }

        internal static bool IsLastElement<T>(this IEnumerable<T> source, T item)
        {
            return (source.Count() - 1) == source.ToList().IndexOf(item);
        }

        internal static T? NextElement<T>(this IEnumerable<T> source, T? item)
        {
            if (item == null) return source.ToList().FirstOrDefault();

            if (!source.IsLastElement(item))
                return source.ToList()[source.ToList().IndexOf(item) + 1];

            return default(T);
        }
    }
}
