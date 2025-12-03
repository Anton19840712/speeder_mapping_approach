namespace TestFunctions.Mapp.Core.Models.BPMData
{
    /// <summary>
    /// Представление массива в BPM
    /// </summary>
    public class BPMList<TElement> : BPMDataElement where TElement : class
    {
        /// <summary>
        /// Массив для BPMList
        /// </summary>
        public IEnumerable<TElement>? Elements { get; set; } 
    }
}
