using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites.georgius
{
    public class JsonObject
    {
        public JsonObject()
        {
            this.Name = String.Empty;
            this.ItemCount = 0;
            this.Level = 0;
            this.IsArray = false;
        }

        public String Name { get; set; }
        public int ItemCount { get; set; }
        public int Level { get; set; }
        public Boolean IsArray { get; set; }
    }

    public class CeskaTelevizeJsonTextWriter : JsonWriter
    {
        #region Private fields

        private String lastObjectName;
        private readonly TextWriter _writer;
        Stack<JsonObject> jsonObjects;
        private JToken _token;
        private String tokenStart;
        private Boolean firstWrite;

        #endregion

        #region Constructors

        public CeskaTelevizeJsonTextWriter(TextWriter writer, JToken token)
            : base()
        {
            this.lastObjectName = String.Empty;
            this.jsonObjects = new Stack<JsonObject>();
            this._token = token;
            this.tokenStart = String.Empty;
            this.firstWrite = true;

            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            _writer = writer;
        }

        #endregion

        #region Methods

        public override void Close()
        {
            base.Close();
            this._writer.Close();
        }

        public override void Flush()
        {
            this._writer.Flush();
        }

        public override void WriteStartObject()
        {
            base.WriteStartObject();

            if (!String.IsNullOrEmpty(this.lastObjectName))
            {
                this.jsonObjects.Push(new JsonObject() { Name = this.lastObjectName, Level = this.Top });
            }
        }

        public override void WriteStartArray()
        {
            base.WriteStartArray();
            this.jsonObjects.Push(new JsonObject() { Name = this.lastObjectName, ItemCount = 0, Level = this.Top, IsArray = true });
            this.lastObjectName = String.Empty;
        }

        protected override void WriteEnd(JsonToken token)
        {
            base.WriteEnd(token);
            this.lastObjectName = String.Empty;

            switch (token)
            {
                case JsonToken.None:
                    break;
                case JsonToken.StartObject:
                    break;
                case JsonToken.StartArray:
                    break;
                case JsonToken.StartConstructor:
                    break;
                case JsonToken.PropertyName:
                    break;
                case JsonToken.Comment:
                    break;
                case JsonToken.Raw:
                    break;
                case JsonToken.Integer:
                    break;
                case JsonToken.Float:
                    break;
                case JsonToken.String:
                    break;
                case JsonToken.Boolean:
                    break;
                case JsonToken.Null:
                    break;
                case JsonToken.Undefined:
                    break;
                case JsonToken.EndObject:
                    if (this.jsonObjects.Count != 0)
                    {
                        JsonObject jsonObject = this.jsonObjects.Pop();
                        if (jsonObject.Level == this.Top)
                        {
                            jsonObject.ItemCount++;
                        }
                        this.jsonObjects.Push(jsonObject);
                    }
                    if (this.jsonObjects.Count != 0)
                    {
                        JsonObject jsonObject = this.jsonObjects.Pop();
                        if (jsonObject.Level != (this.Top + 1))
                        {
                            this.jsonObjects.Push(jsonObject);
                        }
                    }
                    break;
                case JsonToken.EndArray:
                    this.jsonObjects.Pop();
                    break;
                case JsonToken.EndConstructor:
                    break;
                case JsonToken.Date:
                    break;
                case JsonToken.Bytes:
                    break;
                default:
                    break;
            }
        }

        public override void WritePropertyName(string name)
        {
            base.WritePropertyName(name);

            this.lastObjectName = name;
            this.tokenStart = String.Format("{0}[{1}]", this.SerializeJsonObjects(), name);
        }

        protected override void WriteValueDelimiter()
        {
            base.WriteValueDelimiter();
        }

        public override void WriteWhitespace(string ws)
        {
            base.WriteWhitespace(ws);
        }
        
        public override void WriteNull()
        {
            base.WriteNull();
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.Null));
            this.firstWrite = false;
        }

        public override void WriteValue(bool value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(byte value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(DateTime value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(DateTimeOffset value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(decimal value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(double value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(float value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(char value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(int value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(long value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(object value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(sbyte value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(short value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(string value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, value));
            this.firstWrite = false;
        }

        public override void WriteValue(uint value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(ulong value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteValue(ushort value)
        {
            base.WriteValue(value);
            this.WriteRaw(String.Format(this.firstWrite ? "{0}={1}" : "&{0}={1}", this.tokenStart, JsonConvert.ToString(value)));
            this.firstWrite = false;
        }

        public override void WriteComment(string text)
        {
            base.WriteComment(text);
        }

        public override void WriteRaw(string json)
        {
            base.WriteRaw(json);
            this._writer.Write(json);
        }

        public override void WriteRawValue(string json)
        {
            base.WriteRawValue(json);
            this._writer.Write(json);
        }

        protected virtual String SerializeJsonObjects()
        {
            StringBuilder builder = new StringBuilder();
            Boolean first = true;
            foreach (var jsonObject in this.jsonObjects.Reverse())
            {
                if (first)
                {
                    if (jsonObject.IsArray)
                    {
                        builder.AppendFormat("{0}[{1}]", jsonObject.Name, jsonObject.ItemCount);
                    }
                    else
                    {
                        builder.Append(jsonObject.Name);
                    }
                    first = false;
                }
                else
                {
                    if (jsonObject.IsArray)
                    {
                        builder.AppendFormat("[{0}][{1}]", jsonObject.Name, jsonObject.ItemCount);
                    }
                    else
                    {
                        builder.AppendFormat("[{0}]", jsonObject.Name);
                    }
                }
            }

            return builder.ToString();
        }

        #endregion
    }
}
