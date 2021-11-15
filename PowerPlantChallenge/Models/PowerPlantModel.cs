using System;

/// <summary>
/// Summary description for Class1
/// </summary>
public class PowerPlantModel
{
	public string name { get; set; }
	public string type { get; set; }
	public float efficiency { get; set; }
	public int pmin { get; set; }
	public int pmax { get; set; }

	//pMax at Time T (different for windturbines)
	public float pTmax { get; set; }

	//marginalPrice = price/effiency
	public float marginalPrice {set; get;}

}
