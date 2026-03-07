namespace TheContentor.Domain.Enums;

/// <summary>Controls which LLM processing pipeline is applied to a source post.</summary>
public enum ProcessingMode
{
    /// <summary>Single LLM call using the base scriptwriter prompt.</summary>
    Classic = 0,
    /// <summary>Three sequential calls: scriptwriter → creative refiner → retention critic.</summary>
    FullPipeline = 1,
    /// <summary>Refiner + critic applied to an already-processed post without rerunning the scriptwriter.</summary>
    EnhanceExisting = 2
}