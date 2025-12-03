using Newtonsoft.Json.Linq;
using TestFunctions.Mapp.Core.Models.BPMData;

namespace TestFunctions.Mapp.Core.Handlers
{
    /// <summary>
    /// Конвертация BPM структур в JSON
    /// </summary>
    public static class BpmToJsonConverter
    {
        public static JToken ConstructJToken(this object bPMData)
        {
            JToken? outToken = null!;

            switch (bPMData)
            {
                case BPMField field:
                    if (field.Value != null)
                        outToken = field.Value.ConstructJToken();
                    else if (field.TypeField == JTokenType.Null)
                        outToken = JValue.CreateNull();

                    return outToken!;
                case BPMObject @object:
                    outToken = new JObject();
                    foreach (BPMField field in @object.Fields)
                    {
                        if (field.Value != null)
                            ((JObject)outToken).Add(field.Key.GetCurrentKey(), field.ConstructJToken());
                        else if (field.TypeField == JTokenType.Null)
                            ((JObject)outToken).Add(field.Key.GetCurrentKey(), JValue.CreateNull());
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

        internal static string GetCurrentKey(this string path)
        {
            string currentPath = string.Empty;
            string[] pathElems = path.Split(".");

            if (pathElems.Length > 0)
                currentPath = pathElems.Last();

            // Обработка ключей со спецсимволами: ['key with spaces'] -> key with spaces
            if (currentPath.StartsWith("['") && currentPath.EndsWith("']"))
            {
                currentPath = currentPath.Substring(2, currentPath.Length - 4);
            }
            else if (currentPath.EndsWith("]"))
            {
                // Обработка массивов: key[0] -> key
                string[] arrayStruct = currentPath.Split("[");

                if (arrayStruct.Length > 0)
                    currentPath = arrayStruct.First();
            }
            return currentPath;
        }
    }
}
