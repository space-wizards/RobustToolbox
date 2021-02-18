using System;

namespace Robust.Client.UserInterface.XAML
{
    public class RobustXamlLoader
    {
        public static void Load(object obj)
        {
            throw new Exception(
                $"No precompiled XAML found for {obj.GetType()}, make sure to specify Class or name your class the same as your .xaml ");
        }
    }
}
