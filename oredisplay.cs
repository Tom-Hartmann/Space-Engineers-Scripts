List<string> ores = new List<string> {"Stone", "Iron", "Nickel", "Cobalt", "Magnesium", "Silicon", "Silver", "Gold", "Platinum", "Uranium", "Ice"};
List<int> oreLimits = new List<int> {1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000};
List<string> ingots = new List<string> {"Gravel", "Iron", "Nickel", "Cobalt", "Magnesium", "Silicon", "Silver", "Gold", "Platinum", "Uranium"};
List<int> ingotLimits = new List<int> {1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000, 1000000};

int defaultItemLimit = 50000;

void Main() {
    var displayOres = GridTerminalSystem.GetBlockWithName("Ores Display") as IMyTextPanel;
    var displayIngots = GridTerminalSystem.GetBlockWithName("Ingots Display") as IMyTextPanel;
    var displayProgress = GridTerminalSystem.GetBlockWithName("Progress Display") as IMyTextPanel;
    var displayInventory = GridTerminalSystem.GetBlockWithName("Inventory Display") as IMyTextPanel;

    if (displayOres == null || displayIngots == null || displayProgress == null || displayInventory == null) {
        Echo("Error: One or more display(s) not found.");
        return;
    }
    
    displayOres.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    displayOres.FontSize = 0.5f;
    displayOres.WriteText("<< Ore summary >>\n\n", false);
    DisplayOresAndIngotsSummary(displayOres, ores, oreLimits, "Ore");

    displayIngots.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    displayIngots.FontSize = 0.5f;
    displayIngots.WriteText("<< Ingot summary >>\n\n", false);
    DisplayOresAndIngotsSummary(displayIngots, ingots, ingotLimits, "Ingot");

    displayProgress.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    displayProgress.FontSize = 0.5f;
    displayProgress.WriteText("<< Assembler progress >>\n\n", false);
    DisplayAssemblerProgress(displayProgress);

    displayInventory.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
    displayInventory.FontSize = 0.5f;
    displayInventory.WriteText("<< Inventory summary >>\n\n", false);
    DisplayInventorySummary(displayInventory);
}

void DisplayAssemblerProgress(IMyTextPanel display) {
    List<IMyTerminalBlock> assemblers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);

    foreach (IMyAssembler assembler in assemblers) {
        string assemblerName = assembler.CustomName;
        string assemblerStatus = assembler.IsProducing ? "Working" : "Idle";

        string detailedInfo = assembler.DetailedInfo;
        string[] infoLines = detailedInfo.Split('\n');
        string progressLine = infoLines.FirstOrDefault(line => line.Contains("Progress"));
        string progressValue = progressLine != null ? progressLine.Split(':')[1].Trim() : "0%";

        string outputAssembler = $"{assemblerName,-20} Status: {assemblerStatus, -10} Progress: {progressValue, -6}\n\n";
        display.WriteText(outputAssembler, true);
    }
}

void DisplayInventorySummary(IMyTextPanel display) {
    List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);

    float totalVolume = 0;
    float maxVolume = 0;
    Dictionary<string, float> itemAmounts = new Dictionary<string, float>();

    foreach (IMyCargoContainer container in containers) {
        IMyInventory inventory = container.GetInventory(0);
        totalVolume += (float)inventory.CurrentVolume;
        maxVolume += (float)inventory.MaxVolume;

        List<MyInventoryItem> itemsList = new List<MyInventoryItem>();
        inventory.GetItems(itemsList);

        foreach (var item in itemsList) {
            string itemName = item.Type.SubtypeId.ToString();
            string itemType = item.Type.TypeId.ToString();
            if (itemType.EndsWith("_Ore") || itemType.EndsWith("_Ingot")) {
                continue; // skip ores and ingots
            }

            itemName = GetComponentName(itemName, itemType);

            if (!itemAmounts.ContainsKey(itemName)) {
                itemAmounts.Add(itemName, 0);
            }
            itemAmounts[itemName] += (float)item.Amount;
        }
    }

    int progressInventory = (int)((totalVolume / maxVolume) * 100);
    if (progressInventory > 100) progressInventory = 100;
    string progressBarInventory = new string('=', Math.Max(0, (int)(progressInventory / 2))).PadRight(50, ' ');

    string outputInventory = $"Inventory Status: {totalVolume.ToString("N2"), -10} / {maxVolume.ToString("N2"), -10} L\n{progressBarInventory}\n\n";
    display.WriteText(outputInventory, false);

    foreach (var itemAmount in itemAmounts) {
        string itemName = itemAmount.Key;
        int itemLimit = defaultItemLimit; // default limit for non-ores/ingots

        string statusColor = itemAmount.Value >= itemLimit ? "Green" : "Red";
        string outputItem = $"{itemName,-12} Status: {statusColor}   {itemAmount.Value.ToString("N0"), -10} / {itemLimit.ToString("N0"), -10}\n\n";
        display.WriteText(outputItem, true);
    }
}

void DisplayOresAndIngotsSummary(IMyTextPanel display, List<string> items, List<int> limits, string type) {
    for (int i = 0; i < items.Count; i++) {
        string itemName = items[i];
        int itemLimit = limits[i];

        List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);

        float itemAmount = 0;

        foreach (var container in containers) {
            IMyInventory inventory = container.GetInventory(0);
            List<MyInventoryItem> itemsList = new List<MyInventoryItem>();
            inventory.GetItems(itemsList);

            foreach (var item in itemsList) {
                if (item.Type.SubtypeId.ToString() == itemName && item.Type.TypeId.ToString().Contains(type)) {
                    itemAmount += (float)item.Amount;
                }
            }
        }

        int progressItem = (int)((itemAmount / itemLimit) * 100);
        if (progressItem > 100) progressItem = 100;
        string progressBarItem = new string('=', Math.Max(0, (int)(progressItem / 2))).PadRight(50, ' ');

        string statusColor = itemAmount > itemLimit ? "Green" : "Red";
        string outputItem = $"{itemName,-12} Status: {statusColor}   {itemAmount.ToString("N0"), -10} / {itemLimit.ToString("N0"), -10}\n{progressBarItem}\n\n";
        display.WriteText(outputItem, true);
    }
}

string GetComponentName(string subtypeId, string itemType) {
    string[] parts = subtypeId.Split('_');
    if (parts.Length > 1) {
        subtypeId = string.Join(" ", parts.Skip(1));
    }

    if (itemType.Contains("AmmoMagazine")) {
        subtypeId += " Ammo";
    } else if (itemType.Contains("OxygenContainerObject")) {
        subtypeId += " Bottle";
    }

    return subtypeId;
}

