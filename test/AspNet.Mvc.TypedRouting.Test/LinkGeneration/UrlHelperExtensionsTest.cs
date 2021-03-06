﻿#if NET451

namespace AspNet.Mvc.TypedRouting.Test.LinkGeneration
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using Microsoft.AspNetCore.Builder.Internal;
    using Xunit;

    using With = Microsoft.AspNetCore.Mvc.With;

    [Collection("TypedRoutingTests")]
    public class UrlHelperExtensionsTest
    {
        [Fact]
        public void UrlActionWithExpressionAndAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action<NormalController>(c => c.ActionWithoutParameters(),
                values: null,
                protocol: "https",
                host: "remotelyhost",
                fragment: "somefragment");

            // Assert
            Assert.Equal("https://remotelyhost/app/Normal/ActionWithoutParameters#somefragment", url);
        }

        [Fact]
        public void UrlActionWithExpressionActionWithParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action<NormalController>(c => c.ActionWithParameters(1, "sometext"));

            // Assert
            Assert.Equal("/app/Normal/ActionWithParameters/1?text=sometext", url);
        }

        [Fact]
        public void UrlActionWithExpressionActionWithParametersAndAdditionalValues_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action<NormalController>(c => c.ActionWithParameters(1, "sometext"), new { text = "othertext" });

            // Assert
            Assert.Equal("/app/Normal/ActionWithParameters/1?text=othertext", url);
        }

        [Fact]
        public void UrlActionWithExpressionActionWithNoParameterssAndAdditionalValues_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Action<NormalController>(c => c.ActionWithParameters(With.No<int>(), With.No<string>()), new { id = 1, text = "othertext" });

            // Assert
            Assert.Equal("/app/Normal/ActionWithParameters/1?text=othertext", url);
        }

        [Fact]
        public void LinkWithAllParameters_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link<NormalController>("namedroute", c => c.ActionWithParameters(1, "sometext"));

            // Assert
            Assert.Equal("http://localhost/app/named/Normal/ActionWithParameters/1?text=sometext", url);
        }

        [Fact]
        public void LinkWithNullRouteName_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link<NormalController>(null, c => c.ActionWithParameters(1, "sometext"));

            // Assert
            Assert.Equal("http://localhost/app/Normal/ActionWithParameters/1?text=sometext", url);
        }

        [Fact]
        public void LinkWithAdditionalRouteValues_ReturnsExpectedResult()
        {
            // Arrange
            var services = GetServices();
            var urlHelper = CreateUrlHelperWithRouteCollection(services, "/app");

            // Act
            var url = urlHelper.Link<NormalController>(null, c => c.ActionWithParameters(1, "sometext"), new { text = "othertext" });

            // Assert
            Assert.Equal("http://localhost/app/Normal/ActionWithParameters/1?text=othertext", url);
        }

        private static HttpContext CreateHttpContext(
            IServiceProvider services,
            string appRoot)
        {
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            context.Request.PathBase = new PathString(appRoot);
            context.Request.Host = new HostString("localhost");

            return context;
        }

        private static ActionContext CreateActionContext(HttpContext context)
        {
            return CreateActionContext(context, (new Mock<IRouter>()).Object);
        }

        private static ActionContext CreateActionContext(HttpContext context, IRouter router)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(router);

            return new ActionContext(context, routeData, new ActionDescriptor());
        }

        private static IServiceProvider GetServices()
        {
            var services = new Mock<IServiceProvider>();

            var optionsAccessor = new Mock<IOptions<RouteOptions>>();
            optionsAccessor
                .SetupGet(o => o.Value)
                .Returns(new RouteOptions());
            services
                .Setup(s => s.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);

            services
                .Setup(s => s.GetService(typeof(IInlineConstraintResolver)))
                .Returns(new DefaultInlineConstraintResolver(optionsAccessor.Object));

            services
                .Setup(s => s.GetService(typeof(ILoggerFactory)))
                .Returns(new LoggerFactory());

            services
                .Setup(s => s.GetService(typeof(IActionContextAccessor)))
                .Returns(new ActionContextAccessor()
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = new DefaultHttpContext()
                        {
                            RequestServices = services.Object,
                        },
                        RouteData = new RouteData(),
                    },
                });

            return services.Object;
        }

        private static UrlHelper CreateUrlHelperWithRouteCollection(IServiceProvider services, string appPrefix)
        {
            var routeCollection = GetRouter(services);
            return CreateUrlHelper(appPrefix, routeCollection);
        }

        private static IRouter GetRouter(IServiceProvider services)
        {
            return GetRouter(services, "mockRoute", "/mockTemplate");
        }

        private static IRouter GetRouter(
            IServiceProvider services,
            string mockRouteName,
            string mockTemplateValue)
        {
            var applicationBuilder = new ApplicationBuilder(services);
            var routeBuilder = new RouteBuilder(applicationBuilder);

            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(router => router.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Returns<VirtualPathContext>(context => null);
            routeBuilder.DefaultHandler = target.Object;

            routeBuilder.MapRoute(string.Empty,
                        "{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));

            routeBuilder.MapRoute("namedroute",
                        "named/{controller}/{action}/{id}",
                        new RouteValueDictionary(new { id = "defaultid" }));

            var mockHttpRoute = new Mock<IRouter>();
            mockHttpRoute
                .Setup(mock => mock.GetVirtualPath(It.Is<VirtualPathContext>(c => string.Equals(c.RouteName, mockRouteName))))
                .Returns(new VirtualPathData(mockHttpRoute.Object, mockTemplateValue));

            routeBuilder.Routes.Add(mockHttpRoute.Object);
            return routeBuilder.Build();
        }

        private static UrlHelper CreateUrlHelper(string appBase, IRouter router)
        {
            var services = GetServices();
            var context = CreateHttpContext(services, appBase);
            var actionContext = CreateActionContext(context, router);
            
            return new UrlHelper(actionContext);
        }

        private static IUrlHelper CreateUrlHelper()
        {
            var services = GetServices();
            var context = CreateHttpContext(services, string.Empty);
            var actionContext = CreateActionContext(context);
            
            return new UrlHelper(actionContext);
        }
    }
    
    public class NormalController : Controller
    {
        public IActionResult ActionWithoutParameters()
        {
            return null;
        }

        public IActionResult ActionWithParameters(int id, string text)
        {
            return null;
        }
    }

    internal class LoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger();
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }

    internal class Logger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable();
        }
    }

    internal class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

#endif