using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    void WslUpdate(); //v0.2
    void GetVersion();
    bool CheckWsl();
}
