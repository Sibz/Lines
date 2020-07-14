using System.Collections.Generic;

namespace Sibz.Lines
{
    public static class ToolResourcePaths
    {
        public static readonly Dictionary<ushort, string> Paths = new Dictionary<ushort, string>
                                                                  {
                                                                      {
                                                                          (ushort) PlayerToolType.Line1,
                                                                          "Prefabs/Line1Tool"
                                                                      }
                                                                  };
    }
}