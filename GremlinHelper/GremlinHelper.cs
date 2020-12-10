using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GremlinUtilities
{
    public static class GremlinHelper
    {
        public static string UpdateVertexQuery(string partitionId, string id, Dictionary<string, object> props)
        {
            return "g.V(['" + partitionId + "', '" + id + "'])" + GremlinPropertyString(props, "property");
        }
        public static string GetVertexQuery(string label, Dictionary<string, object> props)
        {
            return "g.V().hasLabel('" + label + "')" + GremlinPropertyString(props, "has");
        }
        public static string GetVertexQuery(string partitionId, string id)
        {
            return "g.V(['" + partitionId + "', '" + id + "'])";
        }
        public static string GetVertexQuery(string label, string id, string partitionId, Dictionary<string, object> props)
        {
            return "g.V().hasLabel('" + label + "')" + GremlinPropertyString(props, "has");
        }
        public static string AddVertexQuery(string label, Dictionary<string, object> props)
        {
            return "g.addV('" + label + "')" + GremlinPropertyString(props, "property");
        }
        public static string AddVertexQuery(string label, string id, string partitionId, Dictionary<string, object> props)
        {
            props.Add(partitionId, id);
            return "g.addV('" + label + "')" + GremlinPropertyString(props, "property");
        }
        public static string AddVertexQuery(string label, string partitionIdName, string partitionId, string id, Dictionary<string, object> props)
        {
            if (props==null)
            {
                props = new Dictionary<string, object>();
            }

            SetProperty(props, partitionIdName, partitionId);
            SetProperty(props, "id", id);

            return $"g.addV('{label}')"
                + GremlinPropertyString(props.Where(p => p.Key == partitionIdName).ToList(), "property")
                + GremlinPropertyString(props.Where(p => p.Key != partitionIdName).ToList(), "property");
        }
        public static string AddEdgeQuery(string partitionFrom, string IdFrom, string edgeType, string partitionTo, string IdTo, Dictionary<string, object> props)
        {
            return "g.V(['" + partitionFrom + "', '" + IdFrom + "']).addE('" + edgeType + "').to(g.V(['" + partitionTo + "', '" + IdTo + "']))" + GremlinPropertyString(props, "property");
        }
        private static void SetProperty(Dictionary<string, object> props, string propertyName, object value)
        {
            if (value == null)
            {
                return;
            }

            if (props.ContainsKey(propertyName))
            {
                props[propertyName] = value;
            }
            else
            {
                props.Add(propertyName, value);
            }
        }
        public static Dictionary<string, object> GetProps()
        {
            return new Dictionary<string, object>();
        }

        public static string GremlinPropertyString(Dictionary<string, object> props, string gremlinStep)
        {
            if (props == null || props.Count < 1)
            {
                return "";
            }
            var pairs = props.Select(x => string.Concat("." + gremlinStep + "('", x.Key, "',", ConvertToGremlinString(x.Value), ")"));

            //var pairs = source.Select(x => string.Concat(x.Key, keyValueSeparator, x.Value));
            string xx = string.Concat(pairs);

            return xx;
        }
        public static string GremlinPropertyString(List<KeyValuePair<string, object>> props, string gremlinStep)
        {
            if (props == null || props.Count < 1)
            {
                return "";
            }
            var pairs = props.Select(x => string.Concat("." + gremlinStep + "('", x.Key, "',", ConvertToGremlinString(x.Value), ")"));

            //var pairs = source.Select(x => string.Concat(x.Key, keyValueSeparator, x.Value));
            string xx = string.Concat(pairs);

            return xx;
        }
        //public static string GremlinProperties(Dictionary<string, object> props)
        //{
        //    var pairs = props.Select(x => string.Concat(".property('", x.Key, "',", ConvertToGremlinString(x.Value), ")"));

        //    //var pairs = source.Select(x => string.Concat(x.Key, keyValueSeparator, x.Value));
        //    string xx = string.Concat(pairs);

        //    return xx;
        //}
        //public static string GremlinHas(Dictionary<string, object> props)
        //{
        //    var pairs = props.Select(x => string.Concat(".has('", x.Key, "',", ConvertToGremlinString(x.Value), ")"));

        //    //var pairs = source.Select(x => string.Concat(x.Key, keyValueSeparator, x.Value));
        //    string xx = string.Concat(pairs);

        //    return xx;
        //}//value.GetType().FullName
        //////private static string ConvertToGremlinString(object value)
        //////{
        //////    if (value is string)
        //////    {
        //////        return "'" + ((string)value).Replace("'", @"\'") + "'";
        //////    }
        //////    else if (value is JObject)
        //////    {
        //////        return "'" + (value as JObject).ToString(Formatting.None).Replace("\r\n", "").Replace("'", @"\'") + "'";

        //////    }
        //////    else if (value is DateTime)
        //////    {
        //////        //return "'" + ((DateTime)value).ToUniversalTime().ToString("o").PadRight(19).Substring(0, 19).Trim() + "'";
        //////        return "'" + ((DateTime)value).ToString("o").PadRight(19).Substring(0, 19).Trim() + "'";
        //////    }
        //////    else if (value is bool)
        //////    {
        //////        return Convert.ToString(value).ToLower();
        //////    }
        //////    else if (value is int)
        //////    {
        //////        return Convert.ToString(value).ToLower();
        //////    }
        //////    else
        //////    {
        //////        return "''";
        //////        //return Convert.ToString(value).Replace("'", "\'");
        //////    }
        //////}
        private static string ConvertToGremlinString(object value)
        {
            if (value == null)
            {
                return "''";
            }

            switch (value.GetType().FullName)
            {
                case "System.String":
                    return "'" + ((string)value).Replace("'", @"\'") + "'";
                case "Newtonsoft.Json.Linq.JObject":
                    return "'" + (value as JObject).ToString(Formatting.None).Replace("\r\n", "").Replace("'", @"\'") + "'";
                case "System.DateTime":
                    return "'" + ((DateTime)value).ToString("o").PadRight(19).Substring(0, 19).Trim() + "'";
                case "System.Boolean":
                    return Convert.ToString(value).ToLower();
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Long":
                case "System.Double":
                case "System.Decimal":
                    return Convert.ToString(value).ToLower();
                default:
                    return "''";
            }
        }
        public static string StartsWith(string propertyName, string searchText)
        {
            return $"has('{propertyName}',between('{searchText}','{IncrementString(searchText)}'))";
        }
        public static string IncrementString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }
            string incremented = text;
            text = "";
            if (text.Length == 1)
            {

            }
            else
            {
                text = incremented.Substring(0, incremented.Length - 1);
                incremented = incremented.Substring(incremented.Length - 1);
            }

            char c = Char.Parse(incremented);
            int x = c;
            c = (char)++x;

            return text + c.ToString();
        }
        public static string ReverseTimeSequence(DateTime timeStamp)
        {
            return DateTime
                    .MaxValue
                    .Subtract(timeStamp)
                    .TotalMilliseconds
                    .ToString(CultureInfo.InvariantCulture);
        }
        //private static string ConvertToGremlinStringAll(object value)
        //{
        //    if ((value is string) || (value is JObject))
        //    {
        //        return "'" + value + "'";
        //    }
        //    else if (value is JObject)
        //    {
        //        return "'" + (value as JObject).ToString(Formatting.None) + "'";

        //    }
        //    else if (value is DateTime)
        //    {
        //        return "'" + ((DateTime)value).ToString("o") + "'";
        //    }
        //    else
        //    {
        //        return "'" + Convert.ToString(value) + "'";
        //    }
        //}
    }
    //public static class SafeHTML
    //{
    //    public static Regex regexBase = new Regex(
    //      "\\<base\\s+[.\\w\\s\\=\\.\\/\\\"\\:]+\\/\\>|\\<base\\s+[.\\w" +
    //      "\\s\\=\\.\\/\\\"\\:\\-]+\\>",
    //    RegexOptions.IgnoreCase
    //    | RegexOptions.Multiline
    //    | RegexOptions.CultureInvariant
    //    | RegexOptions.Compiled
    //    );

    //    public static string ParseHTML(string htmlString)
    //    {
    //        string result = htmlString;
    //        //string result = StyleMerge.Inliner.ProcessHtml(htmlString);
    //        //return result;
    //        try
    //        {
    //            var html = new HtmlDocument();

    //            html.LoadHtml(htmlString);
    //            if (html == null)
    //            {
    //                return htmlString;
    //            }
    //            var root = html.DocumentNode;
    //            var nodes = root.Descendants();

    //            //remove style node
    //            var styleNodes = nodes.Where(n => n.Name.Equals("style", StringComparison.CurrentCultureIgnoreCase)).ToList();
    //            if (styleNodes != null && styleNodes.Count > 0)
    //            {
    //                HtmlNode parent = null; //nodeQuery.ParentNode;

    //                foreach (var item in styleNodes)
    //                {
    //                    parent = item.ParentNode;
    //                    parent.RemoveChild(item);
    //                }
    //            }

    //            result = Inliner.ProcessHtml(html.DocumentNode.OuterHtml);

    //            html.LoadHtml(result);
    //            if (html == null)
    //            {
    //                return result;
    //            }

    //            root = html.DocumentNode;
    //            nodes = root.Descendants();

    //            //try to just return body node
    //            var nodeQuery = nodes.Where(n => n.Name.Equals("body", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
    //            if (nodeQuery != null)
    //            {
    //                result = regexBase.Replace(nodeQuery.OuterHtml, "");
    //                //return nodeQuery.OuterHtml;
    //            }
    //            else
    //            {
    //                //revert to html node, with the style node removed
    //                result = regexBase.Replace(html.DocumentNode.OuterHtml, "");
    //                //return html.DocumentNode.OuterHtml;
    //            }

    //            //var totalNodes = nodes.Count();
    //            //var bodyQuery = nodes.Where(n => n.Name.Equals("body", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
    //            //if (bodyQuery != null)
    //            //{
    //            //    string wrapperName = "ExternalWrapper";

    //            //    var body = bodyQuery.Clone();
    //            //    RemoveBadTags(body);

    //            //    if (nodeQuery != null)
    //            //    {
    //            //        var style = nodeQuery.Clone();
    //            //        //ModifyStyle(style, wrapperName);
    //            //        html = new HtmlDocument();

    //            //        string styleString = style.OuterHtml.Trim();
    //            //        styleString = styleString.Replace(" p ", " ." + wrapperName + " P ");
    //            //        styleString = styleString.Replace(" a:", " ." + wrapperName + " a:");
    //            //        styleString = styleString.Replace(" body ", " ." + wrapperName + " ");
    //            //        html.LoadHtml("<div class=\"ExternalWrapper\">" + styleString + body.InnerHtml + "</div>");
    //            //        //html.LoadHtml("<div class=\"ExternalWrapper\">" + style.OuterHtml.Replace("body {", "ExternalWrapper {") + body.InnerHtml + "</div>");
    //            //        //html.LoadHtml("<div class=\""+ wrapperName + "\">" + style.OuterHtml + body.InnerHtml + "</div>");

    //            //        return html.DocumentNode.OuterHtml;
    //            //    }
    //            //    else
    //            //    {
    //            //        html = new HtmlDocument();
    //            //        html.LoadHtml("<div class=\"ExternalWrapper\">" + body.InnerHtml + "</div>");

    //            //        return html.DocumentNode.OuterHtml;
    //            //    }
    //            //}
    //            //else
    //            //{
    //            //    return htmlString;
    //            //}
    //            //var iframes = body.ChildNodes.Where(n => n.Name.Equals("iframe", StringComparison.CurrentCultureIgnoreCase));
    //            //foreach (var item in body.ChildNodes.Where(n => deleteTags.Contains(n.Name.ToLower().Trim())))
    //            //{
    //            //    //body.RemoveChild(item);
    //            //    Debug.WriteLine(item.Name);
    //            //}
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine(ex.Message);
    //        }

    //        return result;
    //    }

    //    //private static void ModifyStyle(HtmlNode style, string wrapperName)
    //    //{
    //    //    if (style.HasChildNodes)
    //    //    {
    //    //        foreach (var item in style.ChildNodes)
    //    //        {
    //    //            if (item.Name.Equals("body", StringComparison.CurrentCultureIgnoreCase))
    //    //            {
    //    //                item.Name = wrapperName;
    //    //            }
    //    //            else
    //    //            {
    //    //                item.Name = "." + wrapperName + " " + item.Name;
    //    //            }
    //    //        }
    //    //    }
    //    //}

    //    //private static void RemoveBadTags(HtmlNode node)
    //    //{
    //    //    if (node.HasChildNodes)
    //    //    {
    //    //        KillBadNodes(node);

    //    //        if (node.HasChildNodes)
    //    //        {
    //    //            foreach (var item in node.ChildNodes)
    //    //            {
    //    //                if (item.HasChildNodes)
    //    //                {
    //    //                    KillBadNodes(item);
    //    //                }
    //    //            }
    //    //        }
    //    //    }
    //    //}

    //    //private static void KillBadNodes(HtmlNode node)
    //    //{
    //    //    try
    //    //    {
    //    //        var listOfNodes = node.ChildNodes.Where(n => deleteTags.Contains(n.Name.ToLower().Trim())).ToList();
    //    //        if (listOfNodes.Count > 0)
    //    //        {
    //    //            for (int i = 0; i < listOfNodes.Count; i++)
    //    //            {
    //    //                node.RemoveChild(listOfNodes[i]);
    //    //            }
    //    //        }

    //    //        //foreach (var item in )
    //    //        //{
    //    //        //    node.RemoveChild(item);
    //    //        //}
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        Debug.WriteLine(ex.Message);
    //    //    }
    //    //}
    //}
}
