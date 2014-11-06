using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Owin;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Authorization;

namespace SignalR.MagicHub.Tests.Authentication
{
    [TestFixture]
    public class AuthorizeAttributeFixture
    {
        #region Helper classes and methods

        public class MockHubInvoker : IHubIncomingInvokerContext
        {
            public IHub Hub { get; set; }
            public MethodDescriptor MethodDescriptor { get; set; }
            public IList<object> Args { get; set; }
            public StateChangeTracker StateTracker { get; set; }
        }

        public class MockHub : IHub
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task OnConnected()
            {
                throw new NotImplementedException();
            }

            public Task OnReconnected()
            {
                throw new NotImplementedException();
            }

            public Task OnDisconnected()
            {
                throw new NotImplementedException();
            }

            public HubCallerContext Context { get; set; }
            public IHubCallerConnectionContext Clients { get; set; }
            public IGroupManager Groups { get; set; }
        }


        private GenericPrincipal GetPrincipal(bool authenticated)
        {
            return new GenericPrincipal(new GenericIdentity(authenticated ? "user" : "", "user"), new string[0]);
        }

        private HubCallerContext GetHubCallerContext(bool authenticatedUser)
        {
            return new HubCallerContext(
                new ServerRequest(
                    new Dictionary<string, object>()
                        {
                            {
                                "server.User", GetPrincipal(authenticatedUser)
                            }
                        }),
                string.Empty);
        }

        private MockHubInvoker GetHubInvoker(bool authorizedUser, params object[] methodArgs)
        {
            return new MockHubInvoker
                {
                    Args = new List<object>(methodArgs),
                    Hub = new MockHub
                        {
                            Context = GetHubCallerContext(authorizedUser)
                        },
                    MethodDescriptor = new MethodDescriptor()
                };
        }

        #endregion

        private Mock<IAuthorize> _mockFooBarAuthorizer;

        [SetUp]
        public void Setup()
        {
            AuthorizeAttribute.IsAnonymousEnabled = false;
            _mockFooBarAuthorizer = new Mock<IAuthorize>();
            _mockFooBarAuthorizer.Setup(a => a.HasClaim(
                                            It.Is<AuthorizationContext>(c => c.Resource == "Foo" || c.Resource == "Bar")))
                                 .Returns(true);
                
        }

        [Test]
        public void Test_ExtensionMethod()
        {
            // Act
            GlobalHost.Configuration.AllowAnonymous();

            // Assert
            Assert.IsTrue(AuthorizeAttribute.IsAnonymousEnabled);
        }

        
        #region Authentication

        [Test]
        public void Test_AuthenticationFail()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute();

            MockHub hub = new MockHub
                {
                    Context = GetHubCallerContext(false)
                };


            // Assert
            Assert.IsFalse(attr.AuthorizeHubConnection(new HubDescriptor(), hub.Context.Request));

            Assert.IsFalse(attr.AuthorizeHubMethodInvocation(new MockHubInvoker() { Hub = hub }, false));
        }

        [Test]
        public void Test_AuthenticationSuccess()
        {
            // Arrange
            MockHub hub = new MockHub
                {
                    Context = GetHubCallerContext(true)
                };

            AuthorizeAttribute attr = new AuthorizeAttribute();
            
            // Assert
            Assert.IsTrue(attr.AuthorizeHubMethodInvocation(new MockHubInvoker() { Hub = hub }, false));
        }

        [Test]
        public void Test_AuthenticationAnonymous()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute();

            MockHub hub = new MockHub
                {
                    Context = GetHubCallerContext(false)
                };

            // Act
            AuthorizeAttribute.IsAnonymousEnabled = true;

            // Assert
            Assert.IsTrue(attr.AuthorizeHubConnection(new HubDescriptor(), hub.Context.Request));
            Assert.IsTrue(attr.AuthorizeHubMethodInvocation(new MockHubInvoker() { Hub = hub }, false));
        }

        #endregion

        #region Authorization

        [Test]
        public void Test_Authorizate_with_implicit_key()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(_mockFooBarAuthorizer.Object) { PermissionType = "Read" };

            // Act
            bool test1 = attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, "Foo"), true);

            // Assert
            Assert.IsTrue(test1);

        }

        [Test]
        public void Test_Authorizate_with_implicit_key_invalid_topic()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(_mockFooBarAuthorizer.Object) { PermissionType = "Read" };
            
            // Act
            bool test2 = attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, "Baz"), true);

            // Assert
            Assert.IsFalse(test2);
        }

        [Test]
        public void Test_Authorize_with_explicit_key()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(_mockFooBarAuthorizer.Object)
            {
                PermissionType = "Read",
                ResourceIndices = new int[] { 1, 3 }
            };

            // Act
            bool test1 = attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, "Baz", "Foo", "Baz", "Bar"), true);

            // Assert
            Assert.IsTrue(test1);
        }

        [Test]
        public void Test_Authorize_with_explicit_key_invalid_topics()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(_mockFooBarAuthorizer.Object)
            {
                PermissionType = "Read",
                ResourceIndices = new int[] { 1, 3 }
            };

            // Act
            bool test2 = attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, "Baz", "Foo", "Bar", "Boo"), true);

            // Assert
            Assert.IsFalse(test2);
        }

        [Test]
        public void Test_Authorize_with_no_authorizer()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(null) { PermissionType = "Read" };

            // Act
            bool test = attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, "Baz"), true);

            // Assert
            // This test was false before, but now there is no authorizer, so it should be true.
            Assert.IsTrue(test);
        }

        [Test]
        public void Test_Authorize_when_no_resource_key_is_found()
        {
            // Arrange
            AuthorizeAttribute attr = new AuthorizeAttribute(_mockFooBarAuthorizer.Object) { PermissionType = "Read" };

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() =>
                                                     attr.AuthorizeHubMethodInvocation(GetHubInvoker(true, ""), true));
        }


        [Test]
        public void Test_whitelisted_key()
        {
            // Arrange 
            Mock<IAuthorize> authMock = new Mock<IAuthorize>();
            AuthorizeAttribute attr = new AuthorizeAttribute(authMock.Object)
                {
                    PermissionType = "Read",
                    WhiteList = new string[] { "Foo2" }
                };

            authMock.Setup(a => a.HasClaim(It.IsAny<AuthorizationContext>())).Returns(false);

            // Act 
            bool ret = attr.AuthorizeHubMethodInvocation(
                new MockHubInvoker
                    {
                        Args = new List<object> { "Foo2" },
                        Hub = new MockHub
                            {
                                Context = GetHubCallerContext(true)
                            },
                        MethodDescriptor = new MethodDescriptor()
                    },
                true);
            ;


            // Assert
            Assert.That(ret, Is.True);
            authMock.Verify(a => a.HasClaim(It.IsAny<AuthorizationContext>()), Times.Never());
        }


        [Test]
        public void Test_blacklisted_key()
        {
            // Arrange 
            Mock<IAuthorize> authMock = new Mock<IAuthorize>();
            AuthorizeAttribute attr = new AuthorizeAttribute(authMock.Object)
            {
                PermissionType = "Read",
                Blacklist = new string[] { "Foo2" }
            };

            // authorization allows everything
            authMock.Setup(a => a.HasClaim(It.IsAny<AuthorizationContext>())).Returns(true);

            // Act 
            bool ret = attr.AuthorizeHubMethodInvocation(
                new MockHubInvoker
                {
                    Args = new List<object> { "Foo2" },
                    Hub = new MockHub
                    {
                        Context = GetHubCallerContext(true)
                    },
                    MethodDescriptor = new MethodDescriptor()
                },
                true);
            ;


            // Assert
            Assert.That(ret, Is.False);
            authMock.Verify(a => a.HasClaim(It.IsAny<AuthorizationContext>()), Times.Never());
        }

        #endregion

    }
}
