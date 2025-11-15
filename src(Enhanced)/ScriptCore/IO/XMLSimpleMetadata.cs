using System;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.IO
{
    /// <summary>
    /// Class to represent simple XML metadata.
    /// </summary>
    public abstract class XMLSimpleMetadata : IXMLSimpleMetadata
    {
        /// <summary>
        /// Method for parsing class members from a <see cref="XMLAttributesCollection"/>
        /// </summary>
        /// <param name="c"></param>
        /// <returns>The parsed metadata.</returns>
        public virtual XMLSimpleMetadata ParseAttributes(XMLAttributesCollection c)
        {
            try
            {
                // Your original parsing logic goes here (currently just returns 'this')
                return this;
            }
            catch (Exception ex)
            {
                // Use AdvancedCrashHandler to log
                CrashLogger.LogError(ex, "XMLSimpleMetadata.ParseAttributes");
                return this;
            }
        }
    }
}
