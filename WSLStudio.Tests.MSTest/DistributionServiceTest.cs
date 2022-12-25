using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLStudio.Contracts.Services;
using WSLStudio.Services;
using WSLStudio.Models;

namespace WSLStudio.Tests.MSTest;

[TestClass]
public class DistributionServiceTest
{
    private readonly IDistributionService _distributionService = new DistributionService();

    [TestMethod]
    public void TestGetAllDistributions()
    {
        var distros = _distributionService.GetAllDistributions();
        Assert.IsNotNull(distros);
        Assert.IsInstanceOfType(distros, typeof(IList<Distribution>));
    }

    [TestMethod]
    public void TestGetDistribution()
    {
        var id = 0;
        Distribution distro = _distributionService.GetDistribution(id);
        Assert.IsNotNull(distro);
        Assert.IsInstanceOfType(distro, typeof(Distribution));
    }

    [TestMethod]
    public void TestAddDistribution()
    {
        Distribution newDistro = new Distribution { 
            Name = "UbuntuTest",
            MemoryLimit= 8.0,
            ProcessorLimit= 2,
        };

        _distributionService.AddDistribution(newDistro);
        var distrosList = _distributionService.GetAllDistributions();
        Assert.IsTrue(distrosList.Contains(newDistro));    
    }
}
