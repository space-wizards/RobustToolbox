using SFML.Graphics;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SS14.Shared
{
    [Serializable]
    public struct DepartmentDefinition
    {
        public string Name;
        public string Description;
        public string DepartmentIcon;

        public string DepartmentColorHex;

        [XmlIgnore]
        public Color DepartmentColor
        {
            get { return ColorUtils.FromHex(DepartmentColorHex); }
            set { DepartmentColorHex = $"#{value.ToInt():X8}"; }
        }
    }

    [Serializable]
    public struct JobSettings
    {
        public List<JobDefinition> JobDefinitions;
        public List<DepartmentDefinition> DepartmentDefinitions;
    }

    [Serializable]
    public struct SpawnEquipDefinition
    {
        public EquipmentSlot Location { get; set; }
        public string ObjectType { get; set; }
    }

    [Serializable]
    public struct SpawnInventoryDefinition // Created an enum just in case more paramaters needed to be added.
    {
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
        public string Department = "";
        public List<SpawnEquipDefinition> SpawnEquipment = new List<SpawnEquipDefinition>();
        public List<SpawnInventoryDefinition> SpawnInventory = new List<SpawnInventoryDefinition>();
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

        private readonly XmlSerializer Serializer = new XmlSerializer(typeof(JobSettings));
        private XmlWriterSettings settings = new XmlWriterSettings();
        public JobSettings JobSettings { get; private set; }

        public void CreateTemplate()
        {
            JobSettings JobSettingsTemplate = new JobSettings();

            JobDefinition jobDef = new JobDefinition();
            jobDef.Name = "Security Officer";
            jobDef.Description = "Keeps the inhabitants of the station safe.";
            jobDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Outer, ObjectType = "Atom.Item.Wearable.Outer.Armour" });
            jobDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Inner, ObjectType = "Atom.Item.Wearable.Inner.Jumpsuit.Assistant_Grey" });
            jobDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Head, ObjectType = "Atom.Item.Wearable.Head.Helmet" });
            jobDef.SpawnEquipment.Add(new SpawnEquipDefinition() { Location = EquipmentSlot.Feet, ObjectType = "Atom.Item.Wearable.Feet.Shoes" });
            jobDef.Department = "Security";
            jobDef.SpawnInventory.Add(new SpawnInventoryDefinition() { ObjectType = "Sword" });

            JobSettingsTemplate.JobDefinitions = new List<JobDefinition> {jobDef};

            DepartmentDefinition depDef = new DepartmentDefinition();
            depDef.DepartmentIcon = "department_security";
            depDef.Description = "The security department handles the security of the station in all matters.";
            depDef.Name = "Security";
            depDef.DepartmentColor = new Color(125, 125, 125);

            JobSettingsTemplate.DepartmentDefinitions = new List<DepartmentDefinition>() {depDef};

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = false;

            XmlWriter writer = XmlTextWriter.Create("JobDefinitions.xml", settings);
            Serializer.Serialize(writer, JobSettingsTemplate);
        }

        public bool LoadDefinitionsFromFile(string path)
        {
            if (File.Exists(path))
            {
                XmlReader reader = XmlReader.Create(path); //Create reader for file.
                JobSettings = (JobSettings)Serializer.Deserialize(reader);
                //Deserialize and save inside class.
                return false;
            }
            else
            {
                CreateTemplate();
                return true;
            }
        }

        public string GetDefinitionsString()
        {
            var outStream = new StringWriter();
            Serializer.Serialize(outStream, JobSettings);
            return outStream.ToString();
        }

        public bool LoadDefinitionsFromString(string data)
        {
            if (data.Length > 1) //YEP. THATS TOTALLY SAFE.
            {
                var tr = new XmlTextReader(new StringReader(data));
                JobSettings = (JobSettings)Serializer.Deserialize(tr); //Deserialize.
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}