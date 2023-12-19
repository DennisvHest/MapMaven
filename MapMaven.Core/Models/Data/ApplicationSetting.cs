namespace MapMaven.Core.Models.Data
{
    public class ApplicationSetting
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string? StringValue { get; set; }

        public T GetValue<T>()
        {
            var type = typeof(T);

            if (StringValue == null)
                return default;

            if (type == typeof(int))
                return (T)(object)int.Parse(StringValue);

            if (type == typeof(string))
                return (T)(object)StringValue;

            return default;
        }
    }
}
