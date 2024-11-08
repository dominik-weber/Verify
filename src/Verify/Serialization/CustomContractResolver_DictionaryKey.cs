partial class CustomContractResolver
{
    string ResolveDictionaryKey(JsonDictionaryContract contract, JsonWriter writer, string name, object original)
    {
        var counter = Counter.Current;
        var keyType = contract.DictionaryKeyType;

#if NET6_0_OR_GREATER

        if (original is Date date)
        {
            if (settings.TryConvert(counter, date, out var result))
            {
                return result;
            }
        }

        if (original is Time time)
        {
            if (settings.TryConvert(counter, time, out var result))
            {
                return result;
            }
        }

#endif

        if (original is Guid guid)
        {
            if (settings.TryConvert(counter, guid, out var result))
            {
                return result;
            }
        }

        if (original is string stringValue)
        {
            if (settings.TryParseConvert(counter, stringValue.AsSpan(), out var result))
            {
                return result;
            }
        }

        if (original is DateTime dateTime)
        {
            if (settings.TryConvert(counter, dateTime, out var result))
            {
                return result;
            }
        }

        if (original is DateTimeOffset dateTimeOffset)
        {
            if (settings.TryConvert(counter, dateTimeOffset, out var result))
            {
                return result;
            }
        }

        if (keyType == typeof(Type))
        {
            var type = Type.GetType(name);
            if (type is null)
            {
                throw new($"Could not load type `{name}`.");
            }

            return type.SimpleName();
        }

        var verifyJsonWriter = (VerifyJsonWriter) writer;
        return ApplyScrubbers.ApplyForPropertyValue(name.AsSpan(), verifyJsonWriter.settings, counter).ToString();
    }
}