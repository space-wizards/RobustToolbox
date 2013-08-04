using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SS13_Shared
{
    [Serializable]
    public struct SpawnEquipDefinition
    {
        public EquipmentSlot Location { get; set; }
        public string ObjectType { get; set; }
    }

    [Serializable]
    public class JobDefinition
    {
        public bool Available = true;
        public string Description = "";
        public string JobIcon = "job-placeholder";
        public int MaxNum = 3;
        public string Name = "JOB_NULL";
        public List<SpawnEquipDefinition> SpawnEquipment = new List<SpawnEquipDefinition>();
    }

    public class JobHandler
    {
        #region Singleton

        private static JobHandler singleton;

        private JobHandler()
        {
        }

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

        private readonly XmlSerializer Serializer = new XmlSerializer(typeof (List<JobDefinition>));
        private XmlWriterSettings settings = new XmlWriterSettings();
        public List<JobDefinition> JobDefinitions { get; private set; }

        //public void CreateTemplate()
        //{
        //    List<JobDefinition> JobDefinitionsTemplate = new List<JobDefinition>();

        //    JobDefinition templateDef = new JobDefinition();
        //    templateDef.Name = "Security Officer";
        //    templateDef.Description = "Keeps the inhabitants of the station safe.";
        //    templateDef.SpawnEquipment.Add(new SpawnEquipDefinition(){Location = EquipmentSlot.Outer, ObjectType = "Atom.Item.Wearable.Outer.Armour"});
        //    templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Inner, ObjectType = "Atom.Item.Wearable.Inner.Jumpsuit.Assistant_Grey" });
        //    templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Head, ObjectType = "Atom.Item.Wearable.Head.Helmet" });
        //    templateDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Feet, ObjectType = "Atom.Item.Wearable.Feet.Shoes" });

        //    JobDefinitionsTemplate.Add(templateDef);

        //    XmlWriterSettings settings = new XmlWriterSettings();
        //    settings.Indent = true;
        //    settings.OmitXmlDeclaration = false;

        //    XmlWriter writer = XmlTextWriter.Create("JobDefinitions.xml", settings);
        //    Serializer.Serialize(writer, JobDefinitionsTemplate);
        //}

        public bool LoadDefinitionsFromFile(string path)
        {
            if (File.Exists(path))
            {
                XmlReader reader = XmlReader.Create(path); //Create reader for file.
                JobDefinitions = (List<JobDefinition>) Serializer.Deserialize(reader);
                //Deserialize and save inside class.
                return false;
            }
            else
            {
                return true;
            }
        }

        public string GetDefinitionsString()
        {
            var outStream = new StringWriter();
            Serializer.Serialize(outStream, JobDefinitions);
            return outStream.ToString();
        }

        public bool LoadDefinitionsFromString(string data)
        {
            if (data.Length > 1) //YEP. THATS TOTALLY SAFE.
            {
                var tr = new XmlTextReader(new StringReader(data));
                JobDefinitions = (List<JobDefinition>) Serializer.Deserialize(tr); //Deserialize.
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}