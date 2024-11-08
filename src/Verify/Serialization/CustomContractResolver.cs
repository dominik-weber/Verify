partial class CustomContractResolver(SerializationSettings settings) :
    DefaultContractResolver
{
    protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
    {
        var contract = base.CreateDictionaryContract(objectType);
        contract.DictionaryKeyResolver = (name, original) => ResolveDictionaryKey(contract, name, original);
        if (settings.SortDictionaries)
        {
            contract.OrderByKey = true;
        }

        contract.InterceptSerializeItem = (key, value) =>
        {
            if (key is string stringKey &&
                settings.TryGetScrubOrIgnoreByName(stringKey, out var scrubOrIgnore))
            {
                return ToInterceptResult(scrubOrIgnore.Value);
            }

            if (value is not null &&
                settings.TryGetScrubOrIgnoreByInstance(value, out scrubOrIgnore))
            {
                return ToInterceptResult(scrubOrIgnore.Value);
            }

            return InterceptResult.Default;
        };

        return contract;
    }

    static InterceptResult ToInterceptResult(ScrubOrIgnore scrubOrIgnore)
    {
        if (scrubOrIgnore == ScrubOrIgnore.Ignore)
        {
            return InterceptResult.Ignore;
        }

        return InterceptResult.Replace("{Scrubbed}");
    }

    static FieldInfo exceptionMessageField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);
        if (type.IsException())
        {
            var stackTrace = properties.Single(_ => _.PropertyName == "StackTrace");
            properties.Remove(stackTrace);
            properties.Add(stackTrace);
            properties.Insert(0,
                new(typeof(string), typeof(Exception))
                {
                    PropertyName = "Type",
                    ValueProvider = new TypeNameProvider(type),
                    Ignored = false,
                    Readable = true,
                    Writable = false
                });
        }

        if (VerifierSettings.sortPropertiesAlphabetically)
        {
            properties = properties
                // Still honor explicit ordering
                .OrderBy(_ => _.Order ?? -1)
                .ThenBy(_ => _.PropertyName, StringComparer.Ordinal)
                .ToList();
        }

        return properties;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
    {
        var property = base.CreateProperty(member, serialization);

        var valueProvider = property.ValueProvider;
        var memberType = property.PropertyType;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (memberType is null || valueProvider is null)
        {
            return property;
        }

        if (settings.TryGetScrubOrIgnore(member, out var scrubOrIgnore))
        {
            switch (scrubOrIgnore)
            {
                case ScrubOrIgnore.AlwaysInclude:
                    property.Ignored = false;
                    property.DefaultValueHandling = DefaultValueHandling.Include;
                    break;
                case ScrubOrIgnore.Ignore:
                    property.Ignored = true;
                    break;
                case ScrubOrIgnore.Scrub:
                    property.PropertyType = typeof(string);
                    property.ValueProvider = new ScrubbedProvider();
                    break;
            }

            return property;
        }

        if (member.Name == "Message")
        {
            if (member.DeclaringType == typeof(ArgumentException))
            {
                valueProvider = new DynamicValueProvider(exceptionMessageField);
            }
        }

        if (memberType.IsException())
        {
            property.TypeNameHandling = TypeNameHandling.All;
        }

        if (property.PropertyType == typeof(bool))
        {
            property.DefaultValueHandling = DefaultValueHandling.Include;
        }

        property.ValueProvider = new CustomValueProvider(
            valueProvider,
            memberType,
            settings.ShouldIgnoreException,
            VerifierSettings.GetMemberConverter(member),
            settings);

        return property;
    }

    protected override JsonArrayContract CreateArrayContract(Type objectType)
    {
        var contract = base.CreateArrayContract(objectType);

        contract.InterceptSerializeItem = item =>
        {
            if (item is not null &&
                settings.TryGetScrubOrIgnoreByInstance(item, out var scrubOrIgnore))
            {
                return ToInterceptResult(scrubOrIgnore.Value);
            }

            return InterceptResult.Default;
        };

        if (contract.CollectionItemType != null &&
            settings.TryGetEnumerableInterceptors(contract.CollectionItemType, out var order))
        {
            contract.InterceptSerializeItems = _ => order(_);
        }

        return contract;
    }
}