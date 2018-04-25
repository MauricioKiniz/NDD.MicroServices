using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace NDDigital.Component.Core.Util
{
    public sealed class SchemaValidation
    {

        private List<string> _errors = new List<string>();

        public string[] Errors
        {
            get
            {
                return _errors.ToArray();
            }
        }

        public void Validate(string xml, string schema)
        {
            _errors.Clear();

            try
            {
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, XmlReader.Create(new StringReader(schema)));

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas.Add(schemaSet);
                /*settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;*/
                settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

                // Create the XmlReader object.
                XmlReader reader = XmlReader.Create(new StringReader(xml), settings);

                // Parse the file. 
                while (reader.Read()) ;
            } catch(Exception e)
            {
                throw e;
            }
        }

        private void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
                _errors.Add(e.Message);
        }
    }
}
