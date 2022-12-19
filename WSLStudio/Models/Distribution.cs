using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLStudio.Models;
public class Distribution
{
    public string Name { get; set; }
    public double MemoryLimit { get; set; } = 2.0;
    public int ProcessorLimit { get; set; } = 4;
}
