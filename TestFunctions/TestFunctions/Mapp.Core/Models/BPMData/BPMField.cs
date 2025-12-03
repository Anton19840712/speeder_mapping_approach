using Newtonsoft.Json.Linq;

namespace TestFunctions.Mapp.Core.Models.BPMData
{
    /// <summary>
    /// Представление свойства объекта в BPM
    /// </summary>
    public class BPMField : BPMDataElement
    {
        /// <summary>
        /// Значение Поля
        /// </summary>
        public object? Value { get; set; }
        /// <summary>   
        ///     Values from JTokenType (namespace Newtonsoft.Json.Linq):
        ///    "None" = 0,
        ///    "Object" = 1,
        ///    "Array" = 2,
        ///    "Constructor" = 3,
        ///    "Property" = 4,
        ///    "Comment" = 5,
        ///    "Integer" = 6,
        ///    "Float" = 7,
        ///    "String" = 8,
        ///    "Boolean" = 9,
        ///    "Undefined" = 10,
        ///    "Date" = 11,
        ///    "Raw" = 12,
        ///    "Bytes" = 13,
        ///    "Guid" = 14,
        ///    "Uri" = 15,
        ///    "TimeSpan" = 16
        /// </summary>
        public JTokenType TypeField { get; set; }
    }
}
