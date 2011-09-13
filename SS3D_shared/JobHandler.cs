using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace SS3D_shared
{
    [Serializable]
    public struct SpawnEquipDefinition
    {
        public GUIBodyPart Location { get; set; }
        public string ObjectType { get; set; }
    }

    [Serializable]
    public class JobDefinition
    {
        public List<SpawnEquipDefinition> SpawnEquipment = new List<SpawnEquipDefinition>();
        public string Name = "";
        public string Description = "";
    }

    public class JobHandler
    {
        #region Singleton
        private static JobHandler singleton;

        private JobHandler() { }

        public static JobHandler Singleton
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new JobHandler();
                }
                return singleton;
            }
        } 
        #endregion

        public List<JobDefinition> JobDefinitions { get; private set; }
        public string JobDefinitionsString { get; private set; }

        XmlSerializer Serializer = new XmlSerializer(typeof(List<JobDefinition>));
        XmlWriterSettings settings = new XmlWriterSettings();

        public void CreateTemplate()
        {
            List<JobDefinition> JobDefinitionsTemplate = new List<JobDefinition>();

            JobDefinition templateDef = new JobDefinition();
            templateDef.Name = "Security Officer";
            templateDef.Description = "Keeps the inhabitants of the station safe.";
            templateDef.SpawnEquipment.Add(new SpawnEquipDefinition(){Location = GUIBodyPart.Outer, ObjectType = "Atom.Item.Wearable.Outer.Armour"});
            templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = GUIBodyPart.Inner, ObjectType = "Atom.Item.Wearable.Inner.Jumpsuit.Assistant_Grey" });
            templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = GUIBodyPart.Head, ObjectType = "Atom.Item.Wearable.Head.Helmet" });
            templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = GUIBodyPart.Feet, ObjectType = "Atom.Item.Wearable.Feet.Shoes" });

            JobDefinitionsTemplate.Add(templateDef);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = false;

            XmlWriter writer = XmlTextWriter.Create("JobDefinitions.xml", settings);
            Serializer.Serialize(writer, JobDefinitionsTemplate);
        }

        public bool LoadDefinitionsFromFile(string path)
        {
            if (File.Exists(path)) //This is all horribly inefficient because xmlreader only one in one direction.
            {
                XmlReader reader = XmlTextReader.Create(path); //Create reader for file.
	            StringBuilder sb = new StringBuilder();        //Create string builder.
	 
	            while (reader.Read())
	                sb.AppendLine(reader.ReadOuterXml()); //Load XML into stringbuilder.

                MemoryStream definitionsMemory = new MemoryStream(UnicodeEncoding.UTF8.GetBytes(sb.ToString())); //Load string into memory Stream (wtf)

                JobDefinitionsString = sb.ToString(); //Save to string.
                JobDefinitions = (List<JobDefinition>)Serializer.Deserialize(definitionsMemory); //Deserialize from memory stream and save inside class.

                return false;
            }
            else
            {
                return true;
            }
        }

        public bool LoadDefinitionsFromString(string data)
        {
            if (data.Length > 1) //YEP. THATS TOTALLY SAFE.
            {
                MemoryStream definitionsMemory = new MemoryStream(UnicodeEncoding.UTF8.GetBytes(data)); //Load string into memory Stream
                JobDefinitionsString = data;
                JobDefinitions = (List<JobDefinition>)Serializer.Deserialize(definitionsMemory); //Deserialize from memory stream and save inside class.
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
