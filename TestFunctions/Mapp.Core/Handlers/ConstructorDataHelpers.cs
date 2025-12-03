using Newtonsoft.Json.Linq;
using OneOf;
using System.IO;
using TestFunctions.Mapp.Core.Models.BPMData;
using TestFunctions.Mapp.Core.Models.MappConfig;

namespace TestFunctions.Mapp.Core.Handlers
{
    public static class ConstructorDataHelpers
    {
        #region JsonToBPMData
        #region Public
        public static object ConstructBPMObject(this JToken jToken)
        {
            object? outStruct = null!;

            if (jToken.TryGetOutStruct(out object? oStruct))
                outStruct = oStruct;

            if (outStruct is BPMField fieldStruct)
            {
                fieldStruct.Key = jToken.Path;
                if (jToken.TryGetValue(out JTokenType jTokenType, out object? value))
                {
                    fieldStruct.TypeField = jTokenType;
                    fieldStruct.Value = value;
                }
            }

            if (outStruct is BPMObject objectStruct)
            {
                objectStruct.Key = jToken.Path;
                objectStruct.Fields = new();

                foreach (JToken token in jToken.Children())
                {
                    if (token.TryGetValue(out JTokenType jTokenType, out object? value))
                    {
                        Console.WriteLine($"{token.Path}: {value}\nJTokenType: {jTokenType}\n\n");
                        objectStruct.Fields.Add(new()
                        {
                            Key = token.Path,
                            TypeField = jTokenType,
                            Value = value
                        });
                    }
                }
            }

            if (outStruct is BPMList<BPMObject> listObjectStruct)
            {
                listObjectStruct.Key = jToken.Path;
                listObjectStruct.Elements = new List<BPMObject>();

                foreach (JToken token in jToken.Children())
                {
                    if (token.ConstructBPMObject() is BPMObject listElem)
                    {
                        ((List<BPMObject>)listObjectStruct.Elements).Add(listElem);
                    }
                    else
                    {
                        Console.WriteLine($"{token.Path} not Construct");
                    }
                }
            }

            if (outStruct is BPMList<BPMField> listFieldStruct)
            {
                listFieldStruct.Key = jToken.Path;
                listFieldStruct.Elements = new List<BPMField>();

                foreach (JToken token in jToken.Children())
                {
                    if (token.ConstructBPMObject() is BPMField listElem)
                    {
                        ((List<BPMField>)listFieldStruct.Elements).Add(listElem);
                    }
                    else
                    {
                        Console.WriteLine($"{token.Path} not Construct");
                    }
                }
            }
            return outStruct!;
        }
        #endregion
        #region Private
        private static bool TryGetValue(this JToken token, out JTokenType jTokenType, out object? value)
        {
            value = null!;
            jTokenType = JTokenType.None;
            try
            {
                switch (token.Type)
                {
                    case JTokenType.Property:

                        if (token.First != null)
                            if (token.First.TryGetValue(out JTokenType tokenType, out object? v))
                            {
                                jTokenType = tokenType;
                                value = v;
                                return true;
                            }

                        return false;
                    case JTokenType.String:
                        jTokenType = JTokenType.String;
                        value = token.Value<string>();
                        return true;
                    case JTokenType.Integer:
                        jTokenType = JTokenType.Integer;
                        value = token.Value<int>();
                        return true;
                    case JTokenType.Float:
                        jTokenType = JTokenType.Float;
                        value = token.Value<float>();
                        return true;
                    case JTokenType.Boolean:
                        jTokenType = JTokenType.Boolean;
                        value = token.Value<bool>();
                        return true;
                    case JTokenType.Guid:
                        jTokenType = JTokenType.Guid;
                        value = token.Value<Guid>();
                        return true;
                    case JTokenType.Date:
                        jTokenType = JTokenType.Date;
                        value = token.Value<DateTime>();
                        return true;
                    case JTokenType.TimeSpan:
                        jTokenType = JTokenType.TimeSpan;
                        value = token.Value<long>();
                        return true;
                    case JTokenType.Object:
                        jTokenType = JTokenType.Object;
                        value = token.ConstructBPMObject();
                        return true;
                    case JTokenType.Array:
                        jTokenType = JTokenType.Array;
                        value = token.ConstructBPMObject();
                        return true;
                    case JTokenType.Null:
                        jTokenType = JTokenType.Null;
                        return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }
        private static bool TryGetOutStruct(this JToken jToken, out object? outStruct)
        {
            outStruct = null;
            try
            {
                switch (jToken.Type)
                {
                    case JTokenType.String:
                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.Boolean:
                    case JTokenType.Guid:
                    case JTokenType.Date:
                    case JTokenType.TimeSpan:
                        outStruct = new BPMField()
                        {
                            Key = jToken.Path
                        };
                        return true;
                    case JTokenType.Object:
                        outStruct = new BPMObject()
                        {
                            Key = jToken.Path
                        };
                        return true;
                    case JTokenType.Array:
                        if (jToken.Children().Count() > 0)
                        {

                            if (jToken.Children().First().TryGetOutStruct(out object? oStruct))
                            {
                                if (oStruct is BPMField)
                                {
                                    outStruct = new BPMList<BPMField>()
                                    {
                                        Key = jToken.Path
                                    };
                                    return true;
                                }
                                if (oStruct is BPMObject)
                                {
                                    outStruct = new BPMList<BPMObject>()
                                    {
                                        Key = jToken.Path
                                    };
                                    return true;
                                }
                            }
                        }
                        return false;
                    case JTokenType.Null:
                        return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        #endregion
        #endregion

        #region BPMDataToJson
        #region Public
        public static JToken ConstructJToken(this object bPMData)
        {
            JToken? outToken = null!;

            switch (bPMData)
            {
                case BPMField field:
                    if (field.Value != null)
                        outToken = field.Value.ConstructJToken();

                    return outToken!;
                case BPMObject @object:
                    outToken = new JObject();
                    foreach (BPMField field in @object.Fields)
                    {
                        if (field.Value != null)
                            ((JObject)outToken).Add(field.Key.GetCurrentKey(), field.ConstructJToken());
                    }
                    return outToken;
                case BPMList<BPMField> fieldList:
                    outToken = new JArray();
                    if (fieldList.Elements != null)
                    {
                        foreach (BPMField field in fieldList.Elements)
                        {
                            if (field.Value != null)
                                ((JArray)outToken).Add(field.ConstructJToken());
                        }
                    }
                    return outToken;
                case BPMList<BPMObject> objectList:
                    outToken = new JArray();
                    if (objectList.Elements != null)
                        foreach (BPMObject field in objectList.Elements)
                        {
                            if (field != null)
                                ((JArray)outToken).Add(field.ConstructJToken());
                        }
                    return outToken;
                default:
                    outToken = new JValue(bPMData);
                    return outToken;
            }
        }
        #endregion
        #region Private
        private static string GetCurrentKey(this string path)
        {
            string currentPath = string.Empty;
            string[] pathElems = path.Split(".");

            if (pathElems.Length > 0)
                currentPath = pathElems.Last();

            if (currentPath.EndsWith("]"))
            {
                string[] arrayStruct = currentPath.Split("[");

                if (arrayStruct.Length > 0)
                    currentPath = arrayStruct.First();
            }
            return currentPath;
        }

        #endregion
        #endregion

        #region Mapper
        #region Public
        public static object? GetValueStruct(this object? data, List<Field> path, Field? iterableField = null!)
        {
            if(data == null) 
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
                    break;
                case BPMList<BPMField> listFields:
                    if (listFields.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return listFields;
                    }
                    return listFields.IterateArrayStruct(path, iterableField).ToList();
                    break;
                case BPMList<BPMObject> listObjects:
                    if (listObjects.Key.GetCurrentKey() == iterableField.Key && path.IsLastElement(iterableField))
                    {
                        return listObjects;
                    }
                    return listObjects.IterateArrayStruct(path, iterableField).ToList();
                    break;
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
        //public static IEnumerable<object?>? GetValue(this object? data)
        //{
        //    switch (data)
        //    {
        //        case BPMField field:
        //            yield return field.Value;
        //            break;
        //        case BPMObject @object:
        //            foreach (BPMField f in @object.Fields)
        //                yield return f.Value;
        //            break;
        //        case BPMList<BPMField> listFields:
        //            foreach (BPMField f in listFields.Elements)
        //                yield return f.Value;
        //            break;
        //        case IEnumerable<BPMField?> listFields:                   
        //            foreach(BPMField? field in listFields)
        //                yield return field;
        //            break;
        //        case IEnumerable<object?> listFields:
        //            int i = 0;
        //            int opt_i = 0;
        //            foreach (object? field in listFields)
        //            {
        //                if (field is IEnumerable<object?> enumerateStruct)
        //                {
        //                    Console.WriteLine($"ValueEnumerateIndex: {opt_i++}");
        //                    yield return enumerateStruct.GetValue()?.ToList();
        //                    continue;
        //                }
        //                Console.WriteLine($"ValueIndex: {i++}");
        //                yield return field;
        //            }
        //            break;
        //        default:
        //            yield return data;
        //            break;
        //    }
        //}
        #endregion
        #region Private		

        private static object? AccessToUnknownChild(this object? data, Field iterableField)
        {
            switch (data)
            {
                case BPMField field:

                    if (field.Key.GetCurrentKey() == iterableField.Key)
                        return field.Value;

                    return data;
                case BPMObject @object:

                    if(@object.Key.GetCurrentKey() == iterableField.Key)
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
        private static bool IsFirstElement<T>(this IEnumerable<T> source, T item)
        {
            return source.ToList().IndexOf(item) == 0;
        }
        private static bool IsLastElement<T>(this IEnumerable<T> source, T item)
        {
            return (source.Count() - 1) == source.ToList().IndexOf(item);
        }
        private static T? NextElement<T>(this IEnumerable<T> source, T? item)
        {
            if (item == null) return source.ToList().FirstOrDefault();

            if (!source.IsLastElement(item))
                return source.ToList()[source.ToList().IndexOf(item) + 1];

            return default(T);
        }
        #endregion
        #endregion

        #region Setter
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
                                // Для массива примитивов на конечном уровне — добавляем индексы
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

        /// <summary>
        /// Записывает значение в структуру по указанному пути (поддержка вложенных массивов)
        /// </summary>
        public static void SetValueStruct(this object? data, List<Field> path, object? value, List<int> indices, object syncLock)
        {
            if (data == null || !path.Any()) return;

            lock (syncLock)
            {
                SetValueRecursive(data, path, null, value, indices, 0);
            }
        }

        private static void SetValueRecursive(object? data, List<Field> path, Field? iterableField, object? value, List<int> indices, int indexLevel)
        {
            if (data == null || !path.Any()) return;

            iterableField = path.NextElement(iterableField);
            if (iterableField == null) return;

            // Получаем текущий индекс для этого уровня массива
            int GetCurrentIndex() => indexLevel < indices.Count ? indices[indexLevel] : 0;

            switch (data)
            {
                case BPMObject obj:
                    if (obj.Key == "" || obj.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (!path.IsLastElement(iterableField))
                        {
                            SetValueRecursive(obj.Fields, path, iterableField, value, indices, indexLevel);
                        }
                    }
                    break;

                case List<BPMField> listFields:
                    var targetField = listFields.FirstOrDefault(e => e.Key.GetCurrentKey() == iterableField.Key);
                    if (targetField != null)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            // Проверяем, является ли целевое поле массивом (по TypeField или по Value)
                            if (targetField.Value is BPMList<BPMField> targetArray)
                            {
                                // Записываем в существующий массив примитивов по индексу
                                int idx = GetCurrentIndex();
                                EnsureFieldArraySize(targetArray, idx);
                                var elements = targetArray.Elements as List<BPMField>;
                                if (elements != null && idx < elements.Count)
                                {
                                    elements[idx].Value = value;
                                }
                            }
                            else if (targetField.TypeField == JTokenType.Array)
                            {
                                // Создаём массив если TypeField указывает на массив, но Value ещё null
                                var newArray = new BPMList<BPMField>
                                {
                                    Key = targetField.Key,
                                    Elements = new List<BPMField>()
                                };
                                targetField.Value = newArray;

                                int idx = GetCurrentIndex();
                                EnsureFieldArraySize(newArray, idx);
                                var elements = newArray.Elements as List<BPMField>;
                                if (elements != null && idx < elements.Count)
                                {
                                    elements[idx].Value = value;
                                }
                            }
                            else
                            {
                                // Записываем простое значение
                                targetField.Value = value;
                            }
                        }
                        else
                        {
                            if (targetField.Value is BPMObject nestedObj)
                            {
                                SetValueRecursive(nestedObj.Fields, path, iterableField, value, indices, indexLevel);
                            }
                            else if (targetField.Value is BPMList<BPMObject> nestedListObj)
                            {
                                int idx = GetCurrentIndex();
                                EnsureArraySize(nestedListObj, idx);
                                var elements = nestedListObj.Elements as List<BPMObject>;
                                if (elements != null && idx < elements.Count)
                                {
                                    SetValueRecursive(elements[idx].Fields, path, iterableField, value, indices, indexLevel + 1);
                                }
                            }
                            else if (targetField.Value is BPMList<BPMField> nestedListField)
                            {
                                int idx = GetCurrentIndex();
                                EnsureFieldArraySize(nestedListField, idx);
                                var elements = nestedListField.Elements as List<BPMField>;
                                if (elements != null && idx < elements.Count)
                                {
                                    elements[idx].Value = value;
                                }
                            }
                            else
                            {
                                SetValueRecursive(targetField.Value, path, iterableField, value, indices, indexLevel);
                            }
                        }
                    }
                    break;

                case BPMList<BPMObject> bpmListObjects:
                    if (bpmListObjects.Key.GetCurrentKey() == iterableField.Key)
                    {
                        int idx = GetCurrentIndex();
                        EnsureArraySize(bpmListObjects, idx);

                        var elements = bpmListObjects.Elements as List<BPMObject>;
                        if (elements != null && idx < elements.Count)
                        {
                            SetValueRecursive(elements[idx].Fields, path, iterableField, value, indices, indexLevel + 1);
                        }
                    }
                    break;

                case BPMList<BPMField> bpmListFields:
                    if (bpmListFields.Key.GetCurrentKey() == iterableField.Key)
                    {
                        if (path.IsLastElement(iterableField))
                        {
                            int idx = GetCurrentIndex();
                            EnsureFieldArraySize(bpmListFields, idx);
                            var elements = bpmListFields.Elements as List<BPMField>;
                            if (elements != null && idx < elements.Count)
                            {
                                elements[idx].Value = value;
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Расширяет массив объектов до нужного размера, клонируя шаблон
        /// </summary>
        private static void EnsureArraySize(BPMList<BPMObject> list, int requiredIndex)
        {
            if (list.Elements == null)
            {
                list.Elements = new List<BPMObject>();
            }

            var elements = list.Elements as List<BPMObject>;
            if (elements == null)
            {
                elements = list.Elements.ToList();
                list.Elements = elements;
            }

            // Получаем шаблон для клонирования (первый элемент)
            BPMObject? template = elements.FirstOrDefault();

            while (elements.Count <= requiredIndex)
            {
                if (template != null)
                {
                    elements.Add(CloneBPMObject(template, elements.Count));
                }
                else
                {
                    elements.Add(new BPMObject { Key = $"{list.Key}[{elements.Count}]", Fields = new List<BPMField>() });
                }
            }
        }

        /// <summary>
        /// Расширяет массив полей до нужного размера
        /// </summary>
        private static void EnsureFieldArraySize(BPMList<BPMField> list, int requiredIndex)
        {
            if (list.Elements == null)
            {
                list.Elements = new List<BPMField>();
            }

            var elements = list.Elements as List<BPMField>;
            if (elements == null)
            {
                elements = list.Elements.ToList();
                list.Elements = elements;
            }

            BPMField? template = elements.FirstOrDefault();

            while (elements.Count <= requiredIndex)
            {
                if (template != null)
                {
                    elements.Add(new BPMField
                    {
                        Key = template.Key.Replace("[0]", $"[{elements.Count}]"),
                        TypeField = template.TypeField,
                        Value = null
                    });
                }
                else
                {
                    elements.Add(new BPMField { Key = $"{list.Key}[{elements.Count}]", Value = null });
                }
            }
        }

        /// <summary>
        /// Глубокое клонирование BPMObject с обновлением индексов в ключах
        /// </summary>
        private static BPMObject CloneBPMObject(BPMObject source, int newIndex)
        {
            var clone = new BPMObject
            {
                Key = UpdateKeyIndex(source.Key, newIndex),
                Fields = new List<BPMField>()
            };

            foreach (var field in source.Fields)
            {
                clone.Fields.Add(CloneBPMField(field, newIndex));
            }

            return clone;
        }

        /// <summary>
        /// Глубокое клонирование BPMField с обновлением индексов
        /// </summary>
        private static BPMField CloneBPMField(BPMField source, int newIndex)
        {
            var clone = new BPMField
            {
                Key = UpdateKeyIndex(source.Key, newIndex),
                TypeField = source.TypeField,
                Value = null // Значение сбрасываем
            };

            // Клонируем вложенные структуры
            if (source.Value is BPMObject nestedObj)
            {
                clone.Value = CloneBPMObject(nestedObj, newIndex);
            }
            else if (source.Value is BPMList<BPMObject> nestedListObj)
            {
                clone.Value = CloneBPMListObject(nestedListObj, newIndex);
            }
            else if (source.Value is BPMList<BPMField> nestedListField)
            {
                clone.Value = CloneBPMListField(nestedListField, newIndex);
            }

            return clone;
        }

        private static BPMList<BPMObject> CloneBPMListObject(BPMList<BPMObject> source, int parentIndex)
        {
            var clone = new BPMList<BPMObject>
            {
                Key = UpdateKeyIndex(source.Key, parentIndex),
                Elements = new List<BPMObject>()
            };

            if (source.Elements != null)
            {
                var elements = clone.Elements as List<BPMObject>;
                int idx = 0;
                foreach (var elem in source.Elements)
                {
                    elements!.Add(CloneBPMObject(elem, idx++));
                }
            }

            return clone;
        }

        private static BPMList<BPMField> CloneBPMListField(BPMList<BPMField> source, int parentIndex)
        {
            var clone = new BPMList<BPMField>
            {
                Key = UpdateKeyIndex(source.Key, parentIndex),
                Elements = new List<BPMField>()
            };

            if (source.Elements != null)
            {
                var elements = clone.Elements as List<BPMField>;
                int idx = 0;
                foreach (var elem in source.Elements)
                {
                    elements!.Add(new BPMField
                    {
                        Key = UpdateKeyIndex(elem.Key, idx++),
                        TypeField = elem.TypeField,
                        Value = null
                    });
                }
            }

            return clone;
        }

        /// <summary>
        /// Обновляет первый индекс массива в ключе
        /// </summary>
        private static string UpdateKeyIndex(string key, int newIndex)
        {
            // Заменяем первый [N] на [newIndex]
            var match = System.Text.RegularExpressions.Regex.Match(key, @"\[\d+\]");
            if (match.Success)
            {
                return key.Substring(0, match.Index) + $"[{newIndex}]" + key.Substring(match.Index + match.Length);
            }
            return key;
        }
        #endregion
    }
}
