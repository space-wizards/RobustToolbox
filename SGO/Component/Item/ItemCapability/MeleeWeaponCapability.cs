using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class MeleeWeaponCapability : ItemCapability
    {
        public int damageAmount = 10;
        public DamageType damType = DamageType.Bludgeoning;

        public MeleeWeaponCapability()
        {
            CapabilityType = SS3D_shared.GO.ItemCapabilityType.MeleeWeapon;
            capabilityName = "MeleeCapability";
            interactsWith = InteractsWith.Actor | InteractsWith.LargeObject;
        }

        public override bool ApplyTo(Entity target)
        {
            if (target.HasComponent(SS3D_shared.GO.ComponentFamily.Damageable))
            {
                target.SendMessage(this, ComponentMessageType.Damage, null, owner.Owner, damageAmount, damType);
                return true;
            }
            return false;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "damageAmount":
                    if (parameter.ParameterType == typeof(int))
                        damageAmount = (int)parameter.Parameter;
                    if (parameter.ParameterType == typeof(string))
                        damageAmount = int.Parse((string)parameter.Parameter);
                    break;
                case "damageType":
                    if (parameter.ParameterType == typeof(string))
                    {
                        //Try to parse it. Set to Bludgeoning damagetype if parsing fails
                        if(!Enum.TryParse<DamageType>((string)parameter.Parameter, true, out damType))
                            damType = DamageType.Bludgeoning;
                    }
                    else if (parameter.ParameterType == typeof(DamageType))
                    {
                        damType = (DamageType)parameter.Parameter;
                    }
                    break;
            }
        }
    }
}
