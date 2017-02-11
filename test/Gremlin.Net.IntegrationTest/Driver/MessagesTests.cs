﻿#region License
/*
 * Copyright 2016 Florian Hockmann
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Driver.Messages;
using Gremlin.Net.Driver.Messages.Standard;
using Gremlin.Net.IntegrationTest.Util;
using Xunit;

namespace Gremlin.Net.IntegrationTest.Driver
{
    public class MessagesTests
    {
        private readonly ScriptRequestMessageProvider _requestMessageProvider = new ScriptRequestMessageProvider();
        private static readonly string TestHost = ConfigProvider.Configuration["TestServerIpAddress"];
        private static readonly int TestPort = Convert.ToInt32(ConfigProvider.Configuration["TestServerPort"]);

        [Fact]
        public async Task AliasForTraversalSourceShouldBeUsed()
        {
            var gremlinServer = new GremlinServer(TestHost, TestPort);
            using (var gremlinClient = new GremlinClient(gremlinServer))
            {
                var aliasTraversalSource = "g2";
                var aliases = new Dictionary<string, string> {{aliasTraversalSource, "g"}};
                var gremlinScript = $"{aliasTraversalSource}.V().count()";
                var requestMsg = new ScriptRequestMessage
                {
                    Arguments = new ScriptRequestArguments {Aliases = aliases, GremlinScript = gremlinScript}
                };

                var result = await gremlinClient.SubmitWithSingleResultAsync<long>(requestMsg);

                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task InvalidOperationShouldThrowException()
        {
            var gremlinServer = new GremlinServer(TestHost, TestPort);
            using (var gremlinClient = new GremlinClient(gremlinServer))
            {
                var ivalidOperationName = "invalid";
                var requestMsg = new TestMessage(ivalidOperationName);

                var thrownException =
                    await Assert.ThrowsAsync<ResponseException>(() => gremlinClient.SubmitAsync<dynamic>(requestMsg));

                Assert.Contains("MalformedRequest", thrownException.Message);
                Assert.Contains(ivalidOperationName, thrownException.Message);
            }
        }

        [Fact]
        public async Task InvalidProcessorShouldThrowException()
        {
            var gremlinServer = new GremlinServer(TestHost, TestPort);
            using (var gremlinClient = new GremlinClient(gremlinServer))
            {
                var invalidProcessorName = "invalid";
                var requestMsg = new TestMessage(processor: invalidProcessorName);

                var thrownException =
                    await Assert.ThrowsAsync<ResponseException>(() => gremlinClient.SubmitAsync<dynamic>(requestMsg));

                Assert.Contains("InvalidRequestArguments", thrownException.Message);
                Assert.Contains(invalidProcessorName, thrownException.Message);
                Assert.Contains("OpProcessor", thrownException.Message);
            }
        }

        [Fact]
        public async Task ScriptEvaluationTimeoutShouldBeConfigurable()
        {
            var gremlinServer = new GremlinServer(TestHost, TestPort);
            using (var gremlinClient = new GremlinClient(gremlinServer))
            {
                const int timeOutInMs = 1;
                const int scriptSleepTimeInMs = 5000;
                var requestMsg = _requestMessageProvider.GetSleepMessage(scriptSleepTimeInMs);
                requestMsg.Arguments.ScriptEvaluationTimeoutInMs = timeOutInMs;
                var evaluationStopWatch = new Stopwatch();
                evaluationStopWatch.Start();

                var thrownException =
                    await Assert.ThrowsAsync<ResponseException>(() => gremlinClient.SubmitAsync(requestMsg));

                evaluationStopWatch.Stop();
                Assert.Contains("ServerTimeout", thrownException.Message);
                Assert.Contains(timeOutInMs.ToString(), thrownException.Message);
                Assert.True(evaluationStopWatch.ElapsedMilliseconds < scriptSleepTimeInMs);
            }
        }

        [Fact]
        public async Task UnsupportedLanguageShouldThrowException()
        {
            var gremlinServer = new GremlinServer(TestHost, TestPort);
            using (var gremlinClient = new GremlinClient(gremlinServer))
            {
                var unknownLanguage = "unknown";
                var requestMsg = _requestMessageProvider.GetDummyMessage();
                requestMsg.Arguments.Language = unknownLanguage;

                var thrownException =
                    await Assert.ThrowsAsync<ResponseException>(() => gremlinClient.SubmitAsync(requestMsg));

                Assert.Contains("ScriptEvaluationError", thrownException.Message);
                Assert.Contains(unknownLanguage, thrownException.Message);
                Assert.Contains("Language", thrownException.Message);
            }
        }
    }

    internal class TestMessage : RequestMessage
    {
        public override string Operation { get; }
        public override string Processor { get; }

        public TestMessage(string operation = "eval", string processor = "")
        {
            Operation = operation;
            Processor = processor;
        }
    }
}