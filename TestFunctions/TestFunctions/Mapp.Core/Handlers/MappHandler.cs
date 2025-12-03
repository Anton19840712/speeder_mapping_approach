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
            Console.WriteLine("###########_InputFields_Start_###############");
            #region
            //foreach (var dependency in mappConfig.Dependencies)
            //{
            //    try
            //    {
            //        Console.WriteLine(
            //            $"\n****____NEXT_DEPENDENCY[{mappConfig.Dependencies.IndexOf(dependency)}]____****"
            //            );
            //        if (dependency.InputPath == null) continue;

            //        var result = inputData.GetValueStruct(dependency.InputPath);

            //        Console.WriteLine($"Source_Result - {dependency.GetFullInputPath()}: {result}");
            //        IEnumerable<object?>? results = result.GetValue()?.ToList();
            //        if (results == null) continue;
            //        foreach (var r in results) 
            //        {
            //            Console.WriteLine($"Prepared_Result - {dependency.GetFullInputPath()}[{results.ToList().IndexOf(r)}]: {r}");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Exception: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException?.Message}\n{ex.InnerException?.Message}");
            //    }
            //}
            #endregion
            Parallel.ForEach(mappConfig.Dependencies, dependency =>
            {
                try
                {
                    Console.WriteLine(
                        $"\n****____DEPENDENCY[{mappConfig.Dependencies.IndexOf(dependency)}]____****"
                        );
                    if (dependency.InputPath == null) return;

                    var result = inputData.GetValueStruct(dependency.InputPath);

               //     Console.WriteLine($"Source_Result_[{mappConfig.Dependencies.IndexOf(dependency)}] - {dependency.GetFullInputPath()}: {result}");
                    IEnumerable<object?>? results = result.GetValue()?.ToList();
                    if (results == null) return;
                    foreach (var r in results)
                    {
                        Console.WriteLine($"Prepared_Result_[{mappConfig.Dependencies.IndexOf(dependency)}] - {dependency.GetFullInputPath()}[{results.ToList().IndexOf(r)}]: {r}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException?.Message}\n{ex.InnerException?.Message}");
                }
            });
            Console.WriteLine("###########_InputFields_Finish_###############");
            return outputStruct;
        }
    }
}
