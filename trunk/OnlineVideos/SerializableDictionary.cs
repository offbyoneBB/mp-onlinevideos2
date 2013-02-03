using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace OnlineVideos
{
	/// <summary>
	/// Represents an XML serializable collection of keys and values.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	[XmlRoot("dictionary")]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
	{
		#region Constants

		/// <summary>
		/// The default XML tag name for an item.
		/// </summary>
		private const string DEFAULT_ITEM_TAG = "Item";

		/// <summary>
		/// The default XML tag name for a key.
		/// </summary>
		private const string DEFAULT_KEY_TAG = "Key";

		/// <summary>
		/// The default XML tag name for a value.
		/// </summary>
		private const string DEFAULT_VALUE_TAG = "Value";

		#endregion

		#region Protected Properties

		/// <summary>
		/// Gets the XML tag name for an item.
		/// </summary>
		protected virtual string ItemTagName
		{
			get
			{
				return DEFAULT_ITEM_TAG;
			}
		}

		/// <summary>
		/// Gets the XML tag name for a key.
		/// </summary>
		protected virtual string KeyTagName
		{
			get
			{
				return DEFAULT_KEY_TAG;
			}
		}

		/// <summary>
		/// Gets the XML tag name for a value.
		/// </summary>
		protected virtual string ValueTagName
		{
			get
			{
				return DEFAULT_VALUE_TAG;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the XML schema for the XML serialization.
		/// </summary>
		/// <returns>An XML schema for the serialized object.</returns>
		public XmlSchema GetSchema()
		{
			return null;
		}

		/// <summary>
		/// Deserializes the object from XML.
		/// </summary>
		/// <param name="reader">The XML representation of the object.</param>
		public void ReadXml(XmlReader reader)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			bool wasEmpty = reader.IsEmptyElement;

			reader.Read();

			if (wasEmpty)
			{
				return;
			}

			while (reader.NodeType != XmlNodeType.EndElement)
			{
				reader.ReadStartElement(ItemTagName);

				reader.ReadStartElement(KeyTagName);
				TKey key = (TKey)keySerializer.Deserialize(reader);
				reader.ReadEndElement();

				reader.ReadStartElement(ValueTagName);
				TValue value = (TValue)valueSerializer.Deserialize(reader);
				reader.ReadEndElement();

				this.Add(key, value);

				reader.ReadEndElement();
				reader.MoveToContent();
			}

			reader.ReadEndElement();
		}

		/// <summary>
		/// Serializes this instance to XML.
		/// </summary>
		/// <param name="writer">The writer to serialize to.</param>
		public void WriteXml(XmlWriter writer)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			foreach (TKey key in this.Keys)
			{
				writer.WriteStartElement(ItemTagName);

				writer.WriteStartElement(KeyTagName);
				keySerializer.Serialize(writer, key);
				writer.WriteEndElement();

				writer.WriteStartElement(ValueTagName);
				TValue value = this[key];
				valueSerializer.Serialize(writer, value);
				writer.WriteEndElement();

				writer.WriteEndElement();
			}
		}

		#endregion
	}
}
