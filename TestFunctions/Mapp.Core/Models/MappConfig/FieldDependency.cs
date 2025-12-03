namespace TestFunctions.Mapp.Core.Models.MappConfig
{
    public class FieldDependency
    {
        public List<Field>? InputPath { get => _inputPath?.OrderBy(e => e.Index).ToList(); set => _inputPath = value; }
        public List<Field>? OutputPath { get => _outputPath?.OrderBy(e => e.Index).ToList(); set => _outputPath = value; }
        private List<Field>? _inputPath;
        private List<Field>? _outputPath;

        public string GetFullInputPath()
        {
            string result = string.Empty;
            InputPath?.Select(e => e.Key).ToList().ForEach(e => result += result == string.Empty ? e : $".{e}");
            return result;
        }
        public string GetFullOutputPath() 
        {
            string result = string.Empty;
            OutputPath?.Select(e => e.Key).ToList().ForEach(e => result += result == string.Empty ? e : $".{e}");
            return result;
        }
    }
}
