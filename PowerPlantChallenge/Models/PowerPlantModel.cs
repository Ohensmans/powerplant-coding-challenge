﻿using System;
using System.Collections.Generic;

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

	public void initPlant(Dictionary<string, float> fuels)
    {
        float priceGas = -1;
        float priceKerozine = -1;
        float wind = -1;

        if (this.type.Equals("gasfired"))
        {
            fuels.TryGetValue("gas(euro/MWh)", out priceGas);
            if (priceGas >= 0)
            {
                this.marginalPrice = (float)priceGas / (float)this.efficiency;
                this.pTmax = this.pmax;
                
            }
        }
        else if (this.type.Equals("turbojet"))
        {
            fuels.TryGetValue("kerosine(euro/MWh)", out priceKerozine);
            if (priceKerozine >= 0)
            {
                this.marginalPrice = (float)priceKerozine / (float)this.efficiency;
                this.pTmax = this.pmax;
            }
        }
        else if (this.type.Equals("windturbine"))
        {
            fuels.TryGetValue("wind(%)", out wind);
            if (wind >= 0)
            {
                this.marginalPrice = 0;
                this.pTmax = (float)Math.Round((wind * (float)this.pmax / 100), 1);
            }
        }
    }

}
