using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SS14.Shared.IoC;
using SS14.Server.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;

namespace SS14.UnitTesting.SS14.Shared.IoC
{
    [TestClass]
   
    public class IoCManager_Test : SS14UnitTest
    {
        [TestMethod]
        public void ResolveIConfigurationManager_ShouldReturnConfigurationManager()
        {
           var temp = IoCManager.Resolve<IServerConfigurationManager>();

           Assert.IsTrue(temp == typeof(IServerConfigurationManager));
        }

        [TestMethod]    
        public void ResolveIResourceManager_ShouldReturnResourceManager()
        {
            var temp = IoCManager.Resolve<IResourceManager>();

            Assert.IsTrue(temp == typeof(IResourceManager));
        }

       
    }
}
