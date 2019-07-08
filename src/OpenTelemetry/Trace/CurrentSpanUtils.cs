﻿// <copyright file="CurrentSpanUtils.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using OpenTelemetry.Context;

    internal class CurrentSpanUtils
    {
        private static readonly ConditionalWeakTable<Activity, ISpan> ActivitySpanTable = new ConditionalWeakTable<Activity, ISpan>();

        public ISpan CurrentSpan
        {
            get
            {
                var currentActivity = Activity.Current;
                if (currentActivity == null)
                {
                    return BlankSpan.Instance;
                }

                if (ActivitySpanTable.TryGetValue(currentActivity, out var currentSpan))
                {
                    return currentSpan;
                }

                return BlankSpan.Instance;
            }
        }

        public IScope WithSpan(ISpan span, bool endSpan)
        {
            return new ScopeInSpan(span, endSpan, this);
        }

        private void SetSpan(Activity activity, ISpan span)
        {
            if (activity == null)
            {
                // log error
                return;
            }

            if (ActivitySpanTable.TryGetValue(activity, out _))
            {
                // log warning
                return;
            }

            ActivitySpanTable.Add(activity, span);
        }

        private void DetachSpanFromActivity(Activity activity)
        {
            ActivitySpanTable.Remove(activity);
        }

        private sealed class ScopeInSpan : IScope
        {
            private readonly ISpan span;
            private readonly bool endSpan;
            private readonly CurrentSpanUtils currentUtils;

            public ScopeInSpan(ISpan span, bool endSpan, CurrentSpanUtils currentUtils)
            {
                this.span = span;
                this.endSpan = endSpan;
                this.currentUtils = currentUtils;
                this.currentUtils.SetSpan(Activity.Current, span);
            }

            public void Dispose()
            {
                bool safeToStopActivity = false;
                var current = (Span)this.span;
                if (current != null && current.Activity == Activity.Current)
                {
                    if (!current.OwnsActivity)
                    {
                        this.currentUtils.DetachSpanFromActivity(current.Activity);
                    }
                    else
                    {
                        safeToStopActivity = true;
                    }
                }

                if (this.endSpan)
                {
                    this.span.End();
                }
                else if (safeToStopActivity)
                {
                    current.Activity.Stop();
                }
            }
        }
    }
}
