using System;
using System.Collections.Generic;

namespace SOPServer.Service.Models
{
    public class CityLocationModel
    {
        public string Name { get; set; }
        public string? LocalName { get; set; }
        public double Latitude { get; set; }
  public double Longitude { get; set; }
   public string? Country { get; set; }
      public string? State { get; set; }
    }

    public class CitySearchResponse
 {
        public List<CityLocationModel> Cities { get; set; } = new List<CityLocationModel>();
    }
}
