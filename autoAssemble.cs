public const string TARGET_ASSEMBLER_NAME = "Auto-Assembler";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
List<IMyAssembler> assemblers = new List<IMyAssembler>();

Dictionary<string, int> componentQuotas = new Dictionary<string, int>
{
    {"SteelPlate", 50000},
    {"Construction", 30000},
    {"Computer", 5000},
    {"InteriorPlate", 25000},
    {"SmallTube", 20000},
    {"Motor", 20000},
    {"Girder", 10000},
    {"LargeTube", 10000},
    {"MetalGrid", 10000},
    {"PowerCell",10000}
};

void QueueComponents(MyItemType component, decimal number)
{
    var targetAssemblers = new List<IMyAssembler>();
    GridTerminalSystem.GetBlocksOfType(targetAssemblers, x => x.CustomName.Contains(TARGET_ASSEMBLER_NAME));
    int numAssemblers = targetAssemblers.Count;

    if (numAssemblers == 0)
    {
        Echo($"Assembler '{TARGET_ASSEMBLER_NAME}' not found.");
        return;  
    }

    decimal splitAmount = Math.Ceiling(number / numAssemblers);

    foreach (var assembler in targetAssemblers)
    {
        MyDefinitionId blueprint = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + ComponentToBlueprint(component));

        if (!assembler.CanUseBlueprint(blueprint))
        {
            Echo($"Assembler '{assembler.CustomName}' can't produce '{component.SubtypeId}'.");
        }
        else
        {
            assembler.AddQueueItem(blueprint, splitAmount);
            Echo($"Queued {splitAmount} {component.SubtypeId} in '{assembler.CustomName}'.");
        }
    }
}



string ComponentToBlueprint(MyItemType component)
{
    switch (component.SubtypeId)
    {
        case "Computer":
            return "ComputerComponent";
        case "Girder":
            return "GirderComponent";
        case "Construction":
            return "ConstructionComponent";
        default:
            return component.SubtypeId;
    }
}

int GetNumberComponents(MyItemType component)
{
    GridTerminalSystem.GetBlocksOfType(cargos);
    GridTerminalSystem.GetBlocksOfType(assemblers, x => x.CustomName.Contains(TARGET_ASSEMBLER_NAME));

    int componentCount = 0;
    List<MyProductionItem> productionQueue = new List<MyProductionItem>();

    foreach (IMyAssembler assembler in assemblers)
    {
        componentCount += assembler.OutputInventory.GetItemAmount(component).ToIntSafe();
        assembler.GetQueue(productionQueue);
        foreach (MyProductionItem queuedItem in productionQueue)
        {
            if (queuedItem.BlueprintId.ToString().Contains(component.SubtypeId))
            {
                componentCount += queuedItem.Amount.ToIntSafe();
            }
        }
    }

    foreach (IMyCargoContainer cargo in cargos)
    {
        componentCount += cargo.GetInventory().GetItemAmount(component).ToIntSafe();
    }

    return componentCount;
}

void CheckComponentQuota(string component, int quota)
{
    MyItemType componentType = new MyItemType("MyObjectBuilder_Component", component);
    int numComponents = GetNumberComponents(componentType);
    if (numComponents < quota)
    {
        QueueComponents(componentType, quota - numComponents);
    }
    else
    {
        Echo("Number of " + component + "s: " + numComponents);
    }
}

public void Main()
{
    foreach (var componentQuota in componentQuotas)
    {
        CheckComponentQuota(componentQuota.Key, componentQuota.Value);
    }
}
