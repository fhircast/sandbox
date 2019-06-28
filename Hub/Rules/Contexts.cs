using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FHIRcastSandbox.Rules {
    public class Contexts : IContexts {
        private ILogger<IContexts> logger;

        private readonly IDictionary<string, object> contexts;

        public Contexts(ILogger<Contexts> logger) {
            this.logger = logger;
            contexts = new ConcurrentDictionary<string, object>();
        }

        public string addContext()
        {
            int count = contexts.Count;
            string topic = $"topic{count++}";
            contexts.Add(topic, null);
            return topic;
        }

        public void setContext(string topic, object context)
        {
            contexts[topic] = context;
        }

        public object getContext(string topic)
        {
            object context;
            if (this.contexts.TryGetValue(topic, out context))
            {
                return context;
            }
            else
            {
                return null;
            }
        }
    }
}
