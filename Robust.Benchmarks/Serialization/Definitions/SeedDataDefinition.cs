using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Benchmarks.Serialization.Definitions
{
    [Prototype("seed")]
    public class SeedDataDefinition : IPrototype
    {
        public const string Prototype = @"
- type: seed
  id: tobacco
  name: tobacco
  seedName: tobacco
  displayName: tobacco plant
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

        [DataField("id", required: true)] public string ID { get; set; } = default!;

        #region Tracking
        [DataField("name")] public string Name { get; set; } = string.Empty;
        [DataField("seedName")] public string SeedName { get; set; } = string.Empty;
        [DataField("seedNoun")] public string SeedNoun { get; set; } = "seeds";
        [DataField("displayName")] public string DisplayName { get; set; } = string.Empty;
        [DataField("roundStart")] public bool RoundStart { get; set; } = true;
        [DataField("mysterious")] public bool Mysterious { get; set; }
        [DataField("immutable")] public bool Immutable { get; set; }
        #endregion

        #region Output
        [DataField("productPrototypes")]
        public List<string> ProductPrototypes { get; set; } = new();
        [DataField("chemicals")]
        public Dictionary<string, SeedChemQuantity> Chemicals { get; set; } = new();
        [DataField("consumeGasses")]
        public Dictionary<Gas, float> ConsumeGasses { get; set; } = new();
        [DataField("exudeGasses")]
        public Dictionary<Gas, float> ExudeGasses { get; set; } = new();
        #endregion

        #region Tolerances
        [DataField("nutrientConsumption")] public float NutrientConsumption { get; set; } = 0.25f;
        [DataField("waterConsumption")] public float WaterConsumption { get; set; } = 3f;
        [DataField("idealHeat")] public float IdealHeat { get; set; } = 293f;
        [DataField("heatTolerance")] public float HeatTolerance { get; set; } = 20f;
        [DataField("idealLight")] public float IdealLight { get; set; } = 7f;
        [DataField("lightTolerance")] public float LightTolerance { get; set; } = 5f;
        [DataField("toxinsTolerance")] public float ToxinsTolerance { get; set; } = 4f;
        [DataField("lowPressureTolerance")] public float LowPressureTolerance { get; set; } = 25f;
        [DataField("highPressureTolerance")] public float HighPressureTolerance { get; set; } = 200f;
        [DataField("pestTolerance")] public float PestTolerance { get; set; } = 5f;
        [DataField("weedTolerance")] public float WeedTolerance { get; set; } = 5f;
        #endregion

        #region General traits
        [DataField("endurance")] public float Endurance { get; set; } = 100f;
        [DataField("yield")] public int Yield { get; set; }
        [DataField("lifespan")] public float Lifespan { get; set; }
        [DataField("maturation")] public float Maturation { get; set; }
        [DataField("production")] public float Production { get; set; }
        [DataField("growthStages")] public int GrowthStages { get; set; } = 6;
        [DataField("harvestRepeat")] public HarvestType HarvestRepeat { get; set; } = HarvestType.NoRepeat;
        [DataField("potency")] public float Potency { get; set; } = 1f;
        [DataField("ligneous")] public bool Ligneous { get; set; }
        #endregion

        #region Cosmetics
        [DataField("plantRsi", required: true)] public ResourcePath PlantRsi { get; set; } = default!;
        [DataField("plantIconState")] public string PlantIconState { get; set; } = "produce";
        [DataField("bioluminescent")] public bool Bioluminescent { get; set; }
        [DataField("bioluminescentColor")] public Color BioluminescentColor { get; set; } = Color.White;
        [DataField("splatPrototype")] public string? SplatPrototype { get; set; }
        #endregion
    }

    public enum HarvestType
    {
        NoRepeat,
        Repeat
    }

    public enum Gas
    {
    }

    [DataDefinition]
    public struct SeedChemQuantity
    {
        [DataField("Min")]
        public int Min;

        [DataField("Max")]
        public int Max;

        [DataField("PotencyDivisor")]
        public int PotencyDivisor;
    }
}
