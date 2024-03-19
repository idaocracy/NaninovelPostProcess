using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel.Commands;
using Naninovel;

public abstract class SpawnPostProcessing : SpawnEffect
{
    protected abstract string PostProcessName { get;} 

    public StringParameter Id;
    protected override string Path => ResolvePath();

    protected virtual string ResolvePath()
    {
        if (Assigned(Id)) return $"{PostProcessName}#{Id}";
        else return $"{PostProcessName}";
    }
}
