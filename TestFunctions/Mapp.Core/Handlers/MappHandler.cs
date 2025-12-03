using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestFunctions.Mapp.Core.Models.BPMData;
using TestFunctions.Mapp.Core.Models.MappConfig;

namespace TestFunctions.Mapp.Core.Handlers
{
    public class MappHandler
    {
        public object? ConvertJsonToBPMData(string json)
        {
            if (json.StartsWith("{"))
            {
                JObject inputData = JObject.Parse(json);

                if (inputData.ConstructBPMObject() is BPMObject oStruct)
                {
                    Console.WriteLine($"ObjectResult:\n{JsonConvert.SerializeObject(oStruct)}");
                    return oStruct;
                }
            }
            if (json.StartsWith("["))
            {
                JArray inputData = JArray.Parse(json);

				if (inputData.ConstructBPMObject() is List<BPMField> aStruct)
				{
					Console.WriteLine($"ObjectResult:\n{JsonConvert.SerializeObject(aStruct)}");
					return aStruct;
				}
				if (inputData.ConstructBPMObject() is List<BPMObject> aoStruct)
				{
					Console.WriteLine($"ObjectResult:\n{JsonConvert.SerializeObject(aoStruct)}");
					return aoStruct;
				}
			}

            return null;
        }
        public string? ConvertBPMDataToJson(object bPMData)
        {
            JToken? result = bPMData.ConstructJToken();

            Console.WriteLine($"Out Result:\n{result}");

			return result.ToString();
        }
        public object? MappData(object inputData, object outputStruct, MappConfig mappConfig)
        {
            Console.WriteLine("###########_Mapping_Start_###############");

            // Объект синхронизации для потокобезопасной записи
            var syncLock = new object();

            Parallel.ForEach(mappConfig.Dependencies, dependency =>
            {
                try
                {
                    if (dependency.InputPath == null || dependency.OutputPath == null) return;

                    // Получаем значения с индексами из входных данных
                    var valuesWithIndices = inputData.GetValuesWithIndices(dependency.InputPath);

                    if (valuesWithIndices == null || !valuesWithIndices.Any()) return;

                    // Записываем каждое значение в выходную структуру
                    foreach (var (value, indices) in valuesWithIndices)
                    {
                        string indicesStr = indices.Any() ? $"[{string.Join(",", indices)}]" : "";
                        Console.WriteLine($"Mapping: {dependency.GetFullInputPath()}{indicesStr} = {value} -> {dependency.GetFullOutputPath()}");
                        outputStruct.SetValueStruct(dependency.OutputPath, value, indices, syncLock);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}\n{ex.StackTrace}");
                }
            });

            Console.WriteLine("###########_Mapping_Finish_###############");
            return outputStruct;
        }
    }
}
