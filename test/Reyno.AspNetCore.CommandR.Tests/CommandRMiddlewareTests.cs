using System;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Reyno.AspNetCore.CommandR.Tests {
    public class CommandRMiddlewareTests {
        [Fact]
        public void IsCommandRRequest_Returns_True() {

            var middleware = new CommandRMiddleware(null);
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString("/_commandr/bob");

            var result = middleware.IsCommandRRequest(context, "_commandr");

            Assert.True(result);

        }

        [Fact]
        public void IsCommandRRequest_Returns_False() {

            var middleware = new CommandRMiddleware(null);
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString("/_commandr/bob");

            var result = middleware.IsCommandRRequest(context, "commandr");

            Assert.False(result);

        }

        [Fact]
        public void GetCommandName_Throws_Exception() {

            var middleware = new CommandRMiddleware(null);
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString("/_commandr");

            Assert.Throws<ArgumentException>(() => middleware.GetCommandName(context));

        }

        [Theory]
        [InlineData("/_commandr/my.command")]
        [InlineData("/anythingcancgohere/my.command")]
        [InlineData("/_commandr/my.command/moretheend")]
        [InlineData("/_commandr/my.command?with=something")]
        public void GetCommandName_Returns_Success(string path) {

            var middleware = new CommandRMiddleware(null);
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(path);

            var result = middleware.GetCommandName(context);

            Assert.Equal("my.command", result);

        }


    }
}
