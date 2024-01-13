using Dapper;
using System.Data;

namespace MapMaven.Core.Tests.TestData
{
    public class TimeSpanSqlMapper : SqlMapper.TypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            var stringValue = (string)value;

            return TimeSpan.Parse(stringValue);
        }

        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.Value = value.ToString();
        }
    }
}
