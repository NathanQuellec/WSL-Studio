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

}
