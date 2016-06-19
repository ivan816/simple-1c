using System.Text;

namespace Simple1C.Interface
{
    public class ConnectionStringBuilder
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public Connection1CType Type { get; set; }
        public string FileLocation { get; set; }

        public string GetConnectionString()
        {
            var builder = new StringBuilder();
            if (Type == Connection1CType.File)
                AddItem("File", FileLocation, builder);
            else
            {
                AddItem("Srvr", Server, builder);
                AddItem("Ref", Database, builder);
            }
            AddItem("Usr", User, builder);
            AddItem("Pwd", Password, builder);
            return builder.ToString();
        }

        private static void AddItem(string name, string value, StringBuilder builder)
        {
            builder.Append(name);
            builder.Append('=');
            builder.Append(Quote(value));
            builder.Append(';');
        }

        private static string Quote(string s)
        {
            return "\"" + (s ?? "").Replace("\"", "\\\"") + "\"";
        }
    }
}