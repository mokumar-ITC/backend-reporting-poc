namespace BIEmbedSystem.Services
{
    public static class CsvHelperUtility
    {
        public static List<T> ReadCsv<T>(StreamReader reader) where T : new()
        {
            var result = new List<T>();

            var headers = reader.ReadLine()?.Split(',');
            if (headers == null)
                return result;

            while (!reader.EndOfStream)
            {
                var values = reader.ReadLine()?.Split(',');
                if (values == null) continue;

                var obj = new T();
                var props = typeof(T).GetProperties();

                for (int i = 0; i < headers.Length; i++)
                {
                    var prop = props.FirstOrDefault(p =>
                        p.Name.Equals(headers[i], StringComparison.OrdinalIgnoreCase));

                    if (prop != null)
                    {
                        var converted = Convert.ChangeType(values[i], prop.PropertyType);
                        prop.SetValue(obj, converted);
                    }
                }

                result.Add(obj);
            }

            return result;
        }
    }

}
