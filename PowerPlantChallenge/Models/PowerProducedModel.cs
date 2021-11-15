using System;

public class PowerProducedModel
{
    public PowerProducedModel(string name, float p)
    {
        this.name = name;
        this.p = p;
    }

    public string name { get; set; }
    public float p { get; set; }
}
