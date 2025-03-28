using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TruthOrDare_API
{
    public class FiltersParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.Name.Equals("filters", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Schema = new OpenApiSchema
                {
                    Type = "string",
                    Example = new OpenApiString("{\"mode\": \"party\", \"type\": \"dare\", \"difficulty\": \"medium\", \"age_group\": \"all\"}\r\n\r\n")
                };
            }
        }
    }
}
