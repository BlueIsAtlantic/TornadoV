using System;
using System.Collections.Generic;
using System.Xml;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.IO
{
    class XMLSimpleParser
    {
        /// <summary>
        /// Lightweight function for grabbing nested xml data in a file.
        /// </summary>
        /// <param name="fileName">The path to the file.</param>
        /// <param name="dataType">The name of the xml element to be parsed.</param>
        /// <returns></returns>
        public static IEnumerable<XMLAttributesCollection> GetNestedAttributes(string fileName, string dataType)
        {
            XmlReader reader = null;
            try
            {
                reader = XmlReader.Create(fileName);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"XMLSimpleParser.GetNestedAttributes: failed to open file {fileName}");
                yield break;
            }

            while (true)
            {
                XMLAttributesCollection childAttributes = null;
                try
                {
                    if (!reader.Read())
                        break;

                    if (reader.NodeType == XmlNodeType.Element && reader.Name == dataType && reader.HasAttributes)
                    {
                        childAttributes = new XMLAttributesCollection();
                        while (reader.MoveToNextAttribute())
                        {
                            childAttributes.Add(reader.Name, reader.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CrashLogger.LogError(ex, $"XMLSimpleParser.GetNestedAttributes: reading element in file {fileName}");
                    continue; // skip faulty element
                }

                if (childAttributes != null)
                    yield return childAttributes;
            }

            reader?.Close();
        }
    }
}
