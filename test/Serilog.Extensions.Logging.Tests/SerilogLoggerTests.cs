﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Debugging;
using Xunit;

namespace Serilog.Extensions.Logging.Test
{
    public class SerilogLoggerTest
    {
        private const string Name = "test";
        private const string TestMessage = "This is a test";

        private Tuple<SerilogLogger, SerilogSink> SetUp(LogLevel logLevel)
        {
            var sink = new SerilogSink();

            var config = new LoggerConfiguration()
                .WriteTo.Sink(sink);

            SetMinLevel(config, logLevel);

            var provider = new SerilogLoggerProvider(config.CreateLogger());
            var logger = (SerilogLogger)provider.CreateLogger(Name);

            return new Tuple<SerilogLogger, SerilogSink>(logger, sink);
        }

        private void SetMinLevel(LoggerConfiguration serilog, LogLevel logLevel)
        {
            serilog.MinimumLevel.Is(MapLevel(logLevel));
        }

        private LogEventLevel MapLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Verbose;
            }
        }

        [Fact]
        public void LogsWhenNullFilterGiven()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(LogLevel.Information, 0, TestMessage, null, null);

            Assert.Equal(1, sink.Writes.Count);
        }

        [Fact]
        public void LogsCorrectLevel()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(LogLevel.Trace, 0, TestMessage, null, null);
            logger.Log(LogLevel.Debug, 0, TestMessage, null, null);
            logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            logger.Log(LogLevel.Warning, 0, TestMessage, null, null);
            logger.Log(LogLevel.Error, 0, TestMessage, null, null);
            logger.Log(LogLevel.Critical, 0, TestMessage, null, null);

            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(LogEventLevel.Verbose, sink.Writes[0].Level);
            Assert.Equal(LogEventLevel.Debug, sink.Writes[1].Level);
            Assert.Equal(LogEventLevel.Information, sink.Writes[2].Level);
            Assert.Equal(LogEventLevel.Warning, sink.Writes[3].Level);
            Assert.Equal(LogEventLevel.Error, sink.Writes[4].Level);
            Assert.Equal(LogEventLevel.Fatal, sink.Writes[5].Level);
        }

        [Theory]
        [InlineData(LogLevel.Trace, LogLevel.Trace, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Debug, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Information, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Error, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Debug, LogLevel.Debug, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Information, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Error, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Information, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Information, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Information, LogLevel.Information, 1)]
        [InlineData(LogLevel.Information, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Information, LogLevel.Error, 1)]
        [InlineData(LogLevel.Information, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Information, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Error, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Error, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Error, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Error, LogLevel.Information, 0)]
        [InlineData(LogLevel.Error, LogLevel.Warning, 0)]
        [InlineData(LogLevel.Error, LogLevel.Error, 1)]
        [InlineData(LogLevel.Error, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Critical, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Information, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Warning, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Error, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Critical, 1)]
        public void LogsWhenEnabled(LogLevel minLevel, LogLevel logLevel, int expected)
        {
            var t = SetUp(minLevel);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(logLevel, 0, TestMessage, null, null);

            Assert.Equal(expected, sink.Writes.Count);
        }

        [Fact]
        public void LogsCorrectMessage()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log<object>(LogLevel.Information, 0, null, null, null);
            logger.Log(LogLevel.Information, 0, TestMessage, null, null);

            Assert.Equal(1, sink.Writes.Count);
            Assert.Equal(TestMessage, sink.Writes[0].RenderMessage());
        }

        [Fact]
        public void CarriesException()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            var exception = new Exception();

            logger.Log(LogLevel.Information, 0, "Test", exception, null);

            Assert.Equal(1, sink.Writes.Count);
            Assert.Same(exception, sink.Writes[0].Exception);
        }

        [Fact]
        public void SingleScopeProperty()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScope(new FoodScope("pizza")))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"pizza\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Fact]
        public void NestedScopeSameProperty()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScope(new FoodScope("avocado")))
            {
                using (logger.BeginScope(new FoodScope("bacon")))
                {
                    logger.Log(LogLevel.Information, 0, TestMessage, null, null);
                }
            }

            // Should retain the property of the most specific scope
            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"bacon\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Fact]
        public void NestedScopesDifferentProperties()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScope(new FoodScope("spaghetti")))
            {
                using (logger.BeginScope(new LuckyScope(7)))
                {
                    logger.Log(LogLevel.Information, 0, TestMessage, null, null);
                }
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"spaghetti\"", sink.Writes[0].Properties["Name"].ToString());
            Assert.True(sink.Writes[0].Properties.ContainsKey("LuckyNumber"));
            Assert.Equal("7", sink.Writes[0].Properties["LuckyNumber"].ToString());
        }

        [Fact]
        public void CarriesMessageTemplateProperties()
        {
            var selfLog = new StringWriter();
            SelfLog.Out = selfLog;

            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.LogInformation("Hello, {Recipient}", "World");

            Assert.True(sink.Writes[0].Properties.ContainsKey("Recipient"));
            Assert.Equal("\"World\"", sink.Writes[0].Properties["Recipient"].ToString());
            Assert.Equal("Hello, {Recipient}", sink.Writes[0].MessageTemplate.Text);

            SelfLog.Out = null;
            Assert.Empty(selfLog.ToString());
        }

        [Fact]
        public void CarriesEventIdIfNonzero()
        {
            var t = SetUp(LogLevel.Trace);
            var logger = t.Item1;
            var sink = t.Item2;

            int expected = 42;

            logger.Log(LogLevel.Information, expected, "Test", null, null);

            Assert.Equal(1, sink.Writes.Count);

            var eventId = (StructureValue) sink.Writes[0].Properties["EventId"];
            var id = (ScalarValue) eventId.Properties.Single(p => p.Name == "Id").Value;
            Assert.Equal(42, id.Value);
        }

        private class FoodScope : IEnumerable<KeyValuePair<string, object>>
        {
            readonly string _name;

            public FoodScope(string name)
            {
                _name = name;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>("Name", _name);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class LuckyScope : IEnumerable<KeyValuePair<string, object>>
        {
            readonly int _luckyNumber;

            public LuckyScope(int luckyNumber)
            {
                _luckyNumber = luckyNumber;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>("LuckyNumber", _luckyNumber);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}