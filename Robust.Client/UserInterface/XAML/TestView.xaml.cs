using JetBrains.Annotations;
using Robust.Client.UserInterface.CustomControls;

namespace Robust.Client.UserInterface.XAML
{
    [GenerateTypedNameReferences]
    public partial class TestView : SS14Window
    {
        public TestView()
        {
            RobustXamlLoader.Load(this);
            //TEST
        }
    }
}
