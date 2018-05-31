namespace Jazornowsky.QuantumStorage.utils
{
    public class MissionUtils
    {
        public static void AddLowPowerMission(string name)
        {
            AddMission(name + " is low on power.");
        }

        public static void AddLowStorageMission(string name)
        {
            AddMission(name + " is full.");
        }

        public static void AddMission(string missionText)
        {
            var missionManager = MissionManager.instance;
            missionManager.RemoveMission(missionText);
            missionManager.AddMission(missionText, 5f, Mission.ePriority.eOptional);
        }

        public delegate void AddMachineMission(string machineName);
    }
}