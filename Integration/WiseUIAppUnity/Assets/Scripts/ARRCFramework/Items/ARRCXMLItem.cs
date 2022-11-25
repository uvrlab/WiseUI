using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ARRC.Framework
{
    public class ARRCXMLItem : ARRCItem
    {
        public string id;
     

        public ARRCXMLItem()
        {

        }
        public ARRCXMLItem(XmlNode node)
        {
            Type type = GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                FieldInfo field = fields.FirstOrDefault(f => f.Name == childNode.Name);
                LoadField(field, this, childNode);
            }
        }

        public void LoadField(FieldInfo field, object target, XmlNode node)
        {
            if (node == null) return;

            string value = node.InnerXml;
            if (string.IsNullOrEmpty(value)) return;

            Type type = field.FieldType;
            if (type == typeof(string)) field.SetValue(target, node.InnerText.Trim());
            else if (type.IsEnum)
            {
                try
                {
                    field.SetValue(target, Enum.Parse(type, value));
                }
                catch
                {
                }
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                     type == typeof(float) || type == typeof(double) ||
                     type == typeof(bool))

            {
                PropertyInfo[] properties = type.GetProperties();
                Type underlyingType = type;

                if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase)) underlyingType = properties[1].PropertyType;

                try
                {
                    MethodInfo method = underlyingType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
                    object obj;
                    if (method != null) obj = method.Invoke(null, new object[] { value, CultureInfo.InvariantCulture.NumberFormat });
                    else
                    {
                        method = underlyingType.GetMethod("Parse", new[] { typeof(string) });
                        obj = method.Invoke(null, new[] { value });
                    }

                    field.SetValue(target, obj);
                }
                catch (Exception exception)
                {
                    Debug.Log(exception.Message + "\n" + exception.StackTrace);
                    throw;
                }
            }
            else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
#if UNITY_EDITOR
                object v = AssetDatabase.LoadAssetAtPath(value, typeof(UnityEngine.Object));
                field.SetValue(target, v);
#endif
            }
            else if (type.IsArray)
            {
                Array v = Array.CreateInstance(type.GetElementType(), node.ChildNodes.Count);

                int index = 0;
                foreach (XmlNode itemNode in node.ChildNodes)
                {
                    Type elementType = type.GetElementType();

                    if (elementType == typeof(string))
                    {
                        v.SetValue(itemNode.FirstChild.Value, index);
                    }
                    else
                    {
                        object item = Activator.CreateInstance(elementType);

                        FieldInfo[] fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        foreach (XmlNode fieldNode in itemNode.ChildNodes)
                        {
                            FieldInfo fieldInfo = fields.FirstOrDefault(f => f.Name == fieldNode.Name);
                            if (fieldInfo == null)
                            {
                                Debug.Log("No info for " + fieldNode.Name);
                                continue;
                            }

                            LoadField(fieldInfo, item, fieldNode);
                        }

                        v.SetValue(item, index);
                    }

                    index++;
                }

                field.SetValue(target, v);
            }
            else if (type.IsGenericType)
            {
                Type listType = type.GetGenericArguments()[0];
                object v = type.Assembly.CreateInstance(type.FullName);

                foreach (XmlNode itemNode in node.ChildNodes)
                {
                    object item = null;

                    if (listType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
#if UNITY_EDITOR
                        item = AssetDatabase.LoadAssetAtPath(itemNode.FirstChild.InnerText, listType);
                        if (item == null) continue;
#endif
                    }
                    else if (listType.IsValueType)
                    {
                        try
                        {
                            MethodInfo method = listType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
                            if (method != null) item = method.Invoke(null, new object[] { itemNode.FirstChild.InnerText, CultureInfo.InvariantCulture.NumberFormat });
                            else
                            {
                                method = listType.GetMethod("Parse", new[] { typeof(string) });
                                item = method.Invoke(null, new[] { itemNode.FirstChild.InnerText });
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        item = listType.Assembly.CreateInstance(listType.FullName);
                        FieldInfo[] fields = listType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        foreach (XmlNode fieldNode in itemNode.ChildNodes)
                        {
                            FieldInfo fieldInfo = fields.FirstOrDefault(f => f.Name == fieldNode.Name);
                            if (fieldInfo == null)
                            {
                                Debug.Log("No info for " + fieldNode.Name);
                                continue;
                            }

                            LoadField(fieldInfo, item, fieldNode);
                        }
                    }

                    try
                    {
                        type.GetMethod("Add").Invoke(v, new[] { item });
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(exception.Message + "\n" + exception.StackTrace);
                    }
                }
                field.SetValue(target, v);
            }
            else
            {
                try
                {
                    object v = type.Assembly.CreateInstance(type.FullName);
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        FieldInfo fieldInfo = fields.FirstOrDefault(f => f.Name == childNode.Name);
                        if (fieldInfo == null) continue;
                        LoadField(fieldInfo, v, childNode);
                    }
                    field.SetValue(target, v);
                }
                catch (Exception)
                {
                    Debug.Log(type.FullName);
                    Debug.Log(node.Name);
                    throw;
                }
            }
        }


        public virtual void AppendToXML(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode parentNode;
            XmlNode newNode;
            if (File.Exists(filepath))
            {
                doc.Load(filepath);
                parentNode = doc.FirstChild;
            }
            else
            {
                parentNode = doc.CreateElement("History");

            }
            newNode = doc.CreateElement("Configuration");
            parentNode.AppendChild(newNode);
            doc.AppendChild(parentNode);

            AppendDataToNode(newNode);
            File.WriteAllText(filepath, doc.OuterXml, Encoding.UTF8);
        }

        public XmlNode AppendDataToNode(XmlNode node)
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(IgnoreInXMLAttribute), false)) continue;
                try
                {
                    CreateChildNode(node, field.Name, field.GetValue(this));
                }
                catch (Exception exception)
                {
                    Debug.Log(exception.Message + "\n" + exception.StackTrace);
                }
            }
            return node;
        }
        private class IgnoreInXMLAttribute : Attribute
        {
        }
        private void CreateChildNode(XmlNode node, string name, object value)
        {
            if (value == null) return;

            if (value is string) CreateNode(node, name, value as string, true);
            else if (value is bool || value is int || value is long || value is short || value is Enum) CreateNode(node, name, value);
            else if (value is float) CreateNode(node, name, ((float)value).ToString());
            else if (value is double) CreateNode(node, name, ((double)value).ToString());

#if UNITY_EDITOR
            else if (value is UnityEngine.Object) CreateNode(node, name, AssetDatabase.GetAssetPath(value as UnityEngine.Object));
#endif

            //else if (IsValueTuple2(value))
            //{
            //    CreateNode(node, name, ((ValueTuple<double, double, double, double>)value).ToString());
            //    XmlNode n = CreateNode(node, name);
            //    foreach (var item in v) CreateChildNode(n, "Item", item);
            //}

            else if (value is IEnumerable)
            {
                IEnumerable v = (IEnumerable)value;
                XmlNode n = CreateNode(node, name);
                foreach (var item in v) CreateChildNode(n, "Item", item);
            }
            else
            {
                XmlNode n = CreateNode(node, name);
                FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields) CreateChildNode(n, field.Name, field.GetValue(value));
            }
        }
        private XmlNode CreateNode(XmlNode node, string nodeName, string value, bool wrapCData)
        {
            XmlDocument doc = node.OwnerDocument;
            if (doc == null) return null;
            XmlNode newNode = doc.CreateElement(nodeName);
            if (!wrapCData) newNode.AppendChild(doc.CreateTextNode(value));
            else newNode.AppendChild(doc.CreateCDataSection(value));
            node.AppendChild(newNode);
            return newNode;
        }
        private XmlNode CreateNode(XmlNode node, string nodeName, object value)
        {
            if (value != null) return CreateNode(node, nodeName, value.ToString(), false);
            return null;
        }
        private XmlNode CreateNode(XmlNode node, string nodeName)
        {
            XmlDocument doc = node.OwnerDocument;
            if (doc == null) return null;
            XmlNode newNode = doc.CreateElement(nodeName);
            node.AppendChild(newNode);
            return newNode;
        }

        private static readonly HashSet<Type> ValTupleTypes = new HashSet<Type>(
    new Type[] { typeof(ValueTuple<>), typeof(ValueTuple<,>),
                 typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
                 typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>),
                 typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>)
    }
);
        static bool IsValueTuple(object obj)
        {
            var type = obj.GetType();
            return type.IsGenericType
                && ValTupleTypes.Contains(type.GetGenericTypeDefinition());
        }




    }
}