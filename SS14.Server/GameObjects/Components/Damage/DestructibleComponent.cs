using System;
using System.Collections.Generic;
using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Server.Interfaces;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Deletes the entity once a certain damage threshold has been reached.
    /// </summary>
    class DestructibleComponent : Component, IOnDamageBehaviour
    {
        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.DESTRUCTIBLE;

        /// <summary>
        /// Damage threshold calculated from the values
        /// given in the prototype declaration.
        /// </summary>
        public DamageThreshold Threshold { get; private set; }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            //TODO currently only supports one threshold pair; gotta figure out YAML better

            YamlNode node;

            DamageType damageType = DamageType.Total;
            int damageValue = 0;

            if (mapping.TryGetNode("thresholdtype", out node))
            {
                string damageTypeName = node.AsString();

                try
                {
                    damageType = (DamageType) Enum.Parse(typeof(DamageType), damageTypeName);
                }
                catch (ArgumentException)
                {
                    Logger.Error(string.Format("In entity {0}, component Destructible: {1} is not a valid DamageType enum value. Setting to Total.", Owner.Name, damageTypeName));
                }
            }
            if (mapping.TryGetNode("thresholdvalue", out node))
            {
                damageValue = node.AsInt();
            }

            Threshold = new DamageThreshold(damageType, damageValue);
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent<DamageableComponent>(out DamageableComponent damageable))
            {
                damageable.DamageThresholdPassed += OnDamageThresholdPassed;
            }
        }
        
        /// <inheritdoc />
        public List<DamageThreshold> GetAllDamageThresholds()
        {
            return new List<DamageThreshold>() { Threshold };
        }

        /// <inheritdoc />
        public void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == Threshold)
            {
                Owner.EntityManager.DeleteEntity(Owner);
            }
        }
    }
}
