using System.Collections.Generic;

public class PayLoadModel
{
	public int load { get; set; }
	public Dictionary <string, float> fuels { get; set; }
	public List<PowerPlantModel> powerplants { get; set; }
}
