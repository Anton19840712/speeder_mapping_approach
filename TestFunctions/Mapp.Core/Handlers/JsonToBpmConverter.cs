using Newtonsoft.Json.Linq;
using TestFunctions.Mapp.Core.Models.BPMData;

namespace TestFunctions.Mapp.Core.Handlers
{
    /// <summary>
    /// Конвертация JSON в BPM структуры
    /// </summary>
    public static class JsonToBpmConverter
    {
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
                        value = token.Value<long>();
                        return true;
                    case JTokenType.Float:
                        jTokenType = JTokenType.Float;
                        value = token.Value<double>();
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
    }
}
