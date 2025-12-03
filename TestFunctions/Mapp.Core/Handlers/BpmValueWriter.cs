using Newtonsoft.Json.Linq;
using TestFunctions.Mapp.Core.Models.BPMData;
using TestFunctions.Mapp.Core.Models.MappConfig;

namespace TestFunctions.Mapp.Core.Handlers
{
    /// <summary>
    /// Запись значений в BPM структуры
    /// </summary>
    public static class BpmValueWriter
    {
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
                            if (targetField.Value is BPMList<BPMField> targetArray)
                            {
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

        private static BPMField CloneBPMField(BPMField source, int newIndex)
        {
            var clone = new BPMField
            {
                Key = UpdateKeyIndex(source.Key, newIndex),
                TypeField = source.TypeField,
                Value = null
            };

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

            if (source.Elements != null && source.Elements.Any())
            {
                var elements = clone.Elements as List<BPMObject>;
                var template = source.Elements.First();
                elements!.Add(CloneBPMObject(template, 0));
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

            if (source.Elements != null && source.Elements.Any())
            {
                var elements = clone.Elements as List<BPMField>;
                var template = source.Elements.First();
                elements!.Add(new BPMField
                {
                    Key = UpdateKeyIndex(template.Key, 0),
                    TypeField = template.TypeField,
                    Value = null
                });
            }

            return clone;
        }

        private static string UpdateKeyIndex(string key, int newIndex)
        {
            var match = System.Text.RegularExpressions.Regex.Match(key, @"\[\d+\]");
            if (match.Success)
            {
                return key.Substring(0, match.Index) + $"[{newIndex}]" + key.Substring(match.Index + match.Length);
            }
            return key;
        }
    }
}
