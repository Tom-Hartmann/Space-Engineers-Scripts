public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update10;
}

private const string PistonGroupName = "shipbuilderpiston";
private const string WelderGroupName = "ShipPrinterWelder";
private const string LcdName = "Progress LCD";
private const string ProjectorName = "Projector shipbuilder";
private const float MoveDistance = 2.5f;
private const int WaitTime = 30;

private IMyBlockGroup pistonsGroup;
private IMyBlockGroup weldersGroup;
private List<IMyPistonBase> pistons;
private List<IMyShipWelder> welders;
private IMyTextSurface lcd;
private bool isRetracting;
private int counter;
private TimeSpan startTime;
private TimeSpan endTime;
private IMyProjector projector;

public void Main(string argument, UpdateType updateSource) {
    if (pistonsGroup == null || weldersGroup == null || lcd == null || projector == null) {
        Initialize();
    }

    if ((updateSource & UpdateType.Update10) != 0) {
        if (isRetracting) {
            if (counter % (WaitTime * 6) == 0) {
                MovePistons(-MoveDistance);
            }
            if (counter % (WaitTime * 6) == WaitTime * 6 - 1) {
                MovePistons(-MoveDistance);
            }
            counter++;
            DisplayProgress();
        } else {
            // Check if the projector is displaying a blueprint
            if (projector.IsProjecting) {
                startTime = DateTime.Now.TimeOfDay;
                ExtendPistons();
                TurnOnWelders();
                isRetracting = true;
            } else {
                lcd.WriteText("No blueprint is being displayed by the projector.");
            }
        }
    }
}

private void Initialize() {
    pistonsGroup = GridTerminalSystem.GetBlockGroupWithName(PistonGroupName);
    weldersGroup = GridTerminalSystem.GetBlockGroupWithName(WelderGroupName);
    lcd = GridTerminalSystem.GetBlockWithName(LcdName) as IMyTextSurface;
    projector = GridTerminalSystem.GetBlockWithName(ProjectorName) as IMyProjector;

    pistons = new List<IMyPistonBase>();
    welders = new List<IMyShipWelder>();

    pistonsGroup.GetBlocksOfType(pistons);
    weldersGroup.GetBlocksOfType(welders);
}

private void ExtendPistons() {
    foreach (var piston in pistons) {
        piston.Velocity = 0.1f;
        piston.MinLimit = 0;
        piston.MaxLimit = piston.HighestPosition;
    }
}

private void TurnOnWelders() {
    foreach (var welder in welders) {
        welder.Enabled = true;
    }
}

private void MovePistons(float distance) {
    foreach (var piston in pistons) {
        piston.MinLimit = Math.Max(0, piston.MinLimit - distance);
        piston.MaxLimit = Math.Max(0, piston.MaxLimit - distance);
        piston.Velocity = distance > 0 ? 0.1f : -0.1f; // Set the correct velocity based on the direction
    }
}


private void DisplayProgress() {
    double remainingDistance = 0;
    foreach (var piston in pistons) {
        remainingDistance += piston.MaxLimit - piston.CurrentPosition;
    }
    remainingDistance /= pistons.Count;

    int remainingSeconds = (int)Math.Ceiling(remainingDistance / MoveDistance) * WaitTime;

    string weldersStatus = welders.TrueForAll(w => w.Enabled) ? "On" : "Off";
    endTime = startTime.Add(TimeSpan.FromSeconds(remainingSeconds));

    string pistonDirection = pistons[0].Velocity > 0 ? "Forward" : "Backward";

    lcd.WriteText($"Welders: {weldersStatus}\n");
    lcd.WriteText($"Piston direction: {pistonDirection}\n", true);
    lcd.WriteText($"Start Time: {startTime.ToString(@"hh\:mm\:ss")}\n", true);
    lcd.WriteText($"End Time: {endTime.ToString(@"hh\:mm\:ss")}\n", true);
    lcd.WriteText($"Time remaining: {remainingSeconds} seconds\n", true);

    // Display Min and Max limits of each piston
    lcd.WriteText("\nPiston Limits:\n", true);
    for (int i = 0; i < pistons.Count; i++) {
        var piston = pistons[i];
        lcd.WriteText($"Piston {i + 1}: Min={piston.MinLimit:F1}m / Max={piston.MaxLimit:F1}m\n", true);
    }
}
