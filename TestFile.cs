using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Hl7.Helper
{
    public interface ISegment
    {
        string Name { get; }
        int Id { get; set; }
        string GetValue(string key);
    }

    public class Segment : ISegment
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        internal Dictionary<string, string> Values { get; set; }
        private bool IsInitialized { get; set; }

        public Segment(int index, string hl7)
        {
            Values = new Dictionary<string, string>();
            Data = hl7;
            Id = index;
            IsInitialized = false;
        }

        public string GetValue(string key)
        {
            if (Data != null)
            {
                Values = Helper.Parse(Data);
                IsInitialized = true;
            }
            return IsInitialized && Values.ContainsKey(key) ? Values[key] : string.Empty;
        }

        public static ISegment GetEmpty()
        {
            return new Segment(0, string.Empty);
        }

    }

    public class Helper
    {
        internal static Dictionary<string, string> Parse(string data)
        {
            var values = new Dictionary<string, string>();
            var sFields = GetMessgeFields(data);
            //Name = sFields.Length > 0 ? sFields[0] : string.Empty;
            for (var a = 0; a < sFields.Length; a++)
            {
                var fieldEl = string.Format("{0}.{1}", sFields[0], a);
                if (sFields[a] != @"^~\&")
                {
                    var components = GetComponents(sFields[a]);
                    if (components.Length > 1)
                    {
                        for (var b = 0; b < components.Length; b++)
                        {
                            var componentEl = string.Format("{0}.{1}.{2}", sFields[0], a, b);
                            var subComponents = GetSubComponents(components[b]);
                            if (subComponents.Length > 1)
                            {
                                for (var c = 0; c < subComponents.Length; c++)
                                {
                                    var subComponentRepetitions = GetRepetitions(subComponents[c]);
                                    if (subComponentRepetitions.Length > 1)
                                    {
                                        for (var d = 0; d < subComponentRepetitions.Length; d++)
                                        {
                                            var subComponentRepEl = string.Format("{0}.{1}.{2}.{3}.{4}", sFields[0], a, b, c, d);
                                            values.Add(subComponentRepEl, subComponentRepetitions[d]);
                                        }
                                    }
                                    else
                                    {
                                        var subComponentEl = string.Format("{0}.{1}.{2}.{3}", sFields[0], a, b, c);
                                        values.Add(subComponentEl, subComponents[c]);
                                    }
                                }
                            }
                            else
                            {
                                var sRepetitions = GetRepetitions(components[b]);
                                if (sRepetitions.Length > 1)
                                {
                                    for (var c = 0; c < sRepetitions.Length; c++)
                                    {
                                        var repetitionEl = string.Format("{0}.{1}.{2}.{3}", sFields[0], a, b, c);
                                        values.Add(repetitionEl, sRepetitions[c]);
                                    }
                                }
                                else
                                {
                                    values.Add(componentEl, components[b]);
                                }
                            }
                        }
                    }
                    else
                    {
                        values.Add(fieldEl, sFields[a]);
                    }
                }
                else
                {
                    values.Add(fieldEl, sFields[a]);
                }
            }
            return values;
        }

        private static string[] GetMessgeFields(string s)
        {
            return s.Split('|');
        }

        private static string[] GetComponents(string s)
        {
            return s.Split('^');
        }

        private static string[] GetSubComponents(string s)
        {
            return s.Split('&');
        }

        private static string[] GetRepetitions(string s)
        {
            return s.Split('~');
        }
    }

    public class Hl7
    {
        public List<Segment> Segments { get; set; }
        public Segment Msh { get; set; }
        public Hl7(List<Segment> segments, Segment msh = null)
        {
            Msh = msh;
            Segments = segments;
            if (Msh == null)
            {
                foreach (var segment in Segments)
                {
                    if (segment.Name.Equals("MSH"))
                    {
                        Msh = segment;
                        break;
                    }
                }
            }
        }
        public Hl7(string hl7)
        {
            Segments = new List<Segment>();
            var segments = hl7.Split('\r');
            for (var i = 0; i < segments.Length; i++)
            {
                var data = Regex.Replace(segments[i], @"[^ -~]", "");//TODO:
                var segment = new Segment(i, data)
                {
                    Name = data.Substring(0, data.IndexOf('|'))
                };
                if (segment.Name.Equals("MSH"))
                {
                    Msh = segment;
                }
                Segments.Add(segment);
            }
        }
        public string GetValue(string path, int index = -1)
        {
            var outvalue = string.Empty;
            //if (index > -1)
            //{
            //    outvalue = Segments.First(x => x.Id == index).GetValue(path);
            //}
            //else if (Segments.Any(x => x.Name.StartsWith(segmentName)))
            //{
            //    // var key = string.Format("{0}.{1}", segmentName, path);
            //    outvalue = Segments.First(x => x.Name.Equals(segmentName)).GetValue(path);
            //}
            if (index > -1)
            {
                foreach (var segment in Segments)
                {
                    if (segment.Id != index) continue;
                    outvalue = segment.GetValue(path);
                    break;
                }
            }
            else
            {
                var segmentName = path.Split('.')[0];
                foreach (var segment in Segments)
                {
                    if (segment.Name.Equals(segmentName))
                    {
                        outvalue = segment.GetValue(path);
                    }
                }
            }
            return outvalue;
        }
        public List<Segment> GetSegments(string name)
        {
            var outValue = new List<Segment>();
            foreach (var segment in Segments)
            {
                if (segment.Name.Equals(name))
                {
                    outValue.Add(segment);
                }
            }
            return outValue;
            // return Segments.Where(x => x.Name.Equals(name));
        }
        public List<Segment> GetSegments(int index)
        {
            var outValue = new List<Segment>();
            foreach (var segment in Segments)
            {
                if (segment.Id == index)
                {
                    outValue.Add(segment);
                }
            }
            return outValue;
            //  return Segments.Where(x => x.Id == index);
        }
        public IEnumerable<List<Segment>> GetGroupSegmets(string segmentName)
        {
            var segmentList = new List<List<Segment>>();
            var temp = new List<Segment>();
            var isFirstPidFound = false;
            foreach (var segment in Segments)
            {
                if (segment.Name.Equals(segmentName))
                {
                    isFirstPidFound = true;
                    segmentList.Add(temp);
                    temp = new List<Segment> { segment };
                }
                if (isFirstPidFound)
                {
                    temp.Add(segment);
                }
            }
            return segmentList;
        }
        public string ToString(bool includeMsh = true)
        {
            var sb = new StringBuilder();
            if (includeMsh)
            {
                sb.AppendFormat("{0}{1}", Msh.Data, Environment.NewLine);
            }
            foreach (var segment in Segments)
            {
                sb.AppendFormat("{0}{1}", segment.Data, Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}

public class EmbeddedResourceReader
{
		public static string ReadContentFromEmbeddedResource(Assembly assembly, string resourceName)
		{
			var result = string.Empty;
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (StreamReader reader = new StreamReader(stream))
			{
				result = reader.ReadToEnd();
			}
			return result;
		}

		public static string GetResource()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var content = ReadContentFromEmbeddedResource(assembly, "DataExtraction.Query.xml");
			return content;
		}
}

public class EnumHelper<T> where T : struct
    {
        public static IEnumerable<T> GetEnumMembers()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static T TryParse(string serverType)
        {
            T type;
            Enum.TryParse<T>(serverType, out type);
            return type;
        }
    }
    
    
    public class XmlUtil
    {
         public static String Serizlize<T>(T obj, Encoding encoding = null, string prifix = "", string nameSpace = "")
        {
            string outValue;
            encoding = encoding ?? Encoding.UTF8;
            using (var ms = new MemoryStream())
            using (var writer = new XmlTextWriter(ms, encoding))
            //using (var writer = new StringWriter() )
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add(prifix, nameSpace);
                new XmlSerializer(typeof(T)).Serialize(writer, obj, ns);
                //outValue = writer.ToString();
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(writer.BaseStream, encoding);
                outValue = reader.ReadToEnd();
            }
            return outValue;
        }
        public static String Serizlize<T>(T Obj)
        {
            string outValue = string.Empty;
            using (StringWriter writer = new StringWriter())
            {
                new XmlSerializer(typeof(T)).Serialize(writer, Obj);
                outValue = writer.ToString();
            }
            return outValue;
        }
        public static void SerializeToFile<T>(T obj, string filepath, Encoding encoding = null, string prifix = "", string nameSpace = "")
        {
            var xml = Serizlize<T>(obj, encoding, prifix, nameSpace);
            File.WriteAllText(filepath, xml);
        }
        public static String SerializeWithCDATA<T>(T Obj)
        {
            string outValue = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    new XmlSerializer(typeof(T)).Serialize(writer, Obj);
                    writer.Flush();
                    writer.WriteCData(Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.OmitXmlDeclaration = true;
            ////settings.Indent = true;
            //StringBuilder sb = new StringBuilder();
            //using (XmlWriter writer = XmlWriter.Create(sb, settings))
            //{
            //    new XmlSerializer(typeof(T)).Serialize(writer, Obj);
            //    writer.WriteCData(sb.ToString());
            //    outValue = writer.ToString();
            //}
            return outValue;
        }

        public static T Deserializer<T>(String Xml)
        {
            T outValue = default(T);
            using (StringWriter writer = new StringWriter())
            {
                outValue = (T)new XmlSerializer(typeof(T)).Deserialize(new StringReader(Xml));
            }
            return outValue;
        }
        public static T DeserializerFromXMLFile<T>(string xmlFilePath)
        {
            //var xmlData = XDocument.Load(xmlFilePath).ToString();
            var xmlData = File.ReadAllText(xmlFilePath);
            return Deserializer<T>(xmlData);
        }
    }
    
    
    public class Command<T> : ICommand
    {
        private Predicate<T> CanExecuteDelegate { get; set; }
        private Action<T> ExecuteDelegate { get; set; }
        public Command(Predicate<T> canExecute, Action<T> execute)
        {
            CanExecuteDelegate = canExecute;
            ExecuteDelegate = execute;
        }
        #region ICommand Members
        public bool CanExecute(object parameter)
        {
            var canExecute = true;
            if (CanExecuteDelegate != null)
            {
                canExecute = CanExecuteDelegate((T)parameter);
            }
            return canExecute;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public void Execute(object parameter)
        {
            if (ExecuteDelegate != null)
            {
                ExecuteDelegate((T)parameter);
            }
        }
        #endregion
    }
    
    
    public class Reader
    {
        public static SqlDataReader ExecuteStoredProcedure(SqlConnection cn, string storedProcedureName, List<SqlParameter> Params = null)
        {
            SqlDataReader reader;
            using (var cmd = new SqlCommand(storedProcedureName, cn))
            {
                if (Params != null)
                {
                    Params.ForEach(x => cmd.Parameters.Add(x));
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                reader = cmd.ExecuteReader();
            }
            return reader;
        }

        public static void ExecuteUpdateStoredProcedure(SqlConnection cn, string storedProcedureName, List<SqlParameter> Params)
        {
            using (var cmd = new SqlCommand(storedProcedureName, cn))
            {
                if (Params != null)
                {
                    Params.ForEach(x => cmd.Parameters.Add(x));
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class Reader<T> : Reader where T : class, new()
    {
        public new static List<T> ExecuteStoredProcedure(SqlConnection cn, string storedProcedureName, List<SqlParameter> Params = null)
        {
            List<T> list;
            using (var cmd = new SqlCommand(storedProcedureName, cn))
            {
                if (Params != null)
                {
                    Params.ForEach(x => cmd.Parameters.Add(x));
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    list = GetItemsFromReader(dr);
                }
            }
            return list;
        }
        public static List<T> GetItemsFromReader(SqlDataReader reader)
        {
            var objs = new List<T>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    T obj = new T();
                    var dic = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = reader[i];
                        if (value is DBNull) continue;
                        if (dic.ContainsKey(name) && !(dic[name].GetType() == typeof(List<>)))
                        {
                            dic[name].SetValue(obj, value, null);
                        }
                    }
                    objs.Add(obj);
                }
            }
            return objs;
        }
    }
