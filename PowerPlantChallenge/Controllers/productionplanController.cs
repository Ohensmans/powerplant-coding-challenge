using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerPlantChallenge.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class productionplan : ControllerBase
    {
        [HttpPost]
        public List<PowerProducedModel> Post(PayLoadModel payload)
        {
            //insure a list init only with plants with a given load and sort by marginalPrice and then by pmin and the by p max at time T
            List<PowerPlantModel> lPlants = initListPowerPlants(payload).OrderBy(p => p.marginalPrice).ThenBy(p => p.pmin).ThenByDescending(p => p.pTmax).ToList();


        }

        private List<PowerPlantModel> initListPowerPlants (PayLoadModel payload)
        {
            //value -1 to test if the value is given
            float priceGas = -1;
            float priceKerozine = -1;
            float wind = -1;
            payload.fuels.TryGetValue("gas(euro/MWh)", out priceGas);
            payload.fuels.TryGetValue("kerosine(euro/MWh)", out priceKerozine);
            payload.fuels.TryGetValue("wind(%)", out wind);

            //list to return including only the plants where a payload was given
            List<PowerPlantModel> lPlants = new List<PowerPlantModel>();

            foreach (PowerPlantModel plant in payload.powerplants)
            {
                if (plant.type == TypePlant.gasfired)
                {
                    if (priceGas > 0)
                    {
                        plant.marginalPrice = priceGas / plant.effiency;
                        lPlants.Add(plant);
                    }
                }
                else if (plant.type == TypePlant.turbojet)
                {
                    if (priceKerozine > 0)
                    {
                        plant.marginalPrice = priceKerozine / plant.effiency;
                        plant.pTmax = plant.pmax;
                        lPlants.Add(plant);
                    }
                }
                else if (plant.type == TypePlant.windturbine)
                {
                    if (wind > 0)
                    {
                        plant.marginalPrice = 0;
                        plant.pTmax = wind * plant.pmax;
                        lPlants.Add(plant);
                    }
                }
            }
            return lPlants;
        }


        //start value of startIndex : 0
        private List<float> optimalProduction(List<PowerPlantModel> lPlants, List<float> lProduction, List<float> lProductionOptimum, float powerneed, float minProductionPrice, float currentPrice)
        {

            


            //the idea here is that lPlants is sorted ascending by margin costs so we repeat until powerneed < pMax
            float supplyMax = lPlants[lProduction.Count].pTmax;
            float supplyMin;
            float priceMin;

            if (lPlants[lProduction.Count].type != TypePlant.windturbine)
            {
                priceMin = (lPlants[lProduction.Count].marginalPrice * lPlants[lProduction.Count].pmin);
                supplyMin = lPlants[lProduction.Count].pmin;
            }
            else
            {
                priceMin = (lPlants[lProduction.Count].marginalPrice * lPlants[lProduction.Count].pTmax);
                supplyMin = lPlants[lProduction.Count].pTmax;
            }

            int indexTest = lProduction.FindLastIndex(p => p != 0);
            //exit from the loop if only 0 for all plant except last one & last one with not enough production
            if (indexTest == -1 && lProduction.Count+1 == lPlants.Count && supplyMax < powerneed) {
                return lProductionOptimum;
            }


            //if the price for the pmin is higher than a minimum production already found go to the next plant
            if(currentPrice + priceMin > minProductionPrice)
            {
                //if it's the last plant find the last index of lProduction[i] != 0 and change it to 0
                //also change powerneed and currentPrice to match the new test
                if(lProduction.Count-1 == lPlants.Count)
                {
                    int i = lProduction.FindLastIndex(p => p != 0);
                    
                    //exit from the loop
                    if(i == -1)
                    {
                        return lProductionOptimum;
                    }
                    else { 
                        powerneed += lProduction[i];
                        lProduction[i] = 0;
                        for(int j = lProduction.Count - 1; j>i; j--)
                        {
                            lProduction.RemoveAt(j);
                        }
                        return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed, minProductionPrice, currentPrice, startIndex);
                    }
                }
                //jump to the next plant (there's maybe a plant with pmin small enough to compensate an higher margin cost)
                else
                {
                    lProduction.Add(0);
                    return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed, minProductionPrice, currentPrice, startIndex);
                }

            }

            //if powerneed is smaller than the pmax of the next Plant then produce the max in that plant 
            if (powerneed> supplyMax)
            {
                float price = lPlants[lProduction.Count].marginalPrice * supplyMax;
                lProduction.Add(supplyMax);
                return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed- supplyMax, minProductionPrice, currentPrice+price, startIndex);
            }

            //if powerneed is equal to the production 
            if(powerneed <= supplyMax && powerneed>=supplyMin && (lPlants[lProduction.Count].marginalPrice * powerneed)+currentPrice < minProductionPrice)
            {
                lProductionOptimum = lProduction;
                lProductionOptimum.Add(powerneed);
                minProductionPrice = currentPrice + (lPlants[lProduction.Count].marginalPrice * powerneed);
                if ()
                lProduction = new List<float>();
                
                return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed- supplyMax, minProductionPrice, currentPrice);
            }

            if(powerneed < lPlants[lProduction.Count].pTmax && powerneed >= lPlants[lProduction.Count].pTmax && lPlants[lProduction.Count].type != TypePlant.windturbine) { }


            
        }


    }
}