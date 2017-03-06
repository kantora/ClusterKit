﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiProvider.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   Declares some api to be provided
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.API.Provider
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using ClusterKit.API.Client;
    using ClusterKit.API.Client.Attributes;
    using ClusterKit.API.Provider.Resolvers;
    using ClusterKit.Security.Client;

    using JetBrains.Annotations;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Public api provider.
    /// </summary>
    public abstract class ApiProvider
    {
        /// <summary>
        /// The list of warnings gathered on generation stage
        /// </summary>
        private readonly List<string> generationErrors = new List<string>();

        /// <summary>
        /// The list of errors gathered on generation stage
        /// </summary>
        private readonly List<string> generationWarnings = new List<string>();

        /// <summary>
        /// The current api resolver
        /// </summary>
        private ObjectResolver resolver;

        /// <summary>
        /// Prepares serializer to deserialize arguments
        /// </summary>
        private JsonSerializer argumentsSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiProvider"/> class.
        /// </summary>
        protected ApiProvider()
        {
            this.Assemble();
        }

        /// <summary>
        /// Gets the api description
        /// </summary>
        public ApiDescription ApiDescription { get; } = new ApiDescription();

        /// <summary>
        /// Gets the list of warnings gathered on generation stage
        /// </summary>
        public IReadOnlyList<string> GenerationErrors => this.generationErrors.AsReadOnly();

        /// <summary>
        /// Gets the list of errors gathered on generation stage
        /// </summary>
        public IReadOnlyList<string> GenerationWarnings => this.generationWarnings.AsReadOnly();

        /// <summary>
        /// Resolves query
        /// </summary>
        /// <param name="requests">
        /// The query request
        /// </param>
        /// <param name="context">
        /// The request context.
        /// </param>
        /// <param name="onErrorCallback">
        /// The method that will be called in case of errors
        /// </param>
        /// <returns>
        /// Resolved query
        /// </returns>
        public virtual Task<JObject> ResolveQuery(
            List<ApiRequest> requests,
            RequestContext context,
            Action<Exception> onErrorCallback)
        {
            return this.resolver.ResolveQuery(
                this,
                new ApiRequest { Fields = requests },
                context,
                this.argumentsSerializer,
                onErrorCallback);
        }

        /// <summary>
        /// Resolves query
        /// </summary>
        /// <param name="request">
        /// The query request
        /// </param>
        /// <param name="context">
        /// The request context.
        /// </param>
        /// <param name="onErrorCallback">
        /// The method that will be called in case of errors
        /// </param>
        /// <returns>
        /// Resolved query
        /// </returns>
        public virtual async Task<JObject> ResolveMutation(
            ApiRequest request,
            RequestContext context,
            Action<Exception> onErrorCallback)
        {
            return null;
        }

        /// <summary>
        /// Searches for the connection node
        /// </summary>
        /// <param name="id">
        /// The node id
        /// </param>
        /// <param name="path">
        /// The node connection path in API
        /// </param>
        /// <param name="nodeRequest">The request to the node value</param>
        /// <param name="context">
        /// The request context.
        /// </param>
        /// <param name="onErrorCallback">
        /// The method that will be called in case of errors
        /// </param>
        /// <returns>
        /// The serialized node value
        /// </returns>
        public async Task<JObject> SearchNode(
            string id,
            List<ApiRequest> path,
            ApiRequest nodeRequest,
            RequestContext context,
            Action<Exception> onErrorCallback)
        {
            try
            {
                if (path.Count == 0)
                {
                    return null;
                }

                var queue = new Queue<ApiRequest>(path);
                object source = this;
                IResolver sourceResolver = this.resolver;
                while (queue.Count > 0)
                {
                    var objectResolver = sourceResolver as ObjectResolver;
                    if (objectResolver == null)
                    {
                        return null;
                    }

                    var field = queue.Dequeue();
                    var result = await objectResolver.ResolvePropertyValue(source, field, context, this.argumentsSerializer, onErrorCallback);
                    if (result == null)
                    {
                        return null;
                    }

                    source = result.Value;
                    sourceResolver = result.Resolver;
                }

                var connectionResolver = sourceResolver as IConnectionResolver;
                if (connectionResolver == null)
                {
                    return null;
                }

                var node = await connectionResolver.GetNodeById(source, id);
                return (JObject)await connectionResolver.NodeResolver.ResolveQuery(node, nodeRequest, context, this.argumentsSerializer, onErrorCallback);
            }
            catch (Exception e)
            {
                onErrorCallback?.Invoke(e);
                return null;
            }
        }

        /// <summary>
        /// Parses the metadata of returning type for the field
        /// </summary>
        /// <param name="type">The original field return type</param>
        /// <param name="attribute">The description attribute</param>
        /// <returns>The type metadata</returns>
        internal static TypeMetadata GenerateTypeMetadata(Type type, PublishToApiAttribute attribute)
        {
            var metadata = new TypeMetadata();
            var asyncType = TypeMetadata.CheckType(type, typeof(Task<>));
            if (asyncType != null)
            {
                metadata.IsAsync = true;
                type = asyncType.GenericTypeArguments[0];
            }

            if (attribute.ReturnType != null)
            {
                type = attribute.ReturnType;
                metadata.IsForwarding = true;
            }

            var scalarType = TypeMetadata.CheckScalarType(type);
            metadata.MetaType = TypeMetadata.EnMetaType.Scalar;
            if (scalarType == EnScalarType.None)
            {
                var enumerable = TypeMetadata.CheckType(type, typeof(IEnumerable<>));
                var connection = TypeMetadata.CheckType(type, typeof(INodeConnection<,>));

                if (connection != null)
                {
                    metadata.MetaType = TypeMetadata.EnMetaType.Connection;
                    metadata.TypeOfId = connection.GenericTypeArguments[1];
                    type = connection.GenericTypeArguments[0];
                    scalarType = TypeMetadata.CheckScalarType(type);
                }
                else if (enumerable != null)
                {
                    metadata.MetaType = TypeMetadata.EnMetaType.Array;
                    type = enumerable.GenericTypeArguments[0];
                    scalarType = TypeMetadata.CheckScalarType(type);
                }
                else
                {
                    metadata.MetaType = TypeMetadata.EnMetaType.Object;
                }
            }

            if (scalarType != EnScalarType.None && type.IsSubclassOf(typeof(Enum)))
            {
                type = Enum.GetUnderlyingType(type);
            }

            // todo: check forwarding type
            metadata.ScalarType = scalarType;
            metadata.Type = type;

            var typeName = type.GetCustomAttribute<ApiDescriptionAttribute>()?.Name ?? type.FullName;
            metadata.TypeName = metadata.TypeOfId != null ? $"{typeName}_{metadata.TypeOfId.FullName}" : typeName;
            return metadata;
        }

        /// <summary>
        /// Generates the api description and prepares all resolvers
        /// </summary>
        private void Assemble()
        {
            this.ApiDescription.Version = this.GetType().Assembly.GetName().Version;
            this.ApiDescription.Types.Clear();
            this.ApiDescription.Mutations.Clear();

            var assembleData = new AssembleTempData();
            var root = (ApiObjectType)this.GenerateTypeDescription(
                new TypeMetadata
                    {
                        Type = this.GetType(),
                        MetaType = TypeMetadata.EnMetaType.Object,
                        ScalarType = EnScalarType.None
                    }, 
                assembleData);

            this.ApiDescription.TypeName = this.ApiDescription.ApiName = root.TypeName;
            this.ApiDescription.Description = root.Description;

            var mutations = this.GenerateMutations(root, new List<string>(), new List<string>(), assembleData);

            this.ApiDescription.Mutations = mutations.ToList();
            this.ApiDescription.Types =
                assembleData.DiscoveredApiTypes.Values.Where(t => t.TypeName != root.TypeName).ToList();
            this.ApiDescription.Fields = root.Fields;

            this.argumentsSerializer = new JsonSerializer
                                           {
                                               ContractResolver =
                                                   new InputContractResolver(assembleData.FieldNames)
                                           };

            this.CompileResolvers(root, assembleData);
        }

        /// <summary>
        /// Perform resolvers compilation
        /// </summary>
        /// <param name="root">
        /// The root api description.
        /// </param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        private void CompileResolvers(ApiObjectType root, AssembleTempData data)
        {
            var code = data.ResolverGenerators.Select(g => g.Generate()).ToList();
            var comDomProvider = CodeDomProvider.CreateProvider("C#");
#if DEBUG
            var compilerParameters = new CompilerParameters { GenerateInMemory = false, IncludeDebugInformation = true };
#else
            var compilerParameters = new CompilerParameters { GenerateInMemory = true, IncludeDebugInformation = false };
#endif

            compilerParameters.ReferencedAssemblies.AddRange(
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .Select(a => a.CodeBase.Replace("file:\\", string.Empty).Replace(
                        Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX ? "file://" : "file:///", string.Empty))
                    .ToArray());

            var compiledResult = comDomProvider.CompileAssemblyFromSource(compilerParameters, code.ToArray());
            foreach (CompilerError error in compiledResult.Errors)
            {
                if (error.IsWarning)
                {
                    this.generationWarnings.Add(error.ToString());
                }
                else
                {
                    this.generationErrors.Add(error.ToString());
                }
            }

            // this.resolvers = new Dictionary<string, PropertyResolver>();
            // this.mutationResolvers = new Dictionary<string, PropertyResolver>();

            if (!compiledResult.Errors.HasErrors)
            {
                this.resolver = (ObjectResolver)compiledResult.CompiledAssembly.CreateInstance(
                            data.ObjectResolverNames[root.TypeName]);
                /*
                foreach (var rootField in root.Fields)
                {
                    this.resolvers[rootField.Name] =
                        (PropertyResolver)
                        compiledResult.CompiledAssembly.CreateInstance(
                            data.ResolverNames[root.TypeName][rootField.Name]);
                }

                foreach (var mutation in this.ApiDescription.Mutations)
                {
                    this.mutationResolvers[mutation.Name] = 
                        (PropertyResolver)
                        compiledResult.CompiledAssembly.CreateInstance(
                            data.MutationResolverNames[mutation.Name]);
                }
                */
            }
        }

        /// <summary>
        /// Generates api field from type method
        /// </summary>
        /// <param name="method">
        /// The method to process
        /// </param>
        /// <param name="apiType">The api type description</param>
        /// <param name="attribute">
        /// The method description attribute
        /// </param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>
        /// The field description
        /// </returns>
        private ApiField GenerateFieldFromMethod(
            MethodInfo method,
            ApiObjectType apiType,
            PublishToApiAttribute attribute,
            AssembleTempData data)
        {
            var type = method.ReflectedType;
            TypeMetadata metadata;
            try
            {
                metadata = GenerateTypeMetadata(method.ReturnType, attribute);
            }
            catch (Exception exception)
            {
                this.generationErrors.Add(
                    $"Error while parsing return type of method {method.Name} of type {type?.FullName}: {exception.Message}");
                return null;
            }

            var name = attribute.Name ?? ResolverGenerator.ToCamelCase(method.Name);
            data.FieldNames[method] = name;
            data.Members[apiType.TypeName][name] = method;

            ApiField field;
            if (metadata.ScalarType != EnScalarType.None)
            {
                field = ApiField.Scalar(
                    name,
                    metadata.ScalarType,
                    metadata.GetFlags(),
                    description: attribute.Description);
            }
            else
            {
                var returnApiType = this.GenerateTypeDescription(metadata, data);
                field = ApiField.Object(
                    name,
                    returnApiType.TypeName,
                    metadata.GetFlags(),
                    description: attribute.Description);
            }

            foreach (var parameterInfo in method.GetParameters())
            {
                if (parameterInfo.ParameterType == typeof(RequestContext))
                {
                    continue;
                }

                if (parameterInfo.ParameterType == typeof(ApiRequest))
                {
                    continue;
                }

                var parameterMetadata = GenerateTypeMetadata(
                    parameterInfo.ParameterType,
                    new DeclareFieldAttribute());
                var description =
                    parameterInfo.GetCustomAttribute(typeof(ApiDescriptionAttribute)) as ApiDescriptionAttribute;

                var parameterName = description?.Name ?? ResolverGenerator.ToCamelCase(parameterInfo.Name);
                if (parameterMetadata.ScalarType != EnScalarType.None)
                {
                    field.Arguments.Add(
                        ApiField.Scalar(
                            parameterName,
                            parameterMetadata.ScalarType,
                            parameterMetadata.GetFlags(),
                            description: description?.Description));
                }
                else
                {
                    var parameterApiType = this.GenerateTypeDescription(parameterMetadata, data);
                    field.Arguments.Add(
                        ApiField.Object(
                            parameterName,
                            parameterApiType.TypeName,
                            parameterMetadata.GetFlags(),
                            description: description?.Description));
                }
            }

            return field;
        }

        /// <summary>
        /// Generates api field from type method
        /// </summary>
        /// <param name="apiType">The type api description</param>
        /// <param name="property">
        /// The property to process
        /// </param>
        /// <param name="attribute">
        /// The method description attribute
        /// </param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>
        /// The field description
        /// </returns>
        private ApiField GenerateFieldFromProperty(
            ApiObjectType apiType,
            PropertyInfo property,
            PublishToApiAttribute attribute,
            AssembleTempData data)
        {
            var declaringType = property.ReflectedType;

            TypeMetadata metadata;
            try
            {
                metadata = GenerateTypeMetadata(property.PropertyType, attribute);
            }
            catch (Exception exception)
            {
                this.generationErrors.Add(
                    $"Error while parsing return type of property {property.Name} of type {declaringType?.FullName}: {exception.Message}");
                return null;
            }

            var name = attribute.Name ?? ResolverGenerator.ToCamelCase(property.Name);
            data.FieldNames[property] = name;
            data.Members[apiType.TypeName][name] = property;

            var flags = metadata.GetFlags();

            if ((metadata.Type.IsSubclassOf(typeof(Enum)) || metadata.ScalarType != EnScalarType.None)
                && !metadata.IsAsync && !metadata.IsForwarding
                && (metadata.MetaType == TypeMetadata.EnMetaType.Object
                    || metadata.MetaType == TypeMetadata.EnMetaType.Scalar))
            {
                flags |= EnFieldFlags.IsFilterable | EnFieldFlags.IsSortable;
            }

            if (!metadata.IsAsync && !metadata.IsForwarding && property.CanWrite
                && metadata.MetaType != TypeMetadata.EnMetaType.Connection)
            {
                flags |= EnFieldFlags.CanBeUsedInInput;
            }

            if (metadata.ScalarType != EnScalarType.None)
            {
                if ((attribute as DeclareFieldAttribute)?.IsKey == true)
                {
                    flags |= EnFieldFlags.IsKey;
                }

                return ApiField.Scalar(name, metadata.ScalarType, flags, description: attribute.Description);
            }

            var returnApiType = this.GenerateTypeDescription(metadata, data);

            return ApiField.Object(
                name,
                returnApiType.TypeName,
                flags,
                description: attribute.Description);
        }

        /// <summary>
        /// Generates mutations
        /// </summary>
        /// <param name="apiType">
        /// The current api type
        /// </param>
        /// <param name="path">
        /// The path to current field
        /// </param>
        /// <param name="typesUsed">
        /// The types already used to avoid circular references
        /// </param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>
        /// The list of mutations
        /// </returns>
        private IEnumerable<ApiField> GenerateMutations(
            ApiObjectType apiType,
            List<string> path,
            List<string> typesUsed,
            AssembleTempData data)
        {
            Type type;
            if (!data.DiscoveredTypes.TryGetValue(apiType.TypeName, out type))
            {
                yield break;
            }

            foreach (var mutation in this.GenerateMutationsDirect(apiType, type, path, data))
            {
                yield return mutation;
            }

            foreach (var mutation in this.GenerateMutationsFromFields(apiType, path, typesUsed, data))
            {
                yield return mutation;
            }

            foreach (var apiField in this.GenerateMutationsFromConnections(type, apiType, path, data))
            {
                yield return apiField;
            }
        }

        /// <summary>
        /// Generate mutations directly declared in the type
        /// </summary>
        /// <param name="apiType">The api type description</param>
        /// <param name="type">The type</param>
        /// <param name="path">The fields path</param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>The list of mutations</returns>
        private IEnumerable<ApiField> GenerateMutationsDirect(
            ApiObjectType apiType,
            Type type,
            List<string> path,
            AssembleTempData data)
        {
            var fields =
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(
                        p =>
                            new
                                {
                                    Method = p,
                                    Attribute =
                                    (DeclareMutationAttribute)p.GetCustomAttribute(typeof(DeclareMutationAttribute))
                                })
                    .Where(p => p.Attribute != null && !p.Method.IsGenericMethod && !p.Method.IsGenericMethodDefinition)
                    .Select(
                        m =>
                            new
                                {
                                    ApiField = this.GenerateFieldFromMethod(m.Method, apiType, m.Attribute, data),
                                    m.Method,
                                    MetaData = GenerateTypeMetadata(m.Method.ReturnType, m.Attribute)
                                })
                    .Where(f => f.ApiField != null);

            foreach (var description in fields)
            {
                apiType.Mutations.Add(description.ApiField);
                var apiField = description.ApiField.Clone();
                apiField.Name = string.Join(".", new List<string>(path) { apiField.Name });
                yield return apiField;
            }
        }

        /// <summary>
        /// Generate mutations directly declared in the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="apiType">The api description of the type</param>
        /// <param name="path">The fields path</param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>The list of mutations</returns>
        private IEnumerable<ApiField> GenerateMutationsFromConnections(
            Type type,
            ApiObjectType apiType,
            List<string> path,
            AssembleTempData data)
        {
            var members = type.GetMembers().Where(m => m is PropertyInfo || m is MethodInfo).Select(
                m =>
                    {
                        var description =
                            (DeclareConnectionAttribute)m.GetCustomAttribute(typeof(DeclareConnectionAttribute));
                        var name = description?.Name ?? ResolverGenerator.ToCamelCase(m.Name);
                        return
                            new
                                {
                                    Name = name,
                                    Member = m,
                                    Description = description,
                                    Type = (m as PropertyInfo)?.PropertyType ?? (m as MethodInfo)?.ReturnType
                                };
                    }).Where(m => m.Description != null).ToList();

            var connections =
                apiType.Fields.Where(
                    f =>
                        !f.Arguments.Any() && f.ScalarType == EnScalarType.None
                        && f.Flags.HasFlag(EnFieldFlags.IsConnection));

            foreach (var connection in connections)
            {
                var description = members.FirstOrDefault(m => m.Name == connection.Name);
                if (description == null)
                {
                    continue;
                }

                var attribute = description.Description;
                var connectionType = attribute.ReturnType ?? description.Type;
                if (connectionType == null)
                {
                    this.generationErrors.Add(
                        $"Type discover error on connection {connection.Name} of type {type.FullName}");
                    continue;
                }

                var checkType = TypeMetadata.CheckType(connectionType, typeof(INodeConnection<,>));
                var idType = checkType?.GenericTypeArguments[1];
                var connectedObjectType = checkType?.GenericTypeArguments[0];
                if (idType == null || connectedObjectType == null)
                {
                    this.generationErrors.Add($"Wrong type on connection {connection.Name} of type {type.FullName}");
                    continue;
                }

                var idScalarType = TypeMetadata.CheckScalarType(idType);
                if (idScalarType == EnScalarType.None)
                {
                    this.generationErrors.Add(
                        $"Defined type of id should be scalar type on connection {connection.Name} of type {type.FullName}");
                    continue;
                }

                ApiType connectionApiType;
                if (!data.DiscoveredApiTypes.TryGetValue(connection.TypeName, out connectionApiType))
                {
                    this.generationErrors.Add(
                        $"Defined type of connection {connection.Name} of type {type.FullName} was not processed on previous steps");
                    continue;
                }

                var mutationResultType = typeof(MutationResult<>).MakeGenericType(connectedObjectType);
                var mutationResultMetadata = GenerateTypeMetadata(mutationResultType, new DeclareFieldAttribute { Description = "The mutation result" });
                var mutationResult = this.GenerateTypeDescription(mutationResultMetadata, data);

                if (attribute.CanCreate)
                {
                    var name = string.Join(".", new List<string>(path) { connection.Name, "create" });

                    yield return
                        ApiField.Object(
                            name,
                            mutationResult.TypeName,
                            arguments:
                            new List<ApiField>
                                {
                                    ApiField.Object(
                                        "newNode",
                                        connection.TypeName,
                                        description: "The object's data")
                                },
                            description: attribute.CreateDescription);
                }

                if (attribute.CanUpdate)
                {
                    var name = string.Join(".", new List<string>(path) { connection.Name, "update" });
                    yield return
                        ApiField.Object(
                            name,
                            mutationResult.TypeName,
                            arguments:
                            new List<ApiField>
                                {
                                    ApiField.Scalar("id", idScalarType, description: "The object's id"),
                                    ApiField.Object(
                                        "newNode",
                                        connection.TypeName,
                                        description: "The object's data")
                                },
                            description: attribute.CreateDescription);
                }

                if (attribute.CanCreate)
                {
                    var name = string.Join(".", new List<string>(path) { connection.Name, "delete" });

                    yield return
                        ApiField.Object(
                            name,
                            mutationResult.TypeName,
                            arguments:
                            new List<ApiField> { ApiField.Scalar("id", idScalarType, description: "The object's id") },
                            description: attribute.CreateDescription);
                }
                
                yield break;
            }
        }

        /// <summary>
        /// Generate mutation from type fields
        /// </summary>
        /// <param name="apiType">The api description of the type</param>
        /// <param name="path">The type field path</param>
        /// <param name="typesUsed">Already used types to avoid circular references</param>
        /// <param name="data">
        /// The temporary data used during assemble process
        /// </param>
        /// <returns>The list of mutations</returns>
        private IEnumerable<ApiField> GenerateMutationsFromFields(
            ApiObjectType apiType,
            List<string> path,
            List<string> typesUsed,
            AssembleTempData data)
        {
            var possibleMutationSubcontainers =
                apiType.Fields.Where(
                    f =>
                        !f.Arguments.Any() && f.ScalarType == EnScalarType.None
                        && !f.Flags.HasFlag(EnFieldFlags.IsArray) && !f.Flags.HasFlag(EnFieldFlags.IsConnection));

            foreach (var subContainer in possibleMutationSubcontainers)
            {
                if (typesUsed.Contains(subContainer.TypeName))
                {
                    // circular type use
                    this.generationWarnings.Add(
                        $"Circular property found for type {apiType.TypeName} field {subContainer.Name}");
                    continue;
                }

                ApiType subType;
                if (!data.DiscoveredApiTypes.TryGetValue(subContainer.TypeName, out subType))
                {
                    this.generationErrors.Add($"Could not find declared api type {subContainer.TypeName}");
                    continue;
                }

                var subObjectType = subType as ApiObjectType;

                if (subObjectType == null)
                {
                    continue;
                }

                var newPath = new List<string>(path) { subContainer.Name };
                var newTypesUsed = new List<string>(typesUsed) { apiType.TypeName };

                foreach (var generateMutation in this.GenerateMutations(subObjectType, newPath, newTypesUsed, data))
                {
                    yield return generateMutation;
                }
            }
        }

        /// <summary>
        /// Generates type description
        /// </summary>
        /// <param name="typeMetaData">
        /// The type to describe
        /// </param>
        /// <param name="data">
        /// The temporary data used during assemble process.
        /// </param>
        /// <returns>
        /// The api type description
        /// </returns>
        private ApiType GenerateTypeDescription([NotNull] TypeMetadata typeMetaData, [NotNull] AssembleTempData data)
        {
            var type = typeMetaData.Type;
            var descriptionAttribute = (ApiDescriptionAttribute)type.GetCustomAttribute(typeof(ApiDescriptionAttribute));
            var typeName = descriptionAttribute?.Name ?? ResolverGenerator.ToCSharpRepresentation(type);

            if (typeMetaData.MetaType == TypeMetadata.EnMetaType.Connection)
            {
                if (!data.ConnectionResolverNames.ContainsKey(typeMetaData.TypeName))
                {
                    var connectionResolverGenerator = new ConnectionResolverGenerator(typeMetaData, data);
                    data.ConnectionResolverNames[typeMetaData.TypeName] = connectionResolverGenerator.FullClassName;
                    data.ResolverGenerators.Add(connectionResolverGenerator);
                }
            }

            ApiType apiType;
            if (data.DiscoveredApiTypes.TryGetValue(type.FullName, out apiType))
            {
                return apiType;
            }

            if (type.IsSubclassOf(typeof(Enum)))
            {
                var apiEnumType =
                    new ApiEnumType(
                        typeName,
                        Enum.GetNames(type)) { Description = descriptionAttribute?.Description };
                data.DiscoveredTypes[apiEnumType.TypeName] = type;
                data.DiscoveredApiTypes[apiEnumType.TypeName] = apiEnumType;
                data.ApiTypeByOriginalTypeNames[type.FullName] = apiEnumType;
                return apiEnumType;
            }

            var apiObjectType = new ApiObjectType(typeName)
                          {
                              Description =
                                  descriptionAttribute?.Description
                          };

            data.DiscoveredTypes[apiObjectType.TypeName] = type;
            data.DiscoveredApiTypes[apiObjectType.TypeName] = apiObjectType;
            data.ApiTypeByOriginalTypeNames[type.FullName] = apiObjectType;
            data.Members[apiObjectType.TypeName] = new Dictionary<string, MemberInfo>();

            var resolverGenerator = new ObjectResolverGenerator(type, data);
            data.ObjectResolverNames[apiObjectType.TypeName] = resolverGenerator.FullClassName;
            data.ResolverGenerators.Add(resolverGenerator);

            apiObjectType.Fields.AddRange(this.GenerateTypeProperties(type, apiObjectType, data));
            apiObjectType.Fields.AddRange(this.GenerateTypeMethods(type, apiObjectType, data));
            return apiObjectType;
        }

        /// <summary>
        /// Generate fields from type methods
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="apiType">The type api description</param>
        /// <param name="data">The temporary data used during assemble process</param>
        /// <returns>The field api description</returns>
        private IEnumerable<ApiField> GenerateTypeMethods(Type type, ApiObjectType apiType, [NotNull] AssembleTempData data)
        {
            return
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(
                        p =>
                            new
                                {
                                    Method = p,
                                    Attribute =
                                    (DeclareFieldAttribute)p.GetCustomAttribute(typeof(DeclareFieldAttribute))
                                })
                    .Where(p => p.Attribute != null && !p.Method.IsGenericMethod && !p.Method.IsGenericMethodDefinition)
                    .Select(m => this.GenerateFieldFromMethod(m.Method, apiType, m.Attribute, data))
                    .Where(f => f != null);
        }

        /// <summary>
        /// Generate fields from type properties
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="apiType">The type api description</param>
        /// <param name="data">The temporary data used during assemble process</param>
        /// <returns>The field api description</returns>
        private IEnumerable<ApiField> GenerateTypeProperties(
            Type type,
            ApiObjectType apiType,
            [NotNull] AssembleTempData data)
        {
            return
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(
                        p =>
                            new
                                {
                                    Property = p,
                                    Attribute =
                                    (DeclareFieldAttribute)p.GetCustomAttribute(typeof(DeclareFieldAttribute))
                                })
                    .Where(p => p.Attribute != null && p.Property.CanRead)
                    .Select(o => this.GenerateFieldFromProperty(apiType, o.Property, o.Attribute, data))
                    .Where(f => f != null);
        }

        /// <summary>
        /// The internal description of published mutation
        /// </summary>
        private class MutationDescription
        {
            public enum EnMutationType
            {
            }
             

            /// <summary>
            /// Gets or sets the mutation field name
            /// </summary>
            public ApiField Field { get; set; }

            /// <summary>
            /// Gets or sets the path to mutation container
            /// </summary>
            public List<ApiRequest> Path { get; set; }
        }
    }
}