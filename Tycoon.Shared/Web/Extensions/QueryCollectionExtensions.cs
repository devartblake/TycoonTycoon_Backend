using System.Collections;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tycoon.Shared.Web.Extensions;

// https://khalidabuhakmeh.com/read-and-convert-querycollection-values-in-aspnet
public static class QueryCollectionExtensions
{
    public static IEnumerable<T> All<T>(this IQueryCollection collection, string key)
    {
        List<T> values = new List<T>();
        if (collection.TryGetValue(key, out var results))
        {
            foreach (var s in results)
            {
                if (s is null)
                {
                    continue;
                }

                try
                {
                    var converted = Convert.ChangeType(s, typeof(T));
                    if (converted is T result)
                    {
                        values.Add(result);
                    }
                }
                catch (System.Exception)
                {
                    // conversion failed
                    // skip value
                }
            }
        }

        return values;
    }

    public static T Get<T>(
        this IQueryCollection collection,
        string key,
        T? @default = default,
        ParameterPick option = ParameterPick.First
    )
    {
        var values = All<T>(collection, key);
        T? value = @default;

        if (values.Any())
        {
            value = option switch
            {
                ParameterPick.First => values.FirstOrDefault(),
                ParameterPick.Last => values.LastOrDefault(),
                _ => value,
            };
        }

        return value ?? @default!;
    }

    public static T GetCollection<T>(this IQueryCollection collection, string key, T? @default = default)
        where T : IEnumerable
    {
        var genericTypeArgs = typeof(T).GetGenericArguments();
        if (genericTypeArgs.Length == 0)
        {
            return @default!;
        }

        var type = genericTypeArgs[0];
        var listType = typeof(List<>);
        var constructedListType = listType.MakeGenericType(type);
        var values = Activator.CreateInstance(constructedListType) as IList;
        if (values is null)
        {
            return @default!;
        }

        if (collection.TryGetValue(key, out var results))
        {
            foreach (var s in results)
            {
                try
                {
                    if (s is not null && s.IsValidJson())
                    {
                        var result = JsonConvert.DeserializeObject(s, type);
                        if (result is not null)
                        {
                            values.Add(result);
                        }
                    }
                    else
                    {
                        var result = Convert.ChangeType(s, type);
                        if (result is not null)
                        {
                            values.Add(result);
                        }
                    }
                }
                catch (System.Exception)
                {
                    // conversion failed
                    // skip value
                }
            }
        }
        else
        {
            return @default;
        }

        return values is T typedValues ? typedValues : @default!;
    }

    private static bool IsValidJson(this string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput))
        {
            return false;
        }

        strInput = strInput.Trim();
        if (
            (strInput.StartsWith("{", StringComparison.Ordinal) && strInput.EndsWith("}", StringComparison.Ordinal))
            || (strInput.StartsWith("[", StringComparison.Ordinal) && strInput.EndsWith("]", StringComparison.Ordinal))
        )
        {
            try
            {
                var obj = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException jex)
            {
                Console.WriteLine(jex.Message);
                return false;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        return false;
    }
}
