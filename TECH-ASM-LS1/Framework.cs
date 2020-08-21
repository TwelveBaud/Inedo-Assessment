using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Xml.Linq;
using EntMgr;

namespace TECH_ASM_LS1
{
    [SecurityCritical]
    internal static class EntityExtensions
    {
        internal static IDictionary<IEntity, Func<IEntityManager>> managers;

        static EntityExtensions()
        {
            managers = new Dictionary<IEntity, Func<IEntityManager>>();
        }

        public static IEntityManager GetManager(this IEntity entity)
        {
            var mgrBuilder = managers[entity];
            var manager = mgrBuilder();
            manager.Load(entity);
            return manager;
        }
    }

    [SecurityCritical]
    public class Framework : MarshalByRefObject
    {
        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        static Framework()
        {
            // Try to convince upversion .NET to permit stronger encryption, if possible
            // (.NET 4.5.2 doesn't support TLS 1.2; when run on upversion CLRs, these
            // are the magic incantations to get out of compatibility mode and potentially
            // use it anyway. Not needed for the HTTP testing URL, but potentially needed for
            // HTTPS URLs in staging or production.
            try
            {
                var AppContext_SetSwitch = Type.GetType("System.AppContext").GetMethod("SetSwitch");
                AppContext_SetSwitch.Invoke(null,
                    new object[] { "Switch.System.Net.DontEnableSchUseStrongCrypto", false });
                AppContext_SetSwitch.Invoke(null,
                    new object[] { "Switch.System.Net.DontEnableSystemDefaultTlsVersions", false });
                AppContext_SetSwitch.Invoke(null,
                    new object[] { "Switch.System.ServiceModel.DontEnableSystemDefaultTlsVersions", false });
            }
            catch (Exception)
            {
            }
        }

        private HttpClient httpClient;

        private IDictionary<Guid, IEntity> entities;
        private IDictionary<string, Assembly> assemblies;
        private IDictionary<string, Type> entityTypes;
        private IDictionary<string, Type> managerTypes;

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        public Framework()
        {
            httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://44.inedo.com/FoxworksEntMgr/")
            };
            entities = new LazyDictionary<Guid, IEntity>(ResolveEntity);
            entityTypes = new LazyDictionary<string, Type>(ResolveType);
            managerTypes = new LazyDictionary<string, Type>(ResolveType);
            assemblies = new LazyDictionary<string, Assembly>(ResolveAssembly);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
             {
                 var bareName = e.Name.Substring(0, e.Name.IndexOf(','));
                 return assemblies[bareName];
             };
        }

        [WebPermission(SecurityAction.Assert, ConnectPattern = "http://44\\.inedo\\.com/FoxworksEntMgr/.+")]
        public IEnumerable<Tuple<Guid, string>> GetCustomers()
        {
            var customerListAsString = httpClient.GetStringAsync(
                new Uri("SearchCustomers.ashx", UriKind.Relative)).Result;
            var customerListAsXml = XDocument.Parse(customerListAsString);
            return customerListAsXml.Descendants("sresult").Select(elem =>
                new Tuple<Guid, string>
                    (Guid.Parse(elem.Attribute("id").Value), elem.Attribute("company").Value))
                .ToArray();
        }

        public string GetEDFForEntity(Guid entityId)
        {
            var outputBuffer = new StringWriter();
            var entity = entities[entityId];
            var entityManager = entity.GetManager();
            entityManager.WriteEDFStream(outputBuffer);
            entityManager.Unload();
            return outputBuffer.ToString();
        }

        public IEnumerable<Guid> GetRelatedEntities(Guid entityId, string relationType)
        {
            var entity = entities[entityId];
            var entityManager = entity.GetManager();
            var relationshipId = entityManager.GetRelationship(relationType);
            entityManager.Unload();
            var relationship = entities[relationshipId];
            if (relationship is IEntitySingletonRelationship singletonRelationship)
            {
                // Relationship ID points to relatee, and shadows Entity ID which points to self.
                // DO NOT optimize to rel.ID
                return new Guid[] { singletonRelationship.ID };
            }
            else if (relationship is IEntityMultiplexRelationship multiplexRelationship)
            {
                return multiplexRelationship.IDs;
            }
            else if (relationship is null)
            {
                return new Guid[] { };
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        [WebPermission(SecurityAction.Assert, ConnectPattern = "http://44\\.inedo\\.com/FoxworksEntMgr/.+")]
        private IEntity ResolveEntity(Guid entityId)
        {
            if (entityId == default) return null;
            var entityAsString = httpClient.GetStringAsync(
                new Uri("GetEntity.ashx?id=" + entityId.ToString(), UriKind.Relative)).Result;
            // "enc " attribute has a space in the name; this can't be parsed as-is
            entityAsString = entityAsString.Replace("enc =\"", "enc=\"");
            var entityAsXml = XDocument.Parse(entityAsString);
            if (entityAsXml.Root.Attribute("enc")?.Value != "base64")
                throw new ArgumentException(null, nameof(entityId));
            var entityDataAsBytes = System.Convert.FromBase64String(entityAsXml.Root.Value);

            // entityDataAsBytes has a series of Pascal strings. The first four are entity type 
            // name, entity assembly name, entity manager type name, and entity manager 
            // assembly name. The remainer are key/value pairs of fields in that entity.

            int ptr = 0;
            string GetStringFromEntityData()
            {
                // BitConverter assumes platform endianness; Entity Manager is big-endian.
                byte[] lengthAsBytes =
                    new byte[] { entityDataAsBytes[ptr + 1], entityDataAsBytes[ptr] };
                short length = BitConverter.ToInt16(lengthAsBytes, 0);
                ptr += 2;
                var @string = Encoding.UTF8.GetString(entityDataAsBytes, ptr, length);
                ptr += length;
                return @string;
            }

            var typeName = GetStringFromEntityData();
            var typeAssm = GetStringFromEntityData();
            var mgrName = GetStringFromEntityData();
            var mgrAssm = GetStringFromEntityData();

            var entityType = entityTypes[$"{typeName}!{typeAssm}"];
            var managerType = managerTypes[$"{mgrName}!{mgrAssm}"];
            // Hat-tip Gąska for letting me know about this. Only works on essentially
            // Plain Old CLR Objects with malevolent constructors; there's not enough
            // information in the spec to reconstitute objects whose constructors do
            // useful work. Which is why I prefer ACI and mocking.
            var entity = (IEntity)FormatterServices.GetSafeUninitializedObject(entityType);
            // Pretty sure Managers are safe to default-construct, and *aren't* safe
            // to poof into existence, even if the ones in the test environment are.
            EntityExtensions.managers[entity] = () =>
                 (IEntityManager)Activator.CreateInstance(managerType);

            while (ptr < entityDataAsBytes.Length)
            {
                var keyString = GetStringFromEntityData();
                var key = entityType.GetField(keyString,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var valueString = GetStringFromEntityData();
                object value = null;
                if (key.FieldType.IsArray)
                {
                    var elementType = key.FieldType.GetElementType();
                    var stringValues = valueString.Split(',');
                    var array = Array.CreateInstance(elementType, stringValues.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array.SetValue(Convert(stringValues[i], elementType), i);
                    }
                    value = array;
                }
                else
                {
                    value = Convert(valueString, key.FieldType);
                }
                key.SetValue(entity, value);
            }

            return entity;
        }

        private object Convert(string value, Type type)
        {
            // GUIDs don't have a standard type converter for some reason.
            if (type == typeof(Guid))
                return Guid.Parse(value);
            return System.Convert.ChangeType(value, type);
        }

        private Type ResolveType(string assemblyQualifiedTypeName)
        {
            var pair = assemblyQualifiedTypeName.Split('!');
            var typeName = pair[0];
            var assemblyName = pair[1];

            var assembly = assemblies[assemblyName];
            return assembly.GetType(typeName, true);
        }

        [WebPermission(SecurityAction.Assert, ConnectPattern = "http://44\\.inedo\\.com/FoxworksEntMgr/.+")]
        private Assembly ResolveAssembly(string assemblyName)
        {
            var assemblyAsString = httpClient.GetStringAsync(
                new Uri("GetEntityNamespace.ashx?name=" + assemblyName, UriKind.Relative)).Result;
            var assemblyAsXml = XDocument.Parse(assemblyAsString);
            if (assemblyAsXml.Root.Attribute("enc")?.Value != "base64")
                throw new ArgumentException(null, nameof(assemblyName));
            if (assemblyAsXml.Root.Attribute("type")?.Value != "dotnet-452-assembly")
                throw new ArgumentException(null, nameof(assemblyName));
            var assemblyDataAsBytes = System.Convert.FromBase64String(assemblyAsXml.Root.Value);
            return Assembly.Load(assemblyDataAsBytes, null, SecurityContextSource.CurrentAppDomain);
        }
    }
}

