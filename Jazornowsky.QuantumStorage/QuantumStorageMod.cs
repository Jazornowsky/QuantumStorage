using Jazornowsky.QuantumStorage;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;

public class QuantumStorageMod : FortressCraftMod
{
    public const int Version = 1;
    public const string ModName = "Quantum Storage Mod";
    private const string QuantumStorageModKey = "Jazornowsky.QuantumStorage";
    private const string QuantumStorageKey = "Jazornowsky.QuantumStorage";
    public const string QuantumStorageMachineKey = "Jazornowsky.QuantumStorageMachineNetworkHandler";
    public const string QuantumStorageControllerWindowKey = "Jazornowsky.QuantumStorageControllerWindow";
    private const string QuantumStorage_1KBlock = "Jazornowsky.QuantumStorage1kBlock";
    private const string QuantumStorage_2KBlock = "Jazornowsky.QuantumStorage2kBlock";
    private const string QuantumStorage_4KBlock = "Jazornowsky.QuantumStorage4kBlock";
    private const string QuantumStorageControllerKey = "Jazornowsky.QuantumStorageController";
    private const string QuantumStorageControllerBlock = "Jazornowsky.QuantumStorageControllerBlock";
    
    public ushort QuantumStorageType = ModManager.mModMappings.CubesByKey[QuantumStorageKey].CubeType;
    public ushort QuantumStorage1KValue = ModManager.mModMappings.CubesByKey[QuantumStorageKey].ValuesByKey[QuantumStorage_1KBlock].Value;
    public ushort QuantumStorage2KValue = ModManager.mModMappings.CubesByKey[QuantumStorageKey].ValuesByKey[QuantumStorage_2KBlock].Value;
    public ushort QuantumStorage4KValue = ModManager.mModMappings.CubesByKey[QuantumStorageKey].ValuesByKey[QuantumStorage_4KBlock].Value;

    public ushort QuantumStorageControllerType = ModManager.mModMappings.CubesByKey[QuantumStorageControllerKey].CubeType;
    public ushort QuantumStorageControllerBlockValue = ModManager.mModMappings.CubesByKey[QuantumStorageControllerKey].ValuesByKey[QuantumStorageControllerBlock].Value;

    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterEntityHandler(QuantumStorageControllerKey);
        modRegistrationData.RegisterEntityHandler(QuantumStorageKey);
        modRegistrationData.RegisterEntityUI(QuantumStorageControllerKey, new QuantumStorageControllerWindow());

        UIManager.NetworkCommandFunctions.Add(QuantumStorageControllerWindowKey, QuantumStorageControllerWindow.HandleNetworkCommand);

        LogUtils.LogDebug(ModName, "registered");

        return modRegistrationData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

        if (parameters.Cube == QuantumStorageControllerType)
        {
            parameters.ObjectType = SpawnableObjectEnum.ResearchStation;
            result.Entity = new QuantumStorageControllerMachine(parameters);
        } else if (parameters.Cube == QuantumStorageType)
        {
            parameters.ObjectType = SpawnableObjectEnum.PowerStorageBlock_T3;
            if (parameters.Value == QuantumStorage1KValue) {
                result.Entity = new QuantumStorageMachine(parameters, 1024);
            } else if (parameters.Value == QuantumStorage2KValue)
            {
                result.Entity = new QuantumStorageMachine(parameters, 2048);
            } else if (parameters.Value == QuantumStorage4KValue)
            {
                result.Entity = new QuantumStorageMachine(parameters, 4096);
            }
            else
            {
                LogUtils.LogDebug(ModName, "QuantumStorageMachine missing entity");
            }
        }

        return result;
    }
}