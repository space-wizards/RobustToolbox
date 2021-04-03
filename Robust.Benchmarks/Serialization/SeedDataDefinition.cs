using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Benchmarks.Serialization
{
    [Prototype("seed")]
    internal class SeedDataDefinition : IPrototype
    {
        public const string Prototype = @"
- type: seed
  id: tobacco
  name: tobacco
  seedName: tobacco
  displayName: tobacco plant
  plantRsi: Objects/Specific/Hydroponics/tobacco.rsi
  productPrototypes:
    - LeavesTobacco
  harvestRepeat: Repeat
  lifespan: 75
  maturation: 5
  production: 5
  yield: 2
  potency: 20
  growthStages: 3
  idealLight: 9
  idealHeat: 298
  chemicals:
    chem.Nicotine:
      Min: 1
      Max: 10
      PotencyDivisor: 10";

        private const string SeedPrototype = "SeedBase";

        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; private init; } = default!;

        /// <summary>
        ///     Unique identifier of this seed. Do NOT set this.
        /// </summary>
        public int Uid { get; internal set; } = -1;

        #region Tracking

        [ViewVariables] [DataField("name")] public string Name { get; set; } = string.Empty;
        [ViewVariables] [DataField("seedName")] public string SeedName { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("seedNoun")]
        public string SeedNoun { get; set; } = "seeds";
        [ViewVariables] [DataField("displayName")] public string DisplayName { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("roundStart")]
        public bool RoundStart { get; private set; } = true;
        [ViewVariables] [DataField("mysterious")] public bool Mysterious { get; set; }
        [ViewVariables] [DataField("immutable")] public bool Immutable { get; set; }
        #endregion

        #region Output

        [ViewVariables]
        [DataField("productPrototypes")]
        public List<string> ProductPrototypes { get; set; } = new();

        [ViewVariables]
        [DataField("chemicals")]
        public Dictionary<string, SeedChemQuantity> Chemicals { get; set; } = new();

        [ViewVariables]
        [DataField("consumeGasses")]
        public Dictionary<Gas, float> ConsumeGasses { get; set; } = new();

        [ViewVariables]
        [DataField("exudeGasses")]
        public Dictionary<Gas, float> ExudeGasses { get; set; } = new();
        #endregion

        #region Tolerances

        [ViewVariables]
        [DataField("nutrientConsumption")]
        public float NutrientConsumption { get; set; } = 0.25f;

        [ViewVariables] [DataField("waterConsumption")] public float WaterConsumption { get; set; } = 3f;
        [ViewVariables] [DataField("idealHeat")] public float IdealHeat { get; set; } = 293f;
        [ViewVariables] [DataField("heatTolerance")] public float HeatTolerance { get; set; } = 20f;
        [ViewVariables] [DataField("idealLight")] public float IdealLight { get; set; } = 7f;
        [ViewVariables] [DataField("lightTolerance")] public float LightTolerance { get; set; } = 5f;
        [ViewVariables] [DataField("toxinsTolerance")] public float ToxinsTolerance { get; set; } = 4f;

        [ViewVariables]
        [DataField("lowPressureTolerance")]
        public float LowPressureTolerance { get; set; } = 25f;

        [ViewVariables]
        [DataField("highPressureTolerance")]
        public float HighPressureTolerance { get; set; } = 200f;

        [ViewVariables]
        [DataField("pestTolerance")]
        public float PestTolerance { get; set; } = 5f;

        [ViewVariables]
        [DataField("weedTolerance")]
        public float WeedTolerance { get; set; } = 5f;
        #endregion

        #region General traits

        [ViewVariables]
        [DataField("endurance")]
        public float Endurance { get; set; } = 100f;
        [ViewVariables] [DataField("yield")] public int Yield { get; set; }
        [ViewVariables] [DataField("lifespan")] public float Lifespan { get; set; }
        [ViewVariables] [DataField("maturation")] public float Maturation { get; set; }
        [ViewVariables] [DataField("production")] public float Production { get; set; }
        [ViewVariables] [DataField("growthStages")] public int GrowthStages { get; set; } = 6;
        [ViewVariables] [DataField("harvestRepeat")] public HarvestType HarvestRepeat { get; set; } = HarvestType.NoRepeat;

        [ViewVariables] [DataField("potency")] public float Potency { get; set; } = 1f;
        // No, I'm not removing these.
        //public PlantSpread Spread { get; set; }
        //public PlantMutation Mutation { get; set; }
        //public float AlterTemperature { get; set; }
        //public PlantCarnivorous Carnivorous { get; set; }
        //public bool Parasite { get; set; }
        //public bool Hematophage { get; set; }
        //public bool Thorny { get; set; }
        //public bool Stinging { get; set; }
        [DataField("ligneous")]
        public bool Ligneous { get; set; }
        // public bool Teleporting { get; set; }
        // public PlantJuicy Juicy { get; set; }
        #endregion

        #region Cosmetics

        [ViewVariables]
        [DataField("plantRsi", required: true)]
        public ResourcePath PlantRsi { get; set; } = default!;

        [ViewVariables]
        [DataField("plantIconState")]
        public string PlantIconState { get; set; } = "produce";

        [ViewVariables]
        [DataField("bioluminescent")]
        public bool Bioluminescent { get; set; }

        [ViewVariables]
        [DataField("bioluminescentColor")]
        public Color BioluminescentColor { get; set; } = Color.White;

        [ViewVariables]
        [DataField("splatPrototype")]
        public string? SplatPrototype { get; set; }

        #endregion
    }

    internal enum HarvestType
    {
        NoRepeat
    }

    internal enum SeedChemQuantity {}

    internal enum Gas {}
}
