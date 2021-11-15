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

            //set maxPrice to the max possible price
            float maxPrice = lPlants.Last().marginalPrice * payload.load;
            List<float> optimum = optimalProduction(lPlants, new List<float>(), new List<float>(), payload.load, maxPrice, 0);

            List<PowerProducedModel> lresult = new List<PowerProducedModel>();

            for(int i = 0; i < lPlants.Count; i++)
            {
                float prod;
                if (optimum.Count - 1 < i)
                {
                    prod = 0;
                }
                else
                {
                    prod = optimum[i];
                }
                PowerProducedModel result = new PowerProducedModel(lPlants[i].name, prod);
                lresult.Add(result);
            }

            return lresult;
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
                if (plant.type.Equals("gasfired"))
                {
                    if (priceGas > 0)
                    {
                        plant.marginalPrice = (float)priceGas / (float)plant.efficiency;
                        plant.pTmax = plant.pmax;
                        lPlants.Add(plant);
                    }
                }
                else if (plant.type.Equals("turbojet"))
                {
                    if (priceKerozine > 0)
                    {
                        plant.marginalPrice = (float)priceKerozine / (float)plant.efficiency;
                        plant.pTmax = plant.pmax;
                        lPlants.Add(plant);
                    }
                }
                else if (plant.type.Equals("windturbine"))
                {
                    if (wind > 0)
                    {
                        plant.marginalPrice = 0;
                        plant.pTmax = wind * (float)plant.pmax/100;
                        lPlants.Add(plant);
                    }
                }
            }
            return lPlants;
        }

        
        //adapt the varaible to the type of plant
        //(windturbine is on or off in the opposite of the other who can produce between a min and a max)
        private float getPriceMin (PowerPlantModel currentPlant)
        {
            if (!currentPlant.type.Equals("windturbine"))
            {
                return(currentPlant.marginalPrice * currentPlant.pmin);
            }
            else
            {
                return (currentPlant.marginalPrice * currentPlant.pTmax);
            }
        }

        //adapt the varaible to the type of plant
        //(windturbine is on or off in the opposite of the other who can produce between a min and a max)
        private float getSupplyMin (PowerPlantModel currentPlant)
        {
            if (!currentPlant.type.Equals("windturbine"))
            {
                return currentPlant.pmin;
            }
            else
            {
                return currentPlant.pTmax;
            }
        }


        private List<float> optimalProduction(List<PowerPlantModel> lPlants, List<float> lProduction, List<float> lProductionOptimum, float powerneed, float minProductionPrice, float currentPrice)
        {
            //if it's the last plant then find the last index of lProduction[i] != 0 and change it to 0
            //also change powerneed and currentPrice to match the new test
            if (lProduction.Count - 1 == lPlants.Count)
            {
                int i = lProduction.FindLastIndex(p => p != 0);

                //exit from the loop
                if (i == -1)
                {
                    return lProductionOptimum;
                }
                else
                {
                    powerneed += lProduction[i];
                    lProduction[i] = 0;
                    for (int j = lProduction.Count - 1; j > i; j--)
                    {
                        lProduction.RemoveAt(j);
                    }
                    return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed, minProductionPrice, currentPrice);
                }
            }


            //the idea here is that lPlants is sorted ascending by margin costs so we repeat until powerneed < pMax
            PowerPlantModel currentPlant = lPlants[lProduction.Count];
            float supplyMax = currentPlant.pTmax;
            float supplyMin = getSupplyMin(currentPlant);
            float priceMin = getPriceMin(currentPlant);

            //if the price for the pmin is higher than a minimum production already found then go to the next plant
            if (currentPrice + priceMin > minProductionPrice)
            {              
                //jump to the next plant (there's maybe a plant with pmin small enough to compensate an higher margin cost)
            }

            //if powerneed is smaller than the pmax of the Plant then produce the max in that plant 
            else if (powerneed> supplyMax)
            {
                float price = currentPlant.marginalPrice * supplyMax;
                lProduction.Add(supplyMax);
                return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed-supplyMax, minProductionPrice, currentPrice+price);
            }

            //if powerneed is between pmin and pmax of the plant and that the solution found is a new optimum
            else if(powerneed <= supplyMax && powerneed>=supplyMin && (currentPlant.marginalPrice * powerneed)+currentPrice < minProductionPrice)
            {
                lProductionOptimum = lProduction;
                lProductionOptimum.Add(powerneed);
                minProductionPrice = currentPrice + (currentPlant.marginalPrice * powerneed);
            }

            //if powerneed is below the min for the current plant
            //the idea is to reduce one or more plant already in the list of production to meet the pmin of that one
            else if(powerneed < supplyMin)
            {
                float minusPower = supplyMin - powerneed;
                List<float> lProductionTest = lProduction;
                float currentPriceTest = currentPrice;
                int i = lProduction.Count -1;

                while (minusPower > 0 && i>=0)
                {
                    if(lProductionTest[i]> 0)
                    {
                        //minusPower is more than the whole production of the i plant
                        if (minusPower >= lProductionTest[i])
                        {
                            minusPower -= lProductionTest[i];
                            currentPriceTest -= (lProductionTest[i]*lPlants[i].marginalPrice);
                            lProductionTest[i] = 0;
                        }
                        //minusPower is less than the whole production of the i plant and also more than its min production
                        else if (minusPower <= lProductionTest[i] - getSupplyMin(lPlants[i]))
                        {
                            lProductionTest[i] -= minusPower;
                            currentPriceTest -= (minusPower*lPlants[i].marginalPrice);
                            minusPower = 0;
                        }
                        //minusPower is more than the min production of the i plant but less than the actual production
                        //a plant cannot produce less than his pmin (or 0)
                        //so put at min that plant and go check the other below the list
                        //to remember the list is sorted by ascending marging cost
                        else                        
                        {
                            minusPower -= (lProductionTest[i] - getSupplyMin(lPlants[i]));
                            currentPriceTest -= (lProductionTest[i] - getSupplyMin(lPlants[i]))*lPlants[i].marginalPrice;
                            lProductionTest[i] = getSupplyMin(lPlants[i]);
                        }
                    }                   
                    i--;
                }

                //if sucessfully reduce other plant -> register the new optimum
                if (minusPower == 0)
                {
                    currentPriceTest += powerneed*currentPlant.marginalPrice;
                    if (currentPriceTest < minProductionPrice)
                    {
                        lProductionTest.Add(powerneed);
                        lProductionOptimum = lProductionTest;
                        minProductionPrice = currentPriceTest;
                    }
                }
            }

            //try the next solution
            lProduction.Add(0);
            return optimalProduction(lPlants, lProduction, lProductionOptimum, powerneed, minProductionPrice, currentPrice);
 
        }


    }
}