using System;
using System.Globalization;
using System.Reflection;
using System.Xml;
using UnityEngine;

public static class XMLExt
{
    public static T GetAttribute<T>(XmlNode node, string name)
    {
        XmlAttribute attribute = node.Attributes[name];

        if (attribute == null) return default(T);

        string value = attribute.Value;
        if (string.IsNullOrEmpty(value)) return default(T);

        Type type = typeof(T);
        if (type == typeof(string)) return (T)Convert.ChangeType(value, type);

        T obj = default(T);
        PropertyInfo[] properties = type.GetProperties();
        Type underlyingType = type;

#if !UNITY_WSA
        if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase)) underlyingType = properties[1].PropertyType;
#else
        if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.OrdinalIgnoreCase)) underlyingType = properties[1].PropertyType;
#endif

        try
        {
            MethodInfo method = underlyingType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
            if (method != null) obj = (T)method.Invoke(null, new object[] { value, CultureInfo.InvariantCulture.NumberFormat });
            else
            {
                method = underlyingType.GetMethod("Parse", new[] { typeof(string) });
                obj = (T)method.Invoke(null, new[] { value });
            }
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
            throw;
        }

        return obj;
    }
}

