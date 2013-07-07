using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.StatusEffect
{
    [Serializable]
    public class StatusEffectState : INetSerializableType
    {
        public uint Uid;
        public int Affected;
        public bool DoesExpire;
        public DateTime ExpiresAt;
        public StatusEffectFamily Family;
        public bool IsDebuff;
        public bool IsUnique;
        public string TypeName;

        public StatusEffectState(uint uid, int affected, bool doesExpire, DateTime expiresAt, StatusEffectFamily family, bool isDebuff, bool isUnique, string typeName)
        {
            Uid = uid;
            Affected = affected;
            DoesExpire = doesExpire;
            ExpiresAt = expiresAt;
            Family = family;
            IsDebuff = isDebuff;
            IsUnique = isUnique;
            TypeName = typeName;
        }
    }
}
