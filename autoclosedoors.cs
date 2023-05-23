/*          
/// Whip's Auto Door/Airlock Script v37 - 3/18/18 ///
/// PUBLIC RELEASE ///         
_______________________________________________________________________          
///DESCRIPTION///   

    This script will close opened doors after 3 seconds (15 seconds for hangar doors).    
    The duration that a door is allowed to be open can be modified lower    
    down in the code (line 65). 

    This script also supports an INFINITE number of airlock systems.        
_______________________________________________________________________          
///AUTO DOOR CLOSER///      

    The script will fetch ALL doors on the grid and automatically close any 
    door that has been open for over 4 seconds (15 seconds for hangar doors). 
    Doors can also be excluded from this feature.

Excluding Doors:       
    * Add the tag "Excluded" to the front or rear of the door(s) name.      
_______________________________________________________________________          
///AIRLOCKS///          

    This script supports the optional feature of simple airlock systems.  
    Airlock systems are composed of AT LEAST one Interior Door AND one Exterior Door.  
    The airlock status light does NOT affect the functionality of the doors  
    so if you don't have space for one, don't fret :)   

Airlock system names should follow these patterns:   

    * Interior Airlock Doors: "[Prefix] Airlock Interior"   

    * Exterior Airlock Doors: "[Prefix] Airlock Exterior"   

    * Airlock Status Lights: "[Prefix] Airlock Light"   

    You can make the [Prefix] whatever you wish, but in order for doors in an airlock   
    system to be linked by the script, they MUST have the same prefix. 
_____________________________________________________________________    

If you have any questions, comments, or concerns, feel free to leave a comment on           
the workshop page: http://steamcommunity.com/sharedfiles/filedetails/?id=416932930          
- Whiplash141   :)   
_____________________________________________________________________      
*/

//-------------------------------------------------------------------
//================== CONFIGURABLE VARIABLES ======================
//-------------------------------------------------------------------

//Main runtime variables
bool enableAutoDoorCloser = true;
bool enableAirlockSystem = true;
bool ignoreAllHangarDoors =true;
bool ignoreDoorsOnOtherGrids = true;

//Door open duration (in seconds) 
double regularDoorOpenDuration = 1.5;
double hangarDoorOpenDuration = 15;

//Door exclusion string 
string doorExcludeString = "Excluded";

//Airlock Light Settings 
Color alarmColor = new Color(255, 40, 40); //color of alarm light         
Color regularColor = new Color(80, 160, 255); //color of regular light 
float alarmBlinkLength = 50f;  //alarm blink length in % 
float regularBlinkLength = 100f; //regular blink length in % 
float blinkInterval = .8f; // blink interval in seconds 


//-------------------------------------------------------------------
//=========== Don't touch anything below here! <3 ==================
//-------------------------------------------------------------------

const double secondsPerUpdate = 1.0 / 6.0;
const double refreshTime = 10;
double currentRefreshTime = 141;
bool isSetup = false;

Program();
{
    Runtime.UpdateFrequency = UpdateFrequency.Once;
}

void Main(string arg, UpdateType updateType)
{
    //------------------------------------------
    //This is a bandaid for keen's shit code
    if ((updateType & UpdateType.Once) != 0)
        Runtime.UpdateFrequency = UpdateFrequency.Update10; //or update10 or update100
    //------------------------------------------
    
    if ((updateType & UpdateType.Update10) == 0)
        return;

    currentRefreshTime += secondsPerUpdate;

    if (!isSetup || currentRefreshTime >= refreshTime)
    {
        isSetup = GrabBlocks();
        currentRefreshTime = 0;
    }

    if (!isSetup)
        return;

    Echo("//Whip's Auto Door and\nAirlock System//");
    Echo($"\nNext refresh in {Math.Round(Math.Max(refreshTime - currentRefreshTime, 0))} seconds");

    try
    {
        if (enableAutoDoorCloser)
        {
            AutoDoors(secondsPerUpdate); //controls auto door closing
        }

        if (enableAirlockSystem)
        {
            Airlocks(); //controls airlock system
        }
    }
    catch
    {
        Echo("Somethin dun broke");
        isSetup = false; //redo setup
    }
    
    Echo($"\nCurrent instructions: {Runtime.CurrentInstructionCount}\nMax instructions: {Runtime.MaxInstructionCount}");
}


HashSet<string> airlockNames = new HashSet<string>();
List<IMyDoor> airlockDoors = new List<IMyDoor>();
List<IMySoundBlock> allSounds = new List<IMySoundBlock>();
List<IMyLightingBlock> allLights = new List<IMyLightingBlock>();
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();

List<Airlock> airlockList = new List<Airlock>();
List<AutoDoor> autoDoors = new List<AutoDoor>();
List<IMyDoor> autoDoorsCached = new List<IMyDoor>();


bool GrabBlocks()
{
    if (ignoreDoorsOnOtherGrids)
        GridTerminalSystem.GetBlocksOfType(allBlocks, x => x.CubeGrid == Me.CubeGrid);
    else
    {
        GetAllowedGrids(Me, 1000);
        if (!isFinished)
            return false; //break setup until accepted grids are done parsing
        
        GridTerminalSystem.GetBlocksOfType(allBlocks, x => allowedGrids.Contains(x.CubeGrid));
    }
    
    airlockDoors.Clear();
    allSounds.Clear();
    allLights.Clear();
    
    autoDoors.RemoveAll(x => x.Door.CustomName.ToLower().Contains(doorExcludeString.ToLower()));
    
    //Fetch all blocks that the code needs
    foreach (var block in allBlocks)
    {
        if (block is IMyDoor)
        {
            if (block.CustomName.ToLower().Contains("airlock"))
                airlockDoors.Add(block as IMyDoor);
                
            if (ShouldAddAutoDoor(block))
            {
                if (!autoDoorsCached.Contains(block as IMyDoor))
                {
                    autoDoors.Add(new AutoDoor(block as IMyDoor, regularDoorOpenDuration, hangarDoorOpenDuration));
                }
            }
        }
        else if (block is IMyLightingBlock && block.CustomName.ToLower().Contains("airlock light"))
            allLights.Add(block as IMyLightingBlock);
        else if (block is IMySoundBlock && block.CustomName.ToLower().Contains("airlock sound"))
            allSounds.Add(block as IMySoundBlock);
    }

    if (airlockDoors.Count == 0)
    {
        Echo(">Info: No airlock doors found");
    }

    //Fetch all airlock door names
    airlockNames.Clear();
    foreach (var thisDoor in airlockDoors)
    {
        if (thisDoor.CustomName.ToLower().Contains("airlock interior"))//lists all airlockDoors with proper name 
        {
            string thisName = thisDoor.CustomName.ToLower().Replace("airlock interior", ""); //remove airlock tag 
            thisName = thisName.Replace($"[{doorExcludeString.ToLower()}]", "").Replace(doorExcludeString.ToLower(), ""); //remove door exclusion string 
            thisName = thisName.Replace(" ", ""); //remove all spaces 

            airlockNames.Add(thisName);//adds name to string list 
        }
    }

    //Evaluate each unique airlock name and get parts associated with it

    foreach (var hashValue in airlockNames)
    {
        
        bool dupe = false;
        foreach(var airlock in airlockList)
        {
            if (airlock.Name.Equals(hashValue))
            {
                airlock.Update(airlockDoors, allLights, allSounds);
                dupe = true;
                break;
            } 
        }

        if (!dupe)
            airlockList.Add(new Airlock(hashValue, airlockDoors, allLights, allSounds, alarmColor, regularColor, alarmBlinkLength, regularBlinkLength, blinkInterval));
    }

    autoDoorsCached.Clear();
    foreach(var autoDoor in autoDoors)
    {
        autoDoorsCached.Add(autoDoor.Door);
    }

    return true;
}

bool ShouldAddAutoDoor(IMyTerminalBlock block)
{
    if (ignoreAllHangarDoors && block is IMyAirtightHangarDoor)
        return false;
    else if (block.CustomName.ToLower().Contains(doorExcludeString.ToLower()))
        return false;
    else
        return true;
}

//Dictionary<IMyTerminalBlock, double> dictDoors = new Dictionary<IMyTerminalBlock, double>();
void AutoDoors(double timeElapsed)
{
    foreach (var thisDoor in autoDoors)
    {
        if (CheckInstructions())
        {
            Echo("Instruction limit hit\nAborting...");
            return;
        }
        
        thisDoor.AutoClose(timeElapsed);
    }

    Echo($"\n===Automatic Doors===\nManaged Doors: {autoDoors.Count}");
}

bool CheckInstructions(double proportion = 0.5)
{
    return Runtime.CurrentInstructionCount >= Runtime.MaxInstructionCount * proportion;
}

void Airlocks()
{
    Echo("\n===Airlock Systems===");

    if (airlockList.Count == 0)
    {
        Echo("No airlock groups found");
        return;
    }

    //Iterate through our airlock groups
    Echo($"Airlock count: {airlockList.Count}");
    foreach (var airlock in airlockList)
    {
        if (CheckInstructions())
        {
            Echo("Instruction limit hit\nAborting...");
            return;
        }
        
        airlock.DoLogic();
        Echo($"---------------------------------------------\nAirlock group '{airlock.Name}' found\n{airlock.Info}");
    }
}

public class AutoDoor
{
    public IMyDoor Door { get; private set; } = null;
    double _doorOpenTime = 0;
    double _autoCloseTime = 0;
    bool _wasOpen = false;

    public AutoDoor(IMyDoor door, double regularDoorCloseTime, double hangarDoorCloseTime)
    {
        Door = door;

        if (door is IMyAirtightHangarDoor)
            _autoCloseTime = hangarDoorCloseTime;
        else
            _autoCloseTime = regularDoorCloseTime;
    }

    public void Update(IMyDoor door, double regularDoorCloseTime, double hangarDoorCloseTime)
    {
        Door = door;

        if (door is IMyAirtightHangarDoor)
            _autoCloseTime = hangarDoorCloseTime;
        else
            _autoCloseTime = regularDoorCloseTime;
    }

    public void AutoClose(double time)
    {
        if (Door.OpenRatio == 0)
        {
            _doorOpenTime = 0;
            _wasOpen = false;
            return;
        }
        else if (!_wasOpen) //begin new count
        {
            _wasOpen = true;
            _doorOpenTime = 0;
            return;
        }
        else //if _wasOpen
        {
            _doorOpenTime += time;
        }

        if (_autoCloseTime <= _doorOpenTime)
        {
            Door.CloseDoor();
            _doorOpenTime = 0;
            _wasOpen = false;
        }
    }
}

public class Airlock
{
    List<IMyDoor> _airlockInteriorList = new List<IMyDoor>();
    List<IMyDoor> _airlockExteriorList = new List<IMyDoor>();
    List<IMyLightingBlock> _airlockLightList = new List<IMyLightingBlock>();
    List<IMySoundBlock> _airlockSoundList = new List<IMySoundBlock>();
    private Color _alarmColor = new Color(255, 40, 40);
    private Color _regularColor = new Color(80, 160, 255);
    private float _alarmBlinkLength = 50f;
    private float _regularBlinkLength = 100f;
    private float _blinkInterval = .8f;
    private const string _soundBlockPlayingString = "%Playing sound...%";
    public string Name { get; private set; }
    public string Info { get; private set; }

    public Airlock(string airlockName, List<IMyDoor> airlockDoors, List<IMyLightingBlock> allLights, List<IMySoundBlock> allSounds, Color alarmColor,
        Color regularColor, float alarmBlinkLength, float regularBlinkLength, float blinkInterval)
    {
        Name = airlockName;
        _alarmColor = alarmColor;
        _regularColor = regularColor;
        _alarmBlinkLength = alarmBlinkLength;
        _regularBlinkLength = regularBlinkLength;
        _blinkInterval = blinkInterval;
        GetBlocks(this.Name, airlockDoors, allLights, allSounds);
        Info = $" Interior Doors: {_airlockInteriorList.Count}\n Exterior Doors: {_airlockExteriorList.Count}\n Lights: {_airlockLightList.Count}\n Sound Blocks: {_airlockSoundList.Count}";
    }

    public void Update(List<IMyDoor> airlockDoors, List<IMyLightingBlock> allLights, List<IMySoundBlock> allSounds)
    {
        GetBlocks(this.Name, airlockDoors, allLights, allSounds);
    }

    private void GetBlocks(string airlockName, List<IMyDoor> airlockDoors, List<IMyLightingBlock> allLights, List<IMySoundBlock> allSounds)
    {
        //sort through all doors
        _airlockInteriorList.Clear();
        _airlockExteriorList.Clear();
        _airlockLightList.Clear();
        _airlockSoundList.Clear();
        
        foreach (var thisDoor in airlockDoors)
        {
            string thisDoorName = thisDoor.CustomName.Replace(" ", "").ToLower();
            if (thisDoorName.Contains(airlockName))
            {
                if (thisDoorName.Contains("airlockinterior"))
                {
                    _airlockInteriorList.Add(thisDoor);
                }
                else if (thisDoorName.Contains("airlockexterior"))
                {
                    _airlockExteriorList.Add(thisDoor);
                }
            }
        }

        //sort through all lights 
        foreach (var thisLight in allLights)
        {
            if (thisLight.CustomName.Replace(" ", "").ToLower().Contains(airlockName))
            {
                _airlockLightList.Add(thisLight);
            }
        }

        //sort through all lights 
        foreach (var thisSound in allSounds)
        {
            if (thisSound.CustomName.Replace(" ", "").ToLower().Contains(airlockName))
            {
                _airlockSoundList.Add(thisSound);
            }
        }
        
        Info = $" Interior Doors: {_airlockInteriorList.Count}\n Exterior Doors: {_airlockExteriorList.Count}\n Lights: {_airlockLightList.Count}\n Sound Blocks: {_airlockSoundList.Count}";
    }

    public void DoLogic()
    {
        bool isInteriorClosed;
        bool isExteriorClosed;

        //Start checking airlock status   
        if (_airlockInteriorList.Count != 0 && _airlockExteriorList.Count != 0) //if we have both door types    
        {
            //we assume the airlocks are closed until proven otherwise        
            isInteriorClosed = true;
            isExteriorClosed = true;

            //Door Interior Check          
            foreach (var airlockInterior in _airlockInteriorList)
            {
                if (airlockInterior.OpenRatio > 0)
                {
                    Lock(_airlockExteriorList);
                    isInteriorClosed = false;
                    break;
                    //if any doors yield false, bool will persist until comparison    
                }
            }

            //Door Exterior Check           
            foreach (var airlockExterior in _airlockExteriorList)
            {
                if (airlockExterior.OpenRatio > 0)
                {
                    Lock(_airlockInteriorList);
                    isExteriorClosed = false;
                    break;
                }
            }

            //if all Interior & Exterior doors closed 
            if (isInteriorClosed && isExteriorClosed)
            {
                LightColorChanger(false, _airlockLightList);
                PlaySound(false, _airlockSoundList);
            }
            else
            {
                LightColorChanger(true, _airlockLightList);
                PlaySound(true, _airlockSoundList);
            }

            //if all Interior doors closed 
            if (isInteriorClosed)
                Unlock(_airlockExteriorList);

            //if all Exterior doors closed     
            if (isExteriorClosed)
                Unlock(_airlockInteriorList);
        }
    }

    private void Lock(List<IMyDoor> doorList)
    {
        //locks all doors with the input list
        foreach (IMyDoor lock_door in doorList)
        {
            //if door is open, then close
            if (lock_door.OpenRatio > 0)
                lock_door.CloseDoor();

            //if door is fully closed, then lock
            if (lock_door.OpenRatio == 0 && lock_door.Enabled)
                lock_door.Enabled = false;
        }
    }

    private void Unlock(List<IMyDoor> doorList)
    {
        //unlocks all doors with input list
        foreach (IMyDoor unlock_door in doorList)
            unlock_door.Enabled = true;
    }

    private void PlaySound(bool shouldPlay, List<IMySoundBlock> soundList)
    {
        foreach (var block in soundList)
        {
            if (shouldPlay)
            {
                if (!block.CustomData.Contains(_soundBlockPlayingString))
                {
                    block.Play();
                    block.LoopPeriod = 100f;
                    block.CustomData += _soundBlockPlayingString;
                }
            }
            else
            {
                block.Stop();
                block.CustomData = block.CustomData.Replace(_soundBlockPlayingString, "");
            }
        }
    }

    private void LightColorChanger(bool alarm, List<IMyLightingBlock> listLights)
    {
        Color lightColor;
        float lightBlinkLength;

        //applies our status colors to the airlock lights        
        if (alarm)
        {
            lightColor = _alarmColor;
            lightBlinkLength = _alarmBlinkLength;
        }
        else
        {
            lightColor = _regularColor;
            lightBlinkLength = _regularBlinkLength;
        }

        foreach (var thisLight in listLights)
        {
            thisLight.Color = lightColor;
            thisLight.BlinkLength = lightBlinkLength;
            thisLight.BlinkIntervalSeconds = _blinkInterval;
        }
    }
}

/*
/ //// / Whip's GetAllowedGrids method v1 - 3/17/18 / //// /
Derived from Digi's GetShipGrids() method - https://pastebin.com/MQUHQTg2
*/
List<IMyMechanicalConnectionBlock> allMechanical = new List<IMyMechanicalConnectionBlock>();
HashSet<IMyCubeGrid> allowedGrids = new HashSet<IMyCubeGrid>();
bool isFinished = true;
void GetAllowedGrids(IMyTerminalBlock reference, int instructionLimit)
{
    if (isFinished)
    {
        allowedGrids.Clear();
        allowedGrids.Add(reference.CubeGrid);
    }

    GridTerminalSystem.GetBlocksOfType(allMechanical, x => x.TopGrid != null);

    bool foundStuff = true;
    while (foundStuff)
    {
        foundStuff = false;

        for (int i = allMechanical.Count - 1; i >= 0; i--)
        {
            var block = allMechanical[i];
            if (allowedGrids.Contains(block.CubeGrid))
            {
                allowedGrids.Add(block.TopGrid);
                allMechanical.RemoveAt(i);
                foundStuff = true;
            }
            else if (allowedGrids.Contains(block.TopGrid))
            {
                allowedGrids.Add(block.CubeGrid);
                allMechanical.RemoveAt(i);
                foundStuff = true;
            }
        }

        if (Runtime.CurrentInstructionCount >= instructionLimit)
        {
            Echo("Instruction limit reached\nawaiting next run");
            isFinished = false;
            return;
        }
    }

    isFinished = true;
}

/*
///CHANGE LOG///
v21
* Massively improved performance by caching a bunch of stuff instead of allocating it every run
* Added variable "ignoreDoorsOnOtherGrids" to allow you to ignore doors on other grids
v22
* Fixed issue where all doors were being closed instead of just auto doors (Thanks CyberFoxx!)
* Disabled variable config framework until I can get a color parser working
v23
* Fixed airlock lights not being recognized
v24
* Fixed airlock lights not recognizing names with spaces
v25
* Added in airlock sound block
* Fixed issue where parachute blocks would be closed automatically
v26
* Fixed code only working for one single airlock group
v27
* Declared lists within the forloop at the suggestion of rexxar
v28
* Changed airlock code to use classes
v29
* Changed auto door code to use classes
v31
* Removed the need for timers
* Made code check if airlocks and auto doors already exist before overwriting them
v32
* Fixed issue where excluded doors added after initialization were not being ignored
v33
* Added a max instruction abort code
* Fixed memory leak that was in the code
v34
* Fixed ReadMe regarding excluding doors
* Removed brackets from strings to avoid issues with legacy exclusion tags
v35
* Readded brackets, but changed code to search for the exclusion string with AND without brackets
v36 
* Fixed issue with keen's shitcode
    - Update frequency that is set in a constructor is duplicated in DS
    - Code has a bandaid to avoid this
v37
* Added method to only search for blocks that are attached to the grid via rotors/pistons (not connectors)
*/