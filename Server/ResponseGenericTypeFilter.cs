using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace TodoList.Server
{
    public class GenericTypeFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!context.Type.IsGenericType)
            {
                return;
            }

            schema.Title = GetGenericTypeTitleRecursive(context.Type);
        }

        public string GetGenericTypeTitleRecursive(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string typeName = type.Name[..type.Name.LastIndexOf('`')];
            Type[] genericTypes = type.GetGenericArguments();

            List<string> names = new(genericTypes.Length);
            foreach (var genericType in genericTypes)
            {
                names.Add(GetGenericTypeTitleRecursive(genericType));
            }

            return $"{typeName}<{names.Aggregate((s1, s2) => $"{s1}, {s2}")}>";
        }
    }
}
