using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace TruthOrDare_API
{
    public class FiltersParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.Name.Equals("filters", StringComparison.OrdinalIgnoreCase))
            {
                // Get the controller and action names to determine which schema to use
                var actionDescriptor = context.ParameterInfo?.Member as MethodInfo;
                var controllerName = actionDescriptor?.DeclaringType?.Name;
                var actionName = actionDescriptor?.Name;

                if (controllerName?.Contains("Room", StringComparison.OrdinalIgnoreCase) == true ||
                    actionName?.Contains("Room", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Room filters
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"roomId\": \"AVC123\"}")
                    };
                }
                else if (controllerName?.Contains("Question", StringComparison.OrdinalIgnoreCase) == true ||
                         actionName?.Contains("Question", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Question filters
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"mode\": \"party\", \"type\": \"dare\", \"difficulty\": \"medium\", \"age_group\": \"all\"}")
                    };
                }
                else if (controllerName?.Contains("GameSession", StringComparison.OrdinalIgnoreCase) == true ||
                         actionName?.Contains("GameSession", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Question filters
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"playerId\": \"ABC\", \"roomId\": \"ABC\"}")
                    };
                }
                else
                {
                    // Default filters schema
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"key\": \"value\"}")
                    };
                }
            }
        }
    }
}
