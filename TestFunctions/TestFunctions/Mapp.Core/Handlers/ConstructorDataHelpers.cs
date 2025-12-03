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
    }
}
