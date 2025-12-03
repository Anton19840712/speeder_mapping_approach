using Newtonsoft.Json;
using TestFunctions.Mapp.Core.Handlers;
using TestFunctions.Mapp.Core.Models.MappConfig;

namespace TestFunctions;

static class Program
{
	static void Main(string[]? args = null!)
	{
		try
		{
			string basePath = AppContext.BaseDirectory;

			string inputStruct = File.ReadAllText(Path.Combine(basePath, "TestData", "input.json"));
			string outStruct = File.ReadAllText(Path.Combine(basePath, "TestData", "output-template.json"));
			string mappConfigJson = File.ReadAllText(Path.Combine(basePath, "TestData", "mapping-config.json"));

			MappConfig? config = JsonConvert.DeserializeObject<MappConfig>(mappConfigJson);

			MappHandler handler = new();

			var inputData = handler.ConvertJsonToBPMData(inputStruct);
			var outData = handler.ConvertJsonToBPMData(outStruct);

			Console.WriteLine($"\nInput Struct:\n{JsonConvert.SerializeObject(inputData)}");
			Console.WriteLine($"\nOutput Struct:\n{JsonConvert.SerializeObject(outData)}");

			if (inputData != null && outData != null && config != null)
			{
				var outEnrichedData = handler.MappData(inputData, outData, config);
				if (outEnrichedData != null)
				{
					string? outEnrichedStruct = handler.ConvertBPMDataToJson(outEnrichedData);
					Console.WriteLine($"\nOutput Data:\n{JsonConvert.SerializeObject(outEnrichedStruct)}");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message + " " + ex.StackTrace);
			if (Console.ReadLine() == "r")
				Main();
		}
	}
}
