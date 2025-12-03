using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestFunctions.Mapp.Core.Handlers;
using TestFunctions.Mapp.Core.Models.MappConfig;
using Xunit;

namespace TestFunctions.Tests;

public class MappingTests
{
    private readonly MappHandler _handler = new();

    public static IEnumerable<object[]> TestScenarios()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "TestData");
        var directories = Directory.GetDirectories(basePath);

        foreach (var dir in directories)
        {
            var scenarioName = Path.GetFileName(dir);
            yield return new object[] { scenarioName };
        }
    }

    [Theory]
    [MemberData(nameof(TestScenarios))]
    public void Mapping_ShouldProduceExpectedOutput(string scenarioName)
    {
        // Arrange
        var basePath = Path.Combine(AppContext.BaseDirectory, "TestData", scenarioName);

        var inputJson = File.ReadAllText(Path.Combine(basePath, "input.json"));
        var outputTemplateJson = File.ReadAllText(Path.Combine(basePath, "output-template.json"));
        var mappingConfigJson = File.ReadAllText(Path.Combine(basePath, "mapping-config.json"));
        var expectedJson = File.ReadAllText(Path.Combine(basePath, "expected.json"));

        var inputData = _handler.ConvertJsonToBPMData(inputJson);
        var outputStruct = _handler.ConvertJsonToBPMData(outputTemplateJson);
        var mappConfig = JsonConvert.DeserializeObject<MappConfig>(mappingConfigJson);

        Assert.NotNull(inputData);
        Assert.NotNull(outputStruct);
        Assert.NotNull(mappConfig);

        // Act
        var result = _handler.MappData(inputData, outputStruct, mappConfig);
        var resultJson = _handler.ConvertBPMDataToJson(result!);

        // Assert
        var expectedJToken = JToken.Parse(expectedJson);
        var resultJToken = JToken.Parse(resultJson!);

        Assert.True(
            JToken.DeepEquals(expectedJToken, resultJToken),
            $"Scenario '{scenarioName}' failed.\n\nExpected:\n{expectedJToken}\n\nActual:\n{resultJToken}"
        );
    }

    [Fact]
    public void SimpleFields_MapsCorrectly()
    {
        // Arrange
        var inputJson = @"{""id"": 42, ""name"": ""Test User"", ""email"": ""test@example.com"", ""isActive"": true, ""balance"": 123.45}";
        var outputTemplateJson = @"{""identifier"": null, ""title"": null, ""contact"": null, ""active"": null, ""amount"": null}";

        var inputData = _handler.ConvertJsonToBPMData(inputJson);
        var outputStruct = _handler.ConvertJsonToBPMData(outputTemplateJson);

        var mappConfig = new MappConfig
        {
            Dependencies = new List<FieldDependency>
            {
                CreateDependency("id", "identifier", JTokenType.Integer),
                CreateDependency("name", "title", JTokenType.String),
                CreateDependency("email", "contact", JTokenType.String),
                CreateDependency("isActive", "active", JTokenType.Boolean),
                CreateDependency("balance", "amount", JTokenType.Float)
            }
        };

        // Act
        var result = _handler.MappData(inputData!, outputStruct!, mappConfig);
        var resultJson = _handler.ConvertBPMDataToJson(result!);
        var resultJToken = JToken.Parse(resultJson!);

        // Assert
        Assert.Equal(42, resultJToken["identifier"]?.Value<int>());
        Assert.Equal("Test User", resultJToken["title"]?.Value<string>());
        Assert.Equal("test@example.com", resultJToken["contact"]?.Value<string>());
        Assert.Equal(true, resultJToken["active"]?.Value<bool>());
        Assert.True(Math.Abs((resultJToken["amount"]?.Value<float>() ?? 0) - 123.45f) < 0.01f);
    }

    [Fact]
    public void ConvertJsonToBPMData_WithValidObject_ReturnsNotNull()
    {
        // Arrange
        var json = @"{""test"": ""value""}";

        // Act
        var result = _handler.ConvertJsonToBPMData(json);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ConvertBPMDataToJson_WithBPMObject_ReturnsJson()
    {
        // Arrange
        var json = @"{""test"": ""value""}";
        var bpmData = _handler.ConvertJsonToBPMData(json);

        // Act
        var result = _handler.ConvertBPMDataToJson(bpmData!);

        // Assert
        Assert.NotNull(result);
        var jToken = JToken.Parse(result);
        Assert.Equal("value", jToken["test"]?.Value<string>());
    }

    private static FieldDependency CreateDependency(string inputKey, string outputKey, JTokenType tokenType)
    {
        return new FieldDependency
        {
            InputPath = new List<Field>
            {
                new Field { Key = "", TokenType = JTokenType.Object, Index = 0 },
                new Field { Key = inputKey, TokenType = tokenType, Index = 1 }
            },
            OutputPath = new List<Field>
            {
                new Field { Key = "", TokenType = JTokenType.Object, Index = 0 },
                new Field { Key = outputKey, TokenType = tokenType, Index = 1 }
            }
        };
    }
}
