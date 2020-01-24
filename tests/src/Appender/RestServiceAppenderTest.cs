using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class RestServiceAppenderTest
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler;

        public void TestFixtureSetup()
        {
            mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHttpMessageHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK
               })
               .Verifiable();
        }

        [Test]
        public void NoBufferingTest()
        {
            TestFixtureSetup();

            string dummyRestEndpoint = "http://test.log4net.apache.org/";

            HttpClient mockHttpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(dummyRestEndpoint),
            };


            RestServiceAppender restServiceAppender = new RestServiceAppender(mockHttpClient);
            restServiceAppender.LoggingEndpoint = dummyRestEndpoint;
            restServiceAppender.HttpMethod = "POST";
            restServiceAppender.Name = "RestServiceAppender";
            restServiceAppender.ContentType = "application/json";
            restServiceAppender.ActivateOptions();

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            BasicConfigurator.Configure(rep, restServiceAppender);

            ILog log = LogManager.GetLogger(rep.Name, "NoBufferingTest");
            log.Debug("Message");

            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post  // we expected a POST request
                        && req.RequestUri == new Uri(dummyRestEndpoint)), //to this rest service
                            ItExpr.IsAny<CancellationToken>() 
                );

            
        }
    }
}
