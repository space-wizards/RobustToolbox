using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using XamlX.Ast;
using XamlX.IL;
using XamlX.Parsers;

namespace Robust.Client.UI
{
    public partial class TestView : SS14Window
    {
        public TestView()
        {
            var content = File.ReadAllText("../../Robust.Client/UI/TestView.xaml");

            var thing = XDocumentXamlParser.Parse(content);

            if (thing.Root is XamlAstObjectNode objectNode)
            {
                AddChild(ParseNode(objectNode));
            }

            System.Console.WriteLine("aaa");
        }

        Control ParseNode(XamlAstObjectNode node)
        {
            foreach (var astNode in node.Children)
            {
                switch (astNode)
                {
                    case XamlAstObjectNode objNode:
                        var type = objNode.Type.GetClrType();
                        break;
                    case XamlAstXamlPropertyValueNode valueNode:
                        break;
                }
            }

            return null;
        }
    }
}
