using System.Runtime.Serialization;

namespace JosephM.Xrm.Autonumber.Core
{
    [DataContract]
    public class RecordField : PicklistOption
    {
        public RecordField()
        {
        }

        public RecordField(string key, string value)
            : base(key, value)
        {
        }
    }
}