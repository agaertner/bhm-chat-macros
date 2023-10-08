using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;

namespace Nekres.ChatMacros.Core.Services.Data {
    public class IgnorePropertyResolver : DefaultContractResolver {
        private readonly string[] _propertyNamesToIgnore;

        public IgnorePropertyResolver(params string[] propertyNamesToIgnore) {
            _propertyNamesToIgnore = propertyNamesToIgnore;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = instance => !_propertyNamesToIgnore.Contains(property.PropertyName);
            
            return property;
        }

        public static JsonSerializerSettings Settings(params string[] propertyNamesToIgnore) {
            return new JsonSerializerSettings {
                ContractResolver = new IgnorePropertyResolver(propertyNamesToIgnore),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }
    }

    public class IncludePropertyResolver : DefaultContractResolver {
        private readonly string[] _propertyNamesToInclude;

        public IncludePropertyResolver(params string[] propertyNamesToInclude) {
            _propertyNamesToInclude = propertyNamesToInclude;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = instance => _propertyNamesToInclude.Contains(property.PropertyName);
            
            return property;
        }

        public static JsonSerializerSettings Settings(params string[] propertyNamesToInclude) {
            return new JsonSerializerSettings {
                ContractResolver     = new IncludePropertyResolver(propertyNamesToInclude),
                Formatting           = Formatting.None,
                NullValueHandling    = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }
    }
}
