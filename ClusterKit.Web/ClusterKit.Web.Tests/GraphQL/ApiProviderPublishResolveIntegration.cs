// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiProviderPublishResolveIntegration.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   Testing publishing and resolving integration
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.Web.Tests.GraphQL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ClusterKit.API.Client;
    using ClusterKit.API.Client.Attributes;
    using ClusterKit.API.Tests.Mock;
    using ClusterKit.Security.Client;
    using ClusterKit.Web.GraphQL.Publisher;

    using global::GraphQL;
    using global::GraphQL.Http;
    using global::GraphQL.Utilities;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Testing publishing and resolving integration
    /// </summary>
    public class ApiProviderPublishResolveIntegration
    {
        /// <summary>
        /// The output.
        /// </summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiProviderPublishResolveIntegration"/> class.
        /// </summary>
        /// <param name="output">
        /// The output.
        /// </param>
        public ApiProviderPublishResolveIntegration(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Removes formatting values from response json
        /// </summary>
        /// <param name="response">The json string</param>
        /// <returns>Cleaned json string</returns>
        public static string CleanResponse(string response)
        {
            return JsonConvert.DeserializeObject<JObject>(response).ToString(Formatting.None);
        }

        /// <summary>
        /// Testing connection query request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionQueryTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                        connection(sort: [value_asc, name_asc], filter: {value_gt: 10}, offset: 1, limit: 2) {
                            count,
                            edges {
                                cursor,
                                node {
                                    id,
                                    __id,
                                    name,
                                    value
                                }                    
                            }
                        }
                }                
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            var expectedResult = @"
                            {
                              ""data"": {
                                ""api"": {
                                  ""connection"": {
                                    ""count"": 4,
                                    ""edges"": [
                                      {
                                        ""cursor"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                        ""node"": {
                                          ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                          ""__id"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                          ""name"": ""3-test"",
                                          ""value"": 50.0
                                        }
                                      },
                                      {
                                        ""cursor"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                        ""node"": {
                                          ""id"": ""H4sIAAAAAAAEAA3IOQ6AIBRF0b28GhIFkaFzD3bG4ochoQES6Qh7l+6eO9DgnoEEB19Lib7nWjBfBmp5zTt+/VrFkMOipCS81ZIHaxQ/klWcNq85kRUmCH0as2P+35sMPFYAAAA="",
                                          ""__id"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                          ""name"": ""4-test"",
                                          ""value"": 70.0
                                        }
                                      }
                                    ]
                                  }
                                }
                              }
                            }
                            ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection query request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionQueryWithAliasesTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                        list: connection(sort: [value_asc, name_asc], filter: {value_gt: 10}, offset: 1, limit: 2) {
                            total: count,
                            total2: count,
                            items: edges {
                                location: cursor,
                                location2: cursor,
                                item: node {
                                    globalId: id,
                                    naming: name,
                                },
                                item2: node {
                                    id2: __id,
                                    v2: value
                                },                      
                            },
                            items2: edges {
                                location: cursor,
                                location2: cursor,
                                item: node {
                                    type
                                },
                                item2: node {
                                    globalId2: id,
                                    id2: __id,
                                    naming2: name,
                                    v2: value,
                                    type
                                },                      
                            }
                        }
                }                
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            var expectedResult = @"
                                    {
                                      ""data"": {
                                        ""api"": {
                                          ""list"": {
                                            ""total"": 4,
                                            ""total2"": 4,
                                            ""items"": [
                                              {
                                                ""location"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                ""location2"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                ""item"": {
                                                  ""globalId"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                                  ""naming"": ""3-test""
                                                },
                                                ""item2"": {
                                                  ""id2"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                  ""v2"": 50.0
                                                }
                                              },
                                              {
                                                ""location"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                ""location2"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                ""item"": {
                                                  ""globalId"": ""H4sIAAAAAAAEAA3IOQ6AIBRF0b28GhIFkaFzD3bG4ochoQES6Qh7l+6eO9DgnoEEB19Lib7nWjBfBmp5zTt+/VrFkMOipCS81ZIHaxQ/klWcNq85kRUmCH0as2P+35sMPFYAAAA="",
                                                  ""naming"": ""4-test""
                                                },
                                                ""item2"": {
                                                  ""id2"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                  ""v2"": 70.0
                                                }
                                              }
                                            ],
                                            ""items2"": [
                                              {
                                                ""location"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                ""location2"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                ""item"": {
                                                  ""type"": ""Good""
                                                },
                                                ""item2"": {
                                                  ""globalId2"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                                  ""id2"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                  ""naming2"": ""3-test"",
                                                  ""v2"": 50.0,
                                                  ""type"": ""Good""
                                                }
                                              },
                                              {
                                                ""location"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                ""location2"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                ""item"": {
                                                  ""type"": ""Good""
                                                },
                                                ""item2"": {
                                                  ""globalId2"": ""H4sIAAAAAAAEAA3IOQ6AIBRF0b28GhIFkaFzD3bG4ochoQES6Qh7l+6eO9DgnoEEB19Lib7nWjBfBmp5zTt+/VrFkMOipCS81ZIHaxQ/klWcNq85kRUmCH0as2P+35sMPFYAAAA="",
                                                  ""id2"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                  ""naming2"": ""4-test"",
                                                  ""v2"": 70.0,
                                                  ""type"": ""Good""
                                                }
                                              }
                                            ]
                                          }
                                        }
                                      }
                                    }
                                ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection create request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionMutationInsertTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_connection_create(newNode: {id: ""251FEEA8-D3AC-461D-A385-0CF2BA7A74E8"", name: ""hello world"", value: 13}, clientMutationId: ""testClientMutationId"") {
                    clientMutationId,
                    node {
                        id,
                        __id,
                        name,
                        value
                    },
                    edge {
                        cursor,
                        node {
                            id,
                            __id,
                            name,
                            value
                        }
                    },
                    deletedId,
                    api {
                        connection(sort: [value_asc, name_asc], filter: {value: 13}) {
                            count,
                            edges {
                                cursor,
                                node {
                                    id,
                                    __id,
                                    name,
                                    value
                                }                    
                            }
                        }
                    }
                }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            
            var expectedResult = @"
                                    {
                                      ""data"": {
                                        ""call"": {
                                          ""clientMutationId"": ""testClientMutationId"",
                                          ""node"": {
                                            ""id"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                            ""__id"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""name"": ""hello world"",
                                            ""value"": 13.0
                                          },
                                          ""edge"": {
                                            ""cursor"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""node"": {
                                              ""id"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                              ""__id"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                              ""name"": ""hello world"",
                                              ""value"": 13.0
                                            }
                                          },
                                          ""deletedId"": null,
                                          ""api"": {
                                            ""connection"": {
                                              ""count"": 1,
                                              ""edges"": [
                                                {
                                                  ""cursor"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                                  ""node"": {
                                                    ""id"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                                    ""__id"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                                    ""name"": ""hello world"",
                                                    ""value"": 13.0
                                                  }
                                                }
                                              ]
                                            }
                                          }
                                        }
                                      }
                                    }
                                    ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection create request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionMutationInsertWithAliasesTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_connection_create(newNode: {id: ""251FEEA8-D3AC-461D-A385-0CF2BA7A74E8"", name: ""hello world"", value: 13}, clientMutationId: ""testClientMutationId"") {
                    mutationId: clientMutationId,
                    nodeElement: node {
                        globalId: id,
                        realId: __id,
                        elementName: name,
                        value
                    },
                    nodeElement2: node {
                        globalId2: id,
                        realId2: __id,
                        elementName2: name,
                        value
                    },
                    edgeObject: edge {
                        id: cursor,
                        object: node {
                            longId: id,
                            edgeName: name,
                        },
                        object2: node {
                            shortId: __id,
                            value
                        },
                    },
                    edgeObject2: edge {
                        id2: cursor,
                        object2: node {
                            longId2: id,
                            shortId2: __id,
                            edgeName2: name,
                            value
                        }
                    },
                    removedId: deletedId,
                    root: api {
                        apiObjects: connection(sort: [value_asc, name_asc], filter: {value: 13}) {
                            total: count,
                            list: edges {
                                itemId: cursor,
                                element: node {
                                    elementGlobalId: id,
                                    elementId: __id,
                                    elementName: name,
                                    elementValue: value
                                }             
                            }
                        }
                    }
                }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                                    {
                                      ""data"": {
                                        ""call"": {
                                          ""mutationId"": ""testClientMutationId"",
                                          ""nodeElement"": {
                                            ""globalId"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                            ""realId"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""elementName"": ""hello world"",
                                            ""value"": 13.0
                                          },
                                          ""nodeElement2"": {
                                            ""globalId2"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                            ""realId2"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""elementName2"": ""hello world"",
                                            ""value"": 13.0
                                          },
                                          ""edgeObject"": {
                                            ""id"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""object"": {
                                              ""longId"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                              ""edgeName"": ""hello world""
                                            },
                                            ""object2"": {
                                              ""shortId"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                              ""value"": 13.0
                                            }
                                          },
                                          ""edgeObject2"": {
                                            ""id2"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                            ""object2"": {
                                              ""longId2"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                              ""shortId2"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                              ""edgeName2"": ""hello world"",
                                              ""value"": 13.0
                                            }
                                          },
                                          ""removedId"": null,
                                          ""root"": {
                                            ""apiObjects"": {
                                              ""total"": 1,
                                              ""list"": [
                                                {
                                                  ""itemId"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                                  ""element"": {
                                                    ""elementGlobalId"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8GhJBEELnHeyMxQpLQgMk0hnvLt28edERzhcZAbHVynGUVvFdAtTLnAc/Y58lUNKktiozk5dppSjNppKk1Vu5xKxvcuQMe3w/aQb7IlYAAAA="",
                                                    ""elementId"": ""251feea8-d3ac-461d-a385-0cf2ba7a74e8"",
                                                    ""elementName"": ""hello world"",
                                                    ""elementValue"": 13.0
                                                  }
                                                }
                                              ]
                                            }
                                          }
                                        }
                                      }
                                    }
                                    ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection delete request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionMutationDeleteTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_connection_delete(id: ""3BEEE369-11DF-4A30-BF11-1D8465C87110"") {
                    node {
                        id,
                        __id,
                        name,
                        value
                    },
                    edge {
                        cursor,
                        node {
                            id,
                            __id,
                            name,
                            value
                        }
                    },
                    deletedId,
                    api {
                        connection(sort: [value_asc, name_asc]) {
                            count,
                            edges {
                                cursor,
                                node {
                                    id,
                                    __id,
                                    name,
                                    value
                                }                    
                            }
                        }
                    }
                }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            
            var expectedResult = @"
                                {
                                  ""data"": {
                                    ""call"": {
                                      ""node"": {
                                        ""id"": ""H4sIAAAAAAAEAA3IOw6AIBBF0b28GhImKKKde7AzFnyGhAZItCPsXbp7bkfDcXckHAi1FA5frgXjEXAtz3nx+52zBHKc1J6ZtdklUUxycVpJn4gkRbuYNdiNSGH8PdZUnlYAAAA="",
                                        ""__id"": ""3beee369-11df-4a30-bf11-1d8465c87110"",
                                        ""name"": ""1-test"",
                                        ""value"": 100.0
                                      },
                                      ""edge"": {
                                        ""cursor"": ""3beee369-11df-4a30-bf11-1d8465c87110"",
                                        ""node"": {
                                          ""id"": ""H4sIAAAAAAAEAA3IOw6AIBBF0b28GhImKKKde7AzFnyGhAZItCPsXbp7bkfDcXckHAi1FA5frgXjEXAtz3nx+52zBHKc1J6ZtdklUUxycVpJn4gkRbuYNdiNSGH8PdZUnlYAAAA="",
                                          ""__id"": ""3beee369-11df-4a30-bf11-1d8465c87110"",
                                          ""name"": ""1-test"",
                                          ""value"": 100.0
                                        }
                                      },
                                      ""deletedId"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8WhI2KCqdd7ATC4QloUES6Qh3l27eNBSYqyHCwL85s6/pzej3BFfSmCd/9Rg1IYVBC/Uws9K7IApRzE5J8UQiQWGb9eK3lUhaoP+q2DHnWgAAAA=="",
                                      ""api"": {
                                        ""connection"": {
                                          ""count"": 4,
                                          ""edges"": [
                                            {
                                              ""cursor"": ""f0607502-5b77-4a3c-9142-e6197a7ee61e"",
                                              ""node"": {
                                                ""id"": ""H4sIAAAAAAAEAA2IOw6AIBAF7/JqNwEFNtp5BztjgbgkNEAiHfHuUs2no2I7OyI2hJKzhJZKxndN8DWNecjb9mET0jMyKqfYqpnszUzGL4FWbWYSp1f2LIOC7wdhM18gVgAAAA=="",
                                                ""__id"": ""f0607502-5b77-4a3c-9142-e6197a7ee61e"",
                                                ""name"": ""5-test"",
                                                ""value"": 6.0
                                              }
                                            },
                                            {
                                              ""cursor"": ""b500ca20-f649-4dcd-bda8-1fa5031ecdd3"",
                                              ""node"": {
                                                ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlAbpKpgUFyopGBbpqZiaWuSUpyim5SSqKFrmFaoqmBsWFqckqKsVItAKGn6lFWAAAA"",
                                                ""__id"": ""b500ca20-f649-4dcd-bda8-1fa5031ecdd3"",
                                                ""name"": ""2-test"",
                                                ""value"": 50.0
                                              }
                                            },
                                            {
                                              ""cursor"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                              ""node"": {
                                                ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                                ""__id"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                                ""name"": ""3-test"",
                                                ""value"": 50.0
                                              }
                                            },
                                            {
                                              ""cursor"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                              ""node"": {
                                                ""id"": ""H4sIAAAAAAAEAA3IOQ6AIBRF0b28GhIFkaFzD3bG4ochoQES6Qh7l+6eO9DgnoEEB19Lib7nWjBfBmp5zTt+/VrFkMOipCS81ZIHaxQ/klWcNq85kRUmCH0as2P+35sMPFYAAAA="",
                                                ""__id"": ""3af2c973-d985-4f95-a0c7-aa928d276881"",
                                                ""name"": ""4-test"",
                                                ""value"": 70.0
                                              }
                                            }
                                          ]
                                        }
                                      }
                                    }
                                  }
                                }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection update request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionMutationUpdateTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_connection_update(id: ""3BEEE369-11DF-4A30-BF11-1D8465C87110"", newNode: {id: ""3BEEE369-11DF-4A30-BF11-1D8465C87111"", name: ""hello world"", value: 13}) {
                    node {
                        id,
                        __id,
                        name,
                        value
                    },
                    edge {
                        cursor,
                        node {
                            id,
                            __id,
                            name,
                            value
                        }
                    },
                    deletedId,
                    api {
                        connection(sort: [value_asc, name_asc], filter: {value: 13}) {
                            count,
                            edges {
                                cursor,
                                node {
                                    id,
                                    __id,
                                    name,
                                    value
                                }                    
                            }
                        }
                    }
                }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            
            var expectedResult = @"
                                    {
                                      ""data"": {
                                        ""call"": {
                                          ""node"": {
                                            ""id"": ""H4sIAAAAAAAEAA3IOw6AIBBF0b28GhInKCKde7AzFnyGhAZIpCPuXbp77kCDvQcSLEIthUPPteB7BFzLc1789nOWQI6TyjOz0ockikmuTi3SJyJJ0ax6C2YnInw/CryWn1YAAAA="",
                                            ""__id"": ""3beee369-11df-4a30-bf11-1d8465c87111"",
                                            ""name"": ""hello world"",
                                            ""value"": 13.0
                                          },
                                          ""edge"": {
                                            ""cursor"": ""3beee369-11df-4a30-bf11-1d8465c87111"",
                                            ""node"": {
                                              ""id"": ""H4sIAAAAAAAEAA3IOw6AIBBF0b28GhInKCKde7AzFnyGhAZIpCPuXbp77kCDvQcSLEIthUPPteB7BFzLc1789nOWQI6TyjOz0ockikmuTi3SJyJJ0ax6C2YnInw/CryWn1YAAAA="",
                                              ""__id"": ""3beee369-11df-4a30-bf11-1d8465c87111"",
                                              ""name"": ""hello world"",
                                              ""value"": 13.0
                                            }
                                          },
                                          ""deletedId"": ""H4sIAAAAAAAEAA3IMQ6AIBAF0bv8WhI2KCqdd7ATC4QloUES6Qh3l27eNBSYqyHCwL85s6/pzej3BFfSmCd/9Rg1IYVBC/Uws9K7IApRzE5J8UQiQWGb9eK3lUhaoP+q2DHnWgAAAA=="",
                                          ""api"": {
                                            ""connection"": {
                                              ""count"": 1,
                                              ""edges"": [
                                                {
                                                  ""cursor"": ""3beee369-11df-4a30-bf11-1d8465c87111"",
                                                  ""node"": {
                                                    ""id"": ""H4sIAAAAAAAEAA3IOw6AIBBF0b28GhInKCKde7AzFnyGhAZIpCPuXbp77kCDvQcSLEIthUPPteB7BFzLc1789nOWQI6TyjOz0ockikmuTi3SJyJJ0ax6C2YnInw/CryWn1YAAAA="",
                                                    ""__id"": ""3beee369-11df-4a30-bf11-1d8465c87111"",
                                                    ""name"": ""hello world"",
                                                    ""value"": 13.0
                                                  }
                                                }
                                              ]
                                            }
                                          }
                                        }
                                      }
                                    }
                                ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing connection update request from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ConnectionMutationFaultedUpdateTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3BEEE369-11DF-4A30-BF11-1D8465C87110}"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{B500CA20-F649-4DCD-BDA8-1FA5031ECDD3}"),
                                                 Name = "2-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{67885BA0-B284-438F-8393-EE9A9EB299D1}"),
                                                 Name = "3-test",
                                                 Value = 50m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{3AF2C973-D985-4F95-A0C7-AA928D276881}"),
                                                 Name = "4-test",
                                                 Value = 70m
                                             },
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "{F0607502-5B77-4A3C-9142-E6197A7EE61E}"),
                                                 Name = "5-test",
                                                 Value = 6m
                                             },
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_connection_update(id: ""3BEEE369-11DF-4A30-BF11-1D8465C87111"", newNode: { name: ""hello world"", value: 13}) {
                    errors {
                        field,
                        message
                    },
                    node {
                        id,
                        __id,
                        name,
                        value
                    },
                    edge {
                       cursor,
                        node {
                            id,
                            __id,
                            name,
                            value
                        }
                    },
                    deletedId,
                    api {
                        connection(sort: [value_asc, name_asc], filter: {value: 13}) {
                            count,
                            edges {
                                cursor,
                                node {
                                    id,
                                    __id,
                                    name,
                                    value
                                }                    
                            }
                        }
                    }
                }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                                    {
                                      ""data"": {
                                        ""call"": {
                                          ""errors"": [
                                            {
                                              ""field"": null,
                                              ""message"": ""Update failed""
                                            },
                                            {
                                              ""field"": ""id"",
                                              ""message"": ""Node not found""
                                            }
                                          ],
                                          ""node"": null,
                                          ""edge"": null,
                                          ""deletedId"": null,
                                          ""api"": {
                                            ""connection"": {
                                              ""count"": 0,
                                              ""edges"": []
                                            }
                                          }
                                        }
                                      }
                                    }
                                ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing node requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task NodeQueryTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "67885ba0-b284-438f-8393-ee9a9eb299d1"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             }
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var globalId = ((JObject)JsonConvert
                                   .DeserializeObject("{\"p\":[{\"f\":\"connection\"}]"
                                                      + ",\"api\":\"TestApi\""
                                                      + ",\"id\":\"67885ba0-b284-438f-8393-ee9a9eb299d1\"}"))
                .PackGlobalId();

            var query = $@"
                {{
                    node(id: ""{globalId.Replace("\"", "\\\"")}"") 
                    {{
                    ...F0
                    }}
                }}

                fragment F0 on TestApi_TestObject_Node 
                {{
                    __id,
                    id,
                    name,
                    value
                }}            
            ";

            this.output.WriteLine(query);

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            var expectedResult = @"
                            {
                              ""data"": {
                                ""node"": {
                                  ""__id"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                  ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                  ""name"": ""1-test"",
                                  ""value"": 100.0
                                }
                              }
                            }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing nested node requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task NodeNestedQueryTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "67885ba0-b284-438f-8393-ee9a9eb299d1"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             }
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine)
                                         {
                                             UseJsonRepack =
                                                 true
                                         };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var globalId =
                ((JObject)
                    JsonConvert.DeserializeObject(
                        "{\"p\":[{\"f\":\"connection\"}]" + ",\"api\":\"TestApi\""
                        + ",\"id\":\"67885ba0-b284-438f-8393-ee9a9eb299d1\"}")).PackGlobalId();

            var query = $@"
                {{
                    api {{
                        
                        __node(id: ""{globalId.Replace("\"", "\\\"")}"") 
                        {{
                            ...F0
                        }}
                    }}
                }}

                fragment F0 on TestApi_TestObject_Node 
                {{
                    __id,
                    id,
                    name,
                    value
                }}            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            var expectedResult = @"
                            {
                              ""data"": {
                                ""api"": {
                                    ""__node"": {
                                      ""__id"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                      ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                      ""name"": ""1-test"",
                                      ""value"": 100.0
                                    }
                                }
                              }
                            }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task NodeQueryWithVariableTest()
        {
            var initialObjects = new List<TestObject>
                                     {
                                         new TestObject
                                             {
                                                 Id =
                                                     Guid.Parse(
                                                         "67885ba0-b284-438f-8393-ee9a9eb299d1"),
                                                 Name = "1-test",
                                                 Value = 100m
                                             }
                                     };

            var internalApiProvider = new TestProvider(initialObjects);
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            query ($id: ID){ 
                node(id: $id) {
                    ...F0
                }            
            }

            fragment F0 on TestApi_TestObject_Node {
                __id,
                id,
                name,
                value
            }            
            ";

            var globalId = ((JObject)JsonConvert
                                   .DeserializeObject("{\"p\":[{\"f\":\"connection\"}]"
                                                      + ",\"api\":\"TestApi\""
                                                      + ",\"id\":\"67885ba0-b284-438f-8393-ee9a9eb299d1\"}"))
                .PackGlobalId();

            var variables = $@"
            {{
                ""id"": ""{globalId}""
            }}
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                     r.Inputs = variables.ToInputs();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);
            var expectedResult = @"
                            {
                              ""data"": {
                                ""node"": {
                                  ""__id"": ""67885ba0-b284-438f-8393-ee9a9eb299d1"",
                                  ""id"": ""H4sIAAAAAAAEAKtWKlCyiq5WSlOyUkrOz8tLTS7JzM9Tqo3VUUosyAQKhqQWlzgCWTpKmSlArpm5hYVpUqKBbpKRhYmuibFFmq6FsaWxbmqqZaJlapKRpWWKoVItAH1tZJZWAAAA"",
                                  ""name"": ""1-test"",
                                  ""value"": 100.0
                                }
                              }
                            }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task MethodsRequestTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                    syncScalarMethod(intArg: 1, stringArg: ""test"", objArg: { syncScalarField: ""nested test"" }),
                    asyncObjectMethod(intArg: 1, stringArg: ""test"", objArg: { syncScalarField: ""nested test"" }, intArrayArg: [7, 8, 9]) {
                        syncScalarField,
                        asyncScalarField
                    },
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""syncScalarMethod"": ""ok"",
                              ""asyncObjectMethod"": {
                                ""syncScalarField"": ""returned type"",
                                ""asyncScalarField"": ""AsyncScalarField""
                              }
                            }
                          }
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task VariablesRequestTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });
            var query = @"
            query Request($intArg: Int, $stringArg: String, $intArrayArg: [Int], $objArg: TestApi_NestedProvider_Input){                
                api {
                    syncScalarMethod(intArg: $intArg, stringArg: $stringArg, objArg: $objArg),
                    asyncObjectMethod(intArg: $intArg, stringArg: $stringArg, intArrayArg: $intArrayArg, objArg: $objArg) {
                        syncScalarField,
                        asyncScalarField
                    },
                }
            }
            ";

            var variables = @"
            {
                ""intArg"": 1,
                ""stringArg"": ""test"",
                ""objArg"": { ""syncScalarField"": ""nested test"" },
                ""intArrayArg"": [7, 8, 9]
            }
            ";

            var inputs = variables.ToInputs();

            Action<ExecutionOptions> configure = r =>
                {
                    r.Schema = schema;
                    r.Query = query;
                    r.Inputs = inputs;
                    r.UserContext = new RequestContext();
                };

            var result = await new DocumentExecuter().ExecuteAsync(
                             configure).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""syncScalarMethod"": ""ok"",
                              ""asyncObjectMethod"": {
                                ""syncScalarField"": ""returned type"",
                                ""asyncScalarField"": ""AsyncScalarField""
                              }
                            }
                          }
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing call of simple mutation
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task MutationSimpleRequestTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_nestedAsync_setName(name: ""hello world"", clientMutationId: ""test client id"") {
                        result {
                            name
                        },
                        clientMutationId,
                        api {
                            syncScalarField
                        }
                    }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""call"": {
                                ""result"": {
                                    ""name"": ""hello world""
                                },
                                ""clientMutationId"": ""test client id"",
                                ""api"": {
                                    ""syncScalarField"": ""SyncScalarField""
                                }
                            }
                          }
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing correct schema generation from generated <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task SchemaGenerationTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };

            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var errors = SchemaGenerator.CheckSchema(schema).Select(e => $"Schema type error: {e}")
                .Union(SchemaGenerator.CheckSchemaIntrospection(schema)).Select(e => $"Schema introspection error: {e}");

            var hasErrors = false;
            foreach (var error in errors)
            {
                hasErrors = true;
                this.output.WriteLine(error);
            }

            using (var printer = new SchemaPrinter(schema))
            {
                var description = printer.Print();
                this.output.WriteLine("-------- Schema -----------");
                this.output.WriteLine(description);
                Assert.False(string.IsNullOrWhiteSpace(description));
            }

            Assert.NotNull(schema.Query);
            Assert.Equal(2, schema.Query.Fields.Count());
            Assert.True(schema.Query.HasField("api"));

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = Resources.IntrospectionQuery;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            Assert.False(hasErrors);
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task SimpleFieldsRequestTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                    asyncArrayOfScalarField,
                    asyncForwardedScalar,
                    nestedAsync {
                        asyncScalarField,
                        syncScalarField                        
                    },
                    asyncScalarField,
                    faultedSyncField,
                    forwardedArray,
                    syncArrayOfScalarField,
                    nestedSync {
                        asyncScalarField,
                        syncScalarField  
                    },
                    syncScalarField,
                    faultedASyncMethod {
                        asyncScalarField,
                        syncScalarField 
                    },
                    syncEnumField,
                    syncFlagsField
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""asyncArrayOfScalarField"": [
                                4.0,
                                5.0
                              ],
                              ""asyncForwardedScalar"": ""AsyncForwardedScalar"",
                              ""nestedAsync"": {
                                ""asyncScalarField"": ""AsyncScalarField"",
                                ""syncScalarField"": ""SyncScalarField""
                              },
                              ""asyncScalarField"": ""AsyncScalarField"",
                              ""faultedSyncField"": null,
                              ""forwardedArray"": [
                                5,
                                6,
                                7
                              ],
                              ""syncArrayOfScalarField"": [
                                1,
                                2,
                                3
                              ],
                              ""nestedSync"": {
                                ""asyncScalarField"": ""AsyncScalarField"",
                                ""syncScalarField"": ""SyncScalarField""
                              },
                              ""syncScalarField"": ""SyncScalarField"",
                              ""faultedASyncMethod"": {
                                ""asyncScalarField"": null,
                                ""syncScalarField"": null
                              },
                              ""syncEnumField"": ""EnumItem1"",
                              ""syncFlagsField"": 1,
                            }
                          }
                        }
                        ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task DateTimeTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                    dateTimeField,
                    dateTimeOffsetField,
                    dateTimeMethod(date: ""1980-09-25T10:00:00Z""),
                    dateTimeOffsetMethod(date: ""1980-09-25T10:00:00Z"")
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                             {
                                 r.Schema = schema;
                                 r.Query = query;
                                 r.UserContext = new RequestContext();
                             }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""dateTimeField"": ""1980-09-25T10:00:00Z"",                             
                              ""dateTimeOffsetField"": ""1980-09-25T10:00:00Z"",                             
                              ""dateTimeMethod"": true,                             
                              ""dateTimeOffsetMethod"": true                           
                            }
                          }
                        }
                        ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }
        
        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task ErrorHandlingTest()
        {
            var internalApiProvider = new TestProvider();
            var faultingProvider = new FaultingProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var successProvider = new DirectProvider(new AdditionalProvider(), this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { faultingProvider, successProvider });

            var faultingQuery = @"
            {                
                api {
                    syncScalarField,
                    helloWorld
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                             {
                                 r.Schema = schema;
                                 r.Query = faultingQuery;
                                 r.UserContext = new RequestContext();
                             }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": null,
                          ""errors"": [
                            {
                              ""message"": ""Error trying to resolve api."",
                              ""locations"": [
                                {
                                  ""line"": 3,
                                  ""column"": 17
                                }
                              ]
                            }
                          ]
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
            
            var successQuery = @"
            {                
                api {                    
                    helloWorld
                }
            }
            ";

            result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                             {
                                 r.Schema = schema;
                                 r.Query = successQuery;
                                 r.UserContext = new RequestContext();
                             }).ConfigureAwait(true);
            response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            expectedResult = @"
                        {
                          ""data"": {
                                ""api"": {
                                ""helloWorld"": ""Hello world""
                                }
                            }
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Checking the work of <see cref="DeclareFieldAttribute.Access"/>
        /// </summary>
        /// <returns>The async task</returns>
        [Fact]
        public async Task FieldAccessTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });
            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                             {
                                 r.Schema = schema;
                                 r.Query = Queries.IntrospectionQuery;
                                 r.UserContext = new RequestContext();
                             }).ConfigureAwait(true);
            var response = JObject.Parse(new DocumentWriter(true).Write(result));
            Assert.NotNull(response);

            var queryType = response.SelectToken("$.data.__schema.types[?(@.name == 'TestApi_NestedProvider')]");
            Assert.NotNull(queryType);
            Assert.Null(queryType.SelectToken("fields[?(@.name == 'argumentField')]"));
            Assert.NotNull(queryType.SelectToken("fields[?(@.name == 'readOnlyField')]"));

            var inputType = response.SelectToken("$.data.__schema.types[?(@.name == 'TestApi_NestedProvider_Input')]");
            Assert.NotNull(inputType);
            Assert.NotNull(inputType.SelectToken("inputFields[?(@.name == 'argumentField')]"));
            Assert.Null(inputType.SelectToken("inputFields[?(@.name == 'readOnlyField')]"));
        }

        /// <summary>
        /// If API defines method that has no published parameters, the mutations declared in method result should be accessable
        /// </summary>
        /// <returns>The async task</returns>
        [Fact]
        public async Task ObjectMethodAsFieldSubMutationTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"                          
            mutation M {
                    call: TestApi_asyncObjectMethodAsField_setName(name: ""hello world"", clientMutationId: ""test client id"") {
                        result {
                            name
                        },
                        clientMutationId,
                        api {
                            syncScalarField
                        }
                    }
            }            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                             {
                                 r.Schema = schema;
                                 r.Query = query;
                                 r.UserContext = new RequestContext();
                             }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""call"": {
                                ""result"": {
                                    ""name"": ""hello world""
                                },
                                ""clientMutationId"": ""test client id"",
                                ""api"": {
                                    ""syncScalarField"": ""SyncScalarField""
                                }
                            }
                          }
                        }";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact(Skip = "Core lib problem")]
        public async Task SimpleFieldsMergeWithRootTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                    asyncArrayOfScalarField,
                    asyncForwardedScalar,
                    nestedAsync {
                        asyncScalarField
                    },
                    nestedAsync {
                        syncScalarField                        
                    },
                    asyncScalarField,
                    faultedSyncField,
                    forwardedArray,
                    syncArrayOfScalarField
                    
                }

                api {
                    nestedSync {
                        asyncScalarField,
                        syncScalarField  
                    },
                    syncScalarField,
                    faultedASyncMethod {
                        asyncScalarField,
                        syncScalarField 
                    },
                    syncEnumField,
                    syncFlagsField
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""asyncArrayOfScalarField"": [
                                4.0,
                                5.0
                              ],
                              ""asyncForwardedScalar"": ""AsyncForwardedScalar"",
                              ""nestedAsync"": {
                                ""asyncScalarField"": ""AsyncScalarField"",
                                ""syncScalarField"": ""SyncScalarField""
                              },
                              ""asyncScalarField"": ""AsyncScalarField"",
                              ""faultedSyncField"": null,
                              ""forwardedArray"": [
                                5,
                                6,
                                7
                              ],
                              ""syncArrayOfScalarField"": [
                                1,
                                2,
                                3
                              ],
                              ""nestedSync"": {
                                ""asyncScalarField"": ""AsyncScalarField"",
                                ""syncScalarField"": ""SyncScalarField""
                              },
                              ""syncScalarField"": ""SyncScalarField"",
                              ""faultedASyncMethod"": {
                                ""asyncScalarField"": null,
                                ""syncScalarField"": null
                              },
                              ""syncEnumField"": ""EnumItem1"",
                              ""syncFlagsField"": 1,
                            }
                          }
                        }
                        ";

            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing simple fields requests from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task SimpleFieldsMergeTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            {                
                api {
                    asyncArrayOfScalarField,
                    asyncForwardedScalar,
                    nestedAsync {
                        asyncScalarField
                    },
                    nestedAsync {
                        syncScalarField                        
                    },
                    asyncScalarField,
                    faultedSyncField,
                    forwardedArray,
                    syncArrayOfScalarField                    
                }                
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""asyncArrayOfScalarField"": [
                                4.0,
                                5.0
                              ],
                              ""asyncForwardedScalar"": ""AsyncForwardedScalar"",
                              ""nestedAsync"": {
                                ""asyncScalarField"": ""AsyncScalarField"",
                                ""syncScalarField"": ""SyncScalarField""
                              },
                              ""asyncScalarField"": ""AsyncScalarField"",
                              ""faultedSyncField"": null,
                              ""forwardedArray"": [
                                5,
                                6,
                                7
                              ],
                              ""syncArrayOfScalarField"": [
                                1,
                                2,
                                3
                              ]
                            }
                          }
                        }
                        ";

            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing requests with use of fragments from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task RequestWithFragmentsTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            query FragmentsRequest {                
                api {
                   ...F
                }
            }

            fragment F on TestApi_TestApi {
                    syncScalarField
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""syncScalarField"": ""SyncScalarField""
                            }
                          }
                        }
                        ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing requests with use of anonymous fragments from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task RequestWithFragmentsAnonymousTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            query FragmentsRequest {                
                api {
                   ...on TestApi_TestApi {
                    syncScalarField
                    }
                }
            }
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""syncScalarField"": ""SyncScalarField""
                            }
                          }
                        }
                        ";
            Assert.Equal(CleanResponse(expectedResult), CleanResponse(response));
        }

        /// <summary>
        /// Testing requests with use of fragments from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task RecursiveFieldsTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            query FragmentsRequest {                
                api {
                   recursion {
                        recursion {
                            recursion {
                                syncScalarField
                            }
                        }
                   }
                }
            }
            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                                ""recursion"" : {
                                    ""recursion"" : {
                                        ""recursion"" : {
                                            ""syncScalarField"" : ""SyncScalarField""                               
                                        }
                                    }
                                }
                              }
                           }
                         }                        
                        ";

            Assert.Equal(
                CleanResponse(expectedResult),
                CleanResponse(response));
        }

        /// <summary>
        /// Testing requests with use of fragments from <see cref="ApiDescription"/>
        /// </summary>
        /// <returns>Async task</returns>
        [Fact]
        public async Task RequestWithAliasesTest()
        {
            var internalApiProvider = new TestProvider();
            var publishingProvider = new DirectProvider(internalApiProvider, this.output.WriteLine) { UseJsonRepack = true };
            var schema = SchemaGenerator.Generate(new List<ApiProvider> { publishingProvider });

            var query = @"
            query FragmentsRequest {                
                api {
                   test1: syncScalarField,
                   test2: syncScalarField,
                   nestedSync {
                        test3: syncScalarField,
                        test4: syncScalarField
                   }
                }
            }
            
            ";

            var result = await new DocumentExecuter().ExecuteAsync(
                             r =>
                                 {
                                     r.Schema = schema;
                                     r.Query = query;
                                     r.UserContext = new RequestContext();
                                 }).ConfigureAwait(true);
            var response = new DocumentWriter(true).Write(result);
            this.output.WriteLine(response);

            var expectedResult = @"
                        {
                          ""data"": {
                            ""api"": {
                              ""test1"": ""SyncScalarField"",
                              ""test2"": ""SyncScalarField"",
                              ""nestedSync"": {
                                  ""test3"": ""SyncScalarField"",
                                  ""test4"": ""SyncScalarField""
                              }
                            }
                          }
                        }
                        ";

            Assert.Equal(
                CleanResponse(expectedResult), 
                CleanResponse(response));
        }

        /// <summary>
        /// An additional provider
        /// </summary>
        [ApiDescription(Name = "AdditionalApi")]
        public class AdditionalProvider : API.Provider.ApiProvider
        {
            /// <summary>
            /// Published string
            /// </summary>
            [DeclareField]
            public string HelloWorld => "Hello world";
        }

        /// <summary>
        /// The provider that throws an exception during resolve
        /// </summary>
        private class FaultingProvider : DirectProvider
        {
            /// <inheritdoc />
            public FaultingProvider(API.Provider.ApiProvider provider, Action<string> errorOutput)
                : base(provider, errorOutput)
            {
            }

            /// <inheritdoc />
            public override async Task<JObject> GetData(List<ApiRequest> requests, RequestContext context)
            {
                await base.GetData(requests, context);
                throw new Exception("Test exception");
            }

            /// <inheritdoc />
            public override async Task<JObject> SearchNode(string id, List<RequestPathElement> path, ApiRequest nodeRequest, RequestContext context)
            {
                await base.SearchNode(id, path, nodeRequest, context);
                throw new Exception("Test exception");
            }
        }
    }
}