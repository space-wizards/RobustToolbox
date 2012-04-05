using ClientInterfaces.GOC;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class PointLightComponent : GameObjectComponent
    {
        //Contains a standard light
        private ILight _light;

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Light; }
        }
    }
}
