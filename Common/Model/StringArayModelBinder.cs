using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FHIRcastSandbox.Model {
    public class EventsArrayModelBinder : IModelBinder {
        private static readonly char[] SplitCharacters = new[] { ',' };
        public Task BindModelAsync(ModelBindingContext bindingContext) {
            var rawInputString = bindingContext.ValueProvider.GetValue("hub.events").FirstValue;

            if (string.IsNullOrEmpty(rawInputString)) {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(rawInputString.Split(SplitCharacters, StringSplitOptions.RemoveEmptyEntries));
            return Task.CompletedTask;
        }
    }
}