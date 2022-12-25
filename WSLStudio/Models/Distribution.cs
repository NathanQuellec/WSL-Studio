using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLStudio.Models;
public record Distribution
{
    public Guid Id { get; init; }
    public string Path { get; set; }
    public bool IsDefault { get; set; }
    public int WslVersion { get; set; }
    public string Name { get; set; }
    public double MemoryLimit { get; set; } = 2.0;
    public int ProcessorLimit { get; set; } = 4;
}
