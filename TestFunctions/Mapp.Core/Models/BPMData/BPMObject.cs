namespace TestFunctions.Mapp.Core.Models.BPMData
{
    /// <summary>
    /// Представление объекта в BPM
    /// </summary>
    public class BPMObject : BPMDataElement
    {
        /// <summary>
        /// Свойства Объекта
        /// </summary>
        public List<BPMField> Fields { get; set; } = null!;
    }
}
