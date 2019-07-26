﻿// <copyright file="AzureSdkCollector.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.Collector.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using OpenTelemetry.Context;
    using OpenTelemetry.Trace;

    public class AzureSdkCollector : IDisposable, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>
    {
        private readonly ITracer tracer;

        private readonly ISampler sampler;

        private List<IDisposable> subscriptions = new List<IDisposable>();

        public AzureSdkCollector(ITracer tracer, ISampler sampler)
        {
            this.tracer = tracer;
            this.sampler = sampler;

            this.subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
        }

        public void Dispose()
        {
            lock (this.subscriptions)
            {
                foreach (var subscription in this.subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                if (value.Key.EndsWith("Start"))
                {
                    this.OnStartActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Stop"))
                {
                    // Current.Parent is used because OT wraps additional Activity over
                    this.OnStopActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Exception"))
                {
                    // Current.Parent is used because OT wraps additional Activity over
                    this.OnException(Activity.Current, value.Value);
                }
            }
            catch (Exception)
            {
                // TODO: Log
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name.StartsWith("Azure"))
            {
                lock (this.subscriptions)
                {
                    this.subscriptions.Add(value.Subscribe(this));
                }
            }
        }

        private void OnStartActivity(Activity current, object valueValue)
        {
            var operationName = current.OperationName;
            foreach (var keyValuePair in current.Tags)
            {
                if (keyValuePair.Key == "http.url")
                {
                    operationName = keyValuePair.Value;
                }
            }

            var span = this.tracer.SpanBuilder(operationName)
                .SetCreateChild(false)
                .SetSpanKind(SpanKind.Client)
                .SetSampler(this.sampler)
                .StartSpan();

            this.tracer.WithSpan(span);
        }

        private void OnStopActivity(Activity current, object valueValue)
        {
            var span = this.tracer.CurrentSpan;
            foreach (var keyValuePair in current.Tags)
            {
                span.SetAttribute(keyValuePair.Key, keyValuePair.Value);
            }

            this.tracer.CurrentSpan.End();
        }

        private void OnException(Activity current, object valueValue)
        {
            var span = this.tracer.CurrentSpan;

            span.Status = Status.Unknown.WithDescription(valueValue?.ToString());
        }
    }
}
