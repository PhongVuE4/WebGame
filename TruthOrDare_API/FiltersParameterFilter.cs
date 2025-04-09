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
                else if (actionName.Equals("GetGameSessions", StringComparison.OrdinalIgnoreCase))
                {
                    // Game session filters
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"playerId\": \"f22cb65a-6a02-4919-a17c-9eb8428fa123\"}")
                    };
                }
                else if (actionName.Equals("GetGameSessionDetail", StringComparison.OrdinalIgnoreCase))
                {
                    // Game session detail filters
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Example = new OpenApiString("{\"playerId\": \"f6b419f9-ac4c-4a4a-88ef-ab4fbd98c795\", \"gamesessionId\": \"67f52b27a56daf59c56f6f12\"}")
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
