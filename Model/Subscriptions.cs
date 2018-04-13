using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Model {
    public abstract class SubscriptionBase {
        [BindRequired]
        public Uri Callback { get; set; }

        [BindRequired]
        public SubscriptionMode? Mode { get; set; }

        [BindRequired]
        public string Topic { get; set; }
        [BindRequired]
        public string[] Events { get; set; }
    }

    public abstract class SubscriptionWithLease : SubscriptionBase {
        [ModelBinder(Name = "lease_seconds")]
        public int? LeaseSeconds { get; set; }

        [BindNever, JsonIgnore]
        public TimeSpan? Lease => this.LeaseSeconds.HasValue ? TimeSpan.FromSeconds(this.LeaseSeconds.Value) : (TimeSpan?)null;
    }

    public class Subscription : SubscriptionWithLease {
        [BindRequired]
        public string Secret { get; set; }
    }

    public class SubscriptionCancelled : SubscriptionBase {
        public string Reason { get; set; }
    }

    public class SubscriptionDenied : SubscriptionWithLease {
        public string Challenge { get; set; }
    }

    public enum SubscriptionMode {
        Subscribe,
        Unsubscribe,
    }
}
